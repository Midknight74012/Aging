using ORAN_Aging;
using System.IO.Ports;

namespace GUI_Template
{

    public partial class ModelSelector : Form
    {
        public int hours = 0;
        public string comName = "";
        public string warranty = "";
        string[] invalidPorts = new string[Form1.slots];
        /*private Dictionary<string, string> ModelNames = new Dictionary<string, string>
        {
            //FriendlyName and official name
            {"8-port 2.1/1.9 GHz AWS/PCS", "SLS-BR0497EAEX" },
            {"4-port 700/850 MHz LOLO", "SLS-BR04C4ECEX" }
        };*/

        public ModelSelector() {
            InitializeComponent();
            for (int i = 0; i < Form1.slots; i++) {
                invalidPorts[i] = "COM" + (i + 11).ToString();
            }
        }
        public void show() {
            this.BringToFront();
            ShowDialog();
        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e) {
            if (radioButton1.Checked) { hours = 2; }
        }
        private void radioButton2_CheckedChanged(object sender, EventArgs e) {
            if (radioButton2.Checked) { hours = 4; }
        }
        private void radioButton3_CheckedChanged(object sender, EventArgs e) {
            if (radioButton3.Checked) { hours = 8; }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e) {
            if (radioButton4.Checked) { hours = 12; }
        }

        private void button1_Click(object sender, EventArgs e) {
            if (comboBox2.SelectedItem != null && comboBox2.Text != "Select Comport" && hours > 0) {
                try {
                    comName = comboBox2.SelectedItem.ToString();
                    using (SerialPort serialPort = new SerialPort(comName)) {
                        serialPort.Open();
                        serialPort.Close();
                    }
                    this.Hide();
                }
                catch (UnauthorizedAccessException) {
                    MessageBox.Show("Com " + comboBox2.Text + " already open");
                }
                catch (Exception ex) {
                    MessageBox.Show("Cannot open " + comboBox2.Text + ": " + ex.Message);
                }
            } else if(hours == 0) {
                MessageBox.Show("Select the number of hours for testing");
            }
            else {
                this.Hide();
            }

        }

        private void comboBox2_DropDown(object sender, EventArgs e) {     
                
                comboBox2.Items.Clear();
                string[] portNames = SerialPort.GetPortNames();
                comboBox2.MaxDropDownItems = portNames.Length;

            for (int i = 0; i < portNames.Length; i++) {
                try {
                    using (SerialPort comPort = new SerialPort(portNames[i], 115200)) {
                        comPort.Open();
                        if (comPort.IsOpen) {
                            comboBox2.Items.Add(portNames[i]);
                        }
                        comPort.Close();
                    }
                }
                catch (UnauthorizedAccessException) { 
                    // Port is already in use by another slot
                }
                catch (Exception) { 
                    // Port unavailable (OperationCanceledException, IOException, etc.) — skip it
                }
            }  
        }

        private void Exit_Click(object sender, EventArgs e) {
            comName = "Nope";
            this.Close();
        }
    }
}
