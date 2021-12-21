using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using RobotArm;

namespace VisualUI
	{
	public partial class ManualControl : UserControl
		{
		public ManualControl()

		{
			InitializeComponent();
		}



		public bool Open()

		{
			bool rtn;

			rtn = CameraPanTilt.InitPosition();
			return(rtn);
		}
		


		public void Close()

		{

		}



		private void DisplayVideoImage(Image img)	

		{
			Graphics g;
			int row,col;

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



		private void ShootButton_Click(object sender, EventArgs e)

		{
			if (D415Camera.CaptureImages())
				{
				DisplayVideoImage((Image) Shared.vimg.Clone());
				LocGroupBox.Enabled = true;
				StatusTextBox.AppendText("Images captured.\r\n");
				}
			else
				{
				StatusTextBox.AppendText("Attempt to capture images failed.\r\n");
				}
		}



		private void SaveButton_Click(object sender, EventArgs e)

		{
			try 
			{
			Shared.SaveVideoPic(Shared.vimg);
			Shared.SaveDeptBin();
			StatusTextBox.AppendText("Images saved.");
			}

			catch(Exception ex)
			{
			StatusTextBox.AppendText("SaveButton exception: " + ex.Message + "\r\n");
			Log.LogEntry("SaveButton exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}
		}



		private void HomeButton_Click(object sender, EventArgs e)
			
		{
			if (ArmControl.Home())
				{
				StatusTextBox.AppendText("Arm homed.\r\n");
				ParkButton.Enabled = true;
				RotNumericUpDown.Value = 0;
				}
			else
				{ 
				StatusTextBox.AppendText("Could not home arm.\r\n");
				if (!ArmControl.cmd_error)
					{
					StatusTextBox.AppendText("Not command error\r\n");
					}
				}
		}



		private void ParkButton_Click(object sender, EventArgs e)

		{
			if (ArmControl.Park())
				{
				StatusTextBox.AppendText("Arm parked.\r\n");
				ParkButton.Enabled = true;
				RotNumericUpDown.Value = 0;
				}
			else
				{
				StatusTextBox.AppendText("Could not park arm.\r\n");
				if (!ArmControl.cmd_error)
					{
					StatusTextBox.AppendText("Not command error.\r\n");
					}
				}
		}



		private void PosButton_Click(object sender, EventArgs e)

		{
			double x,y,z;
			ArmControl.gripper_orient go;

			x = (double) XNumericUpDown.Value;
			y = (double) YNumericUpDown.Value;
			z = (double) ZNumericUpDown.Value;
			if (FlatRadioButton.Checked)
				go = ArmControl.gripper_orient.FLAT;
			else
				go = ArmControl.gripper_orient.DOWN;
			if (ArmControl.Move((int) Math.Round(x * Shared.IN_TO_MM),(int) Math.Round(y * Shared.IN_TO_MM),(int) Math.Round(z * Shared.IN_TO_MM),go,Shared.gripper_rot))
				{
				StatusTextBox.AppendText("Arm positioned.\r\n");
				ParkButton.Enabled = true;
				}
			else
				{
				StatusTextBox.AppendText("Arm positioning failed.\r\n");
				if (!ArmControl.cmd_error)
					{
					StatusTextBox.AppendText("Not command error\r\n");
					}
				}
		}



		private void RowCol_ValueChanged(object sender, EventArgs e)

		{
			if (Shared.vimg != null)
				DisplayVideoImage((Image)Shared.vimg.Clone());
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
					SAPButton.Enabled = true;
					CalButton.Enabled = true;
					}
				else
					RCLocTextBox.Text = "could not determine location";
				}
			else
				CCLocTextBox.Text = "could not determine location";
		}



		private void OGButton_Click(object sender, EventArgs e)
			
		{
			if (ArmControl.Gripper(ArmControl.GRIPPER_FULL_OPEN))
				StatusTextBox.AppendText("gripper opened.\r\n");
			else
				StatusTextBox.AppendText("gripper open failed.\r\n");
		}



		private void CGButton_Click(object sender, EventArgs e)

		{
			ArmControl.CloseGrip(0,StatusTextBox);
			StatusTextBox.AppendText("gripper closed.\r\n");
			if (ArmControl.ReadGripFSR() < ArmControl.MAX_GRIP_VALUE)
				StatusTextBox.AppendText("gripper does not hold anything.\r\n");
		}



		private void GISButton_Click(object sender, EventArgs e)
			
		{
			int space;

			space = (int) GISNumericUpDown.Value;
			if ((space <= ArmControl.GRIPPER_FULL_OPEN) && (space >= ArmControl.GRIPPER_FULL_CLOSE))
				if (ArmControl.Gripper(space))
					StatusTextBox.AppendText("Gripper moved.");
				else
					StatusTextBox.AppendText("Move failed.");
			else
				StatusTextBox.AppendText("Value not within range.");
		}



		private void RotButton_Click(object sender, EventArgs e)

		{
			double angle;

			angle = (double) RotNumericUpDown.Value;
			if (Math.Abs(angle) <= 200)
				if (ArmControl.GripperRotate(angle))
					StatusTextBox.AppendText("Gripper rotated.\r\n");
				else
					StatusTextBox.AppendText("Gripper rotation failed.\r\n");
			else
				StatusTextBox.AppendText("Gripper rotation not in range.\r\n");
		}



		private void WTButton_Click(object sender, EventArgs e)

		{
			int tangle;
			double cangle = 0;

			if (ArmControl.ReadJoint(ArmControl.WRIST_TILT,ref cangle))
				{
				tangle = (int) WTNumericUpDown.Value;
				tangle = (int) (cangle + tangle);
				ArmControl.MoveJoint(ArmControl.WRIST_TILT,tangle);
				}
		}



		private void MapButton_Click(object sender, EventArgs e)
		
		{
			Stopwatch sw = new Stopwatch();
			string msg;

			sw.Start();
			Mapping.MapWorkSpace(0);
			sw.Stop();
			msg = "Work space mapping took " + sw.ElapsedMilliseconds + " msec";
			Mapping.SaveWorkspaceMap(msg);
			StatusTextBox.AppendText(msg + "\r\n");
		}



		private void SAPButton_Click(object sender, EventArgs e)

		{
			int offset;
			double angle,hypo,x,y,z;
			bool flat;

			offset = (int) PONumericUpDown.Value;
			flat = FlatRadioButton.Checked;
			x = Shared.rcloc.x;
			y = Shared.rcloc.y;
			z = Shared.rcloc.z;
			if (flat && offset > 0)
				{
				angle = Math.Atan(x/z);
				hypo = Math.Sqrt((y * y) + (z * z));
				hypo -= offset;
				y = (int) Math.Round(hypo * Math.Cos(angle));
				z = (int) Math.Round(hypo * Math.Sin(angle));
				}
			else if (!flat)			
				{
				y += offset * Shared.IN_TO_MM;
				}
			if (y < 0)
				y = 0;
			XNumericUpDown.Value = (decimal) (x * Shared.MM_TO_IN);
			YNumericUpDown.Value = (decimal) (y * Shared.MM_TO_IN);
			ZNumericUpDown.Value = (decimal) (z * Shared.MM_TO_IN);
		}



		private void CalButton_Click(object sender, EventArgs e)

		{
			int row,col;
			Stopwatch sw = new Stopwatch();

			row = (int) RowNumericUpDown.Value;
			col = (int) ColNumericUpDown.Value;
			sw.Start();
			Mapping.Calibrate(row,col);
			sw.Stop();
			StatusTextBox.AppendText("Calibration time: " + sw.ElapsedMilliseconds + " msec\r\n");
			StatusTextBox.AppendText("Avg height: " + Mapping.fmr.avgh + " mm   Max height: " + Mapping.fmr.maxh + " mm   Min Height: " +  Mapping.fmr.minh + " mm\r\n");
			StatusTextBox.AppendText("Average absolute error: " + Mapping.fmr.avg_abs_err + "mm\r\n");
		}

		}
	}
