using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Vml;
using GUI_Template;
using Org.BouncyCastle.Asn1.Ocsp;
using Renci.SshNet;
using System;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using IOPath = System.IO.Path;  // Alias for System.IO.Path

namespace ORAN_Aging

{
    public partial class Form1 : Form
    {

        ModelSelector modelSelectorForm = new ModelSelector();
        private DataTable excelTable;
        public static Dictionary<int, LogHandler> logger = new Dictionary<int, LogHandler>();
        string excelFolderPath = @"T:\OLP Tracker";
        public static SerialPort[] relayBoard;
        public static int slots = 9;
        int reading_count = 0;
        byte Tx = 170; // bytes to send - This seems to always be 170
        byte two = 3; // seems to always be 3
        byte three = 254; // seems to always be 254
        byte command = 131; // invert relay status
        byte bank = 0; // all banks
        byte ckSum = 46; // checksum of rest of message to board
        private static readonly Regex boardRegex = new Regex(@"BoardTemp\|\s+([\d\.]+)\|\s+([\d\.]+)\|\s+([\d\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex fpgaRegex = new Regex(@"FpgaTemp\|\s+([\d\.]+)\|\s+([\d\.]+)\|\s+([\d\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        enum TestStatus
        {
            IsPassing,
            IsTesting,
            Failed,
            Stopped,
            ToBeTested,
            LostConnection,
            Blank
        }
        Dictionary<TestStatus, Bitmap> currentTestStatus = new Dictionary<TestStatus, Bitmap>()
            {
                { TestStatus.IsPassing, (Bitmap) Image.FromFile(@"Resources\pass_big.png") },
                { TestStatus.IsTesting, (Bitmap)Image.FromFile(@"Resources\time-clock-icon.png") },
                {TestStatus.Failed, (Bitmap)Image.FromFile(@"Resources\fail.png") },
                {TestStatus.Stopped,  (Bitmap)Image.FromFile(@"Resources\stop1.png")},
                {TestStatus.ToBeTested, (Bitmap)Image.FromFile(@"Resources\waiting.jpg") },
               {TestStatus.LostConnection, (Bitmap)Image.FromFile(@"Resources\LostConnection.png") },
                {TestStatus.Blank, (Bitmap)Image.FromFile(@"Resources\white.png") }

            };

        LogHandler[] logHandler;

        // System.Timers.Timer[] timer;
        AgingTestSlot[] timer;
        public Form1()
        {
            InitializeComponent();
            agingGridView.Show();
            AgingGridSetUp(agingGridView, slots);
            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);


        }
        //This AgingDataRow will make it easier to code below for the agingGridView_cellClick method
        public enum AgingDataRow : int
        {
            SerialNumber = 1,
            Model = 2,
            Com_Port = 3,
            Boot_Up = 4,
            Verify_SN = 5,
            RF_Parameters = 6,
            Alarms = 7,
            Result = 8,
            Timer = 9,
            Start_Button_Row = 10,
            Clear_Button_Row = 11,
            Finish_Time = 12
        }
        //This is where you list out the tests. You can add or subtract from it as needed

        List<string> testInfo = new List<string>
        {
            "Slot", //Never edit or remove
            "S/N", //Never edit or remove
            "Model",
            "Com Port",
            "Boot Up",
            "Verify S/N & Firmware",
            "RF Parameters",
            "Alarms"
        };
        RichTextBox[] testLog;
        bool[] testStop;
        string[] timeCheck;
        DataGridViewButtonCell[] startButton;
        DataGridViewButtonCell[] stopButton;
        DataGridViewButtonCell[] clearButton;
        ModelSelector[] modelSelector;
        System.Windows.Forms.ToolTip toolTip = new System.Windows.Forms.ToolTip();
        bool exitisclicked = false;
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Check if the user is trying to close the form
            if (e.CloseReason == CloseReason.UserClosing && exitisclicked == false)
            {
                // Cancel the close event, effectively disabling the close button (X)
                e.Cancel = true;

            }
            else if (exitisclicked == true)
            {
                e.Cancel = false;

            }
        }

        #region FTP Connection and File Download

        private async Task<DataTable> GetDataAsync(int slot, string serialNumber, RichTextBox logBox)
        {
            string ftpServer = "sftp.example.com";
            string ftpUser = "REDACTED_USER";
            string ftpPassword = "REDACTED_PASSWORD";
            string remoteDirPath = "/home/REDACTED_USER/receiving";
            string localDirPath = @"C:\OLP File";

            if (!Directory.Exists(localDirPath))
            {
                Directory.CreateDirectory(localDirPath);
            }

            if (string.IsNullOrEmpty(serialNumber))
            {
                MessageBox.Show("No serial number entered.");
                return null;
            }

            logBox.Invoke(() => {
                logBox.SelectionColor = Color.DarkBlue;
                logBox.AppendText($"[{DateTime.Now}] Checking local Excel for serial number {serialNumber}...\n");
                logBox.SelectionColor = Color.Black;
            });

            string latestLocalFile = await GetLatestExcelFileAsync(localDirPath);
            DataTable localTable = null;

            if (!string.IsNullOrEmpty(latestLocalFile))
            {
                localTable = LoadExcelToDataTable(latestLocalFile);
                if (localTable != null)
                {
                    var match = localTable.AsEnumerable()
                        .FirstOrDefault(row => row["SERIALNBR"].ToString().Trim().Equals(serialNumber, StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                    {
                        logBox.Invoke(() => {
                            logBox.SelectionColor = Color.Green;
                            logBox.AppendText($"[{DateTime.Now}] Serial number found in local file.\n");
                            logBox.SelectionColor = Color.Black;
                        });
                        return localTable; //  Serial found locally
                    }
                }
            }

            // If not found, download latest file from FTP
            logBox.Invoke(() => {
                logBox.SelectionColor = Color.Orange;
                logBox.AppendText($"[{DateTime.Now}] Serial not found. Downloading latest file from FTP...\n");
                logBox.SelectionColor = Color.Black;
            });

            try
            {
                Directory.CreateDirectory(localDirPath);
                await DownloadLatestFileFromFTPAsync(ftpServer, ftpUser, ftpPassword, remoteDirPath, localDirPath, logBox);
            }
            catch (Exception ex)
            {
                logBox.Invoke(() => {
                    logBox.SelectionColor = Color.Red;
                    logBox.AppendText($"[{DateTime.Now}] FTP error: {ex.Message}\n");
                    logBox.SelectionColor = Color.Black;
                });
                return null;
            }

            string latestFileAfterDownload = await GetLatestExcelFileAsync(localDirPath);
            if (string.IsNullOrEmpty(latestFileAfterDownload))
            {
                logBox.Invoke(() => {
                    logBox.SelectionColor = Color.Red;
                    logBox.AppendText($"[{DateTime.Now}] No Excel files found in {localDirPath}\n");
                    logBox.SelectionColor = Color.Black;
                });
                return null;
            }

            var newTable = LoadExcelToDataTable(latestFileAfterDownload);

            logBox.Invoke(() => {
                logBox.SelectionColor = Color.Green;
                logBox.AppendText($"[{DateTime.Now}] Loaded Excel: {IOPath.GetFileName(latestFileAfterDownload)}\n");
                logBox.SelectionColor = Color.Black;
            });

            return newTable;
        }

        private DataTable LoadExcelToDataTable(string excelFilePath)
        {
            try
            {
                using (var workbook = new XLWorkbook(excelFilePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    DataTable table = new DataTable();
                    bool isFirstRow = true;

                    foreach (var row in worksheet.RowsUsed())
                    {
                        if (isFirstRow)
                        {
                            foreach (var cell in row.Cells())
                                table.Columns.Add(cell.Value.ToString().Trim());
                            isFirstRow = false;
                        }
                        else
                        {
                            table.Rows.Add(row.Cells().Select(c => c.Value.ToString().Trim()).ToArray());
                        }
                    }
                    return table;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Excel load error: " + ex.Message);
                return null;
            }
        }


        public static async Task DownloadLatestFileFromFTPAsync(
    string sftpHost, string sftpUser, string sftpPassword,
    string remoteDirPath, string localDirPath, RichTextBox logBox)
        {
            await Task.Run(() => {
                try
                {
                    // Regex for Acme_Receiving_20250808_224831_.xlsx
                    Regex filePattern = new Regex(
                        @"Acme_Receiving_(\d{8})_(\d{6})_\.xlsx",
                        RegexOptions.IgnoreCase
                    );

                    using (var sftpClient = new SftpClient(sftpHost, 22, sftpUser, sftpPassword))
                    {
                        sftpClient.Connect();

                        // Step 1: List files in remote directory
                        var files = sftpClient.ListDirectory(remoteDirPath)
                            .Where(f => !f.IsDirectory)
                            .Select(f => f.Name)
                            .ToList();

                        if (files.Count == 0)
                        {
                            logBox.Invoke(() => logBox.AppendText($"[{DateTime.Now}] No files found in SFTP directory.\n"));
                            sftpClient.Disconnect();
                            return;
                        }

                        // Step 2: Find latest file based on name timestamp
                        string latestFile = null;
                        DateTime latestDate = DateTime.MinValue;

                        foreach (string file in files)
                        {
                            Match match = filePattern.Match(file);
                            if (match.Success)
                            {
                                string datePart = match.Groups[1].Value; // YYYYMMDD
                                string timePart = match.Groups[2].Value; // HHMMSS

                                if (DateTime.TryParseExact(
                                    datePart + timePart,
                                    "yyyyMMddHHmmss",
                                    null,
                                    System.Globalization.DateTimeStyles.None,
                                    out DateTime fileDate))
                                {
                                    if (fileDate > latestDate)
                                    {
                                        latestDate = fileDate;
                                        latestFile = file;
                                    }
                                }
                            }
                        }

                        if (latestFile == null)
                        {
                            logBox.Invoke(() => logBox.AppendText($"[{DateTime.Now}] No matching files found by pattern.\n"));
                            sftpClient.Disconnect();
                            return;
                        }

                        // Step 3: Download the latest file
                        string remotePath = remoteDirPath.TrimEnd('/') + "/" + latestFile;
                        string localFilePath = IOPath.Combine(localDirPath, latestFile);

                        Directory.CreateDirectory(localDirPath);

                        using (var localFileStream = new FileStream(localFilePath, FileMode.Create))
                        {
                            sftpClient.DownloadFile(remotePath, localFileStream);
                            logBox.Invoke(() => logBox.AppendText($"[{DateTime.Now}] Downloaded latest file: {latestFile}\n"));
                        }

                        sftpClient.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    logBox.Invoke(() => logBox.AppendText($"[{DateTime.Now}] SFTP Download Error: {ex.Message}\n"));
                }
            });
        }


        private static Task<string> GetLatestExcelFileAsync(string folderPath)
        {
            return Task.Run(() => {
                var directory = new DirectoryInfo(folderPath);
                var file = directory.GetFiles("*.xlsx")
                                    .OrderByDescending(f => f.LastWriteTime)
                                    .FirstOrDefault();
                return file?.FullName;
            });
        }

        #endregion

        #region Grid Setup

        private void AgingGridSetUp(DataGridView grid, int NumOfSlots)
        {
            testLog = new RichTextBox[NumOfSlots];
            testStop = new bool[NumOfSlots];
            startButton = new DataGridViewButtonCell[NumOfSlots];
            stopButton = new DataGridViewButtonCell[NumOfSlots];
            clearButton = new DataGridViewButtonCell[NumOfSlots];
            modelSelector = new ModelSelector[NumOfSlots];
            timeCheck = new string[NumOfSlots];
            relayBoard = new SerialPort[NumOfSlots];
            logHandler = new LogHandler[NumOfSlots];
            timer = new AgingTestSlot[NumOfSlots];
            //Set each point of the testStop array to false
            for (int i = 0; i < NumOfSlots; i++)
            {
                testStop[i] = false;
            }
            for (int i = 1; i <= NumOfSlots; i++)
            {
                timer[i - 1] = new AgingTestSlot(i, agingGridView);
            }
            for (int i = 0; i < NumOfSlots; i++)
            {
                relayBoard[i] = new SerialPort();
                relayBoard[i].PortName = "COM" + (i + 11).ToString(); //Weird, I know, but this is formula is for the fans which would connect to the fan ports. 
                relayBoard[i].BaudRate = 115200;
                relayBoard[i].Parity = Parity.None;
                relayBoard[i].StopBits = StopBits.One;
            }
            //Take each item in the testInfo list and set the rows to ReadOnly to prevent editting
            //for (int i = 0; i < testInfo.Count; i++) {
            //    grid.Rows.Add(testInfo[i]);
            //    grid.Rows[i].ReadOnly = true;
            //}
            for (int i = 0; i < testInfo.Count; i++)
            {
                grid.Rows.Add();
                grid.Rows[i].Cells[0].Value = testInfo[i];
                grid.Rows[i].ReadOnly = true;
            }

            ///Add two more rows, one for the test results to be image cells and one for timer which will be for text boxes
            ///Then Add all the columns based on the number of slots given in the method
            grid.Rows.Add("Result");
            grid.Rows.Add("Timer");
            for (int i = 1; i <= NumOfSlots; i++)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn());
                grid.Rows[0].Cells[i].Value = i;

                grid.Rows[(int)AgingDataRow.Timer].Cells[i].Value = "00:00:00"; // it was 7
                grid.Rows[(int)AgingDataRow.Timer].Cells[i].Style.Font = new Font("Digital-7", 20F, FontStyle.Regular);
                grid.Rows[(int)AgingDataRow.Timer].Cells[i].Style.ForeColor = Color.DarkGreen;

                for (int j = (int)AgingDataRow.Boot_Up; j < (int)AgingDataRow.Timer; j++)
                {
                    grid.Rows[j].Cells[i] = new DataGridViewImageCell();
                    grid.Rows[j].Cells[i].Value = Image.FromFile(@"Resources\white.png");
                }
            }
            //First add a row for the buttons then create the clear buttons for the slots
            grid.Rows.Add("Start");
            for (int i = 1; i <= NumOfSlots; i++)
            {
                DataGridViewButtonCell btnCell = new DataGridViewButtonCell();
                grid.Columns[i].Width = (grid.Width / grid.ColumnCount) - 10;
                btnCell.Value = "Start Aging";
                btnCell.Style.BackColor = Color.Green;
                startButton[i - 1] = btnCell;
                grid.Rows[(int)AgingDataRow.Start_Button_Row].Cells[i] = btnCell;
            }
            //Create Stop Aging buttons for each ecp, but don't display them yet
            for (int i = 1; i <= NumOfSlots; i++)
            {
                DataGridViewButtonCell stopBtn = new DataGridViewButtonCell();
                stopBtn.Value = "Stop Aging";
                stopButton[i - 1] = stopBtn;
            }
            //First add a row for the buttons then create the clear buttons for the slots
            grid.Rows.Add("Clear");
            for (int i = 1; i <= NumOfSlots; i++)
            {
                DataGridViewButtonCell clearCell = new DataGridViewButtonCell();
                clearCell.Value = "Clear";
                clearButton[i - 1] = clearCell;
                grid.Rows[(int)AgingDataRow.Clear_Button_Row].Cells[i] = clearCell;
            }
            //Create an array of RichTextBoxes based on the number of slots
            for (int i = 1; i <= NumOfSlots; i++)
            {
                RichTextBox rtb = new RichTextBox();
                rtb.Name = "testLog" + i.ToString();
                rtb.Size = agingTestLog.Size;
                rtb.Location = agingTestLog.Location;
                rtb.BackColor = agingTestLog.BackColor;
                rtb.ReadOnly = true;
                rtb.Font = agingTestLog.Font;
                void rtb_TextChanged(object sender, EventArgs e)
                {
                    rtb.SelectionStart = rtb.Text.Length;
                    rtb.ScrollToCaret();
                }
                rtb.TextChanged += rtb_TextChanged;
                testLog[i - 1] = rtb;
                this.Controls.Add(testLog[i - 1]);
            }
            //Create a model selector class for each ecp. This will be handy now that I've learned how to use it.
            for (int i = 0; i < NumOfSlots; i++)
            {
                ModelSelector ms = new ModelSelector();
                ms.Name = "ms" + i + 1;
                modelSelector[i] = ms;
            }
            grid.Rows.Add("Finish Time");
        }

        #endregion

        #region Log Builder
        public List<string> LogBuilder(string serialNumber, string model)
        {
            var list = new List<string>();
            DateTime now = DateTime.Now;
            string date = now.ToString("MM_dd_yyyy__HH_mm_ssfff");
            string fileName = $"{serialNumber}_Aging_{date}.txt";

            // Local path
            string localBase = model switch
            {
                "ORAN PCS" => AppConstants.LogPathPCS,
                "ORAN LOLO" => AppConstants.LogPathLOLO,
                "FAT LOLO" => AppConstants.LogPathFATLOLO,
                _ => AppConstants.LogPathUnknown
            };

            Directory.CreateDirectory(localBase);
            string localPath = IOPath.Combine(localBase, fileName);
            list.Add(localPath);

            // Network path (T drive)
            var networkPaths = new Dictionary<string, string>
            {
                {"ORAN PCS", AppConstants.TDriveBasePCS },
                {"ORAN LOLO", AppConstants.TDriveBaseLOLO },
                {"FAT LOLO", AppConstants.TDriveBaseFATLOLO }
            };

            string networkBase = networkPaths.ContainsKey(model) ? networkPaths[model] : null;
            if (!string.IsNullOrEmpty(networkBase))
            {
                string networkPath = IOPath.Combine(networkBase, fileName);
                list.Add(networkPath);
            }
            else
            {
                list.Add(null); // Fallback if model isn't found
            }

            return list; // list[0] = local, list[1] = T: drive network path
        }
        #endregion

        #region Loagin Cell
        private void showLoadingInCell(int row, int col)
        {
            DataGridViewCell cell = agingGridView.Rows[row].Cells[col];

            // Get rectangle on UI thread
            System.Drawing.Rectangle r = agingGridView.GetCellDisplayRectangle(col, row, false);

            if (cell.Tag == null)
            {
                PictureBox pb2 = new PictureBox
                {
                    Image = currentTestStatus[TestStatus.IsTesting]
                    /* Image = pbSpin.Image,
                     BackColor = pbSpin.BackColor,
                     SizeMode = pbSpin.SizeMode,
                     Height = pbSpin.Height,
                     Width = pbSpin.Width,
                     Left = r.X + (r.Width - pbSpin.Width) / 2,
                     Top = r.Y + (r.Height - pbSpin.Height) / 2*/
                };
                agingGridView.Rows[row].Cells[col].Value = pb2.Image;
                cell.Tag = pb2;
                if (cell.Tag == null) { MessageBox.Show("Nope, still null"); }
            }
        }


        private void stopLoadingInCell(int row, int col)
        {
            // Retrieve the cell and check if the spinner exists
            DataGridViewCell cell = agingGridView.Rows[row].Cells[col];

            if (cell.Tag != null)
            {
                // Remove the spinner control if it exists
                this.Invoke(new MethodInvoker(delegate {
                    agingGridView.Controls.Remove((PictureBox)cell.Tag);
                }));

                // Clear the tag after removing the spinner
                cell.Tag = null;
                cell.Dispose();
            }
        }

        private void repositionSpinners()
        {
            foreach (DataGridViewRow row in agingGridView.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Tag != null)
                    {
                        PictureBox pb2 = (PictureBox)cell.Tag;
                        System.Drawing.Rectangle r = agingGridView.GetCellDisplayRectangle(cell.RowIndex, cell.ColumnIndex, false);
                        pb2.Location = new System.Drawing.Point(r.X + (r.Width - pb2.Width) / 2, r.Y + (r.Height - pb2.Height) / 2);
                    }
                }
            }
        }

        #endregion

        // Currently unused in the active test flow. This fan-relay control logic was built ahead of
        // a planned feature: aging units in environmental chambers held within a target temperature
        // range. The chambers aren't in place yet, so this region isn't called anywhere right now,
        // but it's kept here ready to wire in once that hardware is available.
        // Note: the 80/70 thresholds below are degrees Celsius, not Fahrenheit.
        #region Fan Control
        public void FanControls(SerialPort comport, double temp, string logfile, RichTextBox testLog)
        {
            if (temp >= 80)
            {
                File.AppendAllText(logfile, "Turning on fans");
                this.Invoke(new MethodInvoker(delegate {
                    testLog.AppendText("Turning on fans");
                }));
                Tx = 170; // bytes to send - This seems to always be 170
                two = 3; // seems to always be 3
                three = 254; // seems to always be 254
                command = 130; //  relay on
                bank = 0; // all banks
                ckSum = 45; // checksum of rest of message to board
                Byte[] rawBytesToRelayBoard = new Byte[] { Tx, two, three, command, bank, ckSum };
                Byte[] rawBytesFromRelayBoard = new byte[] { Tx, two, three, ckSum };
                Byte[] expectedBytes = new byte[] { 170, 1, 85, 0 };
                if (!comport.IsOpen)
                {
                    try
                    {
                        comport.Open();
                    }
                    catch (Exception)
                    {
                        //File.AppendAllText(logfile, ex.Message);
                    }
                }
                try { comport.Write(rawBytesToRelayBoard, 0, rawBytesToRelayBoard.Length); } catch (InvalidOperationException) { }
                Thread.Sleep(300);
                try { comport.Read(rawBytesFromRelayBoard, 0, rawBytesFromRelayBoard.Length); } catch (InvalidOperationException) { }
                if (rawBytesFromRelayBoard.SequenceEqual(expectedBytes))
                {
                    // File.AppendAllText(logfile, "** Sucessfully sent On command to relay board" + Environment.NewLine);
                }
                rawBytesToRelayBoard[0] = 170;
                rawBytesToRelayBoard[1] = 3;
                rawBytesToRelayBoard[2] = 254;
                rawBytesToRelayBoard[3] = 124;
                rawBytesToRelayBoard[4] = 1;
                rawBytesToRelayBoard[5] = 40;
                expectedBytes[0] = 170;
                expectedBytes[1] = 1;
                expectedBytes[2] = 0;
                expectedBytes[3] = 171;
                try { comport.Write(rawBytesToRelayBoard, 0, rawBytesToRelayBoard.Length); } catch (InvalidOperationException) { }
                Thread.Sleep(300);
                try { comport.Read(rawBytesFromRelayBoard, 0, rawBytesFromRelayBoard.Length); } catch (InvalidOperationException) { }
                if (rawBytesFromRelayBoard.SequenceEqual(expectedBytes))
                {
                    // File.AppendAllText(logfile, "** Relay Board reports all relays are on" + Environment.NewLine);
                }
                Thread.Sleep(1000);
            }
            else if (temp < 70)
            {
                //File.AppendAllText(logfile, "** Turning Fans Off" + Environment.NewLine);
                Tx = 170; // bytes to send - This seems to always be 170
                two = 3; // seems to always be 3
                three = 254; // seems to always be 254
                command = 129; //  relay off
                bank = 0; // all banks
                ckSum = 44; // checksum of rest of message to board

                Byte[] rawBytesToRelayBoard = new Byte[] { Tx, two, three, command, bank, ckSum };
                Byte[] rawBytesFromRelayBoard = new byte[] { Tx, two, three, ckSum };
                Byte[] expectedBytes = new byte[] { 170, 1, 85, 0 };
                try { comport.Write(rawBytesToRelayBoard, 0, rawBytesToRelayBoard.Length); } catch (InvalidOperationException) { }
                Thread.Sleep(300);
                try { comport.Read(rawBytesFromRelayBoard, 0, rawBytesFromRelayBoard.Length); } catch (InvalidOperationException) { }
                if (rawBytesFromRelayBoard.SequenceEqual(expectedBytes)) // C# can't do array comps.
                {
                    //File.AppendAllText(logfile, "** Sucessfully sent Off command to relay board" + Environment.NewLine);
                }
                rawBytesToRelayBoard[0] = 170;
                rawBytesToRelayBoard[1] = 3;
                rawBytesToRelayBoard[2] = 254;
                rawBytesToRelayBoard[3] = 124;
                rawBytesToRelayBoard[4] = 1;
                rawBytesToRelayBoard[5] = 40;
                expectedBytes[0] = 170;
                expectedBytes[1] = 1;
                expectedBytes[2] = 0;
                expectedBytes[3] = 171;
                if (!comport.IsOpen)
                {
                    try
                    {
                        comport.Open();
                    }
                    catch (Exception)
                    {
                        // File.AppendAllText(logfile, ex.Message);
                    }
                }
                try { comport.Write(rawBytesToRelayBoard, 0, rawBytesToRelayBoard.Length); } catch (InvalidOperationException) { }
                Thread.Sleep(300);
                try { comport.Read(rawBytesFromRelayBoard, 0, rawBytesFromRelayBoard.Length); } catch (InvalidOperationException) { }
                if (rawBytesFromRelayBoard.SequenceEqual(expectedBytes)) // C# can't do array comps. 
                {
                    // File.AppendAllText(logfile, "** Relay Board reports all relays are on" + Environment.NewLine);
                }
                comport.Close();
                Thread.Sleep(1000);
            }
        }
        public void TurnOffFan(SerialPort comport, RichTextBox testLog)
        {
            //File.AppendAllText(logfile, "** Turning Fans Off" + Environment.NewLine);
            this.Invoke(new MethodInvoker(delegate {
                testLog.AppendText("Fan turned Off");
            }));
            Tx = 170; // bytes to send - This seems to always be 170
            two = 3; // seems to always be 3
            three = 254; // seems to always be 254
            command = 129; //  relay off
            bank = 0; // all banks
            ckSum = 44; // checksum of rest of message to board

            Byte[] rawBytesToRelayBoard = new Byte[] { Tx, two, three, command, bank, ckSum };
            Byte[] rawBytesFromRelayBoard = new byte[] { Tx, two, three, ckSum };
            Byte[] expectedBytes = new byte[] { 170, 1, 85, 0 };
            try { comport.Write(rawBytesToRelayBoard, 0, rawBytesToRelayBoard.Length); } catch (InvalidOperationException) { }
            Thread.Sleep(300);
            try { comport.Read(rawBytesFromRelayBoard, 0, rawBytesFromRelayBoard.Length); } catch (InvalidOperationException) { }
            if (rawBytesFromRelayBoard.SequenceEqual(expectedBytes)) // C# can't do array comps.
            {
                //File.AppendAllText(logfile, "** Sucessfully sent Off command to relay board" + Environment.NewLine);
            }
            rawBytesToRelayBoard[0] = 170;
            rawBytesToRelayBoard[1] = 3;
            rawBytesToRelayBoard[2] = 254;
            rawBytesToRelayBoard[3] = 124;
            rawBytesToRelayBoard[4] = 1;
            rawBytesToRelayBoard[5] = 40;
            expectedBytes[0] = 170;
            expectedBytes[1] = 1;
            expectedBytes[2] = 0;
            expectedBytes[3] = 171;
            if (!comport.IsOpen)
            {
                try
                {
                    comport.Open();
                }
                catch (Exception)
                {
                    // File.AppendAllText(logfile, ex.Message);
                }
            }
            try { comport.Write(rawBytesToRelayBoard, 0, rawBytesToRelayBoard.Length); } catch (InvalidOperationException) { }
            Thread.Sleep(300);
            try { comport.Read(rawBytesFromRelayBoard, 0, rawBytesFromRelayBoard.Length); } catch (InvalidOperationException) { }
            if (rawBytesFromRelayBoard.SequenceEqual(expectedBytes)) // C# can't do array comps. 
            {
                // File.AppendAllText(logfile, "** Relay Board reports all relays are on" + Environment.NewLine);
            }
            comport.Close();
            Thread.Sleep(1000);
        }
        public void TurnOnFan(SerialPort comport, RichTextBox testLog)
        {
            //File.AppendAllText(logfile, "** Turning Fans On" + Environment.NewLine);
            this.Invoke(new MethodInvoker(delegate {
                testLog.AppendText("Fan turned ON");
            }));
            //CMTx3Logger.RichTextBoxLogger.testLog[_modemNumber].InsertLog("Fan Turned ON", _testLogType);
            Tx = 170; // bytes to send - This seems to always be 170
            two = 3; // seems to always be 3
            three = 254; // seems to always be 254
            command = 130; //  relay on
            bank = 0; // all banks
            ckSum = 45; // checksum of rest of message to board
            Byte[] rawBytesToRelayBoard = new Byte[] { Tx, two, three, command, bank, ckSum };
            Byte[] rawBytesFromRelayBoard = new byte[] { Tx, two, three, ckSum };
            Byte[] expectedBytes = new byte[] { 170, 1, 85, 0 };
            if (!comport.IsOpen)
            {
                try
                {
                    comport.Open();
                }
                catch (Exception)
                {
                    //File.AppendAllText(logfile, ex.Message);
                }
            }
            try { comport.Write(rawBytesToRelayBoard, 0, rawBytesToRelayBoard.Length); } catch (InvalidOperationException) { }
            Thread.Sleep(300);
            try { comport.Read(rawBytesFromRelayBoard, 0, rawBytesFromRelayBoard.Length); } catch (InvalidOperationException) { }
            if (rawBytesFromRelayBoard.SequenceEqual(expectedBytes))
            {
                // File.AppendAllText(logfile, "** Sucessfully sent On command to relay board" + Environment.NewLine);
            }
            rawBytesToRelayBoard[0] = 170;
            rawBytesToRelayBoard[1] = 3;
            rawBytesToRelayBoard[2] = 254;
            rawBytesToRelayBoard[3] = 124;
            rawBytesToRelayBoard[4] = 1;
            rawBytesToRelayBoard[5] = 40;
            expectedBytes[0] = 170;
            expectedBytes[1] = 1;
            expectedBytes[2] = 0;
            expectedBytes[3] = 171;
            try { comport.Write(rawBytesToRelayBoard, 0, rawBytesToRelayBoard.Length); } catch (InvalidOperationException) { }
            Thread.Sleep(300);
            try { comport.Read(rawBytesFromRelayBoard, 0, rawBytesFromRelayBoard.Length); } catch (InvalidOperationException) { }
            if (rawBytesFromRelayBoard.SequenceEqual(expectedBytes))
            {

            }
            comport.Close();
            Thread.Sleep(1000);
        }
        #endregion

        #region Interrupt
        private void SendInterruptCommand(SerialPort port)
        {
            byte[] interrupt = new byte[] { 0x03 };
            port.Write(interrupt, 0, 1);
        }
        #endregion

        #region ReadPort
        private string ReadPort(SerialPort port)
        {
            StringBuilder reader = new StringBuilder(); ;
            try
            {
                if (!port.IsOpen)
                {
                    port.Open();
                }
                reader.Append(port.ReadExisting());
            }
            catch (Exception ex)
            {
                Task.Run(() => { MessageBox.Show("Communication issues with com port " + port.PortName + "\n" + ex.ToString()); });
                reader.Append("!fail!");
            }
            return reader.ToString();
        }
        #endregion

        #region SendPortCommand
        // Strings that indicate the session was lost and a re-login is needed
        private static readonly string[] SessionLostIndicators = {
            "Waiting for a stable CPRI Link",
            "WARNING: Unauthorized access to this system is forbidden",
            "Login incorrect",
            "login:"
        };

        private string SendPortCommand(SerialPort port, string command, string endPoint, string model, int maxRetries = 2)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                string result = SendPortCommandInternal(port, command, endPoint);

                // Check if the response indicates a lost session
                if (attempt < maxRetries && IsSessionLost(result))
                {
                    Console.WriteLine($"Session lost detected (attempt {attempt + 1}): re-logging in...");
                    bool reloggedIn = AttemptReLogin(port, model);
                    if (!reloggedIn)
                    {
                        Console.WriteLine("Re-login failed. Returning fail.");
                        return "!fail!";
                    }
                    // After successful re-login, retry the original command
                    continue;
                }

                return result;
            }

            return "!fail!";
        }

        private string SendPortCommandInternal(SerialPort port, string command, string endPoint)
        {
            StringBuilder reader = new StringBuilder();
            TimeSpan timeout = TimeSpan.FromSeconds(15);
            DateTime startTime = DateTime.UtcNow;

            try
            {
                if (!port.IsOpen)
                    port.Open();

                port.ReadExisting();

                // Send the command once
                port.WriteLine(command);

                // Loop to read until timeout or end condition is met
                while (DateTime.UtcNow - startTime < timeout)
                {
                    string data = port.ReadExisting();

                    if (!string.IsNullOrEmpty(data))
                        reader.Append(data);

                    // Check if the response contains the endpoint or error signals
                    if (reader.ToString().Contains(endPoint) || reader.ToString().Contains("Broken pipe") || reader.ToString().Contains("reset"))
                        break;

                    // Avoid tight loop, prevent CPU overuse
                    Thread.Sleep(50);
                }

                // Return result after loop, or "fail" if no response received
                return reader.Length > 0 ? RemoveAnsiCodes(reader.ToString()) : "!fail!";
            }
            catch (Exception ex)
            {
                // Log exception to a file or logging service
                File.AppendAllTextAsync(@"C:\Test_TechCo\ErrorLog.txt", $"{port.PortName} threw an exception\n\n{ex}\n\n");
                return "!fail!";
            }
        }

        private bool IsSessionLost(string response)
        {
            if (string.IsNullOrEmpty(response) || response == "!fail!") return false;
            foreach (var indicator in SessionLostIndicators)
            {
                if (response.Contains(indicator, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to re-login to the unit by sending the standard login sequence.
        /// Returns true if login was successful (got "root@" prompt).
        /// </summary>
        private bool AttemptReLogin(SerialPort port, string model)
        {
            try
            {
                // Clear any pending data
                if (!port.IsOpen) port.Open();
                port.ReadExisting();
                Thread.Sleep(500);

                // Send Enter to get a fresh prompt

                // Step 1: Send username
                string result = SendPortCommandInternal(port, "user", "Password:");
                if (string.IsNullOrEmpty(result) || result == "!fail!") return false;

                // Step 2: Send user password
                result = SendPortCommandInternal(port, "REDACTED_PASSWORD", "user@");
                if (string.IsNullOrEmpty(result) || result == "!fail!")
                {
                    // Some units may already be at a login prompt, try alternate flow
                    return false;
                }

                // Step 3: Switch to superuser
                result = SendPortCommandInternal(port, "su -", "Password:");
                if (string.IsNullOrEmpty(result) || result == "!fail!") return false;

                // Step 4: Enter root password
                result = SendPortCommandInternal(port, "REDACTED_PASSWORD", "root@");
                if (string.IsNullOrEmpty(result) || result == "!fail!") return false;

                result = SendPortCommandInternal(port, "ushell", "UShell");
                if (string.IsNullOrEmpty(result) || result == "!fail!") return false;

                switch (model)
                {
                    case "ORAN LOLO":
                        VZ_LOLO vZ_LOLO = new VZ_LOLO();
                        foreach (string line in vZ_LOLO.setupCommands)
                        {
                            result = SendPortCommandInternal(port, line, "UShell");
                        }
                        vZ_LOLO = null;
                        break;
                    case "ORAN PCS":
                        VZ_PCS vZ_PCS = new VZ_PCS();
                        foreach (string line in vZ_PCS.setupCommands)
                        {
                            result = SendPortCommandInternal(port, line, "UShell");
                        }
                        vZ_PCS = null;
                        break;
                    case "FAT LOLO":
                        FAT_LOLO fat_LOLO = new FAT_LOLO();
                        foreach (string line in fat_LOLO.setupCommands)
                        {
                            result = SendPortCommandInternal(port, line, "UShell");
                        }
                        fat_LOLO = null;
                        break;
                }

                Console.WriteLine("Re-login successful.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Re-login exception: " + ex.Message);
                return false;
            }
        }

        #endregion

        #region Remove AnsiCodes
        private string RemoveAnsiCodes(string text)
        {
            return Regex.Replace(text, @"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", "");
        }
        private static (Match, Match) CleanBoardTemp(string text)
        {
            return (boardRegex.Match(text), fpgaRegex.Match(text));
        }
        #endregion

        #region Cell Click
        private void agingGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex <= 0) return;

            this.SuspendLayout();  // Suspend layout updates to prevent flicker during multiple UI changes

            if (e.Button == MouseButtons.Left)
            {
                // Update label to show current column index
                label2.Invoke((MethodInvoker)(() => label2.Text = e.ColumnIndex.ToString()));

                // Bring the corresponding RichTextBox to the front
                var richTextBox = testLog[e.ColumnIndex - 1];
                richTextBox.BringToFront();

                // Deselect text in the RichTextBox to avoid visual overlap of selected text
                richTextBox.SelectionLength = 0;
                richTextBox.SelectionStart = richTextBox.Text.Length;

                // Ensure it scrolls to the end of the content
                richTextBox.ScrollToCaret();

                // Refresh the RichTextBox to update the UI
                richTextBox.Refresh();
            }

            this.ResumeLayout(true);  // Resume layout updates to reflect changes

            //if (e.ColumnIndex <= 0) return;

            //this.SuspendLayout();

            //label2.Invoke((MethodInvoker)(() => label2.Text = e.ColumnIndex.ToString()));

            //if (e.Button == MouseButtons.Right) {
            //    testLog[e.ColumnIndex - 1].BringToFront();
            //    testLog[e.ColumnIndex - 1].DeselectAll();
            //    testLog[e.ColumnIndex - 1].Refresh();
            //}

            //this.ResumeLayout(true);


        }

        private async void agingGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int cellClicked = e.ColumnIndex;
            int rowClicked = e.RowIndex;

            // 1. Ignore clicks on headers or invalid cells
            if (cellClicked < 0 || rowClicked < 0)
                return;

            try
            {
                // Only process if not the first column
                if (cellClicked > 0)
                {
                    switch (rowClicked)
                    {
                        case (int)AgingDataRow.SerialNumber:
                            await HandleSerialNumberClick(cellClicked);
                            break;

                        case (int)AgingDataRow.Start_Button_Row:
                            await HandleStartButtonClick(cellClicked);
                            break;

                        case (int)AgingDataRow.Clear_Button_Row:
                            HandleClearButtonClick(cellClicked);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                //  Exception logging instead of silent swallow
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Optionally log: File.AppendAllText("error.log", ex.ToString());
            }
        }

        private async Task HandleSerialNumberClick(int cellClicked)
        {
            string serialNumber = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter Serial Number Here",
                "Serial Number Slot: " + cellClicked
            ).ToUpper().Trim();

            if (serialNumber.Length != 10)
            {
                MessageBox.Show("Invalid Serial Number....!", "Slot: " + cellClicked);
                return;
            }

            var excelTable = await GetDataAsync(cellClicked, serialNumber, testLog[cellClicked - 1]);
            if (excelTable == null)
            {
                MessageBox.Show("Failed to load Excel data.");
                return;
            }

            agingGridView.Rows[(int)AgingDataRow.SerialNumber].Cells[cellClicked].Value = serialNumber;

            var match = excelTable.AsEnumerable()
                .FirstOrDefault(row => row["SERIALNBR"].ToString().Trim()
                    .Equals(serialNumber, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                string partNumber = match["PARTNBR"].ToString().Trim();
                string model = MapPartNumberToModel(partNumber);
                agingGridView.Rows[(int)AgingDataRow.Model].Cells[cellClicked].Value = model;
                string warranty = match["RECVWARRANTY"].ToString().Trim();
                if (warranty == "V")
                {
                    modelSelector[cellClicked - 1].radioButton3.Enabled = false;
                    modelSelector[cellClicked - 1].radioButton3.Checked = false;
                    modelSelector[cellClicked - 1].radioButton4.Enabled = false;
                    modelSelector[cellClicked - 1].radioButton4.Checked = false;
                }
                else
                {
                    modelSelector[cellClicked - 1].radioButton3.Enabled = true;
                    modelSelector[cellClicked - 1].radioButton4.Enabled = true;
                }
            }
            else
            {
                string partNumberInput = Microsoft.VisualBasic.Interaction.InputBox(
                    "Please Scan Model Number:\n" +
                    "SFG-ARR57201VZ\nSFG-ARR27201VZ\nSFG-ARR26301VZ\nSFG-ARR3J601DI\nSFG-ARR3KM01DI",
                    "Enter Part Number", ""
                ).ToUpper().Trim();

                string model = MapPartNumberToModel(partNumberInput);

                if (model == "UNKNOWN")
                {
                    MessageBox.Show("Invalid part number entered.");
                    agingGridView.Rows[(int)AgingDataRow.Model].Cells[cellClicked].Value = "Not Found";
                }
                else
                {
                    agingGridView.Rows[(int)AgingDataRow.Model].Cells[cellClicked].Value = model;
                }
            }
        }

        private async Task HandleStartButtonClick(int cellClicked)
        {
            string modelValue = agingGridView.Rows[(int)AgingDataRow.Model].Cells[cellClicked].Value?.ToString();
            if (modelValue == "Error")
            {
                MessageBox.Show("Need a valid Model");
                return;
            }

            if (agingGridView.Rows[(int)AgingDataRow.SerialNumber].Cells[cellClicked].Value == null)
            {
                ClearTestCells(cellClicked);
                ShowMessageSafe("Please scan serial number");
                return;
            }

            if (agingGridView.Rows[(int)AgingDataRow.Start_Button_Row].Cells[cellClicked].Value?.ToString() == "Stop Aging")
            {
                StopTest(cellClicked);
                return;
            }


            // Starting new test
            PrepareTest(cellClicked);

            await Task.Factory.StartNew(() => {
                //  Run test logic in background, but no UI updates here
                RunTest(cellClicked);
            }, TaskCreationOptions.LongRunning);

            // ? Once background test completes, update UI on main thread
            this.Invoke((MethodInvoker)(() => {
                agingGridView.Rows[(int)AgingDataRow.Start_Button_Row].Cells[cellClicked].Value = "Start Aging";
            }));
        }

        private void PrepareTest(int cellClicked)
        {
            // Reset all test-related cells to blank
            for (int i = (int)AgingDataRow.Boot_Up; i < (int)AgingDataRow.Timer; i++)
            {
                stopLoadingInCell(i, cellClicked);
                agingGridView.Rows[i].Cells[cellClicked].Value = currentTestStatus[TestStatus.Blank];
            }

            // Clear logs for this slot
            testLog[cellClicked - 1].Clear();
            agingGridView.Rows[(int)AgingDataRow.Result].Cells[cellClicked].Value = currentTestStatus[TestStatus.Blank];

            // Change Start button to Stop button
            var newStopButtonCell = new DataGridViewButtonCell { Value = "Stop Aging" };
            agingGridView.Rows[(int)AgingDataRow.Start_Button_Row].Cells[cellClicked] = newStopButtonCell;

            // Show model selector and handle cancellation
            modelSelector[cellClicked - 1].show();
            if (modelSelector[cellClicked - 1].comName == "Nope")
            {
                ShowMessageSafe("Test cancelled");
                ClearTestCells(cellClicked);
                agingGridView.Rows[(int)AgingDataRow.Start_Button_Row].Cells[cellClicked].Value = "Start Aging";
                return;
            }

            // Set COM port and boot-up cell
            agingGridView.Rows[(int)AgingDataRow.Com_Port].Cells[cellClicked].Value = modelSelector[cellClicked - 1].comName;
            agingGridView.Rows[(int)AgingDataRow.Boot_Up].Cells[cellClicked].Value = currentTestStatus[TestStatus.Blank];

            // Stop timer just in case
            timer[cellClicked - 1].Stop();
        }

        #region T Drive Mapping
        /// <summary>
        /// Checks if T: drive is accessible. If not, attempts to map it using net use.
        /// </summary>
        private bool EnsureTDriveMapped()
        {
            if (Directory.Exists(@"T:\"))
            {
                return true;
            }

            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "net";
                process.StartInfo.Arguments = $@"use T: {AppConstants.TDriveNetworkPath} {AppConstants.TDrivePassword} /user:{AppConstants.TDriveUser} /persistent:no";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit(10000);

                if (process.ExitCode == 0 && Directory.Exists(@"T:\"))
                {
                    return true;
                }

                string error = process.StandardError.ReadToEnd();
                Console.WriteLine("Failed to map T: drive: " + error);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception mapping T: drive: " + ex.Message);
                return false;
            }
        }
        #endregion

        private void RunTest(int cellClicked)
        {
            string serialNumber = null;
            string modelNumber = null;

            // Safely get UI values (must use Invoke to read from UI thread)
            this.Invoke((MethodInvoker)(() => {
                serialNumber = agingGridView.Rows[(int)AgingDataRow.SerialNumber].Cells[cellClicked].Value?.ToString();
                modelNumber = agingGridView.Rows[(int)AgingDataRow.Model].Cells[cellClicked].Value?.ToString();
            }));

            if (string.IsNullOrWhiteSpace(serialNumber) || string.IsNullOrWhiteSpace(modelNumber))
            {
                ShowMessageSafe("Invalid test parameters.");
                return;
            }

            // Ensure T: drive is mapped before starting the test
            if (!EnsureTDriveMapped())
            {
                ShowMessageSafe("T: drive is not accessible and could not be mapped.\nPlease check network connection and try again.");
                return;
            }

            // Run specific test based on model
            if (modelNumber == "ORAN PCS" || modelNumber == "ORAN LOLO")
            {
                StartCarrierAAgingTest(cellClicked, serialNumber, modelNumber, modelSelector[cellClicked - 1],
                    modelSelector[cellClicked - 1].hours, testLog[cellClicked - 1]);
                timer[cellClicked - 1].Dispose();
                timer[cellClicked - 1] = new AgingTestSlot(cellClicked, agingGridView);

            }
            else if (modelNumber == "FAT LOLO")
            {
                StartFatLOLOAging(cellClicked, serialNumber, modelNumber, modelSelector[cellClicked - 1],
                    modelSelector[cellClicked - 1].hours, testLog[cellClicked - 1]);
                timer[cellClicked - 1].Dispose();
                timer[cellClicked - 1] = new AgingTestSlot(cellClicked, agingGridView);
            }

            // If still in stop mode after test
            this.Invoke((MethodInvoker)(() => {
                if (agingGridView.Rows[(int)AgingDataRow.Start_Button_Row].Cells[cellClicked] == stopButton[cellClicked - 1])
                {
                    testStop[cellClicked - 1] = true;
                    stopButton[cellClicked - 1].Value = "Stopping";
                    stopLoadingInCell((int)AgingDataRow.Result, cellClicked);
                    agingGridView.Rows[(int)AgingDataRow.Result].Cells[cellClicked].Value = currentTestStatus[TestStatus.Stopped];
                }

                // Reset Start button after finishing
                testStop[cellClicked - 1] = false;
                startButton[cellClicked - 1].Value = "Start Aging";
                if (agingGridView.Rows[(int)AgingDataRow.Start_Button_Row].Cells[cellClicked] != startButton[cellClicked - 1])
                {
                    agingGridView.Rows[(int)AgingDataRow.Start_Button_Row].Cells[cellClicked] = startButton[cellClicked - 1];
                }
            }));
        }

        private void HandleClearButtonClick(int cellClicked)
        {
            if (agingGridView.Rows[(int)AgingDataRow.Result].Cells[cellClicked].Tag == null)
            {
                ClearTestCells(cellClicked);
                agingGridView.Rows[(int)AgingDataRow.Start_Button_Row].Cells[cellClicked].Value = "Start Aging";
            }
            else
            {
                ShowMessageSafe("Test still running. Stop the test or wait for the test to finish before clearing");
            }
        }

        private void ShowMessageSafe(string message)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)(() => MessageBox.Show(message)));
            else
                MessageBox.Show(message);
        }

        private void ClearTestCells(int cellClicked)
        {
            for (int i = (int)AgingDataRow.Boot_Up; i < (int)AgingDataRow.Timer; i++)
            {
                stopLoadingInCell(i, cellClicked);
                agingGridView.Rows[i].Cells[cellClicked].Value = currentTestStatus[TestStatus.Blank];
            }

            testLog[cellClicked - 1].Clear();
            timer[cellClicked - 1].Stop();
            timer[cellClicked - 1].Reset();
            agingGridView.Rows[(int)AgingDataRow.Timer].Cells[cellClicked].Value = "00:00:00";
            agingGridView.Rows[(int)AgingDataRow.SerialNumber].Cells[cellClicked].Value = string.Empty;
            agingGridView.Rows[(int)AgingDataRow.Model].Cells[cellClicked].Value = string.Empty;
        }

        private void StopTest(int cellClicked)
        {
            testStop[cellClicked - 1] = true;
            stopButton[cellClicked - 1].Value = "Stopping";
            stopLoadingInCell((int)AgingDataRow.Result, cellClicked);
            agingGridView.Rows[(int)AgingDataRow.Result].Cells[cellClicked].Value = currentTestStatus[TestStatus.Stopped];
        }

        private string MapPartNumberToModel(string partNumber) => partNumber switch
        {
            "SFG-ARR57201VZ" => "FAT LOLO",
            "SFG-ARR27201VZ" => "ORAN LOLO",
            "SFG-ARR26301VZ" => "ORAN PCS",
            _ => "UNKNOWN"
        };

        private void agingGridView_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex > 0 && e.RowIndex == (int)AgingDataRow.Timer && timeCheck[e.ColumnIndex - 1] != null)
            {
                toolTip.SetToolTip(agingGridView, "Will be done by " + timeCheck[e.ColumnIndex - 1]);
            }
        }

        private void agingGridView_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            toolTip.RemoveAll();
        }
        #endregion

        #region Board Invt Parsing 
        private bool ParseBoardInvt(string input, string serialNumber, RichTextBox testLog)
        {
            bool result = true;
            string[] lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string serialPattern = @"\[\s*Serial Number\s*\]\s*(\S+)";
            try
            {
                foreach (string line in lines)
                {
                    Match match = Regex.Match(line, serialPattern);
                    if (match.Success)
                    {
                        string foundSerial = match.Groups[1].Value;

                        if (foundSerial == serialNumber)
                        {
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.SelectionColor = Color.DarkBlue;
                                testLog.AppendText("\r\nScan Serial Number : " + serialNumber + "\n");
                                testLog.SelectionColor = Color.Black;
                            }));
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.SelectionColor = Color.Green;
                                testLog.AppendText(("\r\n" + "Internal Serial Number Match :  " + foundSerial + "\n"));
                                testLog.SelectionColor = Color.Black;
                            }));
                        }
                        else
                        {
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.AppendText("\r\nScan Serial Number : " + serialNumber + "\n");
                            }));
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.SelectionColor = Color.Red;
                                testLog.AppendText(("\r\n" + "Internal Serial Does Not Number Match :  " + foundSerial + "\n"));
                                testLog.SelectionColor = Color.Black;
                            }));
                            result = false;
                        }
                        break; // No need to continue loop once matched
                    }
                }
            }
            catch { }

            return result;
        }

        #endregion

        #region Write to File 
        private void WritetoFile(string logfile, int slot, string reader)
        {
            int retries = 0;
            bool success = false;

            // Assume ReturnTimeStamp returns a string like "00:00:15.219"
            string timestamp = "[0 " + ReturnTimeStamp(slot) + "]";

            // Split reader text into individual lines
            string[] lines = reader.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Prefix each line with the timestamp
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = $"{timestamp} {lines[i]}";
            }

            // Rebuild the full message
            string message = string.Join(Environment.NewLine, lines) + Environment.NewLine;

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
        #endregion

        private string ReturnTimeStamp(int slot)
        {
            return agingGridView.Rows[(int)AgingDataRow.Timer].Cells[slot].Value.ToString();
        }

        byte[] interrupt = new byte[] { 0x03 };

        #region AGING CARRIER A MAIN FUNCTION
        private async void StartCarrierAAgingTest(int slot, string serialNumber, string modelNumber, ModelSelector modelSelector, int hours, RichTextBox testLog)
        {
            if (modelSelector.comName == "" || modelSelector.comName == null)
            {
                this.Invoke(new MethodInvoker(() => {
                    testLog.AppendText("Com port not selected");
                }));
                for (int i = (int)AgingDataRow.Boot_Up; i < (int)AgingDataRow.Timer; i++)
                {
                    stopLoadingInCell(i, slot);
                    agingGridView.Rows[i].Cells[slot].Value = currentTestStatus[TestStatus.Blank];
                }
                agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Value = "";
                return;
            }
            try
            {
                StartBackgroundTask(slot);
                agingGridView.Rows[(int)AgingDataRow.Timer].Cells[slot].Value = string.Empty;
                agingGridView.Rows[(int)AgingDataRow.Timer].Cells[slot].Value = "00:00:00";
                int bootcount = 5;
                int reboot_counter = 0;
                string postBooter = "";
                //SerialPort port = new SerialPort(modelSelector.comName);
                DateTime agingTime = DateTime.Now;
                List<(string Text, System.Drawing.Color Color)> logEntries = new List<(string, System.Drawing.Color Color)>();
                timer[slot - 1].Reset();
                bool SnVerification = false;
                bool txPowerPassed = true;
                bool AlarmpnotPresent = true;
                bool returnLossPassed = true;
                bool FirmwareIsHigh = false;
                bool rssiPassed = true;
                bool snIsVerified = false;
                bool unitIsFlagged = false;
                if (modelNumber == "ORAN PCS") { postBooter = "postbooter.a.rf_model_a.0"; } else if (modelNumber == "ORAN LOLO") { postBooter = "postbooter.a.rf_model_b.0"; }
                List<string> LogFileList = LogBuilder(serialNumber, modelNumber);
                Dictionary<string, string> failResultscom = new Dictionary<string, string>();
                string location = "Facility 1";
                string logfile = LogFileList[0];
                if (!logger.ContainsKey(slot))
                    logger[slot] = new LogHandler();

                logger[slot].tfailed.Clear();  // start fresh
                File.WriteAllText(logfile,

                "**================================================================================" + Environment.NewLine
                + "** Date:          " + DateTime.Now.ToString("yyyy-MM-dd") + Environment.NewLine
                + "** Serial Number: " + serialNumber + Environment.NewLine
                + "** Model Number:  " + modelNumber + Environment.NewLine
                + "** Slot:          " + slot + Environment.NewLine
                + "** App Ver.       " + AppConstants.AppVersion + Environment.NewLine
                + "** Com Port  " + modelSelector.comName + Environment.NewLine
                + "** Aging Location:          " + location + Environment.NewLine
                + "** Burn Hours:          " + modelSelector.hours + Environment.NewLine

                + "**================================================================================" + Environment.NewLine + "\n"); ;


                this.Invoke(new MethodInvoker(() => {
                    testLog.SelectionColor = Color.DarkBlue;
                    testLog.AppendText(
                 "**============================================" + Environment.NewLine + "\n"
                + "* *Date:          " + DateTime.Now.ToString("yyyy - MM - dd") + Environment.NewLine
                + "** Serial Number: " + serialNumber + Environment.NewLine
                + "** Model Number:  " + modelNumber + Environment.NewLine
                + "** Slot:          " + slot + Environment.NewLine
                + "** App Ver.       " + AppConstants.AppVersion + Environment.NewLine
                + "** Com Port  " + modelSelector.comName + Environment.NewLine
                + "** Aging Location:          " + location + Environment.NewLine
                + "** Burn Hours:          " + modelSelector.hours + Environment.NewLine
    + "**============================================" + Environment.NewLine + "\n");
                    testLog.SelectionColor = Color.Black;
                }));

                agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Style.Font = new Font("Digital-7", 16F, FontStyle.Regular);
                agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Style.ForeColor = Color.DarkGreen;
                agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Value = "TBD";

                this.Invoke(new MethodInvoker(delegate {
                    testLog.SelectionColor = Color.Green;
                    testLog.AppendText("Plug in the power now\n");
                    testLog.SelectionColor = Color.Black;
                }));
                StringBuilder dataBuildercom = new StringBuilder();
                string FirmwareVersion = "";
                bool skipSetup = false;
                using (SerialPort port = new SerialPort(modelSelector.comName))
                {

                    port.BaudRate = 115200;
                    port.Parity = Parity.None;
                    port.StopBits = StopBits.One;
                    port.Open();
                    port.WriteLine("");
                    Thread.Sleep(300);
                    string reader = ReadPort(port);
                    if (reader.Contains("UShell >"))
                    {
                        skipSetup = true;

                        agingTime = DateTime.Now.AddHours(hours);
                        timeCheck[slot - 1] = agingTime.ToString("hh:mm tt");
                        agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Value = agingTime.ToString("hh:mm tt");
                        this.Invoke(new MethodInvoker(delegate {
                            timer[slot - 1].Start();
                            testLog.AppendText("Test has started at:\n" + DateTime.Now.ToString("hh:mm tt") + "\nWill be done by:\n" + timeCheck[slot - 1] + "\n");
                        }));

                        reader = SendPortCommand(port, "exit", ">", modelNumber);
                        WritetoFile(logfile, slot, reader);

                        reader = SendPortCommand(port, "printenv", ">", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        this.Invoke(new MethodInvoker(delegate {
                            testLog.SelectionColor = Color.DarkBlue;
                            testLog.AppendText("\n" + "*************Unlock Environment*************" + "\n");
                            testLog.AppendText("\n" + reader + "\n");
                            testLog.SelectionColor = Color.Black;
                        }));
                        reader = SendPortCommand(port, "gettail 0", ">", modelNumber);
                        WritetoFile(logfile, slot, reader);

                        reader = SendPortCommand(port, "ushell", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        goto SkipSetup;
                    }
                    else if (reader.Contains("WARNING:"))
                    {
                        goto SkipSetup;

                    }

                    #region Unit Unlock
                    while (!reader.Contains("Input password> <INTERRUPT>") && !testStop[slot - 1])
                    {
                        port.Write(interrupt, 0, 1);
                        Thread.Sleep(300);
                        reader += port.ReadExisting();
                    }
                    WritetoFile(logfile, slot, reader);

                    if (testStop[slot - 1])
                    {
                        goto StopImmediately;
                    }
                    reader = String.Empty;
                    reader = SendPortCommand(port, "REDACTED_PASSWORD", "uRU>>", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "printenv", "uRU>>", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "setenv BOOT_CONSOLE_LOG YES", "uRU>>", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "setenv AUTO_NEGO_STATUS CPRI", "uRU>>", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "saveenv", "uRU>>", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "printenv", "uRU>>", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    if (!reader.Contains("BOOT_CONSOLE_LOG=YES"))
                    {
                        port.WriteLine("setenv BOOT_CONSOLE_LOG YES");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("saveenv");
                        Thread.Sleep(3000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);
                    }
                    if (!reader.Contains("AUTO_NEGO_STATUS=CPRI"))
                    {
                        port.WriteLine("setenv AUTO_NEGO_STATUS CPRI");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("saveenv");
                        Thread.Sleep(3000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);
                    }
                    reader = string.Empty;
                    reader = SendPortCommand(port, "printenv", "uRU>>", modelNumber);
                    this.Invoke(new MethodInvoker(delegate {
                        testLog.SelectionColor = Color.DarkBlue;
                        testLog.AppendText("\n" + "*************Unlock Environment*************" + "\n");
                        testLog.AppendText("\n" + reader + "\n");
                        testLog.SelectionColor = Color.Black;
                    }));
                    WritetoFile(logfile, slot, reader);


                    reader = string.Empty;
                    port.WriteLine("reboot u");
                    Thread.Sleep(1000);
                    reader = ReadPort(port);
                    WritetoFile(logfile, slot, reader);

                    #endregion

                    agingTime = DateTime.Now.AddHours(hours);
                    timeCheck[slot - 1] = agingTime.ToString("hh:mm tt");
                    agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Value = agingTime.ToString("hh:mm tt");

                    this.Invoke(new MethodInvoker(delegate {
                        timer[slot - 1].Start();
                        testLog.AppendText("Test has started at:\n" + DateTime.Now.ToString("hh:mm tt") + "\nWill be done by:\n" + timeCheck[slot - 1] + "\n");
                    }));
                Login:;
                    this.Invoke(() => {
                        if (testLog.TextLength > 100000)
                        {
                            testLog.Clear();
                            testLog.SelectionColor = Color.DarkBlue;
                            testLog.AppendText(
                                "**============================================" + Environment.NewLine + "\n"
                                + "* *Date:          " + DateTime.Now.ToString("yyyy - MM - dd") + Environment.NewLine
                                + "** Serial Number: " + serialNumber + Environment.NewLine
                                + "** Model Number:  " + modelNumber + Environment.NewLine
                                + "** Slot:          " + slot + Environment.NewLine
                                + "** App Ver.       " + AppConstants.AppVersion + Environment.NewLine
                                + "** Com Port       " + modelSelector.comName + Environment.NewLine
                                + "** Aging Location:" + location + Environment.NewLine
                                + "** Burn Hours:    " + modelSelector.hours + Environment.NewLine
                                + "**============================================" + Environment.NewLine + "\n"
                            );
                            testLog.SelectionColor = Color.Black;
                            testLog.AppendText("** Log compacted due to size threshold **\n\n");
                        }
                    });

                    DateTime timeToCheck = DateTime.Now.AddMinutes(5);
                    while (DateTime.Now < timeToCheck)
                    {
                        if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto StopImmediately; }
                        reader = ReadPort(port);
                        dataBuildercom.Append(reader);
                        string bootstring = dataBuildercom.ToString();  // Store accumulated string
                        if (bootstring.Contains("Redirect stdout to /dev/console") || bootstring.Contains("Copyright (C), 2001-2015, Acme Electronic Co., Ltd.") || bootstring.Contains("RU_MODEL_A login:"))
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                Thread.Sleep(5000);  // Wait for 6 seconds (5000 milliseconds)
                            }
                            WritetoFile(logfile, slot, bootstring);
                            break;
                        }
                    }
                    dataBuildercom.Clear();
                SkipSetup:;
                    stopLoadingInCell((int)AgingDataRow.Boot_Up, slot);
                    agingGridView.Rows[(int)AgingDataRow.Boot_Up].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                    if (!snIsVerified)
                    {
                        showLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                    }
                    #region Unit Login

                    if (!skipSetup)
                    {
                        reader = SendPortCommand(port, "user", "Password:", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto StopImmediately; }

                        reader = SendPortCommand(port, "REDACTED_PASSWORD", "user@", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto StopImmediately; }

                        reader = SendPortCommand(port, "su -", "Password:", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto StopImmediately; }

                        reader = SendPortCommand(port, "REDACTED_PASSWORD", "root@", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto StopImmediately; }

                        reader = SendPortCommand(port, "printenv", ">", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto StopImmediately; }

                        reader = SendPortCommand(port, "gettail 0", ">", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto StopImmediately; }

                        if (modelNumber == "ORAN PCS" || modelNumber == "ORAN LOLO")
                        {
                            foreach (string line in reader.Split("\r\n"))
                            {
                                if (line.Contains("postbooter"))
                                {
                                    var values = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                    if (values[1] != postBooter)
                                    {
                                        this.Invoke(new MethodInvoker(delegate {
                                            testLog.SelectionColor = Color.Red;
                                            testLog.AppendText("Postbooter not installed\rSend to repair once test is finished\n");
                                            testLog.SelectionColor = Color.White;
                                        }));
                                    }
                                }
                            }
                        }
                        reader = SendPortCommand(port, "ushell", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto StopImmediately; }
                    }
                    skipSetup = false;
                    #endregion

                    #region SNVerification 
                    reader = SendPortCommand(port, "boardInvtShow", "UShell >", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    //Need to add stuff for passing and failing here.
                    if (!snIsVerified)
                    {
                        SnVerification = ParseBoardInvt(reader, serialNumber, testLog);
                        foreach (string line in reader.Split("\r\n"))
                        {
                            if (line.Contains("FW Version") && !line.Contains("Safe"))
                            {
                                var values = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                logger[slot].tlog.Firmware = values[4].Trim();
                                if (values[4].Contains("25.A.005.078") && modelNumber == "ORAN PCS")
                                {
                                    this.Invoke(new MethodInvoker(delegate {
                                        testLog.SelectionColor = Color.Red;
                                        testLog.AppendText(serialNumber + " has defective firmware\nPlease update firmware");
                                        testLog.SelectionColor = Color.Black;
                                    }));
                                    logger[slot].tfailed.Add(new TestFailed { TestName = "Firmware : ", Result = "FAIL", Value = values[4].Trim(), ErrorCodes = "NA" });
                                    logger[slot].tlog.OverallResult = "FAIL";
                                    FirmwareIsHigh = true;
                                    agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = (Bitmap)Image.FromFile(@"Resources\flag-xxl.png");
                                    goto StopImmediately;
                                }
                                this.Invoke(new MethodInvoker(delegate {
                                    testLog.SelectionColor = Color.Green;
                                    testLog.AppendText("Firmware Version : " + values[4] + "\n");
                                    testLog.SelectionColor = Color.Black;
                                }));
                                FirmwareVersion = values[4].Trim();
                            }

                        }
                    }
                    if (SnVerification == true && snIsVerified == false) //This for loop needs to be redone when officially done
                    {
                        snIsVerified = true;
                        SnVerification = true;
                        stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                        agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];


                    }
                    else if (SnVerification == false)
                    {
                        agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = currentTestStatus[TestStatus.Failed];
                        goto StopImmediately;
                    }
                    if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }
                    #endregion

                    #region Full Power Setup
                    this.Invoke(new MethodInvoker(delegate {
                        testLog.AppendText("Starting setup\n");
                    }));
                    switch (modelNumber)
                    {
                        case "ORAN LOLO":
                            VZ_LOLO vZ_LOLO = new VZ_LOLO();
                            vZ_LOLO.SetupVZLOLO(port, logfile, slot);
                            vZ_LOLO = null;
                            break;
                        case "ORAN PCS":
                            VZ_PCS vZ_PCS = new VZ_PCS();
                            vZ_PCS.SetupVZPCS(port, logfile, slot);
                            vZ_PCS = null;
                            break;
                    }
                    this.Invoke(new MethodInvoker(delegate {
                        testLog.AppendText("Setup is complete\n");
                    }));
                    this.Invoke(new MethodInvoker(delegate {
                        testLog.AppendText("Full power wait has started. Test will continue after 5 minutes.!\n");
                    }));

                    for (int i = 0; i < 60; i++)
                    {
                        if (testStop[slot - 1] == true || DateTime.Now > agingTime) { goto StopImmediately; }
                        port.WriteLine("");
                        Thread.Sleep(5000);  // Wait for 5 seconds (5000 milliseconds)
                    }
                    #endregion

                    #region Aging Loop
                    do
                    {
                        for (int loop = 0; loop < 8; loop++)
                        {
                            reader = SendPortCommand(port, "fwversion", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "sts", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "console sts", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardEnvShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardSourceShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardPowShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            #region Power and RSSI Validation
                            reader = SendPortCommand(port, "boardAntPowShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            var lines = reader.Split("\r\n");
                            foreach (var line in lines)
                            {
                                if (line.Contains("TxAntSum"))
                                {
                                    string[] values = line.Split(new String[] { " ", "|" }, StringSplitOptions.RemoveEmptyEntries);
                                    for (int i = 1; i < values.Length; i++)
                                    {
                                        try
                                        {
                                            double txValue = double.Parse(values[i]);
                                            if (txValue < 44 || txValue > 47)
                                            {
                                                txPowerPassed = false;
                                                if (bootcount < 0)
                                                {

                                                    logger[slot].tfailed.Add(new TestFailed { TestName = "Tx Power : Path " + i, Value = txValue + "db", Result = "FAIL", ErrorCodes = "TT053" });
                                                    string failKey = "Tx Power : Path " + i;
                                                    string failDetails = "Fail: " + values[i].Trim();
                                                    failResultscom[failKey] = failDetails;
                                                }
                                                logEntries.Add(($"Path {i} Failed TxAntSum={values[i]}dbm\n", Color.Red));

                                            }
                                            else
                                            {
                                                logEntries.Add(($"Path {i} Passed TxAntSum={values[i]}dbm\n", Color.Black));
                                            }
                                        }
                                        catch
                                        {
                                            this.Invoke(new MethodInvoker(delegate {
                                                testLog.AppendText("Exception thrown. Send log to engineer\n");
                                            }));
                                        }
                                    }
                                }
                                if (line.Contains("RxAntFa00"))
                                {
                                    logEntries.Add(($"\n", Color.Black));
                                    string[] values = line.Split(new String[] { " ", "|" }, StringSplitOptions.RemoveEmptyEntries);
                                    for (int i = 1; i < values.Length; i++)
                                    {
                                        double rssiValue = double.Parse(values[i]);
                                        if (rssiValue > -80 || rssiValue < -109)
                                        { // ORIGINAL LIMIT -96 AND -107

                                            if (bootcount < 0 && (double.Parse(values[3]) > -30 || double.Parse(values[3]) < -120))
                                            {

                                                logger[slot].tfailed.Add(new TestFailed { TestName = "RSSI : Path " + i, Value = rssiValue, Result = "FAIL", ErrorCodes = "TT045" });
                                                string failKey = "RSSI  : Path " + i;
                                                string failDetails = "Fail: " + values[i].Trim();
                                                failResultscom[failKey] = failDetails;
                                                rssiPassed = false;
                                            }

                                            logEntries.Add(($"Path {i} Failed RSSI ={values[i]}dbm\n", Color.Red));
                                            /*this.Invoke(new MethodInvoker(delegate {
                                                testLog.SelectionColor = Color.Red;
                                                testLog.AppendText("Path " + i + " Failed RSSI = " + values[i] + "dbm\n", modelNumber);
                                                testLog.SelectionColor = Color.Black;
                                            }));*/

                                        }
                                        else
                                        {
                                            logEntries.Add(($"Path {i} Passed RSSI ={values[i]}dbm\n", Color.Black));
                                            /*this.Invoke(new MethodInvoker(delegate {
                                                testLog.AppendText("Path " + i + " Passed RSSI = " + values[i] + "dbm\n", modelNumber);
                                            }));*/
                                        }
                                    }
                                }
                            }
                            this.Invoke(new MethodInvoker(() => {
                                foreach (var entry in logEntries)
                                {
                                    testLog.SelectionColor = entry.Color;
                                    testLog.AppendText(entry.Text);
                                }
                                testLog.SelectionColor = Color.Black;
                                testLog.AppendText("\n");
                            }));
                            logEntries.Clear();
                            #endregion

                            #region Board Temp Show
                            reader = SendPortCommand(port, "boardTempShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            // Regex for capturing all numbers after the label
                            (Match boardMatch, Match fpgaMatch) = CleanBoardTemp(reader);
                            if (fpgaMatch.Success)
                            {
                                for (int i = 1; i <= 3; i++)
                                {
                                    logEntries.Add(($"FpgaTemp Values:\nPath {i}: {fpgaMatch.Groups[i].Value}\n\n", Color.Black)); //Passing
                                    /*this.Invoke(new MethodInvoker(delegate {
                                        testLog.AppendText("\n" + "FpgaTemp Values: " + "\n", modelNumber);
                                        testLog.AppendText($"  Path {i}: {fpgaMatch.Groups[i].Value}" + "\n", modelNumber);
                                    }));*/
                                }
                            }

                            if (boardMatch.Success)
                            {
                                for (int i = 1; i <= 3; i++)
                                {
                                    logEntries.Add(($"BoardTemp Values: \nPath {i}: {boardMatch.Groups[i].Value}\n\n", Color.Black)); //Passing
                                    /*this.Invoke(new MethodInvoker(delegate {
                                        testLog.AppendText("\n" + "BoardTemp Values: " + "\n", modelNumber);
                                        testLog.AppendText($"  Path {i}: {boardMatch.Groups[i].Value}" + "\n", modelNumber);
                                    }));*/
                                }

                            }
                            this.Invoke(new MethodInvoker(() => {
                                foreach (var entry in logEntries)
                                {
                                    testLog.SelectionColor = entry.Color;
                                    testLog.AppendText(entry.Text);
                                }
                                testLog.SelectionColor = Color.Black;
                                testLog.AppendText("\n");
                            }));
                            logEntries.Clear();
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }
                            #endregion

                            reader = SendPortCommand(port, "boardPllShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardAttShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardOptShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardPowVersionShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardFAShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardFAMapShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardInfoShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardVerShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "boardHwShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "Alarm_Print 1", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);

                            for (int i = 0; i < 8; i++)
                            {
                                reader = SendPortCommand(port, "IRF_Get_Drain_Bias_Voltage_Level " + i + " 1", "UShell >", modelNumber);
                                WritetoFile(logfile, slot, reader);
                                if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }
                            }

                            reader = SendPortCommand(port, "dpdsts", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            reader = SendPortCommand(port, "pacalsts", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }

                            #region Return Loss Check
                            reader = SendPortCommand(port, "sts 100", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);

                            // Regex for capturing all numbers after "ReturnLoss"
                            Regex rlRegex = new Regex(
                                @"ReturnLoss\s*\[\s*dB\]\s*:\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]",
                                RegexOptions.IgnoreCase
                            );

                            Match rlMatch = rlRegex.Match(reader);

                            if (rlMatch.Success)
                            {
                                for (int i = 1; i <= 8; i++)
                                {
                                    string valueStr = rlMatch.Groups[i].Value.Trim();
                                    double returnLossValue;

                                    if (double.TryParse(valueStr, out returnLossValue))
                                    {
                                        if (returnLossValue < 10.0)
                                        {
                                            returnLossPassed = false;
                                            if (bootcount < 0) // assuming bootcount is in your scope
                                            {

                                                logger[slot].tfailed.Add(new TestFailed { TestName = "ReturnLoss  : Path " + i, Value = returnLossValue, Result = "FAIL", ErrorCodes = "TT045" });

                                                string failKey = $"ReturnLoss : Path {i}";
                                                string failDetails = $"Fail: {valueStr} dB";
                                                failResultscom[failKey] = failDetails;
                                            }
                                            logEntries.Add(($" Path {i} FailedReturnLoss =  {valueStr} dB\n", Color.Red)); //Failing
                                            /*testLog.SelectionColor = Color.Red;
                                                testLog.AppendText($" Path {i} FailedReturnLoss =  {valueStr} dB\n", modelNumber);
                                                testLog.SelectionColor = Color.Black;*/
                                        }
                                        else
                                        {
                                            logEntries.Add(($" Path {i} Passed ReturnLoss =  {valueStr} dB\n", Color.Black)); //Passing
                                                                                                                              //testLog.AppendText($" Path {i} Passed ReturnLoss =  {valueStr} dB\n", modelNumber);
                                        }
                                    }
                                }
                                this.Invoke(new MethodInvoker(() => {
                                    foreach (var entry in logEntries)
                                    {
                                        testLog.SelectionColor = entry.Color;
                                        testLog.AppendText(entry.Text);
                                    }
                                    testLog.SelectionColor = Color.Black;
                                    testLog.AppendText("\n");
                                }));
                                logEntries.Clear();
                            }

                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }
                            #endregion

                            #region Alarm Parsing 
                            port.ReadExisting();
                            reader = SendPortCommand(port, "almsts", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            string[] alarmKeywords = { "RxOverflowStep", "UDA", "OptTxFault", "RxCommFail", "HighPimLevel", "OptRxLOS", "AbnPowerCount", "LowGainSymptom", " [***ALARM OCCUR***]" };
                            string[] alarmlines = reader.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string line in alarmlines)
                            {
                                // Only process lines with "Occur"
                                if (line.Contains("Occur", StringComparison.OrdinalIgnoreCase))
                                {

                                    bool containsKnownKeyword = alarmKeywords.Any(keyword =>
                                        line.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                                    if (!containsKnownKeyword)
                                    {

                                        var alarmMatch = Regex.Match(line, @"\]\s*(.*?)\s*:");

                                        // Extract AntId and PaId (e.g., "4 ( 3)")
                                        var idMatch = Regex.Match(line, @"\:\s+(\d+)\s+\(\s*(\d+)\)");

                                        string alarmName = alarmMatch.Success ? alarmMatch.Groups[1].Value.Trim() : "Unknown";
                                        string antId = idMatch.Success ? idMatch.Groups[1].Value : "N/A";
                                        string paId = idMatch.Success ? idMatch.Groups[2].Value : "N/A";
                                        AlarmpnotPresent = false;
                                        if (bootcount == 0)
                                        {

                                            string failKey = "Alarm Name : " + paId;
                                            string failDetails = "Type: " + alarmName;
                                            failResultscom[failKey] = failDetails;

                                            logger[slot].tfailed.Add(new TestFailed { TestName = "Alarm : ", Value = alarmName + " Antenna ID: " + antId + "PA ID: " + paId, Result = "FAIL", ErrorCodes = "TT045" });
                                        }
                                        logEntries.Add(($"{alarmName} detected on AntId(PaId): {antId}({paId})\n", Color.Red));
                                        /*this.Invoke(() => {
                                            testLog.SelectionColor = Color.Red;
                                            testLog.AppendText($"{alarmName} detected on AntId(PaId): {antId}({paId})\n", modelNumber);
                                            testLog.SelectionColor = Color.Black;
                                        });*/
                                    }
                                }
                            }
                            this.Invoke(new MethodInvoker(() => {
                                foreach (var entry in logEntries)
                                {
                                    testLog.SelectionColor = entry.Color;
                                    testLog.AppendText(entry.Text);
                                }
                                testLog.SelectionColor = Color.Black;
                                testLog.AppendText("\n");
                            }));
                            logEntries.Clear();
                            #endregion

                            reader = SendPortCommand(port, "boardInvtShow", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto StopImmediately; }
                            Thread.Sleep(15000);
                        }


                        if (bootcount > 0)
                        {
                            AlarmpnotPresent = true;
                            txPowerPassed = true;
                            rssiPassed = true;
                            returnLossPassed = true;
                            bootcount--;
                        }

                        reboot_counter++;

                        if (DateTime.Now < agingTime && reboot_counter > 30 && AlarmpnotPresent && txPowerPassed && rssiPassed && returnLossPassed)
                        {
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.AppendText("\n" + "Break The Loop for Reboot...!" + "\n");
                            }));
                            break;
                        }

                        Thread.Sleep(25000);
                    }
                    while (DateTime.Now < agingTime && AlarmpnotPresent && txPowerPassed && rssiPassed && returnLossPassed);
                #endregion

                EndTest:;
                    #region Reboot Logic
                    if ((DateTime.Now < agingTime && reboot_counter > 30 && AlarmpnotPresent && txPowerPassed && rssiPassed && !testStop[slot - 1] && !reader.Contains("!fail!") && returnLossPassed))
                    {
                        reader = string.Empty;
                        if (DateTime.Now < agingTime && !testStop[slot - 1])
                        {
                            port.WriteLine("reset_psb");
                            Thread.Sleep(1000);
                            reader = ReadPort(port);
                            WritetoFile(logfile, slot, reader);
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.AppendText("Reset command sent\n" + DateTime.Now.ToString("HH:mm:ss") + "\n");
                            }));
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.AppendText("Unit is Booting...! Test will continue after 5 minutes.!\n");
                            }));
                            showLoadingInCell((int)AgingDataRow.Boot_Up, slot);
                        }
                        goto Login;
                    }
                #endregion

                StopImmediately:;
                    this.Invoke(new MethodInvoker(delegate {
                        timer[slot - 1].Stop();
                    }));


                    #region Final Condition Checks 
                    // Consolidate conditions
                    bool isTestStopped = testStop[slot - 1];
                    bool isConnectionLost = reader.Contains("!fail!");
                    bool isTestPassed = returnLossPassed && AlarmpnotPresent && txPowerPassed && rssiPassed && !isTestStopped && !isConnectionLost && !FirmwareIsHigh && SnVerification;

                    if (isTestPassed)
                    {
                        logger[slot].tlog.OverallResult = "PASS";

                        stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                        agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                        stopLoadingInCell((int)AgingDataRow.RF_Parameters, slot);
                        agingGridView.Rows[(int)AgingDataRow.RF_Parameters].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                        stopLoadingInCell((int)AgingDataRow.Alarms, slot);
                        agingGridView.Rows[(int)AgingDataRow.Alarms].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                        stopLoadingInCell((int)AgingDataRow.Result, slot);
                        agingGridView.Rows[(int)AgingDataRow.Result].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                        // Open port if not open
                        try
                        {
                            if (!port.IsOpen)
                            {
                                port.Open();
                            }
                        }
                        catch (Exception ex) { }

                        port.WriteLine("exit");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("delenv p VLAN_LOW");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("delenv p VLAN_HIGH");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("delenv p VLAN_DIS");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("printenv");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("setenv p AUTO_NEGO_STATUS ECPRI");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("setenv p BOOT_CONSOLE_LOG NO");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("printenv");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("reboot");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);
                    }
                    else if (returnLossPassed == false)
                    {
                        for (int i = (int)AgingDataRow.RF_Parameters; i < (int)AgingDataRow.Timer; i++)
                        {
                            stopLoadingInCell(i, slot);
                            agingGridView.Rows[i].Cells[slot].Value = currentTestStatus[TestStatus.Failed];
                        }
                    }
                    else if (FirmwareIsHigh)
                    {
                        for (int i = (int)AgingDataRow.Verify_SN; i < (int)AgingDataRow.Timer; i++)
                        {
                            stopLoadingInCell(i, slot);
                            agingGridView.Rows[i].Cells[slot].Value = currentTestStatus[TestStatus.Failed];
                        }
                    }// Handle connection loss
                    else if (isConnectionLost)
                    {
                        this.Invoke(new MethodInvoker(delegate {
                            testLog.AppendText("Connection to unit lost. No output detected\n");
                        }));

                        stopLoadingInCell((int)AgingDataRow.Boot_Up, slot);
                        agingGridView.Rows[(int)AgingDataRow.Boot_Up].Cells[slot].Value = currentTestStatus[TestStatus.Failed];

                        if (SnVerification == true && FirmwareIsHigh == false)
                        {
                            stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                            agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];
                        }
                        else
                        {
                            stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                            agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = currentTestStatus[TestStatus.Failed];
                            logger[slot].tlog.OverallResult = "FAIL";
                        }

                        // Reset the test status for other rows
                        foreach (var row in new[] { (int)AgingDataRow.RF_Parameters, (int)AgingDataRow.Alarms, (int)AgingDataRow.Result })
                        {
                            stopLoadingInCell(row, slot);
                            agingGridView.Rows[row].Cells[slot].Value = currentTestStatus[TestStatus.LostConnection];
                        }

                        logger[slot].tlog.OverallResult = "FAIL";
                    } // Handle test stop
                    else if (isTestStopped)
                    {
                        stopLoadingInCell((int)AgingDataRow.Boot_Up, slot);
                        agingGridView.Rows[(int)AgingDataRow.Boot_Up].Cells[slot].Value = currentTestStatus[TestStatus.Stopped];

                        stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                        agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = SnVerification ? currentTestStatus[TestStatus.IsPassing] : currentTestStatus[TestStatus.Stopped];

                        foreach (var row in new[] { (int)AgingDataRow.RF_Parameters, (int)AgingDataRow.Alarms, (int)AgingDataRow.Result })
                        {
                            stopLoadingInCell(row, slot);
                            agingGridView.Rows[row].Cells[slot].Value = currentTestStatus[TestStatus.Stopped];
                        }

                        logger[slot].tlog.OverallResult = "TEST STOP";
                    }

                    // Handle general failures
                    else
                    {
                        logger[slot].tlog.OverallResult = "FAIL";

                        // Handle RF Parameters failure
                        if (!txPowerPassed || !rssiPassed)
                        {
                            stopLoadingInCell((int)AgingDataRow.RF_Parameters, slot);
                            agingGridView.Rows[(int)AgingDataRow.RF_Parameters].Cells[slot].Value = currentTestStatus[TestStatus.Failed];

                            if (AlarmpnotPresent)
                            {
                                stopLoadingInCell((int)AgingDataRow.Alarms, slot);
                                agingGridView.Rows[(int)AgingDataRow.Alarms].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];
                            }
                        }

                        // Handle Alarms failure
                        if (!AlarmpnotPresent)
                        {
                            if (txPowerPassed && rssiPassed)
                            {
                                stopLoadingInCell((int)AgingDataRow.RF_Parameters, slot);
                                agingGridView.Rows[(int)AgingDataRow.RF_Parameters].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];
                            }

                            stopLoadingInCell((int)AgingDataRow.Alarms, slot);
                            agingGridView.Rows[(int)AgingDataRow.Alarms].Cells[slot].Value = currentTestStatus[TestStatus.Failed];
                        }

                        // Handle Result failure
                        if (!AlarmpnotPresent || !txPowerPassed || !rssiPassed)
                        {
                            stopLoadingInCell((int)AgingDataRow.Result, slot);
                            agingGridView.Rows[(int)AgingDataRow.Result].Cells[slot].Value = currentTestStatus[TestStatus.Failed];
                        }
                    }
                    #endregion

                    long memoryBefore = GC.GetTotalMemory(false);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    long memoryAfter = GC.GetTotalMemory(true);

                    File.AppendAllText(logfile,
                     $"\n[Memory Report]\nBefore GC: {memoryBefore:N0} bytes\nAfter GC: {memoryAfter:N0} bytes\nFreed: {(memoryBefore - memoryAfter):N0} bytes\n");


                    #region Fail Test Directory 
                    if (!isTestPassed)
                    {
                        try
                        {
                            // To display the dictionary contents in the RichTextBox
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.AppendText("\r\n" + "********** Fail Results **********" + "\r\n");
                                foreach (var entry in failResultscom)
                                {
                                    testLog.SelectionColor = Color.Red;
                                    testLog.AppendText($"Test: {entry.Key} => {entry.Value}\r\n");
                                    testLog.SelectionColor = Color.Black;
                                }

                                // Write to log file
                                string failResultsText = "";
                                foreach (var entry in failResultscom)
                                {
                                    failResultsText += $"Test: {entry.Key} => {entry.Value}\r\n";
                                }
                                File.AppendAllText(logfile, "\nElapsed Time: " + ReturnTimeStamp(slot) + "\n" + failResultsText + "\n");
                                testLog.AppendText("\r\n" + "***********************************" + "\r\n");
                            }));

                        }
                        catch (Exception ex)
                        {
                            WritetoFile(logfile, slot, ex.ToString());
                        }
                        this.Invoke(new MethodInvoker(delegate {
                            testLog.SelectionColor = Color.Red;
                            testLog.AppendText("\n" + "Aging Test Fail...!" + "\n");
                            testLog.SelectionColor = Color.Black;
                        }));

                    }
                    else
                    {
                        this.Invoke(new MethodInvoker(delegate {
                            testLog.SelectionColor = Color.Green;
                            testLog.AppendText("\n" + "Aging Test Pass...!" + "\n");
                            testLog.SelectionColor = Color.Black;
                        }));

                    }
                    #endregion

                    #region OLP Data Entry 
                    if (testStop[slot - 1] == false && !reader.Contains("!fail!"))
                    {
                        if (txPowerPassed == true && rssiPassed == true && returnLossPassed == true && AlarmpnotPresent == true && FirmwareIsHigh == false)
                        {
                            logger[slot].tfailed.Add(new TestFailed { TestName = "Tx Power 1 TO 8 : ", Result = "PASS", ErrorCodes = "NA" });
                            logger[slot].tfailed.Add(new TestFailed { TestName = "RSSI 1 TO 8 : ", Result = "PASS", ErrorCodes = "NA" });
                            logger[slot].tfailed.Add(new TestFailed { TestName = "Return Loss 1 TO 8 : ", Result = "PASS", ErrorCodes = "NA" });
                            logger[slot].tfailed.Add(new TestFailed { TestName = "Alarm : ", Result = "PASS", ErrorCodes = "NA" });
                        }


                        logger[slot].tlog.WorkStation = "ORAN Aging";
                        logger[slot].tlog.SerialNumber = serialNumber;
                        logger[slot].tlog.DateTime = DateTime.Now.ToString();
                        logger[slot].tlog.SlotID = slot.ToString();
                        logger[slot].tlog.BurnHr = modelSelector.hours.ToString();
                        //logger[slot].tlog.Firmware = FirmwareVersion.ToString(); //
                        logger[slot].tlog.Model = modelNumber.ToString();
                        logger[slot].tlog.Locations = "Facility 1";

                        bool Ftp_FileisCopied = logger[slot].WriteToLog(serialNumber);
                        logger[slot].tfailed.Clear();

                        if (Ftp_FileisCopied == true)
                        {
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.SelectionColor = Color.DarkBlue;
                                testLog.AppendText("Json Copied to the Server...!\n");
                                testLog.SelectionColor = Color.Black;
                            }));
                        }
                        else
                        {
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.SelectionColor = Color.Red;
                                testLog.AppendText("Unable to copy the file to the Server...!\n");
                                testLog.SelectionColor = Color.Black;
                            }));

                        }

                    }
                    #endregion

                    this.Invoke(new MethodInvoker(() => {
                        testLog.SelectionColor = Color.DarkBlue;
                        testLog.AppendText(
                    "**============================================" + Environment.NewLine +
                    "* *Date:          " + DateTime.Now.ToString("yyyy - MM - dd") + Environment.NewLine
                    + "** Serial Number: " + serialNumber + Environment.NewLine
                    + "** Model Number:  " + modelNumber + Environment.NewLine
                    + "** Slot:          " + slot + Environment.NewLine
                    + "** App Ver.       " + AppConstants.AppVersion + Environment.NewLine
                    + "** Com Port  " + port.PortName + Environment.NewLine
                    + "** Aging Location:          " + location + Environment.NewLine
                    + "** Burn Hours:          " + modelSelector.hours + Environment.NewLine
        + "**============================================" + Environment.NewLine + "\n");
                        testLog.SelectionColor = Color.Black;
                    }));

                    File.AppendAllText(logfile, "\n"
                       + "**============================================" + Environment.NewLine +
                     "* *Date:          " + DateTime.Now.ToString("yyyy - MM - dd") + Environment.NewLine
                    + "** Serial Number: " + serialNumber + Environment.NewLine
                    + "** Model Number:  " + modelNumber + Environment.NewLine
                    + "** Slot:          " + slot + Environment.NewLine
                    + "** App Ver.       " + AppConstants.AppVersion + Environment.NewLine
                    + "** Com Port  " + port.PortName + Environment.NewLine
                    + "** Aging Location:          " + location + Environment.NewLine
                    + "** Burn Hours:          " + modelSelector.hours + Environment.NewLine
                        + "**============================================" + Environment.NewLine);

                    File.AppendAllText(logfile, "\nTest Ended: " + ReturnTimeStamp(slot));


                    try
                    {
                        this.Invoke(new MethodInvoker(delegate {
                            testLog.AppendText("\r\n" + "********** Fail Results **********" + "\r\n");

                            foreach (var entry in failResultscom)
                            {
                                testLog.SelectionColor = Color.Red;
                                testLog.AppendText($"Test: {entry.Key} => {entry.Value}\r\n");
                                testLog.SelectionColor = Color.Black;
                            }

                            // Write to log file
                            string failResultsText = "";
                            foreach (var entry in failResultscom)
                            {
                                failResultsText += $"Test: {entry.Key} => {entry.Value}\r\n";
                            }


                            WritetoFile(logfile, slot, failResultsText);
                            testLog.AppendText("\r\n" + "***********************************" + "\r\n");
                        }));
                    }//This bracket is to end the  using (SerialPort port = new SerialPort) on line 1274
                    catch { }
                    #region T Drive Log Transfer
                    try
                    {
                        if (!string.IsNullOrEmpty(LogFileList[1]))
                        {
                            string destDir = IOPath.GetDirectoryName(LogFileList[1]);
                            Directory.CreateDirectory(destDir);
                            File.Copy(logfile, LogFileList[1], true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() => { MessageBox.Show("Failed to copy log to T: drive\n" + ex.ToString()); });
                    }
                    #endregion
                    port.Close();
                }


            }
            catch (Exception ex)
            {
                this.Invoke(new MethodInvoker(delegate {
                    testLog.AppendText($"Log copy error: {ex.Message}\n");
                }));
            }

        }
        #endregion

        #region AGING FAT LOLO MAIN FUNCTION
        private void StartFatLOLOAging(int slot, string serialNumber, string modelNumber, ModelSelector modelSelector, int hours, RichTextBox testLog)
        {
            if (modelSelector.comName == "" || modelSelector.comName == null)
            {
                this.Invoke(new MethodInvoker(() => {
                    testLog.AppendText("Com port not selected");
                }));
                for (int i = (int)AgingDataRow.Boot_Up; i < (int)AgingDataRow.Timer; i++)
                {
                    stopLoadingInCell(i, slot);
                    agingGridView.Rows[i].Cells[slot].Value = currentTestStatus[TestStatus.Blank];
                }
                agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Value = "";
                return;
            }
            try
            {
                Dictionary<string, string> failResults = new Dictionary<string, string>();
                StartBackgroundTask(slot);
                this.Invoke(new MethodInvoker(delegate {
                    timer[slot - 1].Reset();
                }));
                agingGridView.Rows[(int)AgingDataRow.Timer].Cells[slot].Value = string.Empty;
                agingGridView.Rows[(int)AgingDataRow.Timer].Cells[slot].Value = "00:00:00";
                int bootcount = 2;
                int reboot_counter = 0;

                DateTime agingTime = DateTime.Now;
                bool isPassing = true;
                string FirmwareVersion = "";
                bool SnVerification = false;
                bool txPowerPassed = true;
                bool returnLossPassed = true;
                bool AlarmpnotPresent = true;
                bool UnitIsLocked = false;
                bool rssiPassed = true;
                bool snIsVerified = false;
                bool unitIsFlagged = false;
                List<(string Text, System.Drawing.Color Color)> logEntries = new List<(string, System.Drawing.Color Color)>();
                List<string> LogFileList = LogBuilder(serialNumber, modelNumber);
                Dictionary<string, string> failResultscom = new Dictionary<string, string>();
                string location = "Facility 1";
                string logfile = LogFileList[0];
                if (!logger.ContainsKey(slot))
                    logger[slot] = new LogHandler();

                logger[slot].tfailed.Clear();  // start fresh
                File.WriteAllText(logfile,

                "**================================================================================" + Environment.NewLine
                + "** Date:          " + DateTime.Now.ToString("yyyy-MM-dd") + Environment.NewLine
                + "** Serial Number: " + serialNumber + Environment.NewLine
                + "** Model Number:  " + modelNumber + Environment.NewLine
                + "** Slot:          " + slot + Environment.NewLine
                + "** App Ver.       " + AppConstants.AppVersion + Environment.NewLine
                + "** Com Port  " + modelSelector.comName + Environment.NewLine
                + "** Aging Location:          " + location + Environment.NewLine
                + "** Burn Hours:          " + modelSelector.hours + Environment.NewLine

                + "**================================================================================" + Environment.NewLine + "\n"); ;

                this.Invoke(new MethodInvoker(() => {
                    testLog.SelectionColor = Color.DarkBlue;
                    testLog.AppendText(
                 "**============================================" + Environment.NewLine + "\n"
                + "* *Date:          " + DateTime.Now.ToString("yyyy - MM - dd") + Environment.NewLine
                + "** Serial Number: " + serialNumber + Environment.NewLine
                + "** Model Number:  " + modelNumber + Environment.NewLine
                + "** Slot:          " + slot + Environment.NewLine
                + "** App Ver.       " + AppConstants.AppVersion + Environment.NewLine
                + "** Com Port  " + modelSelector.comName + Environment.NewLine
                + "** Aging Location:          " + location + Environment.NewLine
                + "** Burn Hours:          " + modelSelector.hours + Environment.NewLine
    + "**============================================" + Environment.NewLine + "\n");
                    testLog.SelectionColor = Color.Black;
                }));
                agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Style.Font = new Font("Digital-7", 16F, FontStyle.Regular);
                agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Style.ForeColor = Color.DarkGreen;
                agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Value = "TBD";

                this.Invoke(new MethodInvoker(delegate {
                    testLog.SelectionColor = Color.Green;
                    testLog.AppendText("Plug in the power now\n");
                    testLog.SelectionColor = Color.Black;
                }));
                StringBuilder dataBuildercom = new StringBuilder();
                bool skipSetup = false;
                using (SerialPort port = new SerialPort(modelSelector.comName))
                {
                    port.BaudRate = 115200;
                    port.Parity = Parity.None;
                    port.StopBits = StopBits.One;
                    port.Open();
                    port.WriteLine("");
                    Thread.Sleep(300);
                    string reader = ReadPort(port);
                    if (reader.Contains("UShell >"))
                    {
                        skipSetup = true;

                        agingTime = DateTime.Now.AddHours(hours);
                        timeCheck[slot - 1] = agingTime.ToString("hh:mm tt");
                        agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Value = agingTime.ToString("hh:mm tt");
                        this.Invoke(new MethodInvoker(delegate {
                            timer[slot - 1].Start();
                            testLog.AppendText("Test has started at:\n" + DateTime.Now.ToString("hh:mm tt") + "\nWill be done by:\n" + timeCheck[slot - 1] + "\n");
                        }));

                        reader = SendPortCommand(port, "exit", ">", modelNumber);
                        WritetoFile(logfile, slot, reader);

                        reader = SendPortCommand(port, "printenv", ">", modelNumber);
                        WritetoFile(logfile, slot, reader);

                        this.Invoke(new MethodInvoker(delegate {
                            testLog.SelectionColor = Color.DarkBlue;
                            testLog.AppendText("\n" + "*************Unlock Environment*************" + "\n");
                            testLog.AppendText("\n" + reader + "\n");
                            testLog.SelectionColor = Color.Black;
                        }));

                        reader = SendPortCommand(port, "gettail 0", ">", modelNumber);
                        WritetoFile(logfile, slot, reader);

                        reader = SendPortCommand(port, "ushell", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);

                        goto SkipSetup;
                    }
                    else if (reader.Contains("WARNING:"))
                    {
                        goto SkipSetup;

                    }

                    #region Unlock Unit
                    while (!reader.Contains("<INTERRUPT>") && !testStop[slot - 1])
                    {
                        port.Write(interrupt, 0, 1);
                        Thread.Sleep(300);
                        reader += port.ReadExisting();
                    }
                    WritetoFile(logfile, slot, reader);

                    if (testStop[slot - 1])
                        goto EndTest;

                    // Login to U-Boot
                    reader = SendPortCommand(port, "REDACTED_PASSWORD", "uRU>>", modelNumber);
                    WritetoFile(logfile, slot, reader);
                    // Set environment variables
                    string[] commands = new[]
                    {
    "flashenv BOOT_CONSOLE_LOG yes",
    "flashenv AUTO_NEGO_STATUS",
    "printenv"
};

                    foreach (var cmd in commands)
                    {
                        reader = SendPortCommand(port, cmd, "uRU>>", modelNumber);
                        WritetoFile(logfile, slot, reader);
                    }

                    this.Invoke(new MethodInvoker(delegate {
                        testLog.SelectionColor = Color.DarkBlue;
                        testLog.AppendText("\n*************Unlock Environment*************\n");
                        testLog.AppendText("\n" + reader + "\n");
                        testLog.SelectionColor = Color.Black;
                    }));

                    // Reboot
                    port.WriteLine("reboot u");
                    Thread.Sleep(1000);
                    reader = ReadPort(port);
                    WritetoFile(logfile, slot, reader);

                    this.Invoke(new MethodInvoker(delegate {
                        testLog.AppendText("Environment has been flashed.\n" + DateTime.Now.ToString("hh:mm:ss tt") + "\n");
                    }));

                    agingTime = DateTime.Now.AddHours(hours);
                    timeCheck[slot - 1] = agingTime.ToString("hh:mm tt");
                    agingGridView.Rows[(int)AgingDataRow.Finish_Time].Cells[slot].Value = agingTime.ToString("hh:mm tt");
                    this.Invoke(new MethodInvoker(delegate {
                        timer[slot - 1].Start();
                        testLog.AppendText("Test has started at:\n" + DateTime.Now.ToString("hh:mm tt") + "\nWill be done by:\n" + timeCheck[slot - 1] + "\n");
                    }));
                    // Wait for system to boot (max 5 mins)
                    StringBuilder dataBuilderCom = new StringBuilder();
                    DateTime bootTimeout = DateTime.Now.AddMinutes(5);

                    while (DateTime.Now < bootTimeout)
                    {
                        if (testStop[slot - 1] || reader.Contains("!fail!"))
                            goto EndTest;

                        reader = ReadPort(port);
                        if (!string.IsNullOrEmpty(reader))
                            dataBuilderCom.Append(reader);

                        string bootString = dataBuilderCom.ToString();

                        if (bootString.Contains("Redirect stdout to /dev/console") ||
                            bootString.Contains("Copyright (C), 2001-2015, Acme Electronic Co., Ltd.") ||
                            bootString.Contains("RU_MODEL_B login:"))
                        {
                            WritetoFile(logfile, slot, bootString);
                            break;
                        }
                    }

                    dataBuildercom.Clear();

                    reader = string.Empty;
                    reader = SendPortCommand(port, "user", "Password:", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "REDACTED_PASSWORD", "user@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "su -", "Password:", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "REDACTED_PASSWORD", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "printenv", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "delenv p VLAN_LOW", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "delenv p VLAN_HIGH", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "delenv p VLAN_SEL", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "delenv p VLAN_DIS", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "delenv p AUTO_NEGO_STATUS", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "delenv p slot#0", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "delenv p slot#1", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "delenv p slot#2", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = string.Empty;
                    reader = SendPortCommand(port, "printenv", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);
                    this.Invoke(new MethodInvoker(delegate {
                        testLog.SelectionColor = Color.DarkBlue;
                        testLog.AppendText("\n" + "*************Set Environment*************" + "\n");
                        testLog.AppendText("\n" + reader + "\n");
                        testLog.SelectionColor = Color.Black;
                    }));

                    reader = SendPortCommand(port, "printenv | grep VLAN", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "rm -rf /mnt/storage_misc/dhcp_*", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "ls -al /mnt/storage_misc/dhcp_*", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    reader = SendPortCommand(port, "ucmd INTF_PowerReset", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);

                    this.Invoke(new MethodInvoker(delegate {
                        testLog.AppendText("Environment changes applied. Rebooting...\n");
                    }));
                    #endregion

                    DateTime timeToCheck = DateTime.Now.AddMinutes(5);
                    while (DateTime.Now < timeToCheck)
                    {
                        if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }
                        reader = ReadPort(port);
                        dataBuildercom.Append(reader);
                        string bootstring = dataBuildercom.ToString();  // Store accumulated string RU_MODEL_B login:
                        if (bootstring.Contains("Redirect stdout to /dev/console") || bootstring.Contains("Copyright (C), 2001-2015, Acme Electronic Co., Ltd.") || bootstring.Contains("RU_MODEL_B login:"))
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                Thread.Sleep(5000);  // Wait for 6 seconds (5000 milliseconds)
                            }
                            WritetoFile(logfile, slot, bootstring);
                            break;
                        }
                    }
                    dataBuildercom.Clear();


                SkipSetup:;

                    stopLoadingInCell((int)AgingDataRow.Boot_Up, slot);
                    agingGridView.Rows[(int)AgingDataRow.Boot_Up].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                    showLoadingInCell((int)AgingDataRow.Verify_SN, slot);


                    bool skipWait = false;

                    reader = string.Empty;
                    try
                    {
                        #region Unit Login 
                        if (!skipSetup)
                        {
                            reader = SendPortCommand(port, "user", "Password:", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }

                            reader = SendPortCommand(port, "REDACTED_PASSWORD", "user@", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }

                            reader = SendPortCommand(port, "su -", "Password:", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }

                            reader = SendPortCommand(port, "REDACTED_PASSWORD", "root@", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }

                            reader = SendPortCommand(port, "ushell", "UShell >", modelNumber);
                            WritetoFile(logfile, slot, reader);
                            if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(@"C:\Test_TechCo\ErrorLog.txt", DateTime.Now.ToString() + "\n" + ex.ToString() + "\n\n");
                    }
                    //---------------------------SET UP START-----------------//
                    #endregion

                    #region SNVerification 
                    reader = SendPortCommand(port, "boardInvtShow", "UShell >", modelNumber);
                    WritetoFile(logfile, slot, reader);
                    if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto StopImmediately; }

                    //Need to add stuff for passing and failing here.
                    if (!snIsVerified)
                    {
                        SnVerification = ParseBoardInvt(reader, serialNumber, testLog);
                        foreach (string line in reader.Split("\r\n"))
                        {
                            if (line.Contains("FW Version") && !line.Contains("Safe"))
                            {
                                var values = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                logger[slot].tlog.Firmware = values[4].Trim();
                                this.Invoke(new MethodInvoker(delegate {
                                    testLog.SelectionColor = Color.Green;
                                    testLog.AppendText("Firmware Version : " + values[4] + "\n");
                                    testLog.SelectionColor = Color.Black;
                                }));

                            }

                        }
                    }
                    if (SnVerification == true && snIsVerified == false) //This for loop needs to be redone when officially done
                    {
                        snIsVerified = true;
                        SnVerification = true;
                        stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                        agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];


                    }
                    else if (SnVerification == false)
                    {
                        goto StopImmediately;
                    }
                    #endregion

                    #region XL_Setup
                    FAT_LOLO fat_LOLO = new FAT_LOLO();
                    fat_LOLO.SetupVZ_XLLOLO(port, logfile, slot);
                    fat_LOLO = null;

                    this.Invoke(new MethodInvoker(delegate {
                        testLog.AppendText("Setup is complete\n");
                    }));
                    this.Invoke(new MethodInvoker(delegate {
                        testLog.AppendText("Full power wait has started. Test will continue after 5 minutes.!\n");
                    }));

                    for (int i = 0; i < 60; i++)
                    {
                        if (testStop[slot - 1] == true || DateTime.Now > agingTime) { goto StopImmediately; }
                        port.WriteLine("");
                        Thread.Sleep(5000);  // Wait for 5 seconds (5000 milliseconds)
                    }
                    #endregion XL_Setup
                    #region Root Gettail and Getinv Reading 

                    reader = SendPortCommand(port, "exit", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);
                    if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }

                    reader = SendPortCommand(port, "printenv", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);
                    if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }

                    reader = SendPortCommand(port, "getinv", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);
                    if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }

                    reader = SendPortCommand(port, "gettail 0", "root@", modelNumber);
                    WritetoFile(logfile, slot, reader);
                    if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }

                    reader = SendPortCommand(port, "ushell", "UShell >", modelNumber);
                    WritetoFile(logfile, slot, reader);
                    if (testStop[slot - 1] == true || reader.Contains("!fail!")) { goto EndTest; }
                    #endregion

                    #region Aging Loop
                    do
                    {
                        this.Invoke(() => {
                            if (testLog.TextLength > 100000)
                            {
                                testLog.Clear();
                                testLog.SelectionColor = Color.DarkBlue;
                                testLog.AppendText(
                                    "**============================================" + Environment.NewLine + "\n"
                                    + "* *Date:          " + DateTime.Now.ToString("yyyy - MM - dd") + Environment.NewLine
                                    + "** Serial Number: " + serialNumber + Environment.NewLine
                                    + "** Model Number:  " + modelNumber + Environment.NewLine
                                    + "** Slot:          " + slot + Environment.NewLine
                                    + "** App Ver.       " + AppConstants.AppVersion + Environment.NewLine
                                    + "** Com Port       " + modelSelector.comName + Environment.NewLine
                                    + "** Aging Location:" + location + Environment.NewLine
                                    + "** Burn Hours:    " + modelSelector.hours + Environment.NewLine
                                    + "**============================================" + Environment.NewLine + "\n"
                                );
                                testLog.SelectionColor = Color.Black;
                                testLog.AppendText("** Log compacted due to size threshold **\n\n");
                            }
                        });
                        if (testStop[slot - 1])
                        {
                            goto EndTest;
                        }

                        reader = SendPortCommand(port, "boardInvtShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (!snIsVerified)
                        {
                            SnVerification = ParseBoardInvt(reader, serialNumber, testLog);
                            foreach (string line in reader.Split("\r\n"))
                            {
                                if (line.Contains("FW Version") && !line.Contains("Safe"))
                                {
                                    var values = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                    logger[slot].tlog.Firmware = values[4].Trim();
                                    this.Invoke(new MethodInvoker(delegate {
                                        testLog.SelectionColor = Color.Green;
                                        testLog.AppendText("Firmware Version : " + values[4] + "\n");
                                        testLog.SelectionColor = Color.Black;
                                    }));

                                }

                            }
                        }
                        if (SnVerification == true && snIsVerified == false) //This for loop needs to be redone when officially done
                        {
                            snIsVerified = true;
                            stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                            agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                        }
                        else if (SnVerification == false)
                        {
                            goto EndTest;
                        }

                        reader = SendPortCommand(port, "fwversion", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "console sts", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardEnvShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardSourceShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardPowShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        #region Power and RSSI Validation
                        reader = SendPortCommand(port, "boardAntPowShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        var lines = reader.Split("\r\n");
                        foreach (var line in lines)
                        {
                            if (line.Contains("TxAntSum"))
                            {
                                string[] values = line.Split(new String[] { " ", "|" }, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 1; i < values.Length; i++)
                                {
                                    try
                                    {
                                        double txValue = double.Parse(values[i]);
                                        if (txValue < 44 || txValue > 47)
                                        {
                                            txPowerPassed = false;
                                            if (bootcount < 0)
                                            {

                                                logger[slot].tfailed.Add(new TestFailed { TestName = "Tx Power : Path " + i, Value = txValue + "db", Result = "FAIL", ErrorCodes = "TT053" });
                                                string failKey = "Tx Power : Path " + i;
                                                string failDetails = "Fail: " + values[i].Trim();
                                                failResultscom[failKey] = failDetails;
                                            }
                                            logEntries.Add(($"Path {i} Failed TxAntSum={values[i]}dbm\n", Color.Red));

                                        }
                                        else
                                        {
                                            logEntries.Add(($"Path {i} Passed TxAntSum={values[i]}dbm\n", Color.Black));
                                        }
                                    }
                                    catch
                                    {
                                        this.Invoke(new MethodInvoker(delegate {
                                            testLog.AppendText("Exception thrown. Send log to engineer\n");
                                        }));
                                    }
                                }
                            }
                            if (line.Contains("RxAntFa00"))
                            {
                                logEntries.Add(($"\n", Color.Black));
                                string[] values = line.Split(new String[] { " ", "|" }, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 1; i < values.Length; i++)
                                {
                                    double rssiValue = double.Parse(values[i]);
                                    if (rssiValue > -80 || rssiValue < -109)
                                    { // ORIGINAL LIMIT -96 AND -107

                                        if (bootcount < 0 && (double.Parse(values[3]) > -30 || double.Parse(values[3]) < -120))
                                        {

                                            logger[slot].tfailed.Add(new TestFailed { TestName = "RSSI : Path " + i, Value = rssiValue, Result = "FAIL", ErrorCodes = "TT045" });
                                            string failKey = "RSSI  : Path " + i;
                                            string failDetails = "Fail: " + values[i].Trim();
                                            failResultscom[failKey] = failDetails;
                                            rssiPassed = false;
                                        }

                                        logEntries.Add(($"Path {i} Failed RSSI ={values[i]}dbm\n", Color.Red));
                                        /*this.Invoke(new MethodInvoker(delegate {
                                            testLog.SelectionColor = Color.Red;
                                            testLog.AppendText("Path " + i + " Failed RSSI = " + values[i] + "dbm\n");
                                            testLog.SelectionColor = Color.Black;
                                        }));*/

                                    }
                                    else
                                    {
                                        logEntries.Add(($"Path {i} Passed RSSI ={values[i]}dbm\n", Color.Black));
                                        /*this.Invoke(new MethodInvoker(delegate {
                                            testLog.AppendText("Path " + i + " Passed RSSI = " + values[i] + "dbm\n");
                                        }));*/
                                    }
                                }
                            }
                        }
                        this.Invoke(new MethodInvoker(() => {
                            foreach (var entry in logEntries)
                            {
                                testLog.SelectionColor = entry.Color;
                                testLog.AppendText(entry.Text);
                            }
                            testLog.SelectionColor = Color.Black;
                            testLog.AppendText("\n");
                        }));
                        logEntries.Clear();
                        #endregion

                        #region Return Loss Check
                        reader = SendPortCommand(port, "sts", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);

                        // Regex for capturing all numbers after "ReturnLoss"
                        Regex rlRegex = new Regex(
                            @"ReturnLoss\s*\[\s*dB\]\s*:\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]\s*\[\s*([\d\.\-]+)\]",
                            RegexOptions.IgnoreCase
                        );

                        Match rlMatch = rlRegex.Match(reader);

                        if (rlMatch.Success)
                        {
                            for (int i = 1; i <= 8; i++)
                            {
                                string valueStr = rlMatch.Groups[i].Value.Trim();
                                double returnLossValue;

                                if (double.TryParse(valueStr, out returnLossValue))
                                {
                                    if (returnLossValue < 10.0)
                                    {
                                        returnLossPassed = false;
                                        if (bootcount < 0) // assuming bootcount is in your scope
                                        {

                                            logger[slot].tfailed.Add(new TestFailed { TestName = "ReturnLoss  : Path " + i, Value = returnLossValue, Result = "FAIL", ErrorCodes = "TT045" });

                                            string failKey = $"ReturnLoss : Path {i}";
                                            string failDetails = $"Fail: {valueStr}";
                                            failResultscom[failKey] = failDetails;
                                        }
                                        logEntries.Add(($" Path {i} Failed ReturnLoss =  {valueStr} dB\n", Color.Red)); //Failing
                                        /*testLog.SelectionColor = Color.Red;
                                            testLog.AppendText($" Path {i} FailedReturnLoss =  {valueStr} dB\n");
                                            testLog.SelectionColor = Color.Black;*/
                                    }
                                    else
                                    {
                                        logEntries.Add(($" Path {i} Passed ReturnLoss =  {valueStr} dB\n", Color.Black)); //Passing
                                                                                                                          //testLog.AppendText($" Path {i} Passed ReturnLoss =  {valueStr} dB\n");
                                    }
                                }
                            }
                            this.Invoke(new MethodInvoker(() => {
                                foreach (var entry in logEntries)
                                {
                                    testLog.SelectionColor = entry.Color;
                                    testLog.AppendText(entry.Text);
                                }
                                testLog.SelectionColor = Color.Black;
                                testLog.AppendText("\n");
                            }));
                            logEntries.Clear();
                        }

                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }
                        #endregion

                        #region Alarm Parsing 
                        reader = SendPortCommand(port, "almsts", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        string[] alarmKeywords = { "RxOverflowStep", "UDA", "OptTxFault", "RxCommFail", "HighPimLevel", "OptRxLOS", "***ALARM OCCUR***" };
                        string[] alarmlines = reader.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string line in alarmlines)
                        {
                            // Only process lines with "Occur"
                            if (line.Contains("Occur", StringComparison.OrdinalIgnoreCase))
                            {

                                bool containsKnownKeyword = alarmKeywords.Any(keyword =>
                                    line.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                                if (!containsKnownKeyword)
                                {

                                    var alarmMatch = Regex.Match(line, @"\]\s*(.*?)\s*:");

                                    // Extract AntId and PaId (e.g., "4 ( 3)")
                                    var idMatch = Regex.Match(line, @"\:\s+(\d+)\s+\(\s*(\d+)\)");

                                    string alarmName = alarmMatch.Success ? alarmMatch.Groups[1].Value.Trim() : "Unknown";
                                    string antId = idMatch.Success ? idMatch.Groups[1].Value : "N/A";
                                    string paId = idMatch.Success ? idMatch.Groups[2].Value : "N/A";
                                    AlarmpnotPresent = false;
                                    if (bootcount < 0)
                                    {
                                        string failKey = "Alarm Name : " + paId;
                                        string failDetails = "Type: " + alarmName;
                                        failResultscom[failKey] = failDetails;
                                        logger[slot].tfailed.Add(new TestFailed { TestName = "Alarm : ", Value = alarmName + "Antenna ID: " + antId + " PA ID: " + paId, Result = "FAIL", ErrorCodes = "TT045" });
                                    }
                                    logEntries.Add(($"{alarmName} detected on AntId(PaId): {antId}({paId})\n", Color.Red));
                                    /*this.Invoke(() => {
                                        testLog.SelectionColor = Color.Red;
                                        testLog.AppendText($"{alarmName} detected on AntId(PaId): {antId}({paId})\n");
                                        testLog.SelectionColor = Color.Black;
                                    });*/
                                }
                            }
                        }
                        this.Invoke(new MethodInvoker(() => {
                            foreach (var entry in logEntries)
                            {
                                testLog.SelectionColor = entry.Color;
                                testLog.AppendText(entry.Text);
                            }
                            testLog.SelectionColor = Color.Black;
                            testLog.AppendText("\n");
                        }));
                        logEntries.Clear();
                        #endregion

                        reader = SendPortCommand(port, "boardPllShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardAttShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardOptShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardPowVersionShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardFaShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardFAMapShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardInfoShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardVerShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        reader = SendPortCommand(port, "boardHwShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        #region Board Temp Show
                        reader = SendPortCommand(port, "boardTempShow", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        // Regex for capturing all numbers after the label
                        (Match boardMatch, Match fpgaMatch) = CleanBoardTemp(reader);
                        if (fpgaMatch.Success)
                        {
                            for (int i = 1; i <= 3; i++)
                            {
                                logEntries.Add(("\n" + "FpgaTemp Values: " + "\n", Color.Black)); //Passing
                                logEntries.Add(($"  Path {i}: {fpgaMatch.Groups[i].Value}\n", Color.Black)); //Passing
                                /*this.Invoke(new MethodInvoker(delegate {
                                    testLog.AppendText("\n" + "FpgaTemp Values: " + "\n");
                                    testLog.AppendText($"  Path {i}: {fpgaMatch.Groups[i].Value}\n");
                                }));*/
                            }
                        }

                        if (boardMatch.Success)
                        {
                            for (int i = 1; i <= 3; i++)
                            {
                                logEntries.Add(("\nBoardTemp Values: \n", Color.Black)); //Passing
                                logEntries.Add(($"  Path {i}: {fpgaMatch.Groups[i].Value}\n", Color.Black)); //Passing
                                /*this.Invoke(new MethodInvoker(delegate {
                                    testLog.AppendText("\n" + "BoardTemp Values: " + "\n");
                                    testLog.AppendText($"  Path {i}: {fpgaMatch.Groups[i].Value}\n");
                                }));*/
                            }
                        }
                        this.Invoke(new MethodInvoker(() => {
                            foreach (var entry in logEntries)
                            {
                                testLog.SelectionColor = entry.Color;
                                testLog.AppendText(entry.Text);
                            }
                            testLog.SelectionColor = Color.Black;
                            testLog.AppendText("\n");
                        }));
                        logEntries.Clear();
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }
                        this.Invoke(new MethodInvoker(delegate {
                            testLog.AppendText("\n");

                        }));
                        #endregion

                        reader = SendPortCommand(port, "Alarm_Print 1", "UShell >", modelNumber);
                        WritetoFile(logfile, slot, reader);
                        if (testStop[slot - 1] == true || reader.Contains("!fail!") || DateTime.Now > agingTime) { goto EndTest; }

                        if (bootcount > 0)
                        {
                            AlarmpnotPresent = true;
                            txPowerPassed = true;
                            rssiPassed = true;
                            returnLossPassed = true;
                            bootcount--;
                        }
                        Thread.Sleep(25000);
                    }
                    while (DateTime.Now < agingTime && AlarmpnotPresent && txPowerPassed && rssiPassed && returnLossPassed);
                #endregion

                EndTest:;
                StopImmediately:;
                    this.Invoke(new MethodInvoker(delegate {
                        timer[slot - 1].Stop();
                    }));

                    #region Final Condition Checks 
                    // Consolidate conditions
                    bool isTestStopped = testStop[slot - 1];
                    bool isConnectionLost = reader.Contains("!fail!");
                    bool isSnVerificationFailed = SnVerification == false;
                    bool isTestPassed = returnLossPassed && AlarmpnotPresent && txPowerPassed && rssiPassed && !isTestStopped && isConnectionLost == false;
                    if (isTestPassed)
                    {
                        logger[slot].tlog.OverallResult = "PASS";

                        stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                        agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                        stopLoadingInCell((int)AgingDataRow.RF_Parameters, slot);
                        agingGridView.Rows[(int)AgingDataRow.RF_Parameters].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                        stopLoadingInCell((int)AgingDataRow.Alarms, slot);
                        agingGridView.Rows[(int)AgingDataRow.Alarms].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                        stopLoadingInCell((int)AgingDataRow.Result, slot);
                        agingGridView.Rows[(int)AgingDataRow.Result].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];

                        // Open port if not open
                        try
                        {
                            if (!port.IsOpen)
                            {
                                port.Open();
                            }
                        }
                        catch (Exception ex) { }
                        port.WriteLine("INTF_PASwitchOFFall");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("exit");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("delenv p VLAN_LOW");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("delenv p VLAN_HIGH");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("delenv p VLAN_DIS");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("printenv");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("setenv p BOOT_CONSOLE_LOG NO");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("printenv");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);

                        port.WriteLine("reboot");
                        Thread.Sleep(1000);
                        reader = ReadPort(port);
                        WritetoFile(logfile, slot, reader);


                    SkipLock:;
                    } // Handle connection loss

                    else if (isConnectionLost)
                    {
                        this.Invoke(new MethodInvoker(delegate {
                            testLog.AppendText("Connection to unit lost. No output detected\n");
                        }));

                        stopLoadingInCell((int)AgingDataRow.Boot_Up, slot);
                        agingGridView.Rows[(int)AgingDataRow.Boot_Up].Cells[slot].Value = currentTestStatus[TestStatus.Failed];

                        if (SnVerification == true)
                        {
                            stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                            agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];
                        }
                        else
                        {
                            stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                            agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = currentTestStatus[TestStatus.LostConnection];
                        }

                        // Reset the test status for other rows
                        foreach (var row in new[] { (int)AgingDataRow.RF_Parameters, (int)AgingDataRow.Alarms, (int)AgingDataRow.Result })
                        {
                            stopLoadingInCell(row, slot);
                            agingGridView.Rows[row].Cells[slot].Value = currentTestStatus[TestStatus.LostConnection];
                        }

                        logger[slot].tlog.OverallResult = "FAIL";
                    } // Handle test stop
                    else if (isTestStopped)
                    {
                        stopLoadingInCell((int)AgingDataRow.Boot_Up, slot);
                        agingGridView.Rows[(int)AgingDataRow.Boot_Up].Cells[slot].Value = currentTestStatus[TestStatus.Stopped];

                        stopLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                        agingGridView.Rows[(int)AgingDataRow.Verify_SN].Cells[slot].Value = SnVerification ? currentTestStatus[TestStatus.IsPassing] : currentTestStatus[TestStatus.Stopped];

                        foreach (var row in new[] { (int)AgingDataRow.RF_Parameters, (int)AgingDataRow.Alarms, (int)AgingDataRow.Result })
                        {
                            stopLoadingInCell(row, slot);
                            agingGridView.Rows[row].Cells[slot].Value = currentTestStatus[TestStatus.Stopped];
                        }

                        logger[slot].tlog.OverallResult = "TEST STOP";
                    }

                    // Handle general failures
                    else
                    {
                        logger[slot].tlog.OverallResult = "FAIL";

                        // Handle RF Parameters failure
                        if (!txPowerPassed || !rssiPassed)
                        {
                            stopLoadingInCell((int)AgingDataRow.RF_Parameters, slot);
                            agingGridView.Rows[(int)AgingDataRow.RF_Parameters].Cells[slot].Value = currentTestStatus[TestStatus.Failed];

                            if (AlarmpnotPresent)
                            {
                                stopLoadingInCell((int)AgingDataRow.Alarms, slot);
                                agingGridView.Rows[(int)AgingDataRow.Alarms].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];
                            }
                        }

                        // Handle Alarms failure
                        if (!AlarmpnotPresent)
                        {
                            Task.Run(() => { MessageBox.Show(serialNumber + " is throwing alarms. Send to repair"); });
                            if (txPowerPassed && rssiPassed)
                            {
                                stopLoadingInCell((int)AgingDataRow.RF_Parameters, slot);
                                agingGridView.Rows[(int)AgingDataRow.RF_Parameters].Cells[slot].Value = currentTestStatus[TestStatus.IsPassing];
                            }

                            stopLoadingInCell((int)AgingDataRow.Alarms, slot);
                            agingGridView.Rows[(int)AgingDataRow.Alarms].Cells[slot].Value = currentTestStatus[TestStatus.Failed];
                        }

                        // Handle Result failure
                        if (!AlarmpnotPresent || !txPowerPassed || !rssiPassed)
                        {
                            stopLoadingInCell((int)AgingDataRow.Result, slot);
                            agingGridView.Rows[(int)AgingDataRow.Result].Cells[slot].Value = currentTestStatus[TestStatus.Failed];
                        }
                    }
                    #endregion

                    #region Fail Test Directory 
                    if (!isTestPassed)
                    {
                        try
                        {
                            // To display the dictionary contents in the RichTextBox
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.AppendText("\r\n" + "********** Fail Results **********" + "\r\n");
                                foreach (var entry in failResultscom)
                                {
                                    testLog.SelectionColor = Color.Red;
                                    testLog.AppendText($"Test: {entry.Key} => {entry.Value}\r\n");
                                    testLog.SelectionColor = Color.Black;
                                }

                                // Write to log file
                                string failResultsText = "";
                                foreach (var entry in failResultscom)
                                {
                                    failResultsText += $"Test: {entry.Key} => {entry.Value}\r\n";
                                }
                                File.AppendAllText(logfile, "\nElapsed Time: " + ReturnTimeStamp(slot) + "\n" + failResultsText + "\n");
                                testLog.AppendText("\r\n" + "***********************************" + "\r\n");
                            }));

                        }
                        catch (Exception ex)
                        {
                            WritetoFile(logfile, slot, ex.ToString());
                        }
                        this.Invoke(new MethodInvoker(delegate {
                            testLog.SelectionColor = Color.Red;
                            testLog.AppendText("\n" + "Aging Test Fail...!" + "\n");
                            testLog.SelectionColor = Color.Black;
                        }));

                    }
                    else
                    {
                        this.Invoke(new MethodInvoker(delegate {
                            testLog.SelectionColor = Color.Green;
                            testLog.AppendText("\n" + "Aging Test Pass...!" + "\n");
                            testLog.SelectionColor = Color.Black;
                        }));

                    }
                    #endregion

                    #region OLP Data Entry 
                    if (testStop[slot - 1] == false && !reader.Contains("!fail!"))
                    {
                        if (txPowerPassed == true && rssiPassed == true && returnLossPassed == true && AlarmpnotPresent == true)
                        {
                            logger[slot].tfailed.Add(new TestFailed { TestName = "Tx Power 1 TO 8 : ", Result = "PASS", ErrorCodes = "NA" });
                            logger[slot].tfailed.Add(new TestFailed { TestName = "RSSI 1 TO 8 : ", Result = "PASS", ErrorCodes = "NA" });
                            logger[slot].tfailed.Add(new TestFailed { TestName = "Return Loss 1 TO 8 : ", Result = "PASS", ErrorCodes = "NA" });
                            logger[slot].tfailed.Add(new TestFailed { TestName = "Alarm : ", Result = "PASS", ErrorCodes = "NA" });
                        }


                        logger[slot].tlog.WorkStation = "ORAN Aging";
                        logger[slot].tlog.SerialNumber = serialNumber;
                        logger[slot].tlog.DateTime = DateTime.Now.ToString();
                        logger[slot].tlog.SlotID = slot.ToString();
                        logger[slot].tlog.BurnHr = modelSelector.hours.ToString();
                        //logger[slot].tlog.Firmware = FirmwareVersion.ToString();
                        logger[slot].tlog.Model = modelNumber.ToString();
                        logger[slot].tlog.Locations = "Facility 1";

                        bool Ftp_FileisCopied = logger[slot].WriteToLog(serialNumber);
                        logger[slot].tfailed.Clear();

                        if (Ftp_FileisCopied == true)
                        {
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.SelectionColor = Color.DarkBlue;
                                testLog.AppendText("Json Copied to the Server...!\n");
                                testLog.SelectionColor = Color.Black;
                            }));
                        }
                        else
                        {
                            this.Invoke(new MethodInvoker(delegate {
                                testLog.SelectionColor = Color.Red;
                                testLog.AppendText("Unable to copy the file to the Server...!\n");
                                testLog.SelectionColor = Color.Black;
                            }));

                        }

                    }
                    #endregion

                    this.Invoke(new MethodInvoker(() => {
                        testLog.SelectionColor = Color.DarkBlue;
                        testLog.AppendText(
                    "**============================================" + Environment.NewLine +
                    "* *Date:          " + DateTime.Now.ToString("yyyy - MM - dd") + Environment.NewLine
                    + "** Serial Number: " + serialNumber + Environment.NewLine
                    + "** Model Number:  " + modelNumber + Environment.NewLine
                    + "** Slot:          " + slot + Environment.NewLine
                    + "** App Ver.       " + AppConstants.AppVersion + Environment.NewLine
                    + "** Com Port  " + port.PortName + Environment.NewLine
                    + "** Aging Location:          " + location + Environment.NewLine
                    + "** Burn Hours:          " + modelSelector.hours + Environment.NewLine
        + "**============================================" + Environment.NewLine + "\n");
                        testLog.SelectionColor = Color.Black;
                    }));

                    File.AppendAllText(logfile, "\n"
                      + "**============================================" + Environment.NewLine +
                    "* *Date:          " + DateTime.Now.ToString("yyyy - MM - dd") + Environment.NewLine
                   + "** Serial Number: " + serialNumber + Environment.NewLine
                   + "** Model Number:  " + modelNumber + Environment.NewLine
                   + "** Slot:          " + slot + Environment.NewLine
                   + "** App Ver.       " + AppConstants.AppVersion + Environment.NewLine
                   + "** Com Port  " + port.PortName + Environment.NewLine
                   + "** Aging Location:          " + location + Environment.NewLine
                   + "** Burn Hours:          " + modelSelector.hours + Environment.NewLine
                       + "**============================================" + Environment.NewLine);

                    File.AppendAllText(logfile, "\nTest Ended: " + ReturnTimeStamp(slot));

                    try
                    {
                        string failResultsText = "";
                        this.Invoke(new MethodInvoker(delegate {
                            testLog.AppendText("\r\n" + "********** Fail Results **********" + "\r\n");

                            foreach (var entry in failResultscom)
                            {
                                testLog.SelectionColor = Color.Red;
                                testLog.AppendText($"Test: {entry.Key} => {entry.Value}\r\n");
                                testLog.SelectionColor = Color.Black;
                            }

                            // Write to log file
                            foreach (var entry in failResultscom)
                            {
                                failResultsText += $"Test: {entry.Key} => {entry.Value}\r\n";
                            }
                            testLog.AppendText("\r\n" + "***********************************" + "\r\n");
                        }));
                        WritetoFile(logfile, slot, failResultsText);
                    } //This bracket is to end the  using (SerialPort port = new SerialPort) on line 3241
                    catch { }
                    #region T Drive Log Transfer
                    try
                    {
                        if (!string.IsNullOrEmpty(LogFileList[1]))
                        {
                            string destDir = IOPath.GetDirectoryName(LogFileList[1]);
                            Directory.CreateDirectory(destDir);
                            File.Copy(logfile, LogFileList[1], true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() => { MessageBox.Show("Failed to copy log to T: drive\n" + ex.ToString()); });
                    }
                    #endregion
                    port.Close();
                }
            }
            catch (Exception ex)
            {
                this.Invoke(new MethodInvoker(delegate {
                    testLog.AppendText($"Log copy error: {ex.Message}\n");
                }));
            }

        }
        #endregion

        private void StartBackgroundTask(int slot)
        {
            // Run sequentially to avoid overlapping column mixups
            Task.Run(() => {
                showLoadingInCell((int)AgingDataRow.Boot_Up, slot);
                showLoadingInCell((int)AgingDataRow.Verify_SN, slot);
                showLoadingInCell((int)AgingDataRow.RF_Parameters, slot);
                showLoadingInCell((int)AgingDataRow.Alarms, slot);
                showLoadingInCell((int)AgingDataRow.Result, slot);
            });
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 1; i < agingGridView.ColumnCount; i++)
            {
                DataGridViewCell cell = agingGridView.Rows[(int)AgingDataRow.Result].Cells[i];
                if (cell.Tag != null)
                {
                    var formClosing = MessageBox.Show("Units are still running\nAre you sure you want to close?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (formClosing == DialogResult.Yes)
                    {
                        e.Cancel = false;
                        break;
                    }
                    else
                    {
                        e.Cancel = true;
                        break;
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            // Check if the user clicked "Yes"
            if (result == DialogResult.Yes)
            {
                // Close the form
                exitisclicked = true;
                this.Close();
            }
            // If the user clicked "No", do nothing and keep the form open
            else
            {

            }
        }

        // Manual test hook - copies a known sample file through the path/format logic
        // instead of waiting on a live unit, useful when a string variable's format
        // doesn't match what's expected and I need to isolate whether it's a parsing
        // issue vs. a hardware/timing issue.
        private void button2_Click(object sender, EventArgs e)
        {
            string localFile = @"C:\Log\CarrierA_Lo_Lo_XL\SAMPLE0001_Aging_01_01_2026__00_00_00000.txt";
            string destDir = @"T:\Acme Test Logs\5G RU ORAN\CarrierA FAT LOLO\Aging\";
            string destFile = IOPath.Combine(destDir, IOPath.GetFileName(localFile));

            try
            {
                Directory.CreateDirectory(destDir);
                File.Copy(localFile, destFile, true);
                MessageBox.Show($"File copied successfully to:\n{destFile}", "T: Drive Copy Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed: " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
