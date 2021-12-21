using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RobotArm;

namespace VisualUI
	{
	public partial class MainForm : Form
		{

		private int current_tab = 0;
		private bool initialized = false;
		private bool speech_avail = false;


		public MainForm()

		{
			DateTime now = DateTime.Now;
			string fname;
			DateTime fdt;

			InitializeComponent();
			Shared.app_time.Start();
			Log.OpenLog("arm control log" + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + Shared.TEXT_TILE_EXT, true);
			fname = Application.StartupPath + "\\" + Application.ProductName + ".exe";
			fdt = File.GetLastWriteTime(fname);
			this.Text += " " + fdt.ToShortDateString() + " " + fdt.ToShortTimeString();
		}



		private void StartButton_Click(object sender, EventArgs e)

		{
			StartTextBox.AppendText("Initializing the robotic arm's core systems. This may take a couple of minutes.\r\n");
			if (ArmControl.InitArm())
				{
				StartTextBox.AppendText("Robotic arm is operational.\r\n");

				try
				{
				if (D415Camera.COpen())
					{
					StartTextBox.AppendText("Camera is operational\r\n");
					if (CameraPanTilt.Open())
						{
						StartTextBox.AppendText("Camera pan and tilt is operational\r\n");
						initialized = true;
						if (VisualObjectDetection.Open())
							StartTextBox.AppendText("Visual object detection is operational\r\n");
						else
							StartTextBox.AppendText("Could not initialize visual object detection.\r\n");
						if (ImageSegmentation.Open())
							StartTextBox.AppendText("Image segmentation is operational\r\n");
						else
							StartTextBox.AppendText("Could not initialize image segmentation\r\n");
						if (Speech.PvSpeech.StartTTS() && Speech.PvSpeech.StartSpeechRecognition())
							{
							StartTextBox.AppendText("Speech is operational.\r\nTabs are now enabled.\r\n");
							speech_avail = true;
							Speech.PvSpeech.SpeakAsync("Tabs are now enabled.");
							}
						else
							StartTextBox.AppendText("Could not initialize speech.\r\n");
						}
					else
						{
						ArmControl.Close();
						D415Camera.CClose();
						StartTextBox.AppendText("Could not initialize camera pan and tilt\r\n");
						tabControl1.Enabled = false;
						}
					}
				else
					{
					ArmControl.Close();
					StartTextBox.AppendText("Could not initialize camera.\r\n");
					tabControl1.Enabled = false;
					}
				}

			catch (Exception ex)
			{
			Log.LogEntry("Exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			}
		else
			{
			StartTextBox.AppendText("Could not initialize arm control.\r\n");
			tabControl1.Enabled = false;
			}
		}


		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		
		{
			Log.CloseLog();
			if (tabControl1.Enabled)
				{
				switch (current_tab)
					{
					case 0:
						break;

					case 1:
						ManualCntrl.Close();
						break;

					case 2:
						AutoOpsCntrl.Close();
						break;

					}
				initialized = false;
				ArmControl.Close();
				D415Camera.CClose();
				CameraPanTilt.Close();
				if (speech_avail)
					Speech.PvSpeech.StopSpeechRecognition();
				if (VisualObjectDetection.Initialized())
					VisualObjectDetection.Close();
				if (ImageSegmentation.Initialized())
					ImageSegmentation.Close();
				}

		}



		private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)

		{
			if (initialized)
				{
				switch (current_tab)
					{
					case 0:
						break;

					case 1:
						ManualCntrl.Close();
						break;

					case 2:
						AutoOpsCntrl.Close();
						break;

					}
				current_tab = tabControl1.SelectedIndex;
				switch (current_tab)
					{
					case 0:
						break;

					case 1:
						ManualCntrl.Open();
						break;

					case 2:
						AutoOpsCntrl.Open();
						break;
					}
				}
		}



		private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
		
		{
			if (!initialized)
				e.Cancel = true;
			else if ((e.TabPageIndex == 2) && (!speech_avail || !VisualObjectDetection.Initialized() || !ImageSegmentation.Initialized()))
				e.Cancel = true;
		}


		}
	}
