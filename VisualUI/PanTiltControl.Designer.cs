namespace RobotArm
	{
	partial class PanTiltControl
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
			this.CtlGroupBox = new System.Windows.Forms.GroupBox();
			this.TButton = new System.Windows.Forms.Button();
			this.PosButton = new System.Windows.Forms.Button();
			this.IPButton = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.TiltNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.PanNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.CtlGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TiltNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.PanNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// CtlGroupBox
			// 
			this.CtlGroupBox.Controls.Add(this.TButton);
			this.CtlGroupBox.Controls.Add(this.PosButton);
			this.CtlGroupBox.Controls.Add(this.IPButton);
			this.CtlGroupBox.Controls.Add(this.label2);
			this.CtlGroupBox.Controls.Add(this.TiltNumericUpDown);
			this.CtlGroupBox.Controls.Add(this.label1);
			this.CtlGroupBox.Controls.Add(this.PanNumericUpDown);
			this.CtlGroupBox.Location = new System.Drawing.Point(4, 2);
			this.CtlGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.CtlGroupBox.Name = "CtlGroupBox";
			this.CtlGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.CtlGroupBox.Size = new System.Drawing.Size(224, 99);
			this.CtlGroupBox.TabIndex = 17;
			this.CtlGroupBox.TabStop = false;
			this.CtlGroupBox.Text = "Camera Pan/Tilt Control";
			// 
			// TButton
			// 
			this.TButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TButton.Location = new System.Drawing.Point(118, 71);
			this.TButton.Name = "TButton";
			this.TButton.Size = new System.Drawing.Size(72, 25);
			this.TButton.TabIndex = 21;
			this.TButton.Text = "Tilted";
			this.TButton.UseVisualStyleBackColor = true;
			this.TButton.Click += new System.EventHandler(this.TButton_Click);
			// 
			// PosButton
			// 
			this.PosButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.PosButton.Location = new System.Drawing.Point(76, 43);
			this.PosButton.Name = "PosButton";
			this.PosButton.Size = new System.Drawing.Size(72, 25);
			this.PosButton.TabIndex = 20;
			this.PosButton.Text = "Position";
			this.PosButton.UseVisualStyleBackColor = true;
			this.PosButton.Click += new System.EventHandler(this.PosButton_Click);
			// 
			// IPButton
			// 
			this.IPButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.IPButton.Location = new System.Drawing.Point(34, 70);
			this.IPButton.Name = "IPButton";
			this.IPButton.Size = new System.Drawing.Size(72, 25);
			this.IPButton.TabIndex = 19;
			this.IPButton.Text = "Flat";
			this.IPButton.UseVisualStyleBackColor = true;
			this.IPButton.Click += new System.EventHandler(this.IPButton_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(118, 21);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(26, 16);
			this.label2.TabIndex = 16;
			this.label2.Text = "Tilt";
			// 
			// TiltNumericUpDown
			// 
			this.TiltNumericUpDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TiltNumericUpDown.Location = new System.Drawing.Point(159, 18);
			this.TiltNumericUpDown.Maximum = new decimal(new int[] {
            2500,
            0,
            0,
            0});
			this.TiltNumericUpDown.Name = "TiltNumericUpDown";
			this.TiltNumericUpDown.Size = new System.Drawing.Size(55, 22);
			this.TiltNumericUpDown.TabIndex = 15;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(11, 21);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(32, 16);
			this.label1.TabIndex = 14;
			this.label1.Text = "Pan";
			// 
			// PanNumericUpDown
			// 
			this.PanNumericUpDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.PanNumericUpDown.Location = new System.Drawing.Point(52, 18);
			this.PanNumericUpDown.Maximum = new decimal(new int[] {
            2500,
            0,
            0,
            0});
			this.PanNumericUpDown.Name = "PanNumericUpDown";
			this.PanNumericUpDown.Size = new System.Drawing.Size(55, 22);
			this.PanNumericUpDown.TabIndex = 13;
			this.PanNumericUpDown.Tag = "";
			// 
			// PanTiltControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.CtlGroupBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "PanTiltControl";
			this.Size = new System.Drawing.Size(233, 106);
			this.CtlGroupBox.ResumeLayout(false);
			this.CtlGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TiltNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.PanNumericUpDown)).EndInit();
			this.ResumeLayout(false);

			}

		#endregion

		private System.Windows.Forms.GroupBox CtlGroupBox;
		private System.Windows.Forms.Button PosButton;
		private System.Windows.Forms.Button IPButton;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown TiltNumericUpDown;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown PanNumericUpDown;
		private System.Windows.Forms.Button TButton;
		}
	}
