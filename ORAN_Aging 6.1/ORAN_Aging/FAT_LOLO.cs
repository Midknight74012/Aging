using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORAN_Aging
{
    internal class FAT_LOLO
    {
        string reader = string.Empty;
        private string SendPortCommand(SerialPort port, string command, string endPoint, string logfile, int slot, int maxRetries = 2) {
            return PortCommandHelper.SendCommand(port, command, endPoint, logfile, slot, AppConstants.CommandTimeoutSeconds, maxRetries);
        }
        public string[] setupCommands = {
            "gulInitialDiagnosticPause=1",
            "INTF_Set_Pattern_Data",
            "INTF_Set_Pattern_Active",
            "almchg 4 7 3 100",
            "almchg 21 7 3 100",
            "TestAddFA 0 0xF 0xF 751000 782000 10000 32 4600 0",
            "TestAddFA 1 0xF0 0xF0  876500 831500 10000 32 4600 0",
            "TestTurnOnPath 0 0xF 0xF",
            "mem_test -w 0x88029840 4 0x0",
            "mem_test -w 0x88028038 4 0x0",
            "mem_test -w 0x88029848 4 0x0",
            "mem_test -w 0x88029848 4 0xF",
            "mem_test 0x8802a054 4 0x0 -w",
            "TestTurnOnPath 1 0xFF 0xFF",
            "mem_test -w 0x98029840 4 0x0",
            "mem_test -w 0x98028038 4 0x0",
            "mem_test -w 0x98029848 4 0x0",
            "mem_test -w 0x98029848 4 0xF",
            "mem_test 0x9802a054 4 0x0 -w",
            "INTF_DL_MapperAddressBufferSet 0 0 0 0x0",
            "INTF_DL_MapperAddressBufferSet 0 0 1 0x1",
            "INTF_DL_MapperAddressBufferSet 0 0 02 0x2",
            "INTF_DL_MapperAddressBufferSet 0 0 3 0x3",
            "INTF_DL_MapperAddressBufferSet 0 0 8 0x10",
            "INTF_DL_MapperAddressBufferSet 0 0 9 0x11",
            "INTF_DL_MapperAddressBufferSet 0 0 10 0x12",
            "INTF_DL_MapperAddressBufferSet 0 0 11 0x13",
            "INTF_DL_MapperAddressBufferSet 0 0 16 0x20",
            "INTF_DL_MapperAddressBufferSet 0 0 17 0x21",
            "INTF_DL_MapperAddressBufferSet 0 0 18 0x22",
            "INTF_DL_MapperAddressBufferSet 0 0 19 0x23",
            "INTF_DL_MapperAddressBufferSet 0 0 24 0x30",
            "INTF_DL_MapperAddressBufferSet 0 0 25 0x31",
            "INTF_DL_MapperAddressBufferSet 0 0 26 0x32",
            "INTF_DL_MapperAddressBufferSet 0 0 27 0x33",
            "INTF_DL_MapperAddressBufferSet 1 0 0 0x0",
            "INTF_DL_MapperAddressBufferSet 1 0 1 0x1",
            "INTF_DL_MapperAddressBufferSet 1 0 02 0x2",
            "INTF_DL_MapperAddressBufferSet 1 0 3 0x3",
            "INTF_DL_MapperAddressBufferSet 1 0 8 0x10",
            "INTF_DL_MapperAddressBufferSet 1 0 9 0x11",
            "INTF_DL_MapperAddressBufferSet 1 0 10 0x12",
            "INTF_DL_MapperAddressBufferSet 1 0 11 0x13",
            "INTF_DL_MapperAddressBufferSet 1 0 16 0x20",
            "INTF_DL_MapperAddressBufferSet 1 0 17 0x21",
            "INTF_DL_MapperAddressBufferSet 1 0 18 0x22",
            "INTF_DL_MapperAddressBufferSet 1 0 19 0x23",
            "INTF_DL_MapperAddressBufferSet 1 0 24 0x30",
            "INTF_DL_MapperAddressBufferSet 1 0 25 0x31",
            "INTF_DL_MapperAddressBufferSet 1 0 26 0x32",
            "INTF_DL_MapperAddressBufferSet 1 0 27 0x33",
            "mem_test 0x8802a054 4 0x2 -w",
            "mem_test 0x9802a054 4 0x2 -w",
        };
        public void SetupVZ_XLLOLO(SerialPort port, string logfile, int slot) {
            foreach (string line in setupCommands) {
                reader = SendPortCommand(port, line, "UShell >", logfile, slot);
                if (reader.Contains("!fail")) {
                    break;
                }
            }
        }
    }
}
