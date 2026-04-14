
namespace IpevoSdkDemo
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.CameraComboBox = new System.Windows.Forms.ComboBox();
            this.buttonWbDec = new System.Windows.Forms.Button();
            this.buttonWbAdd = new System.Windows.Forms.Button();
            this.checkBoxWB = new System.Windows.Forms.CheckBox();
            this.wbLabel = new System.Windows.Forms.Label();
            this.focusButton = new System.Windows.Forms.Button();
            this.focusLabel = new System.Windows.Forms.Label();
            this.checkBoxAF = new System.Windows.Forms.CheckBox();
            this.buttonFocusAdd = new System.Windows.Forms.Button();
            this.buttonFocusDec = new System.Windows.Forms.Button();
            this.afModeLabel = new System.Windows.Forms.Label();
            this.buttonAfMode = new System.Windows.Forms.Button();
            this.openCamButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // CameraComboBox
            // 
            this.CameraComboBox.FormattingEnabled = true;
            this.CameraComboBox.Location = new System.Drawing.Point(52, 87);
            this.CameraComboBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.CameraComboBox.Name = "CameraComboBox";
            this.CameraComboBox.Size = new System.Drawing.Size(283, 23);
            this.CameraComboBox.TabIndex = 1;
            this.CameraComboBox.SelectedIndexChanged += new System.EventHandler(this.CameraComboBox_SelectedIndexChanged);
            // 
            // buttonWbDec
            // 
            this.buttonWbDec.Location = new System.Drawing.Point(183, 134);
            this.buttonWbDec.Margin = new System.Windows.Forms.Padding(2);
            this.buttonWbDec.Name = "buttonWbDec";
            this.buttonWbDec.Size = new System.Drawing.Size(51, 25);
            this.buttonWbDec.TabIndex = 3;
            this.buttonWbDec.Text = "-";
            this.buttonWbDec.UseVisualStyleBackColor = true;
            this.buttonWbDec.Click += new System.EventHandler(this.buttonWbDec_Click);
            // 
            // buttonWbAdd
            // 
            this.buttonWbAdd.Location = new System.Drawing.Point(246, 134);
            this.buttonWbAdd.Margin = new System.Windows.Forms.Padding(2);
            this.buttonWbAdd.Name = "buttonWbAdd";
            this.buttonWbAdd.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.buttonWbAdd.Size = new System.Drawing.Size(51, 25);
            this.buttonWbAdd.TabIndex = 4;
            this.buttonWbAdd.Text = "+";
            this.buttonWbAdd.UseVisualStyleBackColor = true;
            this.buttonWbAdd.Click += new System.EventHandler(this.buttonWbAdd_Click);
            // 
            // checkBoxWB
            // 
            this.checkBoxWB.AutoSize = true;
            this.checkBoxWB.Location = new System.Drawing.Point(62, 138);
            this.checkBoxWB.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxWB.Name = "checkBoxWB";
            this.checkBoxWB.Size = new System.Drawing.Size(61, 19);
            this.checkBoxWB.TabIndex = 5;
            this.checkBoxWB.Text = "AWB";
            this.checkBoxWB.UseVisualStyleBackColor = true;
            this.checkBoxWB.Click += new System.EventHandler(this.checkBoxWB_Click);
            // 
            // wbLabel
            // 
            this.wbLabel.AutoSize = true;
            this.wbLabel.Location = new System.Drawing.Point(325, 141);
            this.wbLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.wbLabel.Name = "wbLabel";
            this.wbLabel.Size = new System.Drawing.Size(14, 15);
            this.wbLabel.TabIndex = 6;
            this.wbLabel.Text = "0";
            this.wbLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // focusButton
            // 
            this.focusButton.Location = new System.Drawing.Point(262, 226);
            this.focusButton.Margin = new System.Windows.Forms.Padding(2);
            this.focusButton.Name = "focusButton";
            this.focusButton.Size = new System.Drawing.Size(69, 32);
            this.focusButton.TabIndex = 7;
            this.focusButton.Text = "Focus";
            this.focusButton.UseVisualStyleBackColor = true;
            this.focusButton.Click += new System.EventHandler(this.focusButton_Click);
            // 
            // focusLabel
            // 
            this.focusLabel.AutoSize = true;
            this.focusLabel.Location = new System.Drawing.Point(325, 191);
            this.focusLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.focusLabel.Name = "focusLabel";
            this.focusLabel.Size = new System.Drawing.Size(14, 15);
            this.focusLabel.TabIndex = 11;
            this.focusLabel.Text = "0";
            this.focusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // checkBoxAF
            // 
            this.checkBoxAF.AutoSize = true;
            this.checkBoxAF.Location = new System.Drawing.Point(62, 188);
            this.checkBoxAF.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxAF.Name = "checkBoxAF";
            this.checkBoxAF.Size = new System.Drawing.Size(47, 19);
            this.checkBoxAF.TabIndex = 10;
            this.checkBoxAF.Text = "AF";
            this.checkBoxAF.UseVisualStyleBackColor = true;
            this.checkBoxAF.Click += new System.EventHandler(this.checkBoxAF_Click);
            // 
            // buttonFocusAdd
            // 
            this.buttonFocusAdd.Location = new System.Drawing.Point(246, 184);
            this.buttonFocusAdd.Margin = new System.Windows.Forms.Padding(2);
            this.buttonFocusAdd.Name = "buttonFocusAdd";
            this.buttonFocusAdd.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.buttonFocusAdd.Size = new System.Drawing.Size(51, 25);
            this.buttonFocusAdd.TabIndex = 9;
            this.buttonFocusAdd.Text = "+";
            this.buttonFocusAdd.UseVisualStyleBackColor = true;
            this.buttonFocusAdd.Click += new System.EventHandler(this.buttonFocusAdd_Click);
            // 
            // buttonFocusDec
            // 
            this.buttonFocusDec.Location = new System.Drawing.Point(183, 184);
            this.buttonFocusDec.Margin = new System.Windows.Forms.Padding(2);
            this.buttonFocusDec.Name = "buttonFocusDec";
            this.buttonFocusDec.Size = new System.Drawing.Size(51, 25);
            this.buttonFocusDec.TabIndex = 8;
            this.buttonFocusDec.Text = "-";
            this.buttonFocusDec.UseVisualStyleBackColor = true;
            this.buttonFocusDec.Click += new System.EventHandler(this.buttonFocusDec_Click);
            // 
            // afModeLabel
            // 
            this.afModeLabel.AutoSize = true;
            this.afModeLabel.Location = new System.Drawing.Point(62, 235);
            this.afModeLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.afModeLabel.Name = "afModeLabel";
            this.afModeLabel.Size = new System.Drawing.Size(13, 15);
            this.afModeLabel.TabIndex = 12;
            this.afModeLabel.Text = "?";
            this.afModeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // buttonAfMode
            // 
            this.buttonAfMode.Location = new System.Drawing.Point(183, 225);
            this.buttonAfMode.Margin = new System.Windows.Forms.Padding(2);
            this.buttonAfMode.Name = "buttonAfMode";
            this.buttonAfMode.Size = new System.Drawing.Size(66, 32);
            this.buttonAfMode.TabIndex = 13;
            this.buttonAfMode.Text = "Mode";
            this.buttonAfMode.UseVisualStyleBackColor = true;
            this.buttonAfMode.Click += new System.EventHandler(this.buttonAfMode_Click);
            // 
            // openCamButton
            // 
            this.openCamButton.Location = new System.Drawing.Point(113, 12);
            this.openCamButton.Name = "openCamButton";
            this.openCamButton.Size = new System.Drawing.Size(184, 58);
            this.openCamButton.TabIndex = 15;
            this.openCamButton.Text = "Open Capture App";
            this.openCamButton.UseVisualStyleBackColor = true;
            this.openCamButton.Click += new System.EventHandler(this.OpenCamButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(428, 283);
            this.Controls.Add(this.openCamButton);
            this.Controls.Add(this.buttonAfMode);
            this.Controls.Add(this.afModeLabel);
            this.Controls.Add(this.focusLabel);
            this.Controls.Add(this.checkBoxAF);
            this.Controls.Add(this.buttonFocusAdd);
            this.Controls.Add(this.buttonFocusDec);
            this.Controls.Add(this.focusButton);
            this.Controls.Add(this.wbLabel);
            this.Controls.Add(this.checkBoxWB);
            this.Controls.Add(this.buttonWbAdd);
            this.Controls.Add(this.buttonWbDec);
            this.Controls.Add(this.CameraComboBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "MainForm";
            this.Text = "IPEVO SDK Demo";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ComboBox CameraComboBox;
        private System.Windows.Forms.Button buttonWbDec;
        private System.Windows.Forms.Button buttonWbAdd;
        private System.Windows.Forms.CheckBox checkBoxWB;
        private System.Windows.Forms.Label wbLabel;
        private System.Windows.Forms.Button focusButton;
        private System.Windows.Forms.Label focusLabel;
        private System.Windows.Forms.CheckBox checkBoxAF;
        private System.Windows.Forms.Button buttonFocusAdd;
        private System.Windows.Forms.Button buttonFocusDec;
        private System.Windows.Forms.Label afModeLabel;
        private System.Windows.Forms.Button buttonAfMode;
        private System.Windows.Forms.Button openCamButton;
    }
}

