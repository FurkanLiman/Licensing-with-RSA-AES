namespace LicenceApp
{
    public static class Logger
    {
        public static void Log(string message)
        {
            string logEntry = $"{DateTime.Now.ToString("o")}, {message}";
            WriteLog(logEntry);
        }
        public static void Log(string aesKey, string aesIV, string deviceID,string state)
        {
            string logEntry = $"{DateTime.Now.ToString("o")}, [AES] aesKey: {aesKey}, aesIV: {aesIV}, deviceID: {deviceID}, state={state}";
            WriteLog(logEntry);
        }
        private static void WriteLog(string logEntry)
        {
            using (StreamWriter sw = new StreamWriter(FileManager.LogFilePath, true))
            {
                sw.WriteLine(logEntry);
            }
        }
    }
}
