using System.Diagnostics;
using System.Text;

namespace SFTPOut_Listener
{
    internal static class ProcessRunner
    {
        // Returns the process exit code.
        // On timeout the process is killed and -1 is returned.
        public static int Run(string exe, string args, int timeoutMs, out string output)
        {
            var sb = new StringBuilder();
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            using (var p = new Process { StartInfo = psi })
            {
                DataReceivedEventHandler handler = (s, e) =>
                {
                    if (e.Data != null) { lock (sb) { sb.AppendLine(e.Data); } }
                };
                p.OutputDataReceived += handler;
                p.ErrorDataReceived += handler;

                p.Start();

                // Close stdin so a password prompt gets EOF instead of hanging forever
                p.StandardInput.Close();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { }
                    lock (sb) { output = sb.ToString() + "TIMEOUT"; }
                    return -1;
                }

                // Second call without a timeout makes sure all async output has been flushed
                p.WaitForExit();
                lock (sb) { output = sb.ToString(); }
                return p.ExitCode;
            }
        }
    }
}