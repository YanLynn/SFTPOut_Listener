using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SFTPOut_Listener
{
    public class BankElement : ConfigurationElement
    {
        [ConfigurationProperty("issBk", IsRequired = true, IsKey = true)]
        public string IssBk { get { return (string)this["issBk"]; } }

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name { get { return (string)this["name"]; } }

        [ConfigurationProperty("transferMode", DefaultValue = "TECTIA")]
        public string TransferMode { get { return (string)this["transferMode"]; } }

        [ConfigurationProperty("program", DefaultValue = "scpg3")]
        public string Program { get { return (string)this["program"]; } }

        [ConfigurationProperty("host", IsRequired = true)]
        public string Host { get { return (string)this["host"]; } }

        [ConfigurationProperty("port", DefaultValue = "22")]
        public string Port { get { return (string)this["port"]; } }

        [ConfigurationProperty("user", IsRequired = true)]
        public string User { get { return (string)this["user"]; } }

        [ConfigurationProperty("pass", DefaultValue = "")]
        public string Pass { get { return (string)this["pass"]; } }

        [ConfigurationProperty("remoteDir", IsRequired = true)]
        public string RemoteDir { get { return (string)this["remoteDir"]; } }

        [ConfigurationProperty("cmd", IsRequired = true)]
        public string Cmd { get { return (string)this["cmd"]; } }


        // Optional path to a script/process file template (needed by some tools, e.g. Connect:Direct)
        [ConfigurationProperty("scriptTemplate", DefaultValue = "")]
        public string ScriptTemplate { get { return (string)this["scriptTemplate"]; } }

        // Comma separated exit codes treated as success, e.g. "0" or "0,4"
        [ConfigurationProperty("okExitCodes", DefaultValue = "0")]
        public string OkExitCodes { get { return (string)this["okExitCodes"]; } }
    }

    public class BankCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new BankElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((BankElement)element).IssBk;
        }


        public BankElement Get(string issBk)
        {
            return (BankElement)BaseGet(issBk);
        }

        public System.Collections.Generic.IEnumerable<BankElement> All()
        {
            foreach (BankElement b in this) yield return b;
        }
    }

    public class BankSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]

        public BankCollection Banks
        {
            get { return (BankCollection)this[""]; }
        }

        public static BankSection Load()
        {
            return (BankSection)ConfigurationManager.GetSection("bankSettings");
        }


    }
}