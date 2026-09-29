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
            serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
            //serviceProcessInstaller1.Username = null;
            //serviceProcessInstaller1.Password = null;

            serviceInstaller1.ServiceName = "SFTPOut_Listener";
            serviceInstaller1.DisplayName = "SFTPOut Listener";
            serviceInstaller1.Description = "Pushes PGP files to bank via SFTP";
            serviceInstaller1.StartType = ServiceStartMode.Automatic;
        }


    }
 }
