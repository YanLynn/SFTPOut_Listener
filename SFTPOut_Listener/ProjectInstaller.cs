using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace SFTPOut_Listener
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            var processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.User
            };

            var serviceInstaller = new ServiceInstaller
            {
                ServiceName = "SFTPOut_Listener",
                DisplayName = "SFTPOut Listener",
                Description = "Pushes PGP files to bank via SFTP",
                StartType = ServiceStartMode.Automatic
            };

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
 }
