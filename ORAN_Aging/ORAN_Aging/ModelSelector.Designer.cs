namespace GUI_Template
{
    partial class ModelSelector
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModelSelector));
            label1 = new Label();
            radioButton1 = new RadioButton();
            radioButton2 = new RadioButton();
            radioButton3 = new RadioButton();
            radioButton4 = new RadioButton();
            comboBox1 = new ComboBox();
            button1 = new Button();
            comboBox2 = new ComboBox();
            Exit = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.White;
            label1.Font = new Font("Segoe UI", 20.25F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(144, 9);
            label1.Name = "label1";
            label1.Size = new Size(374, 37);
            label1.TabIndex = 0;
            label1.Text = "Please Select the Radio Model";
            // 
            // radioButton1
            // 
            radioButton1.AutoSize = true;
            radioButton1.BackColor = Color.White;
            radioButton1.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            radioButton1.Location = new Point(144, 60);
            radioButton1.Name = "radioButton1";
            radioButton1.Size = new Size(82, 25);
            radioButton1.TabIndex = 1;
            radioButton1.TabStop = true;
            radioButton1.Text = "2 hours";
            radioButton1.UseVisualStyleBackColor = false;
            radioButton1.CheckedChanged += radioButton1_CheckedChanged;
            // 
            // radioButton2
            // 
            radioButton2.AutoSize = true;
            radioButton2.BackColor = Color.White;
            radioButton2.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            radioButton2.Location = new Point(236, 60);
            radioButton2.Name = "radioButton2";
            radioButton2.Size = new Size(82, 25);
            radioButton2.TabIndex = 2;
            radioButton2.TabStop = true;
            radioButton2.Text = "4 hours";
            radioButton2.UseVisualStyleBackColor = false;
            radioButton2.CheckedChanged += radioButton2_CheckedChanged;
            // 
            // radioButton3
            // 
            radioButton3.AutoSize = true;
            radioButton3.BackColor = Color.White;
            radioButton3.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            radioButton3.Location = new Point(341, 60);
            radioButton3.Name = "radioButton3";
            radioButton3.Size = new Size(82, 25);
            radioButton3.TabIndex = 3;
            radioButton3.TabStop = true;
            radioButton3.Text = "8 hours";
            radioButton3.UseVisualStyleBackColor = false;
            radioButton3.CheckedChanged += radioButton3_CheckedChanged;
            // 
            // radioButton4
            // 
            radioButton4.AutoSize = true;
            radioButton4.BackColor = Color.White;
            radioButton4.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            radioButton4.Location = new Point(445, 60);
            radioButton4.Name = "radioButton4";
            radioButton4.Size = new Size(88, 25);
            radioButton4.TabIndex = 4;
            radioButton4.TabStop = true;
            radioButton4.Text = "12 hours";
            radioButton4.UseVisualStyleBackColor = false;
            radioButton4.CheckedChanged += radioButton4_CheckedChanged;
            // 
            // comboBox1
            // 
            comboBox1.Enabled = false;
            comboBox1.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "ORAN PCS", "ORAN LOLO", "ORAN FAT LOLO", "DISH DUAL", "DISH TRI" });
            comboBox1.Location = new Point(199, 103);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(334, 29);
            comboBox1.TabIndex = 5;
            comboBox1.Text = "Select Model";
            comboBox1.Visible = false;
            // 
            // button1
            // 
            button1.Font = new Font("Segoe UI Semibold", 20.25F, FontStyle.Bold, GraphicsUnit.Point);
            button1.Location = new Point(29, 230);
            button1.Name = "button1";
            button1.Size = new Size(224, 48);
            button1.TabIndex = 6;
            button1.Text = "Confirm";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // comboBox2
            // 
            comboBox2.AllowDrop = true;
            comboBox2.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            comboBox2.FormattingEnabled = true;
            comboBox2.Items.AddRange(new object[] { "" });
            comboBox2.Location = new Point(199, 168);
            comboBox2.MaxDropDownItems = 100;
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(334, 29);
            comboBox2.TabIndex = 7;
            comboBox2.Text = "Select Comport";
            comboBox2.DropDown += comboBox2_DropDown;
            // 
            // Exit
            // 
            Exit.Font = new Font("Segoe UI Semibold", 20.25F, FontStyle.Bold, GraphicsUnit.Point);
            Exit.Location = new Point(445, 230);
            Exit.Name = "Exit";
            Exit.Size = new Size(224, 48);
            Exit.TabIndex = 8;
            Exit.Text = "Exit";
            Exit.UseVisualStyleBackColor = true;
            Exit.Click += Exit_Click;
            // 
            // ModelSelector
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(684, 300);
            Controls.Add(Exit);
            Controls.Add(comboBox2);
            Controls.Add(button1);
            Controls.Add(comboBox1);
            Controls.Add(radioButton4);
            Controls.Add(radioButton3);
            Controls.Add(radioButton2);
            Controls.Add(radioButton1);
            Controls.Add(label1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "ModelSelector";
            Text = "ModelSelector";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private RadioButton radioButton1;
        private RadioButton radioButton2;
        private RadioButton radioButton3;
        private RadioButton radioButton4;
        private ComboBox comboBox1;
        private Button button1;
        private ComboBox comboBox2;
        private Button Exit;
    }
}