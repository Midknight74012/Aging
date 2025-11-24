using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace DualBandSetup
{
    public class DualBand
    {
        // DualBand.cs
        // Status: LEGACY / Kept for reference
        // Primary author: <Engineering Manager Name>
        // Maintainer: <Your Name or Team>
        // Created: <approx date>
        // Notes: This code configures older DualBand units. Kept for historical/maintenance reasons.
        //        Consider moving to a legacy folder or removing from the project when confident it's unused.
        //
        // Obsolete: Use only for historical reference. Will be removed in a future cleanup.
        public SerialPort port = new SerialPort();
        public string reader = string.Empty;
        public string logfile = string.Empty;

        private static readonly string DeviceMgmtPassword = Environment.GetEnvironmentVariable("DEVICE_MGMT_PASSWORD") ?? "REDACTED_MGMT";
        private static readonly string DeviceRootPassword = Environment.GetEnvironmentVariable("DEVICE_ROOT_PASSWORD") ?? "REDACTED_ROOT";
        public void SetupDualBand(SerialPort port,string logfile, int slot) {
            port.WriteLine("user");
            Thread.Sleep(1000);
            reader = port.ReadExisting();

            File.AppendAllText(logfile, reader);
            Console.WriteLine(reader);

            port.WriteLine(DeviceMgmtPassword);
            Thread.Sleep(1000);
            reader = port.ReadExisting();

            File.AppendAllText(logfile, reader);
            Console.WriteLine(reader);

            port.WriteLine("su -");
            Thread.Sleep(1000);
            reader = port.ReadExisting();

            File.AppendAllText(logfile, reader);

            port.WriteLine(DeviceRootPassword);
            Thread.Sleep(1000);
            reader = port.ReadExisting();

            File.AppendAllText(logfile, reader);

            reader = SendPortCommand(port, "gettail 0", ">", logfile,slot);

            File.AppendAllText(logfile, reader);

            reader = SendPortCommand(port, "getinv", ">", logfile,slot);

            File.AppendAllText(logfile, reader);

            reader = port.ReadExisting();
            reader = string.Empty;
            port.WriteLine("ucmd SetMsgPrint 0");
            Thread.Sleep(1000);
            reader = port.ReadExisting();

            File.AppendAllText(logfile, reader);

            reader = port.ReadExisting();
            reader = string.Empty;
            port.WriteLine("echo 20 > /proc/axiEnetDbg");
            Thread.Sleep(1000);
            reader = port.ReadExisting();

            File.AppendAllText(logfile, reader);

            reader = SendPortCommand(port, "ushell", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "gulInitialDiagnosticPause = 1", "UShell >", logfile, slot);
            //Disable ClockFail
            reader = SendPortCommand(port, "almchg 3 7 3 100", "UShell >", logfile, slot);
            //Disable CpriFail
            reader = SendPortCommand(port, "almchg 17 7 3 100", "UShell >", logfile, slot);
            //Disable UnitBlock
            reader = SendPortCommand(port, "almchg 43 7 3 100", "UShell >", logfile, slot);
            //Disable SFNFail
            reader = SendPortCommand(port, "almchg 23 7 3 100", "UShell >", logfile, slot);
            //Disable TransceiverFault
            reader = SendPortCommand(port, "almchg 42 7 3 100", "UShell >", logfile, slot);
            //Disable Lowgain(log) 
            reader = SendPortCommand(port, "almchg 34 6 2 100 100", "UShell >", logfile, slot);
            //Disable [ 6] VswrFail(mn)
            reader = SendPortCommand(port, "almchg 6 7 3 100", "UShell >", logfile, slot);
            //Disable SyncError
            reader = SendPortCommand(port, "almchg 41 5 2 100 100", "UShell >", logfile, slot);
            //Disable [40] NoExtSyncSrc
            reader = SendPortCommand(port, "almchg 40 6 2", "UShell >", logfile, slot);
            //Disable[41] SyncError
            reader = SendPortCommand(port, "almchg 41 7 3", "UShell >", logfile, slot);
            //Disable [44] ConfigCorrupted
            reader = SendPortCommand(port, "almchg 44 6 2 100", "UShell >", logfile, slot);
            //Disable [ 1] ShutDown
            reader = SendPortCommand(port, "almchg 1 7 3 100 100", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "INTF_PASwitchONall", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "almchg 18 7 3 100 100", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "almchg 17 7 3 100 100", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "almchg 24 7 3 100 100", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "almchg 27 6 2 100 100", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "ForcePathEn 1", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "ForcePathEn 2", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "ForcePathEn 3", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "ForcePathEn 4", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "ForcePathEn 5", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "ForcePathEn 6", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "ForcePathEn 7", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "ForcePathEn 8", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "debug_level 0x00980006 0", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "debug_level 0x0098000B 0", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "ifconfig eth4 down", ">", logfile, slot);
            reader = SendPortCommand(port, "confd_cli --noaaa", "Welcome to the ConfD CLI", logfile, slot);
            reader = SendPortCommand(port, "configure", "Entering configuration mode private", logfile, slot);
            reader = SendPortCommand(port, "set interfaces interface uplane_0_0_0.100 type l2vlan mac-address 00:53:54:41:36:22 vlan-id 100", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set processing-elements transport-session-type ETH-INTERFACE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set processing-elements ru-elements re_0_936 transport-flow interface-name uplane_0_0_0.100 eth-flow o-du-mac-address 11:22:33:44:55:66 ru-mac-address 00:53:54:41:36:22 vlan-id 100", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-000 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 512", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-000 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-000 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-001 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 514", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-001 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-001 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-002 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 513", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-002 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-002 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-003 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 515", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-003 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-003 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-004 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1040", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-004 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-004 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-005 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1042", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-005 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-005 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-006 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1041", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-006 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-006 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-007 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1043", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-007 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-008 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1568", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-008 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-008 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-009 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1570", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-009 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-009 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-010 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1569", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-010 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-010 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-011 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1571", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-011 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-011 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-012 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 2176", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-012 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-012 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-013 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 2178", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-013 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-013 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-014 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 2177", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-014 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-014 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-015 cp-length-other 144 cp-length 160 offset-to-absolute-frequency-center -1596 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 2179", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-015 frame-structure 144 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-endpoints Low-Level-Tx-Endpoint-015 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-000 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 512", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-000 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-000 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-000 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-001 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 513", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-001 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-001 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-001 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-002 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 514", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-002 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-002 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-002 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-003 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 515", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-003 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-003 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-008 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1040", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-008 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-008 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-008 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-009 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1041", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-009 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-009 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-009 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-010 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1042", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-010 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-010 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-010 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-011 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1043", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-011 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-011 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-011 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-016 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1568", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-016 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-016 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-016 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-017 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1569", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-017 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-017 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-017 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-018 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1570", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-018 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-018 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-018 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-019 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 1571", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-019 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-019 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-019 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-024 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 2176", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-024 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-024 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-024 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-025 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 2177", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-025 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-025 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-025 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-026 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 2178", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-026 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-026 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-026 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-027 cp-length 160 cp-length-other 144 offset-to-absolute-frequency-center -1624 e-axcid o-du-port-bitmask 65024 band-sector-bitmask 384 ccid-bitmask 112 ru-port-bitmask 15 eaxc-id 2179", "[DYNAMIC,STATIC]:", logfile, slot);
            reader = SendPortCommand(port, "STATIC", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-027 frame-structure 160 number-of-prb-per-scs KHZ_15 number-of-prb 25", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-027 ul-fft-sampling-offsets KHZ_15 ul-fft-sampling-offset 36", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-endpoints Low-Level-Rx-Endpoint-027 compression compression-type STATIC iq-bitwidth 9", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-000 absolute-frequency-center 422500 center-of-channel-bandwidth 2112500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-001 absolute-frequency-center 422500 center-of-channel-bandwidth 2112500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-002 absolute-frequency-center 422500 center-of-channel-bandwidth 2112500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-003 absolute-frequency-center 422500 center-of-channel-bandwidth 2112500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-004 absolute-frequency-center 431000 center-of-channel-bandwidth 2155000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-005 absolute-frequency-center 431000 center-of-channel-bandwidth 2155000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-006 absolute-frequency-center 431000 center-of-channel-bandwidth 2155000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-007 absolute-frequency-center 431000 center-of-channel-bandwidth 2155000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 40 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-008 absolute-frequency-center 439500 center-of-channel-bandwidth 2197500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-009 absolute-frequency-center 439500 center-of-channel-bandwidth 2197500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-010 absolute-frequency-center 439500 center-of-channel-bandwidth 2197500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-011 absolute-frequency-center 439500 center-of-channel-bandwidth 2197500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 43 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-012 absolute-frequency-center 399500 center-of-channel-bandwidth 1997500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 46 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-013 absolute-frequency-center 399500 center-of-channel-bandwidth 1997500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 46 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-014 absolute-frequency-center 399500 center-of-channel-bandwidth 1997500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 46 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration tx-array-carriers Tx-Array-Carrier-015 absolute-frequency-center 399500 center-of-channel-bandwidth 1997500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain 46 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-000 absolute-frequency-center 342500 center-of-channel-bandwidth 1712500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-001 absolute-frequency-center 342500 center-of-channel-bandwidth 1712500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-002 absolute-frequency-center 342500 center-of-channel-bandwidth 1712500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-003 absolute-frequency-center 342500 center-of-channel-bandwidth 1712500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-004 absolute-frequency-center 349000 center-of-channel-bandwidth 1745000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-005 absolute-frequency-center 349000 center-of-channel-bandwidth 1745000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-006 absolute-frequency-center 349000 center-of-channel-bandwidth 1745000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-007 absolute-frequency-center 349000 center-of-channel-bandwidth 1745000000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-008 absolute-frequency-center 355500 center-of-channel-bandwidth 1777500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-009 absolute-frequency-center 355500 center-of-channel-bandwidth 1777500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-010 absolute-frequency-center 355500 center-of-channel-bandwidth 1777500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-011 absolute-frequency-center 355500 center-of-channel-bandwidth 1777500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-012 absolute-frequency-center 339500 center-of-channel-bandwidth 1697500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-013 absolute-frequency-center 339500 center-of-channel-bandwidth 1697500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-014 absolute-frequency-center 339500 center-of-channel-bandwidth 1697500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration rx-array-carriers Rx-Array-Carrier-015 absolute-frequency-center 339500 center-of-channel-bandwidth 1697500000 channel-bandwidth 5000000 downlink-radio-frame-offset 0 downlink-sfn-offset 0 gain-correction 0 n-ta-offset 0 type NR active ACTIVE", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-000 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-000 low-level-tx-endpoint Low-Level-Tx-Endpoint-000", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-001 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-001 low-level-tx-endpoint Low-Level-Tx-Endpoint-001", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-002 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-002 low-level-tx-endpoint Low-Level-Tx-Endpoint-002", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-003 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-003 low-level-tx-endpoint Low-Level-Tx-Endpoint-003", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-004 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-004 low-level-tx-endpoint Low-Level-Tx-Endpoint-004", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-005 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-005 low-level-tx-endpoint Low-Level-Tx-Endpoint-005", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-006 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-006 low-level-tx-endpoint Low-Level-Tx-Endpoint-006", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-007 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-007 low-level-tx-endpoint Low-Level-Tx-Endpoint-007", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-008 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-008 low-level-tx-endpoint Low-Level-Tx-Endpoint-008", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-009 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-009 low-level-tx-endpoint Low-Level-Tx-Endpoint-009", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-010 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-010 low-level-tx-endpoint Low-Level-Tx-Endpoint-010", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-011 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-011 low-level-tx-endpoint Low-Level-Tx-Endpoint-011", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-012 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-012 low-level-tx-endpoint Low-Level-Tx-Endpoint-012", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-013 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-013 low-level-tx-endpoint Low-Level-Tx-Endpoint-013", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-014 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-014 low-level-tx-endpoint Low-Level-Tx-Endpoint-014", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-tx-links Low-Level-Tx-Link-015 processing-element re_0_936 tx-array-carrier Tx-Array-Carrier-015 low-level-tx-endpoint Low-Level-Tx-Endpoint-015", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-000 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-000 low-level-rx-endpoint Low-Level-Rx-Endpoint-000", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-001 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-001 low-level-rx-endpoint Low-Level-Rx-Endpoint-001", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-002 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-002 low-level-rx-endpoint Low-Level-Rx-Endpoint-002", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-003 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-003 low-level-rx-endpoint Low-Level-Rx-Endpoint-003", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-008 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-004 low-level-rx-endpoint Low-Level-Rx-Endpoint-008", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-009 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-005 low-level-rx-endpoint Low-Level-Rx-Endpoint-009", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-010 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-006 low-level-rx-endpoint Low-Level-Rx-Endpoint-010", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-011 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-007 low-level-rx-endpoint Low-Level-Rx-Endpoint-011", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-016 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-008 low-level-rx-endpoint Low-Level-Rx-Endpoint-016", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-017 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-009 low-level-rx-endpoint Low-Level-Rx-Endpoint-017", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-018 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-010 low-level-rx-endpoint Low-Level-Rx-Endpoint-018", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-019 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-011 low-level-rx-endpoint Low-Level-Rx-Endpoint-019", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-024 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-012 low-level-rx-endpoint Low-Level-Rx-Endpoint-024", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-025 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-013 low-level-rx-endpoint Low-Level-Rx-Endpoint-025", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-026 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-014 low-level-rx-endpoint Low-Level-Rx-Endpoint-026", "%", logfile, slot);
            reader = SendPortCommand(port, "set user-plane-configuration low-level-rx-links Low-Level-Rx-Link-027 processing-element re_0_936 rx-array-carrier Rx-Array-Carrier-015 low-level-rx-endpoint Low-Level-Rx-Endpoint-027", "%", logfile, slot);
            reader = SendPortCommand(port, "commit", "%", logfile, slot);
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "exit", ">", logfile, slot);
            reader = SendPortCommand(port, "ushell", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "INTF_Set_Pattern_Data", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "INTF_Set_Pattern_Active", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "sts", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88012d00 4 0x44444444 -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88012e00 4 0x1 -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x98017200 4 0xff -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88010400 4 0x22222222 -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c00 4 0xE8EB -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c04 4 0xE8EB -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c08 4 0xE8EB -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c0c 4 0xE8EB -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c10 4 0xE8EB -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c14 4 0xE8EB -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c18 4 0xE8EB -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "mem_test 0x88015c1c 4 0xE8EB -w", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "debug_level 0x00980006 0", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "debug_level 0x0098000B 0", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "almchg 42 5 2 100 100", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "INTF_PASwitchONall", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "sts", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "pacalsts", "UShell >", logfile, slot);
            reader = SendPortCommand(port, "cal sts -P -a", "UShell >", logfile, slot);
        EndSetup:;
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
