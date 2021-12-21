using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using RobotArm;
using Speech;


namespace VisualUI
	{
	public partial class AutoOpsControl : UserControl,AutoOpInterface
		{

		private const int CROP_SIZE = 640;  //since only concerned with an object in the work space, crop and shift picture from D415 camera to get best inference results
		private const int SHIFT_RIGHT = 100;
		private const double CAL_X_CORRECT = 3.4;
		private const double CAL_Y_CORRECT = 3.9;


		private DomainInterface di = null;
		public delegate void VideoUpdate(Image img);
		private VideoUpdate vu;
		public delegate void TextUpdate(string msg);
		private TextUpdate tu;
		private string calibrate_model;


		public AutoOpsControl()

		{
			InitializeComponent();
			vu = VideoOutput;
			tu = TextOutput;
		}



		public bool Open()

		{
			bool rtn = false;
			string[] files;

			if (Shared.TENSORFLOW)
				calibrate_model = "calmodel.csv";
			else
#pragma warning disable CS0162 // Unreachable code detected
				calibrate_model = "ovcalmodel.csv";
#pragma warning restore CS0162 // Unreachable code detected
			CameraPanTilt.TiltedPosition();
			Log.KeyLogEntry("Opening autonomous scenario control");
			StopButton.Enabled = false;
			if (!Shared.vision_calibrated)
				{
				TextOutput("Vision needs to be calibrated before autonomous scenarios are enabled.  This will take up to a  minute.");
				Speech.PvSpeech.Speak("Vision needs to be calibrated before autonomous scenarios are enabled.  This will take up to a minute.");
				if (!AutoCalibrate())
					{
					TextOutput("Automatic calibration failed. You will need to manually calibrate.");
					Speech.PvSpeech.SpeakAsync("Automatic calibration failed. You will need to manually calibrate.");
					DomainsFlowLayoutPanel.Enabled = false;
					}
				else
					DomainsFlowLayoutPanel.Enabled = true;
				}
			else
				DomainsFlowLayoutPanel.Enabled = true;
			if (AddDomains())
				{
				files = Directory.GetFiles(Shared.base_path + Shared.CAL_SUB_DIR, "*.rhn");
				if (PvSpeech.SetContext(files[0]) && PvSpeech.RegisterHandler(SpeechHandler))
					TextOutput("Verbal domain command accepted.");
				}
			rtn = true;
			return (rtn);
		}



		public void Close()

		{
		}



		public void TextOutput(string msg)

		{
			if (this.InvokeRequired)
				this.BeginInvoke(tu,msg);
			else
				{
				StatusTextBox.AppendText(msg + "\r\n");
				Log.LogEntry(msg);
				}
		}



		public void VideoOutput(Image img)	

		{
			Graphics g;
			int row,col;

			if (this.InvokeRequired)
				this.BeginInvoke(vu,img);
			else
				{

				try 
				{
				if (img != null)
					{
					VideoPictureBox.Image = img;
					g = System.Drawing.Graphics.FromImage(VideoPictureBox.Image);
					row = (int)RowNumericUpDown.Value;
					g.DrawLine(Pens.Red, 0, row,img.Width, row);
					col = (int)ColNumericUpDown.Value;
					g.DrawLine(Pens.Red, col, 0, col, img.Height);
					}
				}

				catch(AccessViolationException)

				{
				}

				catch (Exception ex)
				{
				StatusTextBox.AppendText("DisplayVideoImage exception: " + ex.Message + "\r\n");
				Log.LogEntry("DisplayVideoImage exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				}
		}



		private void SpeechHandler(string msg)

		{
			if (msg == "DynamicWorkAssist")
				{
				OpenDomain(msg);
				if (di == null)
					TextOutput("Failed to open " + msg);
				}
		}



		private bool AutoCalibrate()

		{
			bool rtn = false;
			ArrayList al;
			Bitmap bm;
			VisualObjectDetection.visual_detected_object vdo;
			int row,col;
			string dir,fname,line,nline;
			TextReader tr;
			string[] data;

			dir = Shared.base_path + Shared.CAL_SUB_DIR;
			fname = dir + calibrate_model;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				while ((line = tr.ReadLine()) != null)
					{
					data = line.Split(',');
					if (data.Length == 3)
						{
						nline = data[0] + "," + dir + "," + data[1] + "," + data[2];
						if (VisualObjectDetection.Load(nline))
							{
							D415Camera.CaptureImages();	//gives camera a change to adjust to lighting
							D415Camera.CaptureImages();
							D415Camera.CaptureImages();
							if (D415Camera.CaptureImages())
								{
								VideoOutput((Image) Shared.vimg.Clone());
								bm = VisualObjectDetection.CropPicture((Bitmap)Shared.vimg,CROP_SIZE,SHIFT_RIGHT);
								al = Shared.Detect(data[0], bm,.6,1);
								if (al.Count == 1)
									{
									vdo = (VisualObjectDetection.visual_detected_object)al[0];
									vdo = VisualObjectDetection.UnCropVDO(vdo, CROP_SIZE, SHIFT_RIGHT);
									col = (int) Math.Round((double) vdo.x + CAL_X_CORRECT + (vdo.width/2));
									row = (int) Math.Round((double) vdo.y + CAL_Y_CORRECT + (vdo.height/2));
									RowNumericUpDown.Value = row;
									ColNumericUpDown.Value = col;
									MeasureButton_Click(null,null);
									CalButton_Click(null,null);
									if (DomainsFlowLayoutPanel.Enabled == true)
										rtn = true;
									Application.DoEvents();
									Shared.SaveVideoPic(VideoPictureBox.Image);
									}
								else
									TextOutput("Could not detect the calibration point.");
								}
							else
								TextOutput("Could not capture image.");
							VisualObjectDetection.Unload();
							}
						else
							TextOutput("Could not load calibrate OD model.");
						}
					else
						TextOutput("Calibrate model CSV has wrong format.");
					}
				}
			else
				TextOutput("Could not locate calibrate model CSV");
			return (rtn);
		}



		private void ShootButton_Click(object sender, EventArgs e)

		{
			if (D415Camera.CaptureImages())
				{
				VideoOutput((Image) Shared.vimg.Clone());
				LocGroupBox.Enabled = true;
				MeasureButton.Enabled = true;
				StatusTextBox.AppendText("Images captured.\r\n");
				}
			else
				{
				StatusTextBox.AppendText("Attempt to capture images failed.\r\n");
				}
		}



		private void RowCol_ValueChanged(object sender, EventArgs e)

		{
			if (Shared.vimg != null)
				VideoOutput((Image)Shared.vimg.Clone());
		}



		private void MeasureButton_Click(object sender, EventArgs e)

		{
			int row,col;
			Shared.space_3d_mm ccloc = new Shared.space_3d_mm();

			row = (int) RowNumericUpDown.Value;
			col = (int) ColNumericUpDown.Value;
			if (D415Camera.DetermineLocCC(row,col,CameraPanTilt.tilt_deg,ref ccloc))
				{
				StatusTextBox.AppendText("Hor " + D415Camera.ha.ToString("F2") + "  Vert " + D415Camera.va.ToString("F2") + "\r\n");
				CCLocTextBox.Text = (ccloc.x * Shared.MM_TO_IN).ToString("F2") + ", " + (ccloc.y *Shared.MM_TO_IN).ToString("F2") + ", " + (ccloc.z * Shared.MM_TO_IN).ToString("F2");
				if (Shared.ConvertCC_to_RC(ccloc,ref Shared.rcloc, CameraPanTilt.tilt_deg))
					{
					RCLocTextBox.Text = (Shared.rcloc.x * Shared.MM_TO_IN).ToString("F2") + ", " + (Shared.rcloc.y * Shared.MM_TO_IN).ToString("F2") + ", " + (Shared.rcloc.z * Shared.MM_TO_IN).ToString("F2");
					CalButton.Enabled = true;
					}
				else
					RCLocTextBox.Text = "could not determine location";
				}
			else
				CCLocTextBox.Text = "could not determine location";
		}



		private void CalButton_Click(object sender, EventArgs e)

		{
			int row,col;
			Stopwatch sw = new Stopwatch();

			row = (int) RowNumericUpDown.Value;
			col = (int) ColNumericUpDown.Value;
			sw.Start();
			TextOutput("Running vision calibration.");
			Application.DoEvents();
			Mapping.Calibrate(row,col);
			sw.Stop();
			StatusTextBox.AppendText("Calibration time: " + sw.ElapsedMilliseconds + " msec\r\n");
			StatusTextBox.AppendText("Avg height: " + Mapping.fmr.avgh.ToString("F2") + " mm   Max height: " + Mapping.fmr.maxh + " mm   Min Height: " +  Mapping.fmr.minh + " mm\r\n");
			StatusTextBox.AppendText("Average absolute error: " + Mapping.fmr.avg_abs_err.ToString("F2") + "mm\r\n");
			Speech.PvSpeech.SpeakAsync("Autonomous scenarios are now available.");
			DomainsFlowLayoutPanel.Enabled = true;
		}



		private bool AddDomains()

		{
			ArrayList domains;
			int i;
			Button b;
			string fname;
			bool rtn = false;

			DomainsFlowLayoutPanel.Controls.Clear();
			domains = Domains.ListDomains();
			for (i = 0;i < domains.Count;i++)
				{
				b = new Button();
				b.Name = "button" + i;
				fname = (string) domains[i];
				b.Text = fname.Substring(fname.LastIndexOf('\\') + 1);
				b.Height = 28;
				b.Width = 10 * b.Text.Length;
				b.FlatStyle = FlatStyle.Flat;
				DomainsFlowLayoutPanel.Controls.Add(b);
				b.Click += Domain_Click;
				}
			if (domains.Count > 0)
				rtn = true;
			return(rtn);
		}



		private void OpenDomain(string text)

		{
			string type_name, dir_name;

			if (di != null)
				di.Close();
			dir_name = Application.StartupPath + Shared.DOMAIN_DIR + text + "\\";
			type_name = text.Replace(' ', '_');
			type_name = type_name.Replace('-', '_');
			type_name += ".Domain";
			di = Domains.OpenDomain(dir_name,type_name,this);
			if (di == null)
				TextOutput("Domain open failed.");
		}


		private void Domain_Click(object sender, EventArgs e)

		{

			StopButton.Enabled = false;
			OpenDomain(((Button)sender).Text);
			if (di == null)
				TextOutput("Failed to open " + ((Button)sender).Text);
			else
				StopButton.Enabled = true;
		}



		public void OpDone()

		{
			string[] files;

			if (di != null)
				{
				di.Close();
				di = null;
				files = Directory.GetFiles(Shared.base_path + Shared.CAL_SUB_DIR, "*.rhn");
				PvSpeech.SetContext(files[0]);
				PvSpeech.RegisterHandler(SpeechHandler);
				}
		}



		private void StopButton_Click(object sender, EventArgs e)

		{
			di.Stop();
		}

		}
	}
