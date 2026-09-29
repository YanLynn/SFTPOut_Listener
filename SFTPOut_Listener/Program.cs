using System;
using System.ServiceProcess;

namespace SFTPOut_Listener
{
    internal static class Program
    {
        static void Main()
        {
            var svc = new SFTPOutListener();

            if (Environment.UserInteractive)
            {
                // Console mode when started from Visual Studio (F5) or a command prompt
                svc.DebugStart();
                Console.WriteLine("Running. Press Enter to stop.");
                Console.ReadLine();
                svc.DebugStop();
            }
            else
            {
                // Normal Windows Service mode
                ServiceBase.Run(new ServiceBase[] { svc });
            }
        }
    }
}