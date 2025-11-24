using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ORAN_Aging.Form1;

namespace ORAN_Aging
{
    internal class VZ_PCS
    {
        public SerialPort port = new SerialPort();
        public string reader = string.Empty;
        private DataGridView agingGridView;
        bool isPassing = true;
  
        private string ReadPort(SerialPort port) {
            string reader = string.Empty;
            try {
                if (!port.IsOpen) {
                    port.Open();
                }
                reader = port.ReadExisting();
            }
            catch (Exception ex) {
                Task.Run(() => { MessageBox.Show("Communication issues with com port " + port.PortName + "\n" + ex.ToString()); });
                reader = "!fail!";
            }
            return reader;
        }

        private void WritetoFile(string logfile, int slot, string reader) {
            int retries = 0;
            bool success = false;

           
  

            // Split reader text into individual lines
            string[] lines = reader.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Prefix each line with the timestamp
            for (int i = 0; i < lines.Length; i++) {
                lines[i] = $" {lines[i]}";
            }

            // Rebuild the full message
            string message = string.Join(Environment.NewLine, lines) + Environment.NewLine;

            while (retries < 10 && !success) {
                try {
                    using (FileStream fs = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.None))
                    using (StreamWriter writer = new StreamWriter(fs)) {
                        writer.Write(message);
                    }
                    success = true;
                }
                catch {
                    retries++;
                    Thread.Sleep(500);
                }
            }
        }
        private string SendPortCommand(SerialPort port, string command, string endPoint, string logfile, int slot) {
            DateTime timeout = DateTime.Now.AddSeconds(5);
            StringBuilder readerBuilder = new StringBuilder();
            try {
                if (!port.IsOpen) {
                    port.Open();
                }
                port.WriteLine(command);
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (!readerBuilder.ToString().Contains(endPoint) && stopwatch.Elapsed < TimeSpan.FromSeconds(30)) {
                    readerBuilder.Append(ReadPort(port));
                    if (readerBuilder.ToString().Contains("Broken pipe") || readerBuilder.ToString().Contains("reset")) {
                        readerBuilder.Append("\n!fail!\n");
                        break;
                    }
                }

            }
            catch (Exception e) {
                File.AppendAllText(@"C:\Test_CTDI\ErrorLog.txt", port.PortName + " threw an exception\n\n" + e.ToString() + "\n\n");
                readerBuilder.Clear();
            }

            string reader = readerBuilder.ToString();
            bool isPassing = reader.Length > 0 && reader.Contains(endPoint);
            WritetoFile(logfile, slot, reader);

            return reader;
        }
        public void SetupVZPCS(SerialPort port, string logfile,int slot) {
            SendPortCommand(port, "gulInitialDiagnosticPause=1", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test 0x880040c0 4 0x00001f00 -w", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_Set_Pattern_Data", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_Set_Pattern_Active", "UShell >", logfile, slot);
            SendPortCommand(port, "almchg 4 7 3 100", "UShell >", logfile, slot);
            SendPortCommand(port, "almchg 21 7 3 100", "UShell >", logfile, slot);
            SendPortCommand(port, "TestAddFA 0 0xF 0xF 2145000 1745000 20000 32 4600 0", "UShell >", logfile, slot);
            SendPortCommand(port, "TestAddFA 1 0xF0 0xF0 1960000 1880000 20000 32 4600 0", "UShell >", logfile, slot);
            SendPortCommand(port, "TestTurnOnPath 0 0xF 0xF", "UShell >", logfile, slot); //different
            SendPortCommand(port, "mem_test -w 0x88029840 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x88028038 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x88029848 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x88029848 4 0xF", "UShell >", logfile, slot);
            SendPortCommand(port, "TestTurnOnPath 1 0xFF 0xFF", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x98029840 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x98028038 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x98029848 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x98029848 4 0xF", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 0 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 1 0x1", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 2 0x2", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 3 0x3", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 4 0x8", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 5 0x9", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 6 0xA", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 7 0xB", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 8 0x10", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 9 0x11", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 10 0x12", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 11 0x13", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 12 0x18", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 13 0x19", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 14 0x1A", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 15 0x1B", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 16 0x20", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 17 0x21", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 18 0x22", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 19 0x23", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 20 0x28", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 21 0x29", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 22 0x2A", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 23 0x2B", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 24 0x30", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 25 0x31", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 26 0x32", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 27 0x33", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 28 0x38", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 29 0x39", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 30 0x3A", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 31 0x3B", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 0 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 1 0x1", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 2 0x2", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 3 0x3", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 4 0x8", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 5 0x9", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 6 0xA", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 7 0xB", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 8 0x10", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 9 0x11", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 10 0x12", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 11 0x13", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 12 0x18", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 13 0x19", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 14 0x1A", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 15 0x1B", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 16 0x20", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 17 0x21", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 18 0x22", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 19 0x23", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 20 0x28", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 21 0x29", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 22 0x2A", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 23 0x2B", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 24 0x30", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 25 0x31", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 26 0x32", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 27 0x33", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 28 0x38", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 29 0x39", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 30 0x3A", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 4 31 0x3B", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 0 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 1 0x1", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 2 0x2", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 3 0x3", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 4 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 5 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 6 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 7 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 8 0x4", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 9 0x5", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 10 0x6", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 11 0x7", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 12 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 13 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 14 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 15 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 16 0x8", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 17 0x9", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 18 0xA", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 19 0xB", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 20 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 21 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 22 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 23 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 24 0xC", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 25 0xD", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 26 0xE", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 27 0xF", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 28 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 29 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 30 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 31 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 32 0x10", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 33 0x11", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 34 0x12", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 35 0x13", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 36 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 37 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 38 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 39 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 40 0x14", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 41 0x15", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 42 0x16", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 43 0x17", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 44 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 45 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 46 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 47 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 48 0x18", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 49 0x19", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 50 0x1A", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 51 0x1B", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 52 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 53 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 54 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 55 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 56 0x1C", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 57 0x1D", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 58 0x1E", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 59 0x1F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 60 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 61 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 62 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 0 63 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 0 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 1 0x1", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 2 0x2", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 3 0x3", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 4 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 5 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 6 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 7 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 8 0x4", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 9 0x5", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 10 0x6", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 11 0x7", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 12 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 13 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 14 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 15 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 16 0x8", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 17 0x9", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 18 0xA", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 19 0xB", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 20 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 21 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 22 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 23 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 24 0xC", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 25 0xD", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 26 0xE", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 27 0xF", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 28 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 29 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 30 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 31 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 32 0x10", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 33 0x11", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 34 0x12", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 35 0x13", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 36 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 37 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 38 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 39 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 40 0x14", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 41 0x15", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 42 0x16", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 43 0x17", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 44 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 45 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 46 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 47 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 48 0x18", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 49 0x19", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 50 0x1A", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 51 0x1B", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 52 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 53 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 54 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 55 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 56 0x1C", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 57 0x1D", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 58 0x1E", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 59 0x1F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 60 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 61 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 62 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_UL_MapperAddressBufferSet 0 4 63 0x7F", "UShell >", logfile, slot);
            SendPortCommand(port, "", "UShell >", logfile, slot);

        }
    }
}
