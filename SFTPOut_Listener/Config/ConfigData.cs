using System.Configuration;

namespace SFTPOut_Listener
{
    internal sealed class ConfigData
    {
        public int SleepMs { get; private set; }
        public string DbConnString { get; private set; }
        public string StatusReady { get; private set; }
        public string StatusProcessing { get; private set; }
        public string StatusSuccess { get; private set; }
        public string StatusFailed { get; private set; }
        public string SourceRoot { get; private set; }
        public string SentSubfolder { get; private set; }
        public int TransferTimeoutMs { get; private set; }

        public static ConfigData Load()
        {
            return new ConfigData
            {
                SleepMs = GetInt("SLEEP_MS", 3000),
                DbConnString = Get("DB_CONN_STRING"),
                StatusReady = Get("STATUS_READY"),
                StatusProcessing = Get("STATUS_PROCESSING"),
                StatusSuccess = Get("STATUS_SUCCESS"),
                StatusFailed = Get("STATUS_FAILED"),
                SourceRoot = Get("SOURCE_ROOT"),
                SentSubfolder = Get("SENT_SUBFOLDER"),
                TransferTimeoutMs = GetInt("TRANSFER_TIMEOUT_MS", 600000)
            };
        }

        private static string Get(string key)
        {
            string v = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(v))
                throw new ConfigurationErrorsException("Missing appSetting: " + key);
            return v.Trim();
        }

        private static int GetInt(string key, int def)
        {
            int n;
            return int.TryParse(ConfigurationManager.AppSettings[key], out n) ? n : def;
        }
    }
}