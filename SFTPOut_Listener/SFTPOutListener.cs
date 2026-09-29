using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace SFTPOut_Listener
{
    public partial class SFTPOutListener : ServiceBase
    {
        private ManualResetEvent _stopEvent;
        private Thread _worker;
        private ConfigData _cfg;
        private BatchDao _dao;
        private BankCollection _banks;

        public SFTPOutListener()
        {
            InitializeComponent();
        }

        // Entry points for console debugging (OnStart/OnStop are protected)
        public void DebugStart() { OnStart(null); }
        public void DebugStop() { OnStop(); }

        protected override void OnStart(string[] args)
        {
            try
            {
                _cfg = ConfigData.Load();
                _dao = new BatchDao(_cfg);

                BankSection section = BankSection.Load();
                if (section == null || section.Banks.Count == 0)
                    throw new ConfigurationErrorsException("No <bankSettings> configured.");
                _banks = section.Banks;

                // Rows left in the processing status mean the service stopped mid-transfer.
                // Only warn here; do not reset them automatically (the bank may already have the file).
                int stuck = _dao.Count(_cfg.StatusProcessing);
                if (stuck > 0)
                    Logger.Instance.Warn(stuck + " batch(es) stuck in " + _cfg.StatusProcessing +
                        ". Check the bank side, then reset manually if needed.");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Startup failed", ex);
                throw;
            }

            _stopEvent = new ManualResetEvent(false);
            _worker = new Thread(RunLoop) { IsBackground = true };
            _worker.Start();
            Logger.Instance.Info("SFTPOut_Listener started");
        }

        protected override void OnStop()
        {
            _stopEvent.Set();
            _worker.Join(30000);
            Logger.Instance.Info("SFTPOut_Listener stopped");
        }

        private void RunLoop()
        {
            // Process once immediately, then wait SleepMs between rounds.
            // WaitOne returns true as soon as the service is stopping, so stop is not delayed.
            do
            {
                try { ProcessOnce(); }
                catch (Exception ex) { Logger.Instance.Error("Loop error", ex); }
            }
            while (!_stopEvent.WaitOne(_cfg.SleepMs));
        }

        private void ProcessOnce()
        {
            List<BatchInfo> batches = _dao.GetByStatus(_cfg.StatusReady, 50);
            foreach (BatchInfo b in batches)
            {
                if (_stopEvent.WaitOne(0)) return;

                // Claim the batch: ready -> processing.
                // The update is conditional on the current status, so only one instance can claim it.
                if (!_dao.TryChangeStatus(b, _cfg.StatusReady, _cfg.StatusProcessing)) continue;

                SendBatch(b);
            }
        }

        private void SendBatch(BatchInfo b)
        {
            try
            {
                // Every part of the file name comes from the DB row, so make sure none is NULL
                if (string.IsNullOrEmpty(b.ProcDate) || string.IsNullOrEmpty(b.IssBk) ||
                    string.IsNullOrEmpty(b.IssBr) || string.IsNullOrEmpty(b.Batch) ||
                    string.IsNullOrEmpty(b.TranType) || string.IsNullOrEmpty(b.Sequence))
                {
                    Fail(b, "Incomplete batch row, ID=" + b.Id);
                    return;
                }

                BankElement bank = _banks.Get(b.IssBk);
                if (bank == null) { Fail(b, "No bank config for IssBk=" + b.IssBk); return; }

                // The PGP file parked by PGPOut_Listener must match the DB row
                string dir = Path.Combine(_cfg.SourceRoot, b.ProcDate);
                string pgp = Path.Combine(dir, b.FileName);
                string sgl = Path.Combine(dir, b.SglFileName);

                if (!File.Exists(pgp)) { Fail(b, "PGP file not found: " + pgp); return; }

                ITransferClient client = TransferClientFactory.Create(bank, _cfg.TransferTimeoutMs);

                // Step 1: send the PGP file
                if (!client.Send(pgp, b.FileName, bank)) { Fail(b, "PGP upload failed: " + b.FileName); return; }
                Logger.Instance.Info("PGP file sent to " + bank.Name + ": " + b.FileName);

                // Step 2: build the SGL by copying the PGP file and adding the .SGL extension.
                // It is created only after the PGP upload succeeded; overwrite if a previous attempt left one.
                File.Copy(pgp, sgl, true);

                // Step 3: send the SGL last (the bank picks the file up when it sees the SGL)
                if (!client.Send(sgl, b.SglFileName, bank)) { Fail(b, "SGL upload failed: " + b.SglFileName); return; }
                Logger.Instance.Info("SGL file sent to " + bank.Name + ": " + b.SglFileName);

                // Step 4: both files are at the bank, so update the status
                _dao.SetStatus(b, _cfg.StatusSuccess);
                MoveToSent(dir, b);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("SendBatch error: " + b.FileName, ex);
                SafeSetFailed(b);
            }
        }

        private void Fail(BatchInfo b, string reason)
        {
            Logger.Instance.Error(reason);
            SafeSetFailed(b);
        }

        private void SafeSetFailed(BatchInfo b)
        {
            try { _dao.SetStatus(b, _cfg.StatusFailed); }
            catch (Exception ex) { Logger.Instance.Error("Cannot set failed status: " + b.FileName, ex); }
        }

        private void MoveToSent(string dir, BatchInfo b)
        {
            try
            {
                string sentDir = Path.Combine(dir, _cfg.SentSubfolder);
                Directory.CreateDirectory(sentDir);
                foreach (string name in new[] { b.FileName, b.SglFileName })
                {
                    string src = Path.Combine(dir, name);
                    if (!File.Exists(src)) continue;
                    string dst = Path.Combine(sentDir, name);
                    if (File.Exists(dst)) File.Delete(dst);
                    File.Move(src, dst);
                }
            }
            catch (Exception ex)
            {
                // The file is already at the bank, so keep the success status and only log a warning
                Logger.Instance.Warn("Move to Sent failed: " + ex.Message);
            }
        }
    }
}