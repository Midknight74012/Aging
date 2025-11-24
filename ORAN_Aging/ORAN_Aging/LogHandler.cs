using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;


// LogHandler.cs
// Author: <Engineering Manager Name>
// Created: <approx date if known>
// Notes: Primary author: Engineering Manager.
//        Maintainer: <Your Name or Team>
//        This file was authored by the engineering manager; changes should preserve original intent.
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
        private static readonly string FtpServer = Environment.GetEnvironmentVariable("FTP_SERVER") ?? "ftp://example.com";
        private static readonly string FtpUser = Environment.GetEnvironmentVariable("FTP_USER") ?? "REDACTED_USER";
        private static readonly string FtpPassword = Environment.GetEnvironmentVariable("FTP_PASSWORD") ?? "REDACTED_PASSWORD";
        public bool WriteToLog(string sn) {
            bool fileCopied = false;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string localJsonPath = Path.Combine(@"C:\JsonLog", sn + "_Aging_" + timestamp + ".json");
            string localTxtPath = Path.Combine(@"C:\Logs", sn + "_Aging_" + timestamp + ".txt");

            tlog.TestDetail = tfailed;
            tlog.DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            tlog.SerialNumber = sn;

            // Serialize to JSON
            string JSONResult = JsonConvert.SerializeObject(tlog, Formatting.Indented);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(localJsonPath));
            Directory.CreateDirectory(Path.GetDirectoryName(localTxtPath));

            // Write JSON log
            File.WriteAllText(localJsonPath, JSONResult);

            // ======== FTP Upload ========
            try {
                string ftpServer = FtpServer;
                string remoteDir = "/production_json";
                string ftpUser = FtpUser;
                string ftpPass = FtpPassword;

                if (!ftpServer.EndsWith("/")) ftpServer += "/";
                if (!remoteDir.EndsWith("/")) remoteDir += "/";

                string ftpUrl = ftpServer + remoteDir + Path.GetFileName(localJsonPath);

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPass);

                byte[] fileContents = File.ReadAllBytes(localJsonPath);
                request.ContentLength = fileContents.Length;

                using (Stream requestStream = request.GetRequestStream()) {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse()) {
                    fileCopied = true;
                    Console.WriteLine($"FTP Upload complete, status: {response.StatusDescription}");
                }
            }
            catch (Exception ex) {
                Console.WriteLine("FTP upload failed: " + ex.Message);
            }
            // ======== End FTP Upload ========

            TestLog deserialized = JsonConvert.DeserializeObject<TestLog>(JSONResult);
            // Write Text Log
            try {
                using (StreamWriter r = File.Exists(localTxtPath) ? File.AppendText(localTxtPath) : File.CreateText(localTxtPath)) {
                    r.WriteLine("\n\nCOMMUNICATIONS TEST DESIGN INC.");
                    r.WriteLine("SERIAL NUMBER: " + deserialized.SerialNumber);
                    r.WriteLine("DATE/TIME: " + deserialized.DateTime);
                    r.WriteLine("SLOT: " + deserialized.SlotID);
                    r.WriteLine("MODEL: " + deserialized.Model);
                    r.WriteLine("LOCATION: " + deserialized.Locations);
                    r.WriteLine("BURN HOURS: " + deserialized.BurnHr);
                    r.WriteLine("");

                    foreach (var item in deserialized.TestDetail) {
                        r.WriteLine(item.TestName.ToString() + ":" + item.Result.ToString());
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
