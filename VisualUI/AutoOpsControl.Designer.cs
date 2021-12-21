
namespace VisualUI
	{
	partial class AutoOpsControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
			{
			this.VideoPictureBox = new System.Windows.Forms.PictureBox();
			this.LocGroupBox = new System.Windows.Forms.GroupBox();
			this.ShootButton = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.CalButton = new System.Windows.Forms.Button();
			this.RCLocTextBox = new System.Windows.Forms.TextBox();
			this.CCLocTextBox = new System.Windows.Forms.TextBox();
			this.MeasureButton = new System.Windows.Forms.Button();
			this.ColNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label10 = new System.Windows.Forms.Label();
			this.RowNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label11 = new System.Windows.Forms.Label();
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.DomainsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.label1 = new System.Windows.Forms.Label();
			this.StopButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).BeginInit();
			this.LocGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ColNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.RowNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// VideoPictureBox
			// 
			this.VideoPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.VideoPictureBox.Location = new System.Drawing.Point(5, 7);
			this.VideoPictureBox.Name = "VideoPictureBox";
			this.VideoPictureBox.Size = new System.Drawing.Size(1067, 600);
			this.VideoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.VideoPictureBox.TabIndex = 33;
			this.VideoPictureBox.TabStop = false;
			// 
			// LocGroupBox
			// 
			this.LocGroupBox.Controls.Add(this.ShootButton);
			this.LocGroupBox.Controls.Add(this.label7);
			this.LocGroupBox.Controls.Add(this.label6);
			this.LocGroupBox.Controls.Add(this.CalButton);
			this.LocGroupBox.Controls.Add(this.RCLocTextBox);
			this.LocGroupBox.Controls.Add(this.CCLocTextBox);
			this.LocGroupBox.Controls.Add(this.MeasureButton);
			this.LocGroupBox.Controls.Add(this.ColNumericUpDown);
			this.LocGroupBox.Controls.Add(this.label10);
			this.LocGroupBox.Controls.Add(this.RowNumericUpDown);
			this.LocGroupBox.Controls.Add(this.label11);
			this.LocGroupBox.Location = new System.Drawing.Point(1079, 7);
			this.LocGroupBox.Name = "LocGroupBox";
			this.LocGroupBox.Size = new System.Drawing.Size(255, 199);
			this.LocGroupBox.TabIndex = 36;
			this.LocGroupBox.TabStop = false;
			this.LocGroupBox.Text = "Vision Calibration";
			// 
			// ShootButton
			// 
			this.ShootButton.Location = new System.Drawing.Point(63, 43);
			this.ShootButton.Name = "ShootButton";
			this.ShootButton.Size = new System.Drawing.Size(129, 28);
			this.ShootButton.TabIndex = 23;
			this.ShootButton.Text = "Shoot";
			this.ShootButton.UseVisualStyleBackColor = true;
			this.ShootButton.Click += new System.EventHandler(this.ShootButton_Click);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(27, 168);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(27, 16);
			this.label7.TabIndex = 22;
			this.label7.Text = "RC";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(26, 142);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(26, 16);
			this.label6.TabIndex = 21;
			this.label6.Text = "CC";
			// 
			// CalButton
			// 
			this.CalButton.Enabled = false;
			this.CalButton.Location = new System.Drawing.Point(63, 107);
			this.CalButton.Name = "CalButton";
			this.CalButton.Size = new System.Drawing.Size(129, 28);
			this.CalButton.TabIndex = 20;
			this.CalButton.Text = "Calibrate";
			this.CalButton.UseVisualStyleBackColor = true;
			this.CalButton.Click += new System.EventHandler(this.CalButton_Click);
			// 
			// RCLocTextBox
			// 
			this.RCLocTextBox.Location = new System.Drawing.Point(55, 165);
			this.RCLocTextBox.Name = "RCLocTextBox";
			this.RCLocTextBox.Size = new System.Drawing.Size(173, 22);
			this.RCLocTextBox.TabIndex = 14;
			// 
			// CCLocTextBox
			// 
			this.CCLocTextBox.Location = new System.Drawing.Point(55, 139);
			this.CCLocTextBox.Name = "CCLocTextBox";
			this.CCLocTextBox.Size = new System.Drawing.Size(173, 22);
			this.CCLocTextBox.TabIndex = 12;
			// 
			// MeasureButton
			// 
			this.MeasureButton.Enabled = false;
			this.MeasureButton.Location = new System.Drawing.Point(63, 75);
			this.MeasureButton.Name = "MeasureButton";
			this.MeasureButton.Size = new System.Drawing.Size(129, 28);
			this.MeasureButton.TabIndex = 6;
			this.MeasureButton.Text = "Measure";
			this.MeasureButton.UseVisualStyleBackColor = true;
			this.MeasureButton.Click += new System.EventHandler(this.MeasureButton_Click);
			// 
			// ColNumericUpDown
			// 
			this.ColNumericUpDown.Location = new System.Drawing.Point(168, 17);
			this.ColNumericUpDown.Maximum = new decimal(new int[] {
            1280,
            0,
            0,
            0});
			this.ColNumericUpDown.Name = "ColNumericUpDown";
			this.ColNumericUpDown.Size = new System.Drawing.Size(55, 22);
			this.ColNumericUpDown.TabIndex = 3;
			this.ColNumericUpDown.Value = new decimal(new int[] {
            640,
            0,
            0,
            0});
			this.ColNumericUpDown.ValueChanged += new System.EventHandler(this.RowCol_ValueChanged);
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(136, 19);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(28, 16);
			this.label10.TabIndex = 2;
			this.label10.Text = "Col";
			// 
			// RowNumericUpDown
			// 
			this.RowNumericUpDown.Location = new System.Drawing.Point(70, 17);
			this.RowNumericUpDown.Maximum = new decimal(new int[] {
            720,
            0,
            0,
            0});
			this.RowNumericUpDown.Name = "RowNumericUpDown";
			this.RowNumericUpDown.Size = new System.Drawing.Size(55, 22);
			this.RowNumericUpDown.TabIndex = 1;
			this.RowNumericUpDown.Value = new decimal(new int[] {
            360,
            0,
            0,
            0});
			this.RowNumericUpDown.ValueChanged += new System.EventHandler(this.RowCol_ValueChanged);
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(31, 20);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(35, 16);
			this.label11.TabIndex = 0;
			this.label11.Text = "Row";
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.Location = new System.Drawing.Point(1079, 212);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(255, 395);
			this.StatusTextBox.TabIndex = 37;
			// 
			// DomainsFlowLayoutPanel
			// 
			this.DomainsFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DomainsFlowLayoutPanel.AutoScroll = true;
			this.DomainsFlowLayoutPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.DomainsFlowLayoutPanel.Location = new System.Drawing.Point(13, 660);
			this.DomainsFlowLayoutPanel.Name = "DomainsFlowLayoutPanel";
			this.DomainsFlowLayoutPanel.Size = new System.Drawing.Size(1315, 44);
			this.DomainsFlowLayoutPanel.TabIndex = 38;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 641);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(132, 16);
			this.label1.TabIndex = 39;
			this.label1.Text = "Autonomus Domains";
			// 
			// StopButton
			// 
			this.StopButton.BackColor = System.Drawing.Color.Red;
			this.StopButton.Enabled = false;
			this.StopButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StopButton.Location = new System.Drawing.Point(606, 616);
			this.StopButton.Name = "StopButton";
			this.StopButton.Size = new System.Drawing.Size(140, 35);
			this.StopButton.TabIndex = 40;
			this.StopButton.Text = "STOP";
			this.StopButton.UseVisualStyleBackColor = false;
			this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
			// 
			// AutoOpsControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.StopButton);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.DomainsFlowLayoutPanel);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.LocGroupBox);
			this.Controls.Add(this.VideoPictureBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "AutoOpsControl";
			this.Size = new System.Drawing.Size(1340, 713);
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).EndInit();
			this.LocGroupBox.ResumeLayout(false);
			this.LocGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.ColNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.RowNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.PictureBox VideoPictureBox;
		private System.Windows.Forms.GroupBox LocGroupBox;
		private System.Windows.Forms.Button ShootButton;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Button CalButton;
		private System.Windows.Forms.TextBox RCLocTextBox;
		private System.Windows.Forms.TextBox CCLocTextBox;
		private System.Windows.Forms.Button MeasureButton;
		private System.Windows.Forms.NumericUpDown ColNumericUpDown;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.NumericUpDown RowNumericUpDown;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.FlowLayoutPanel DomainsFlowLayoutPanel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button StopButton;
		}
	}
