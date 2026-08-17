using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace ORAN_Aging
{
    /// <summary>
    /// Shared serial port communication helper. Eliminates duplicated SendPortCommand,
    /// ReadPort, WritetoFile, and re-login logic across VZ_PCS, VZ_LOLO, TriBand, DualBand.
    /// </summary>
    public static class PortCommandHelper
    {
        /// <summary>
        /// Sends a command to the serial port and waits for the expected endpoint string.
        /// Automatically detects session loss and re-logs in before retrying.
        /// </summary>
        public static string SendCommand(SerialPort port, string command, string endPoint,
            string logfile, int slot, int timeoutSeconds = 5, int maxRetries = 2)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                string result = SendCommandInternal(port, command, endPoint, logfile, slot, timeoutSeconds);

                if (attempt < maxRetries && IsSessionLost(result))
                {
                    Console.WriteLine($"[{port.PortName}] Session lost detected (attempt {attempt + 1}): re-logging in...");
                    bool reloggedIn = AttemptReLogin(port, logfile, slot);
                    if (!reloggedIn)
                    {
                        Console.WriteLine($"[{port.PortName}] Re-login failed.");
                        return "!fail!";
                    }
                    continue;
                }

                return result;
            }
            return "!fail!";
        }

        /// <summary>
        /// Internal command send without retry/re-login logic.
        /// </summary>
        private static string SendCommandInternal(SerialPort port, string command, string endPoint,
            string logfile, int slot, int timeoutSeconds)
        {
            StringBuilder readerBuilder = new StringBuilder();
            try
            {
                if (!port.IsOpen)
                {
                    port.Open();
                }
                port.WriteLine(command);
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (!readerBuilder.ToString().Contains(endPoint) && stopwatch.Elapsed < TimeSpan.FromSeconds(timeoutSeconds))
                {
                    readerBuilder.Append(ReadPort(port));
                    string current = readerBuilder.ToString();
                    if (current.Contains("Broken pipe") || current.Contains("reset"))
                    {
                        readerBuilder.Append("\n!fail!\n");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                File.AppendAllText(AppConstants.ErrorLogPath,
                    port.PortName + " threw an exception\n\n" + e.ToString() + "\n\n");
                readerBuilder.Clear();
            }

            string reader = readerBuilder.ToString();
            WriteToFile(logfile, slot, reader);
            return reader;
        }

        /// <summary>
        /// Reads all available data from the serial port.
        /// </summary>
        public static string ReadPort(SerialPort port)
        {
            try
            {
                if (!port.IsOpen)
                {
                    port.Open();
                }
                return port.ReadExisting();
            }
            catch (Exception ex)
            {
                Task.Run(() => {
                    MessageBox.Show("Communication issues with com port " + port.PortName + "\n" + ex.ToString());
                });
                return "!fail!";
            }
        }

        /// <summary>
        /// Writes timestamped content to the log file with retry logic.
        /// </summary>
        public static void WriteToFile(string logfile, int slot, string reader)
        {
            if (string.IsNullOrEmpty(logfile) || string.IsNullOrEmpty(reader)) return;

            string[] lines = reader.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = $" {lines[i]}";
            }
            string message = string.Join(Environment.NewLine, lines) + Environment.NewLine;

            int retries = 0;
            bool success = false;
            while (retries < 10 && !success)
            {
                try
                {
                    using (FileStream fs = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.None))
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.Write(message);
                    }
                    success = true;
                }
                catch
                {
                    retries++;
                    Thread.Sleep(500);
                }
            }
        }

        /// <summary>
        /// Checks if a response indicates the session was lost.
        /// </summary>
        public static bool IsSessionLost(string response)
        {
            if (string.IsNullOrEmpty(response) || response == "!fail!") return false;
            foreach (var indicator in AppConstants.SessionLostIndicators)
            {
                if (response.Contains(indicator, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to re-login to the unit using the standard login sequence.
        /// </summary>
        public static bool AttemptReLogin(SerialPort port, string logfile, int slot)
        {
            try
            {
                if (!port.IsOpen) port.Open();
                port.ReadExisting();
                Thread.Sleep(500);

                // Send Enter to get a fresh prompt
                port.WriteLine("");
                Thread.Sleep(1000);
                port.ReadExisting();

                string result = SendCommandInternal(port, AppConstants.UnitUsername,
                    AppConstants.PasswordPrompt, logfile, slot, AppConstants.CommandTimeoutSeconds);
                if (string.IsNullOrEmpty(result) || !result.Contains(AppConstants.PasswordPrompt)) return false;

                result = SendCommandInternal(port, AppConstants.UnitUserPassword,
                    AppConstants.UnitUserPrompt, logfile, slot, AppConstants.CommandTimeoutSeconds);
                if (string.IsNullOrEmpty(result) || !result.Contains(AppConstants.UnitUserPrompt)) return false;

                result = SendCommandInternal(port, "su -",
                    AppConstants.PasswordPrompt, logfile, slot, AppConstants.CommandTimeoutSeconds);
                if (string.IsNullOrEmpty(result) || !result.Contains(AppConstants.PasswordPrompt)) return false;

                result = SendCommandInternal(port, AppConstants.UnitRootPassword,
                    AppConstants.UnitRootPrompt, logfile, slot, AppConstants.CommandTimeoutSeconds);
                if (string.IsNullOrEmpty(result) || !result.Contains(AppConstants.UnitRootPrompt)) return false;

                Console.WriteLine($"[{port.PortName}] Re-login successful.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{port.PortName}] Re-login exception: " + ex.Message);
                return false;
            }
        }
    }
}
