using System;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;

namespace RobotArm
	{
	public partial class PanTiltControl : UserControl
		{

		public PanTiltControl()

		{
			InitializeComponent();
		}


		public void SetPanTiltDisplay(int pan,int tilt)

		{
			PanNumericUpDown.Value = pan;
			TiltNumericUpDown.Value = tilt;
		}


		private void PosButton_Click(object sender, EventArgs e)

		{
			CameraPanTilt.Position((int) PanNumericUpDown.Value,(int) TiltNumericUpDown.Value);
		}



		private void IPButton_Click(object sender, EventArgs e)

		{
			CameraPanTilt.InitPosition();
			PanNumericUpDown.Value = CameraPanTilt.pan_center_pt;
			TiltNumericUpDown.Value = CameraPanTilt.tilt_center_pt;
		}



		private void TButton_Click(object sender, EventArgs e)

		{
			CameraPanTilt.TiltedPosition();
			PanNumericUpDown.Value = CameraPanTilt.pan_center_pt;
			TiltNumericUpDown.Value = (decimal) CameraPanTilt.last_tilt_pwm;
		}

		}
	}
