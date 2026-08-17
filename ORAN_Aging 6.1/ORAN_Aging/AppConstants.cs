namespace ORAN_Aging
{
    /// <summary>
    /// Centralized application constants. Change values here instead of hunting through multiple files.
    /// </summary>
    public static class AppConstants
    {
        // Application version — update this single value for version bumps
        public const string AppVersion = "V6.1";

        // File paths
        public const string JsonLogPath = @"C:\JsonLog";
        public const string TextLogPath = @"C:\Logs";
        public const string ErrorLogPath = @"C:\Test_TechCo\ErrorLog.txt";
        public const string OlpFilePath = @"C:\OLP File";

        // Local log base paths per model
        public const string LogPathPCS = @"C:\Log\CarrierA_PCS\";
        public const string LogPathLOLO = @"C:\Log\CarrierA_Lo_Lo\";
        public const string LogPathFATLOLO = @"C:\Log\CarrierA_Lo_Lo_XL\";
        public const string LogPathUnknown = @"C:\Log\Unknown\";

        // T: Drive network paths
        public const string TDriveBasePCS = @"T:\Acme Test Logs\5G RU ORAN\CarrierA PCS\Aging\";
        public const string TDriveBaseLOLO = @"T:\Acme Test Logs\5G RU ORAN\CarrierA LOLO\Aging\";
        public const string TDriveBaseFATLOLO = @"T:\Acme Test Logs\5G RU ORAN\CarrierA FAT LOLO\Aging\";

        // Serial port settings
        public const int BaudRate = 115200;
        public const int CommandTimeoutSeconds = 5;
        public const int MaxReLoginRetries = 2;

        // Unit credentials
        public const string UnitUsername = "user";
        public const string UnitUserPassword = "REDACTED_PASSWORD";
        public const string UnitRootPassword = "REDACTED_PASSWORD";
        public const string UnitUserPrompt = "user@";
        public const string UnitRootPrompt = "root@";
        public const string PasswordPrompt = "Password:";

        // Session lost indicators
        public static readonly string[] SessionLostIndicators = {
            "Waiting for a stable CPRI Link",
            "WARNING: Unauthorized access to this system is forbidden",
            "Login incorrect",
            "login:"
        };

        // T: Drive mapping
        public const string TDriveNetworkPath = @"\\REDACTED_IP\Shared_Folder";
        public const string TDrivePassword = "REDACTED_PASSWORD";
        public const string TDriveUser = "REDACTED_USER";

        // Fan temperature thresholds
        public const double FanOnTemperature = 80.0;
        public const double FanOffTemperature = 70.0;

        // Test location
        public const string TestLocation = "Facility 1";
    }
}