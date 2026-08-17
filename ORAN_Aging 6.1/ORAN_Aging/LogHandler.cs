using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ORAN_Aging
{
    public class TestLog
    {
        public string WorkStation;
        public string SerialNumber;
        public string DateTime;
        public string SlotID;
        public string Model;
        public string Locations;
        public string BurnHr;
        public string Firmware;
        public string OverallResult;
        public List<TestFailed> TestDetail;
    }

    public class TestFailed
    {
        public string TestName;
        public string Result;
        public dynamic Value;
        public string ErrorCodes;
    }

    public class LogHandler
    {
        public TestLog tlog = new TestLog();
        public List<TestFailed> tfailed = new List<TestFailed>();

        private static readonly Dictionary<string, string> NetworkPaths = new Dictionary<string, string>
        {
            {"ORAN PCS", AppConstants.TDriveBasePCS },
            {"ORAN LOLO", AppConstants.TDriveBaseLOLO },
            {"FAT LOLO", AppConstants.TDriveBaseFATLOLO }
        };

        public bool WriteToLog(string sn) {
            bool fileCopied = false;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string localJsonPath = Path.Combine(AppConstants.JsonLogPath, sn + "_Aging_" + timestamp + ".json");
            string localTxtPath = Path.Combine(AppConstants.TextLogPath, sn + "_Aging_" + timestamp + ".txt");

            tlog.TestDetail = new List<TestFailed>(tfailed); // copy list to avoid shared reference
            tlog.DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            tlog.SerialNumber = sn;

            // Serialize to JSON
            string JSONResult = JsonConvert.SerializeObject(tlog, Formatting.Indented);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(localJsonPath));
            Directory.CreateDirectory(Path.GetDirectoryName(localTxtPath));

            // Write JSON log
            File.WriteAllText(localJsonPath, JSONResult);

            // ======== Copy JSON to T: Drive ========
            try {
                string model = tlog.Model ?? "";
                if (NetworkPaths.ContainsKey(model)) {
                    string destDir = NetworkPaths[model];
                    Directory.CreateDirectory(destDir);
                    string destPath = Path.Combine(destDir, Path.GetFileName(localJsonPath));
                    File.Copy(localJsonPath, destPath, true);
                    fileCopied = true;
                } else {
                    Console.WriteLine("T: drive copy skipped: unknown model '" + model + "'");
                }
            }
            catch (Exception ex) {
                Console.WriteLine("T: drive copy failed: " + ex.Message);
            }
            // ======== End T: Drive Copy ========

            // Write Text Log
            try {
                using (StreamWriter r = File.Exists(localTxtPath) ? File.AppendText(localTxtPath) : File.CreateText(localTxtPath)) {
                    r.WriteLine("\n\nTELCOM INC.");
                    r.WriteLine("SERIAL NUMBER: " + tlog.SerialNumber);
                    r.WriteLine("DATE/TIME: " + tlog.DateTime);
                    r.WriteLine("SLOT: " + tlog.SlotID);
                    r.WriteLine("MODEL: " + tlog.Model);
                    r.WriteLine("LOCATION: " + tlog.Locations);
                    r.WriteLine("BURN HOURS: " + tlog.BurnHr);
                    r.WriteLine("");

                    if (tlog.TestDetail != null) {
                        foreach (var item in tlog.TestDetail) {
                            string testName = item.TestName ?? "Unknown";
                            string result = item.Result ?? "N/A";
                            r.WriteLine(testName + ":" + result);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine("Text log writing failed: " + ex.Message);
            }

            // Clear failed tests for next log
            //tfailed.Clear();

            return fileCopied;
        }
    }
}
