
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
            this.openCamButton = new System.Windows.Forms.Button();
            this.CameraComboBox = new System.Windows.Forms.ComboBox();
            this.deviceInfoLabel = new System.Windows.Forms.Label();
            this.mainTabControl = new System.Windows.Forms.TabControl();
            this.tabPage3A = new System.Windows.Forms.TabPage();
            this.tabPageImage = new System.Windows.Forms.TabPage();
            this.tabPageFunctions = new System.Windows.Forms.TabPage();
            this.logTextBox = new System.Windows.Forms.TextBox();
            this.mainTabControl.SuspendLayout();
            this.SuspendLayout();
            //
            // openCamButton
            //
            this.openCamButton.Location = new System.Drawing.Point(12, 12);
            this.openCamButton.Name = "openCamButton";
            this.openCamButton.Size = new System.Drawing.Size(150, 28);
            this.openCamButton.TabIndex = 0;
            this.openCamButton.Text = "Open Capture App";
            this.openCamButton.UseVisualStyleBackColor = true;
            this.openCamButton.Click += new System.EventHandler(this.OpenCamButton_Click);
            //
            // CameraComboBox
            //
            this.CameraComboBox.Enabled = false;
            this.CameraComboBox.FormattingEnabled = true;
            this.CameraComboBox.Location = new System.Drawing.Point(175, 15);
            this.CameraComboBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.CameraComboBox.Name = "CameraComboBox";
            this.CameraComboBox.Size = new System.Drawing.Size(350, 23);
            this.CameraComboBox.TabIndex = 1;
            this.CameraComboBox.SelectedIndexChanged += new System.EventHandler(this.CameraComboBox_SelectedIndexChanged);
            //
            // deviceInfoLabel
            //
            this.deviceInfoLabel.AutoSize = true;
            this.deviceInfoLabel.Location = new System.Drawing.Point(12, 48);
            this.deviceInfoLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.deviceInfoLabel.Name = "deviceInfoLabel";
            this.deviceInfoLabel.Size = new System.Drawing.Size(115, 15);
            this.deviceInfoLabel.TabIndex = 2;
            this.deviceInfoLabel.Text = "No camera selected";
            //
            // mainTabControl
            //
            this.mainTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainTabControl.Controls.Add(this.tabPage3A);
            this.mainTabControl.Controls.Add(this.tabPageImage);
            this.mainTabControl.Controls.Add(this.tabPageFunctions);
            this.mainTabControl.Location = new System.Drawing.Point(12, 72);
            this.mainTabControl.Name = "mainTabControl";
            this.mainTabControl.SelectedIndex = 0;
            this.mainTabControl.Size = new System.Drawing.Size(966, 360);
            this.mainTabControl.TabIndex = 3;
            //
            // tabPage3A
            //
            this.tabPage3A.AutoScroll = true;
            this.tabPage3A.Name = "tabPage3A";
            this.tabPage3A.Text = "3A (AE / AWB / AF)";
            this.tabPage3A.UseVisualStyleBackColor = true;
            //
            // tabPageImage
            //
            this.tabPageImage.AutoScroll = true;
            this.tabPageImage.Name = "tabPageImage";
            this.tabPageImage.Text = "Image Adjustment";
            this.tabPageImage.UseVisualStyleBackColor = true;
            //
            // tabPageFunctions
            //
            this.tabPageFunctions.AutoScroll = true;
            this.tabPageFunctions.Name = "tabPageFunctions";
            this.tabPageFunctions.Text = "Functions";
            this.tabPageFunctions.UseVisualStyleBackColor = true;
            //
            // logTextBox
            //
            this.logTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logTextBox.Location = new System.Drawing.Point(12, 444);
            this.logTextBox.Multiline = true;
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logTextBox.Size = new System.Drawing.Size(966, 293);
            this.logTextBox.TabIndex = 4;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(990, 749);
            this.Controls.Add(this.logTextBox);
            this.Controls.Add(this.mainTabControl);
            this.Controls.Add(this.deviceInfoLabel);
            this.Controls.Add(this.CameraComboBox);
            this.Controls.Add(this.openCamButton);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "MainForm";
            this.Text = "IPEVO SDK Demo";
            this.mainTabControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        /// <summary>
        /// Button that opens the Windows built-in camera app for verifying SDK operations visually.
        /// </summary>
        private System.Windows.Forms.Button openCamButton;

        /// <summary>
        /// Combo box listing the instance names of all connected IPEVO cameras.
        /// </summary>
        private System.Windows.Forms.ComboBox CameraComboBox;

        /// <summary>
        /// Label showing model, PID and firmware version of the selected camera.
        /// </summary>
        private System.Windows.Forms.Label deviceInfoLabel;

        /// <summary>
        /// Tab control that groups the camera property rows into three pages.
        /// </summary>
        private System.Windows.Forms.TabControl mainTabControl;

        /// <summary>
        /// Tab page holding the 3A rows (exposure, white balance, focus).
        /// </summary>
        private System.Windows.Forms.TabPage tabPage3A;

        /// <summary>
        /// Tab page holding the UVC image adjustment rows.
        /// </summary>
        private System.Windows.Forms.TabPage tabPageImage;

        /// <summary>
        /// Tab page holding the device function rows (filter, zoom, rotate, light, power line).
        /// </summary>
        private System.Windows.Forms.TabPage tabPageFunctions;

        /// <summary>
        /// Read-only message box at the bottom that shows the inner log of CameraKit.Core.
        /// </summary>
        private System.Windows.Forms.TextBox logTextBox;
    }
}
