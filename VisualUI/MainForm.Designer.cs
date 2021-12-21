
namespace VisualUI
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.StartButton = new System.Windows.Forms.Button();
			this.StartTextBox = new System.Windows.Forms.TextBox();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.ManualCntrl = new VisualUI.ManualControl();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.AutoOpsCntrl = new VisualUI.AutoOpsControl();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Bottom;
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(1349, 746);
			this.tabControl1.TabIndex = 0;
			this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
			this.tabControl1.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl1_Selecting);
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.StartButton);
			this.tabPage1.Controls.Add(this.StartTextBox);
			this.tabPage1.Location = new System.Drawing.Point(4, 4);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(1341, 717);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Start";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// StartButton
			// 
			this.StartButton.Location = new System.Drawing.Point(621, 137);
			this.StartButton.Name = "StartButton";
			this.StartButton.Size = new System.Drawing.Size(100, 25);
			this.StartButton.TabIndex = 3;
			this.StartButton.Text = "START";
			this.StartButton.UseVisualStyleBackColor = true;
			this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
			// 
			// StartTextBox
			// 
			this.StartTextBox.Location = new System.Drawing.Point(358, 284);
			this.StartTextBox.Multiline = true;
			this.StartTextBox.Name = "StartTextBox";
			this.StartTextBox.Size = new System.Drawing.Size(624, 295);
			this.StartTextBox.TabIndex = 2;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.ManualCntrl);
			this.tabPage2.Location = new System.Drawing.Point(4, 4);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Size = new System.Drawing.Size(1341, 720);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Manual";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// ManualCntrl
			// 
			this.ManualCntrl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ManualCntrl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ManualCntrl.Location = new System.Drawing.Point(0, 0);
			this.ManualCntrl.Name = "ManualCntrl";
			this.ManualCntrl.Size = new System.Drawing.Size(1341, 720);
			this.ManualCntrl.TabIndex = 0;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.AutoOpsCntrl);
			this.tabPage3.Location = new System.Drawing.Point(4, 4);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Size = new System.Drawing.Size(1341, 720);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Autonomous";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// AutoOpsCntrl
			// 
			this.AutoOpsCntrl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.AutoOpsCntrl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AutoOpsCntrl.Location = new System.Drawing.Point(0, 0);
			this.AutoOpsCntrl.Name = "AutoOpsCntrl";
			this.AutoOpsCntrl.Size = new System.Drawing.Size(1341, 720);
			this.AutoOpsCntrl.TabIndex = 0;
			// 
			// MainForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(1349, 746);
			this.Controls.Add(this.tabControl1);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "MainForm";
			this.Text = "Autonomous Robotic Arm Test Visual UI";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.tabPage2.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.ResumeLayout(false);

			}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.Button StartButton;
		private System.Windows.Forms.TextBox StartTextBox;
		private System.Windows.Forms.TabPage tabPage2;
		private ManualControl ManualCntrl;
		private System.Windows.Forms.TabPage tabPage3;
		private AutoOpsControl AutoOpsCntrl;
		}
	}

