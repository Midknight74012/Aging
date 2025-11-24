using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace Close_the_Port
{
    internal class TriBand
    {
        // TriBand.cs
        // Status: LEGACY / Kept for reference
        // Primary author: <Engineering Manager Name>
        // Maintainer: <Your Name or Team>
        // Created: <approx date>
        // Notes: This code configures older TriBand units. Kept for historical/maintenance reasons.
        //        Consider moving to a legacy folder or removing from the project when confident it's unused.
        //
        // Obsolete: Use only for historical reference. Will be removed in a future cleanup.
        public SerialPort port = new SerialPort();
        public string reader = string.Empty;
        public string logfile = string.Empty;
        bool isPassing = true;

        private static readonly string DeviceMgmtPassword = Environment.GetEnvironmentVariable("DEVICE_MGMT_PASSWORD") ?? "REDACTED_MGMT";
        private static readonly string DeviceRootPassword = Environment.GetEnvironmentVariable("DEVICE_ROOT_PASSWORD") ?? "REDACTED_ROOT";
        public void SetupTriBand(SerialPort port, string logfile, int slot) {
            port.WriteLine("user");
            Thread.Sleep(1000);
            reader = port.ReadExisting();
            Console.Write(reader);

            Console.WriteLine(reader);

            port.WriteLine(DeviceRootPassword);
            Thread.Sleep(1000);
            reader = port.ReadExisting();
            Console.Write(reader);

            Console.WriteLine(reader);

            port.WriteLine("su -");
            Thread.Sleep(1000);
            reader = port.ReadExisting();
            Console.Write(reader);


            port.WriteLine(DeviceRootPassword);
            Thread.Sleep(1000);
            reader = port.ReadExisting();
            Console.Write(reader);


            reader = SendPortCommand(port, "gettail 0", ">", logfile, slot);
            reader = SendPortCommand(port, "getinv", ">", logfile, slot);

            reader = port.ReadExisting();
            reader = string.Empty;
            port.WriteLine("ucmd SetMsgPrint 0");
            Thread.Sleep(1000);
            reader = port.ReadExisting();

            reader = port.ReadExisting();
            reader = string.Empty;
            port.WriteLine("echo 20 > /proc/axiEnetDbg");
            Thread.Sleep(1000);
            reader = port.ReadExisting();

            reader = SendPortCommand(port, "ushell", "UShell >", logfile, slot);
            //;FITF mode 해지
            reader = SendPortCommand(port, "gulInitialDiagnosticPause = 1", "UShell >", logfile, slot);
            //Disable ClockFail
            reader = SendPortCommand(port, "almchg 3 7 3 100", "UShell >", logfile, slot);
            //Disable CpriFail
            reader = SendPortCommand(port, "almchg 17 7 3 100", "UShell >", logfile, slot);
            //Disable UnitBlock
            reader = SendPortCommand(port, "almchg 46 7 3 100", "UShell >", logfile, slot);
            //Disable SFNFail
            reader = SendPortCommand(port, "almchg 23 7 3 100", "UShell >", logfile, slot);
            //Disable TransceiverFault
            reader = SendPortCommand(port, "almchg 45 7 3 100", "UShell >", logfile, slot);
            //Disable Lowgain(log)
            reader = SendPortCommand(port, "almchg 35 6 2 100 100", "UShell >", logfile, slot);
            //Disable VSWRFailVer3
            reader = SendPortCommand(port, "almchg 39 6 2 100", "UShell >", logfile, slot);
            //Disable [ 6] VswrFail(mn)
            reader = SendPortCommand(port, "almchg 6 7 3 100", "UShell >", logfile, slot);
            //Disable SyncError
            reader = SendPortCommand(port, "almchg 44 5 2 100 100", "UShell >", logfile, slot);
            //Disable [43] NoExtSyncSrc
            reader = SendPortCommand(port, "almchg 43 6 2 100 100", "UShell >", logfile, slot);
            //Disable [44] SyncError
            reader = SendPortCommand(port, "almchg 44 6 2 100 100", "UShell >", logfile, slot);
            //Disable [47] ConfigCorrupted
            reader = SendPortCommand(port, "almchg 47 7 3 100", "UShell >", logfile, slot);
            //Disable [1] Shutdown
            reader = SendPortCommand(port, "almchg 1 6 2 100 100", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "INTF_PASwitchONall", "UShell >", logfile, slot);
            //Enable 5MHz TP
            reader = SendPortCommand(port, "mem_test 0x88012e00 4 0x1 -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88012d00 4 0x44444444 -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "debug_level 0x00980006 0", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "debug_level 0x0098000B 0", "UShell >", logfile, slot);
            //DS_MOD_DAC_IN 0x8000 setting
            reader = SendPortCommand(port, "mem_test 0xb0000054 4 0x8000 -w", "UShell >", logfile, slot);
            //Delay setting
            reader = SendPortCommand(port, "mem_test 0x88010800 4 0xeb -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88010804 4 0xb00 -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "ifconfig eth4 down", ">", logfile, slot);
            reader = SendPortCommand(port, "ucmd SetMsgPrint", ">", logfile, slot);
            reader = SendPortCommand(port, "confd_cli --noaaa", "Welcome", logfile, slot);
            reader = SendPortCommand(port, "configure", "%", logfile, slot);
            reader = SendPortCommand(port, "set interfaces interface uplane_0.936 type l2vlan mac-address 20:44:50:07:1a:a0 vlan-id 936", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "Commit complete", logfile, slot);
            reader = SendPortCommand(port, "set processing-elements transport-session-type ETH-INTERFACE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set processing-elements ru-elements re_0_0_0_936 transport-flow interface-name uplane_0.936 eth-flow o-du-mac-address 11:22:33:44:55:66 ru-mac-address 20:44:50:07:1a:a0 vlan-id 936", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-000 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 0", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-000 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-000 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-001 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 2", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-001 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-001 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-002 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-002 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-002 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-003 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 3", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-003 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-003 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-000 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 0", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-000 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-000 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-000 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-001 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-001 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-001 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-001 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-002 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 2", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-002 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-002 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-002 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-003 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 3", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-003 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-003 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-003 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-000 absolute-frequency-center 123900 center-of-channel-bandwidth 619500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-001 absolute-frequency-center 123900 center-of-channel-bandwidth 619500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-002 absolute-frequency-center 123900 center-of-channel-bandwidth 619500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-003 absolute-frequency-center 123900 center-of-channel-bandwidth 619500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-000 absolute-frequency-center 133100 center-of-channel-bandwidth 665500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-001 absolute-frequency-center 133100 center-of-channel-bandwidth 665500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-002 absolute-frequency-center 133100 center-of-channel-bandwidth 665500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-003 absolute-frequency-center 133100 center-of-channel-bandwidth 665500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-000 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-000 low-level-tx-endpoint Low-Level-Tx-Endpoint-000", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                string input = string.Empty;
                Console.WriteLine("\nUnit aborted the set up command.\nContinue?", logfile, slot);
                while (input != "n" && input != "N") {
                    input = Console.ReadLine();
                    port.WriteLine(input);
                    Thread.Sleep(300);
                    reader = port.ReadExisting();

                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-001 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-001 low-level-tx-endpoint Low-Level-Tx-Endpoint-001", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-002 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-002 low-level-tx-endpoint Low-Level-Tx-Endpoint-002", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-003 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-003 low-level-tx-endpoint Low-Level-Tx-Endpoint-003", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-000 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-000 low-level-rx-endpoint Low-Level-Rx-Endpoint-000", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-001 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-001 low-level-rx-endpoint Low-Level-Rx-Endpoint-001", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }

            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-002 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-002 low-level-rx-endpoint Low-Level-Rx-Endpoint-002", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-003 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-003 low-level-rx-endpoint Low-Level-Rx-Endpoint-003", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88010c40 4 -w 0x000f00ff", ">", logfile, slot);
            reader = SendPortCommand(port, "confd_cli --noaaa", ">", logfile, slot);
            reader = SendPortCommand(port, "configure", "%", logfile, slot);
            reader = SendPortCommand(port, "set interfaces interface uplane_0.936 type l2vlan mac-address 20:44:50:07:1a:a0 vlan-id 936", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set processing-elements transport-session-type ETH-INTERFACE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set processing-elements ru-elements re_0_0_0_936 transport-flow interface-name uplane_0.936 eth-flow o-du-mac-address 11:22:33:44:55:66 ru-mac-address 20:44:50:07:1a:a0 vlan-id 936", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-008 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 528", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-008 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-008 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-009 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 530", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-009 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-009 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-010 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 529", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-010 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-010 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-011 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 531", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-011 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-011 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-016 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 528", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-016 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-016 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-016 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-017 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 529", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-017 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-017 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-017 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-018 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 530", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-018 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-018 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-019 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 531", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-019 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-019 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-019 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-008 absolute-frequency-center 129900 center-of-channel-bandwidth 649500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-009 absolute-frequency-center 129900 center-of-channel-bandwidth 649500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-010 absolute-frequency-center 129900 center-of-channel-bandwidth 649500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-011 absolute-frequency-center 129900 center-of-channel-bandwidth 649500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-008 absolute-frequency-center 139100 center-of-channel-bandwidth 695500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-009 absolute-frequency-center 139100 center-of-channel-bandwidth 695500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-010 absolute-frequency-center 139100 center-of-channel-bandwidth 695500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-011 absolute-frequency-center 139100 center-of-channel-bandwidth 695500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-008 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-008 low-level-tx-endpoint Low-Level-Tx-Endpoint-008", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-009 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-009 low-level-tx-endpoint Low-Level-Tx-Endpoint-009", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-010 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-010 low-level-tx-endpoint Low-Level-Tx-Endpoint-010", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-011 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-011 low-level-tx-endpoint Low-Level-Tx-Endpoint-011", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-016 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-008 low-level-rx-endpoint Low-Level-Rx-Endpoint-016", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-017 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-009 low-level-rx-endpoint Low-Level-Rx-Endpoint-017", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-018 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-010 low-level-rx-endpoint Low-Level-Rx-Endpoint-018", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-019 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-011 low-level-rx-endpoint Low-Level-Rx-Endpoint-019", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88010c40 4 -w 0x000f00ff", ">", logfile, slot);
            reader = SendPortCommand(port, "confd_cli --noaaa", "Welcome", logfile, slot);
            reader = SendPortCommand(port, "configure", "%", logfile, slot);
            reader = SendPortCommand(port, "set interfaces interface uplane_0.936 type l2vlan mac-address 20:44:50:07:1a:a0 vlan-id 936", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set processing-elements transport-session-type ETH-INTERFACE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set processing-elements ru-elements re_0_0_0_936 transport-flow interface-name uplane_0.936 eth-flow o-du-mac-address 11:22:33:44:55:66 ru-mac-address 20:44:50:07:1a:a0 vlan-id 936", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-024 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1280", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-024 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-024 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-025 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1282", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-025 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-025 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-026 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1281", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-026 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-026 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-027 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1283", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-027 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-027 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-024 absolute-frequency-center 144600 center-of-channel-bandwidth 723000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-025 absolute-frequency-center 144600 center-of-channel-bandwidth 723000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-026 absolute-frequency-center 144600 center-of-channel-bandwidth 723000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-027 absolute-frequency-center 144600 center-of-channel-bandwidth 723000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-024 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-024 low-level-tx-endpoint Low-Level-Tx-Endpoint-024", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-025 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-025 low-level-tx-endpoint Low-Level-Tx-Endpoint-025", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-026 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-026 low-level-tx-endpoint Low-Level-Tx-Endpoint-026", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-027 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-027 low-level-tx-endpoint Low-Level-Tx-Endpoint-027", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88010c40 4 -w 0x000f00ff", ">", logfile, slot);
            reader = SendPortCommand(port, "confd_cli --noaaa", ">", logfile, slot);
            reader = SendPortCommand(port, "configure", "%", logfile, slot);
            reader = SendPortCommand(port, "set interfaces interface uplane_0.936 type l2vlan mac-address 20:44:50:07:1a:a0 vlan-id 936", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set processing-elements transport-session-type ETH-INTERFACE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set processing-elements ru-elements re_0_0_0_936 transport-flow interface-name uplane_0.936 eth-flow o-du-mac-address 11:22:33:44:55:66 ru-mac-address 20:44:50:07:1a:a0 vlan-id 936", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-016 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1664", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-016 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-016 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-017 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1666", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-017 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-017 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-018 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1665", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-018 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-018 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-019 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1667", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-019 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-019 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-032 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1664", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-032 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-032 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-032 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-033 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1665", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-033 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-033 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-033 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-034 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1666", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-034 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-034 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-034 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-035 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -300 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1667", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-035 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-035 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-035 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-016 absolute-frequency-center 173300 center-of-channel-bandwidth 866500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-017 absolute-frequency-center 173300 center-of-channel-bandwidth 866500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-018 absolute-frequency-center 173300 center-of-channel-bandwidth 866500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-019 absolute-frequency-center 173300 center-of-channel-bandwidth 866500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-016 absolute-frequency-center 164300 center-of-channel-bandwidth 821500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-017 absolute-frequency-center 164300 center-of-channel-bandwidth 821500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-018 absolute-frequency-center 164300 center-of-channel-bandwidth 821500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-019 absolute-frequency-center 164300 center-of-channel-bandwidth 821500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-016 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-016 low-level-tx-endpoint Low-Level-Tx-Endpoint-016", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-017 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-017 low-level-tx-endpoint Low-Level-Tx-Endpoint-017", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-018 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-018 low-level-tx-endpoint Low-Level-Tx-Endpoint-018", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-019 processing-element re_0_0_0_936 tx-array-carrier Tx-Array-Carrier-019 low-level-tx-endpoint Low-Level-Tx-Endpoint-019", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-032 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-016 low-level-rx-endpoint Low-Level-Rx-Endpoint-032", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-033 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-017 low-level-rx-endpoint Low-Level-Rx-Endpoint-033", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-034 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-018 low-level-rx-endpoint Low-Level-Rx-Endpoint-034", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-035 processing-element re_0_0_0_936 rx-array-carrier Rx-Array-Carrier-019 low-level-rx-endpoint Low-Level-Rx-Endpoint-035", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);

            if (reader.Contains("Aborted: illegal reference")) {
                Console.WriteLine("Unit aborted the set up command.\nContinue?", logfile, slot);
                string input = Console.ReadLine();
                if (input == "n" || input == "N") {
                    port.Close();
                    goto End;
                }
            }
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88010c40 4 -w 0x000f00ff", "%", logfile, slot);
            reader = string.Empty;
            int countdown = 20;
            while (!reader.Contains("[INFO:RampUp] Path[07]") && countdown > 0) {
                Thread.Sleep(500);
                reader += port.ReadExisting();
                countdown--;
            }
            Console.WriteLine(reader);
            reader = SendPortCommand(port, "ushell", "UShell >", logfile, slot);


            reader = SendPortCommand(port, "mem_test 0x88015c00 4 0xDBE3 -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c04 4 0xDBE3 -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c08 4 0xDBE3 -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c0c 4 0xDBE3 -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88010c40 4 -w 0x000f00ff", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c10 4 0xCDAF -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c14 4 0xCDAF -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c18 4 0xCDAF -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c1c 4 0xCDAF -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c1c 4 0xCDAF -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88010c40 4 -w 0x000f00ff", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "debug_level 0x00980006 0", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "debug_level 0x0098000B 0", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "almchg 44 5 2 100 100", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "INTF_PASwitchONall", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "sts", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "pacalsts", "UShell >", logfile, slot);
            Thread.Sleep(45000);
            reader = SendPortCommand(port, "gettail 0", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "sts", "UShell >", logfile, slot);
        End:;
        }
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
    }
}
