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
    internal class VZ_LOLO
    {
        public SerialPort port = new SerialPort();
        public string reader = string.Empty;
        private DataGridView agingGridView;
        bool isPassing = true;
        //private string ReturnTimeStamp(int slot) {
        //    return agingGridView.Rows[(int)AgingDataRow.Timer].Cells[slot].Value.ToString();
        //}
        byte[] interrupt = new byte[] { 0x03 };
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

            //// Assume ReturnTimeStamp returns a string like "00:00:15.219"
            //string timestamp = "[0 " + ReturnTimeStamp(slot) + "]";

            // Split reader text into individual lines
            string[] lines = reader.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Prefix each line with the timestamp
            for (int i = 0; i < lines.Length; i++) {
                lines[i] = $"{lines[i]}";
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


        public void SetupVZLOLO(SerialPort port, string logfile,int slot) {
            SendPortCommand(port, "gulInitialDiagnostic=1", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_Set_Pattern_Data", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_Set_Pattern_Active", "UShell >", logfile, slot);
            SendPortCommand(port, "almchg 4 7 3 100", "UShell >", logfile, slot);
            SendPortCommand(port, "almchg 21 7 3 100", "UShell >", logfile, slot);
            SendPortCommand(port, "TestAddFA 0 0xF 0xF 751000 782000 10000 32 4600 0", "UShell >", logfile, slot);
            SendPortCommand(port, "TestAddFA 1 0xF0 0xF0  876500 831500 10000 32 4600 0", "UShell >", logfile, slot);
            SendPortCommand(port, "TestTurnOnPath 0 0xF 0xF", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x88029840 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x88028038 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x88029848 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x88029848 4 0xF", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test 0x8802a054 4 0x0 -w", "UShell >", logfile, slot);
            SendPortCommand(port, "TestTurnOnPath 1 0xFF 0xFF", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x98029840 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x98028038 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x98029848 4 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test -w 0x98029848 4 0xF", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test 0x9802a054 4 0x0 -w", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 0 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 1 0x1", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 2 0x2", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 3 0x3", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 8 0x10", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 9 0x11", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 10 0x12", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 11 0x13", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 16 0x20", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 17 0x21", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 18 0x22", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 19 0x23", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 24 0x30", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 25 0x31", "UShell >", logfile,slot );
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 26 0x32", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 0 0 27 0x33", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 0 0x0", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 1 0x1", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 2 0x2", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 3 0x3", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 8 0x10", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 9 0x11", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 10 0x12", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 11 0x13", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 16 0x20", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 17 0x21", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 18 0x22", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 19 0x23", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 24 0x30", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 25 0x31", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 26 0x32", "UShell >", logfile, slot);
            SendPortCommand(port, "INTF_DL_MapperAddressBufferSet 1 0 27 0x33", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test 0x8802a054 4 0x2 -w", "UShell >", logfile, slot);
            SendPortCommand(port, "mem_test 0x9802a054 4 0x2 -w", "UShell >", logfile, slot);

        }
    }
}
