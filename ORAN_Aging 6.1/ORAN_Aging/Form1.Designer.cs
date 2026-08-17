namespace ORAN_Aging
{
    partial class Form1
    { 
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        Version version = new Version();
        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            pictureBox1 = new PictureBox();
            agingGridView = new DataGridView();
            Column1 = new DataGridViewTextBoxColumn();
            agingTestLog = new RichTextBox();
            pbSpin = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            button1 = new Button();
            button2 = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)agingGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbSpin).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.InitialImage = null;
            pictureBox1.Location = new Point(12, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(1816, 143);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // agingGridView
            // 
            agingGridView.AllowUserToAddRows = false;
            agingGridView.AllowUserToDeleteRows = false;
            agingGridView.AllowUserToResizeColumns = false;
            agingGridView.AllowUserToResizeRows = false;
            agingGridView.BackgroundColor = SystemColors.Control;
            agingGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            agingGridView.ColumnHeadersVisible = false;
            agingGridView.Columns.AddRange(new DataGridViewColumn[] { Column1 });
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = Color.White;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            agingGridView.DefaultCellStyle = dataGridViewCellStyle1;
            agingGridView.Location = new Point(22, 186);
            agingGridView.Name = "agingGridView";
            agingGridView.ReadOnly = true;
            agingGridView.RowHeadersVisible = false;
            agingGridView.RowTemplate.Height = 58;
            agingGridView.ShowCellToolTips = false;
            agingGridView.ShowEditingIcon = false;
            agingGridView.Size = new Size(1252, 783);
            agingGridView.TabIndex = 1;
            agingGridView.CellClick += agingGridView_CellClick;
            agingGridView.CellMouseDown += agingGridView_CellMouseDown;
            agingGridView.CellMouseEnter += agingGridView_CellMouseEnter;
            agingGridView.CellMouseLeave += agingGridView_CellMouseLeave;
            // 
            // Column1
            // 
            Column1.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Column1.HeaderText = "Column1";
            Column1.Name = "Column1";
            Column1.ReadOnly = true;
            // 
            // agingTestLog
            // 
            agingTestLog.BackColor = Color.White;
            agingTestLog.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            agingTestLog.Location = new Point(1280, 186);
            agingTestLog.Name = "agingTestLog";
            agingTestLog.ReadOnly = true;
            agingTestLog.Size = new Size(548, 783);
            agingTestLog.TabIndex = 2;
            agingTestLog.Text = "";
            // 
            // pbSpin
            // 
            pbSpin.BackColor = Color.White;
            pbSpin.Image = (Image)resources.GetObject("pbSpin.Image");
            pbSpin.Location = new Point(226, 73);
            pbSpin.Name = "pbSpin";
            pbSpin.Size = new Size(33, 34);
            pbSpin.SizeMode = PictureBoxSizeMode.StretchImage;
            pbSpin.TabIndex = 3;
            pbSpin.TabStop = false;
            pbSpin.Visible = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label1.ForeColor = Color.Navy;
            label1.Location = new Point(1270, 158);
            label1.Name = "label1";
            label1.Size = new Size(75, 21);
            label1.TabIndex = 4;
            label1.Text = "Test Log:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label2.ForeColor = Color.Navy;
            label2.Location = new Point(1351, 158);
            label2.Name = "label2";
            label2.Size = new Size(16, 21);
            label2.TabIndex = 5;
            label2.Text = "-";
            // 
            // button1
            // 
            button1.BackColor = Color.Red;
            button1.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            button1.ForeColor = Color.White;
            button1.Location = new Point(22, 975);
            button1.Name = "button1";
            button1.Size = new Size(100, 30);
            button1.TabIndex = 6;
            button1.Text = "Exit";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Enabled = false;
            button2.Location = new Point(620, 84);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 7;
            button2.Text = "button2";
            button2.UseVisualStyleBackColor = true;
            button2.Visible = false;
            button2.Click += button2_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1805, 1004);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(pbSpin);
            Controls.Add(agingTestLog);
            Controls.Add(agingGridView);
            Controls.Add(pictureBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "ORAN Aging V 6.1";
            FormClosing += Form1_FormClosing;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)agingGridView).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbSpin).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private RichTextBox agingTestLog;
        private DataGridView agingGridView;
        private PictureBox pbSpin;
        private DataGridViewTextBoxColumn Column1;
        private Label label1;
        private Label label2;
        private Button button1;
        private Button button2;
    }
}
