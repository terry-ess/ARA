using System;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;

namespace RobotArm
	{
	static public class CameraPanTilt
		{

		private const int PAN_CENTER = 1450;
		private const int TILT_CENTER = 1495;
		private const int TILT_59 = 935;
		public const int PAN_CHANNEL = 0;
		public const int TILT_CHANNEL = 2;
		public const int D10_PWM = 100;
		public const int D10_MS = 500;

		public const string SERVO_PARAM_FILE = "servo.param";

		static private SerialPort sp = new SerialPort();
		static public int last_tilt_pwm = -1;
		static private int last_pan_pwm = -1;
		static private string port_name;
		static private int tilt_59 = TILT_59;
		static private double tilt_pwm_deg = ((double) (TILT_CENTER - TILT_59)/50);

		static public int tilt_center_pt = TILT_CENTER;
		static public int pan_center_pt = PAN_CENTER;
		static public double tilt_deg = 0;


		static private bool OpenPort(string com_port)

		{
			bool rtn = false;

			if (com_port.Length > 0)
				{

				try
				{
				sp.PortName = com_port;
				sp.BaudRate = 9600;
				sp.NewLine = "\r";		
				sp.DataBits = 8;
				sp.StopBits = StopBits.One;
				sp.Parity = Parity.None;
				sp.Open();
				rtn = true;
				}
				
				catch (Exception ex)
				{
				Log.LogEntry("OpenPort exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}
				
				}
			else
				Log.LogEntry("No comm port selected.");
			return(rtn);
		}



		static private bool ReadParam()

		{
			bool rtn = false;

			string fname;
			TextReader tr;

			fname = Application.StartupPath + Shared.CAL_SUB_DIR + SERVO_PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				port_name = tr.ReadLine();

				try
				{
				tilt_center_pt = int.Parse(tr.ReadLine());
				pan_center_pt = int.Parse(tr.ReadLine());
				tilt_59= int.Parse(tr.ReadLine());
				tilt_pwm_deg = ((double) tilt_center_pt - tilt_59)/59;
				rtn = true;
				}

				catch (Exception ex)
				{
				Log.LogEntry("ReadParm exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				tr.Close();
				}
			return (rtn);
		}



		static public bool Open()

		{
			bool rtn = false;

			if (ReadParam() && OpenPort(port_name) && InitPosition())
				rtn = true;
			return(rtn);
		}



		static public void Close()

		{
			if (sp.IsOpen)
				{
				InitPosition();
				sp.Close();
				}
		}



		static public bool Position(int pan,int tilt)
		
		{
			string cmd;
			int mtt = 0,mtp = 0,mt;
			bool rtn = false;

			try
			{
			if (last_tilt_pwm > 0)
				mtt = (Math.Abs(last_tilt_pwm - tilt)/D10_PWM) * D10_MS;
			if(last_pan_pwm > 0)
				mtp = (Math.Abs(last_pan_pwm - pan)/D10_PWM) * D10_MS;
			mt = Math.Max(mtt,mtp);
			if (mt == 0)
				mt = 1000;
			cmd = "#" + PAN_CHANNEL + "P" + pan + "#" + TILT_CHANNEL + "P" + tilt + "T" + mt;
			last_tilt_pwm = tilt;
			last_pan_pwm = pan;
			tilt_deg = ((double) tilt_center_pt - tilt)/tilt_pwm_deg;
			sp.WriteLine(cmd);
			rtn = true;
			}

			catch (Exception ex)
			{
			Log.LogEntry("PT position exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}
			
			return(rtn);
		}



		static public bool InitPosition()

		{
			tilt_deg = 0;
			return(Position(pan_center_pt,tilt_center_pt));
		}



		static public bool TiltedPosition()

		{
			tilt_deg = 59;
			return(Position(pan_center_pt,tilt_59));
		}



		public static int ReadAnalogInput()

		{
			int value = -1;

			try
			{
			sp.DiscardInBuffer();
			sp.WriteLine("VA");
			value = sp.ReadByte();
			}

			catch(Exception ex)
			{
			Log.LogEntry("ReadAnalogInput exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return(value);
		}

		}
	}
