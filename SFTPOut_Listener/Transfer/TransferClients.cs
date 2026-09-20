using System;
using System.IO;
using System.Text;

namespace SFTPOut_Listener
{
    internal interface ITransferClient
    {
        bool Send(string localFile, string remoteFileName, BankElement bank);
    }

    internal static class TransferClientFactory
    {
        public static ITransferClient Create(BankElement bank, int timeoutMs)
        {
            // transferMode is only a label for command-line based tools.
            // Add a new case here if a non-command-line transfer method is ever needed.
            switch ((bank.TransferMode ?? "").Trim().ToUpperInvariant())
            {
                case "TECTIA":
                case "CONNECTDIRECT":
                case "CLI":
                    return new CommandLineClient(timeoutMs);
                default:
                    throw new InvalidOperationException(
                        "Unknown transferMode '" + bank.TransferMode + "' for bank " + bank.Name);
            }
        }
    }

    // Runs any command-line transfer tool (Tectia scpg3, Connect:Direct, ...).
    // The program, arguments and optional script file all come from the bank's config.
    internal sealed class CommandLineClient : ITransferClient
    {
        private readonly int _timeoutMs;

        public CommandLineClient(int timeoutMs) { _timeoutMs = timeoutMs; }

        public bool Send(string localFile, string remoteFileName, BankElement bank)
        {
            string scriptPath = null;
            try
            {
                // Optional script/process file: fill the template and write it to a temp file
                if (!string.IsNullOrWhiteSpace(bank.ScriptTemplate))
                {
                    string tpl = File.ReadAllText(bank.ScriptTemplate);
                    string content = Fill(tpl, bank, localFile, remoteFileName, "", bank.Pass);
                    scriptPath = Path.Combine(Path.GetTempPath(),
                        "sftpout_" + Guid.NewGuid().ToString("N") + ".tmp");
                    File.WriteAllText(scriptPath, content, new UTF8Encoding(false));
                }

                string args = Fill(bank.Cmd, bank, localFile, remoteFileName, scriptPath ?? "", bank.Pass);

                // Same command with the password masked, safe to write to the log
                string logArgs = Fill(bank.Cmd, bank, localFile, remoteFileName, scriptPath ?? "", "****");
                Logger.Instance.Info("Run: " + bank.Program + " " + logArgs);

                string output;
                int exit = ProcessRunner.Run(bank.Program, args, _timeoutMs, out output);

                if (IsOk(exit, bank.OkExitCodes))
                {
                    Logger.Instance.Info("Transfer OK (exit " + exit + "): " + remoteFileName + " " + output.Trim());
                    return true;
                }

                Logger.Instance.Error("Transfer FAILED (exit " + exit + "): " + remoteFileName + " " + output.Trim());
                return false;
            }
            finally
            {
                // Always remove the temp script (it may contain credentials)
                if (scriptPath != null)
                {
                    try { File.Delete(scriptPath); } catch { }
                }
            }
        }

        // Supported placeholders:
        // {port} {local} {fileName} {user} {host} {remoteDir} {remoteFile} {script} {pass}
        private static string Fill(string t, BankElement bank, string localFile,
                                   string fileName, string script, string pass)
        {
            string remoteFile = bank.RemoteDir.TrimEnd('/') + "/" + fileName;
            return t.Replace("{port}", bank.Port)
                    .Replace("{local}", localFile)
                    .Replace("{fileName}", fileName)
                    .Replace("{user}", bank.User)
                    .Replace("{host}", bank.Host)
                    .Replace("{remoteDir}", bank.RemoteDir)
                    .Replace("{remoteFile}", remoteFile)
                    .Replace("{script}", script)
                    .Replace("{pass}", pass);
        }

        // okCodes is a comma separated list of exit codes treated as success, e.g. "0" or "0,4"
        private static bool IsOk(int exit, string okCodes)
        {
            foreach (string part in (okCodes ?? "0").Split(','))
            {
                int n;
                if (int.TryParse(part.Trim(), out n) && n == exit) return true;
            }
            return false;
        }
    }
}