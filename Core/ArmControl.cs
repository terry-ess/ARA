#define LOG_PACKET

using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace RobotArm
	{

	public static class ArmControl

	{

		public const double ANGLE_CORRECT = 0.0;

		public const byte WAIST = 0;
		public const byte SHOULDER = 1;
		public const byte ELBOW = 2;
		public const byte WRIST_TILT = 3;
		public const byte WRIST_ROTATE = 4;
		public const byte GRIPPER = 5;

		private const string PARAM_FILE = "arm.param";

		private const byte HEADER = 0;
		private const byte LEN = 1;
		private const byte CMD = 2;
		private const byte SUB_CMD = 3;
		private const byte DETAIL1 = 4;
		private const byte DETAIL2 = 5;
		private const byte DETAIL3 = 6;
		private const byte DETAIL4 = 7;
		private const byte DATA1 = 8;
		private const byte DATA2 = 9;
		private const byte DATA3 = 10;
		private const byte DATA4 = 11;
		private const byte CHK_SUM = 12;

		private const byte ACCEL = 50;

		private const byte ERROR = 200;

		public const int Z_OFFSET_MM = 178;
		public const int Y_OFFSET_MM = 129;
		public const int GRIPPER_LEN_MM = 176;
		private const int DOWN_SURFACE_Y_MM = 40;	//THE RC Y = 0 SHOULD BE 47 MM WITH THE GRIPPER "DOWN", BUT 35 MM IS WHAT WORKS! USING 40 TO ASSURE CLEARENCE

		private const int MIN_MOV_TIME_MS = 500;
		private const int MAX_MOVE_TIME_MS = 4000;
		private const int TIME_TO_DIST = 6;
		private const int NO_SERVOS = 8;
		public const int GRIPPER_EXT_MIN_MM = 24;
		public const int GRIPPER_EXT_MAX_MM = 74;
		public const int GRIPPER_90_OVERHANG = 50;

		public const int GRIPPER_FULL_OPEN = GRIPPER_EXT_MAX_MM;
		public const int GRIPPER_FULL_CLOSE = GRIPPER_EXT_MIN_MM;
		private const int GRIPPER_INC_MM = 1;
		private const int DELAY = 5;
		public const int MAX_GRIP_VALUE = 100;


		public const int MAX_REACH_FLAT_MM = 635;
		public const int MAX_REACH_DOWN_MM = 495;
		
		public enum gripper_orient {FLAT,DOWN };

		public struct ac
		{
			public int wtx;
			public int wty;
			public double wja;
		};

		private static string port_name = "";
		private static int arm_model = 0;
		private static double shoulder_sag_angle = 0;
		private static double elbow_sag_angle = 0;
		private static double wrist_sag_angle = 0;
		private static SerialPort csp = new SerialPort();
		private static byte[] out_packet = new byte[13];
		private static byte[] in_packet = new byte[13];

		public static bool cmd_error = false;
		public static int eno = 0;


		static ArmControl()

		{
			string fname;
			TextReader tr;

			fname =  Application.StartupPath + Shared.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				port_name = tr.ReadLine();

				try
				{
				arm_model = int.Parse(tr.ReadLine());
				shoulder_sag_angle = double.Parse(tr.ReadLine());
				elbow_sag_angle = double.Parse(tr.ReadLine());
				wrist_sag_angle = double.Parse(tr.ReadLine());
				}

				catch(Exception)
				{
				if (arm_model == 0)
					arm_model = 30250;
				}
				
				tr.Close();
				out_packet[HEADER] = 255;
				out_packet[LEN] = 13;
				}
		}



		private static bool Open(string port_name)

		{
			bool rtn = false;

			try
			{
			csp.PortName = port_name;
			csp.BaudRate = 57600;
			csp.DataBits = 8;
			csp.StopBits = StopBits.One;
			csp.Parity = Parity.None;
			csp.ReadTimeout = (int) Math.Round( MAX_MOVE_TIME_MS * 1.25);
			csp.WriteTimeout = 1000;
			csp.Handshake = Handshake.None;
			csp.Open();
			Thread.Sleep(1000);
			csp.DiscardOutBuffer();
			csp.ReadExisting();
			rtn = Hello();
			if (!rtn)
				csp.Close();
			}

			catch (Exception ex)
			{
			rtn = false;
			Log.LogEntry("ArmControl.Open exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return (rtn);
		}



		private static bool Open()

		{
			bool rtn = false;

			if (!csp.IsOpen && (port_name.Length > 0))
				rtn = Open(port_name);
			else if (csp.IsOpen)
				rtn = true;
			return(rtn);
		}


		private static bool SendPacket(int no_response = 1)

		{
			bool rtn = true;
			int i,j;
			int chk_sum = 0;

			cmd_error = false;

			try
			{
			for (i = 1; i < out_packet.Length - 1;i++)
				chk_sum += out_packet[i];
			out_packet[CHK_SUM] = (byte) (chk_sum % 256);

#if LOG_PACKET
			Log.LogEntry("Command: " + out_packet[HEADER] + "," + out_packet[LEN] + "," + out_packet[CMD] + "," + out_packet[SUB_CMD] + "," + out_packet[DETAIL1] + "," + out_packet[DETAIL2] + "," + out_packet[DETAIL3] + "," + out_packet[DETAIL4] + "," + out_packet[DATA1] + "," + out_packet[DATA2] + "," + out_packet[DATA3] + "," + out_packet[DATA4] + "," + out_packet[CHK_SUM]);
#endif

			csp.Write(out_packet,0,13);
			for (j = 0;j < no_response;j++)
				{
				for (i = 0;i < in_packet.Length;i++)
					in_packet[i] = (byte)csp.ReadByte();

#if LOG_PACKET
				Log.LogEntry("Response: " + in_packet[HEADER] + "," + in_packet[LEN] + "," + in_packet[CMD] + "," + in_packet[SUB_CMD] + "," + in_packet[DETAIL1] + "," + in_packet[DETAIL2] + "," + in_packet[DETAIL3] + "," + in_packet[DETAIL4] + "," + in_packet[DATA1] + "," + in_packet[DATA2] + "," + in_packet[DATA3] + "," + in_packet[DATA4] + "," + in_packet[CHK_SUM]);
#endif

				if (in_packet[CMD] == ERROR)
					{
					rtn = false;
					Log.LogEntry("SendPacket error response received.");
					eno = (in_packet[DATA4] << 24) + (in_packet[DATA3] << 16) + (in_packet[DATA2] << 8) + in_packet[DATA1];
					Log.LogEntry("Error no: " + eno);
					cmd_error = true;
					}
				}
			}

			catch(Exception ex)
			{
			rtn = false;
			Log.LogEntry("SendPacket exception: " + ex.Message);
			Log.LogEntry("stack trace: " + ex.StackTrace);
			cmd_error = false;
			}
			
			return(rtn);
		}



		private static bool Hello()

		{
			bool rtn = true;
			int i;

			out_packet[CMD] = 1;
			out_packet[SUB_CMD] = 0;
			out_packet[DETAIL1] = 1;
			out_packet[DETAIL2] = 2;
			out_packet[DETAIL3] = 3;
			out_packet[DETAIL4] = 4;
			out_packet[DATA1] = 5;
			out_packet[DATA2] = 6;
			out_packet[DATA3] = 7;
			out_packet[DATA4] = 8;
			if (SendPacket())
				{
				for (i = 0; i < in_packet.Length; i++)
					if (in_packet[i] != out_packet[i])
						{
						rtn = false;
						Log.LogEntry("Hello response did not match command.");
						break;
						}
				}
			return (rtn);
		}



		private static bool SetArmModel()

		{
			bool rtn = false;

			out_packet[CMD] = 41;
			out_packet[SUB_CMD] = 1;
			out_packet[DETAIL1] = 1;
			out_packet[DETAIL2] = 0;
			out_packet[DETAIL3] = 0;
			out_packet[DETAIL4] = 0;
			out_packet[DATA1] = (byte)(arm_model % 256);
			out_packet[DATA2] = (byte)(arm_model / 256);
			out_packet[DATA3] = 0;
			out_packet[DATA4] = 0;
			rtn = SendPacket();
			if (!rtn)
				Log.LogEntry("SetArmModel failed.");
			return (rtn);
		}



		private static bool SetExpBoard()

		{
			bool rtn = false;

			out_packet[CMD] = 1;
			out_packet[SUB_CMD] = 2;
			out_packet[DETAIL1] = 0;
			out_packet[DETAIL2] = 3;
			out_packet[DETAIL3] = 0;
			out_packet[DETAIL4] = 0;
			out_packet[DATA1] = 1;
			out_packet[DATA2] = 0;
			out_packet[DATA3] = 0;
			out_packet[DATA4] = 0;
			rtn = SendPacket();
			if (!rtn)
				Log.LogEntry("SetArmMode failed.");
			return(rtn);
		}



		private static bool SetSag(byte joint,int angle)

		{
			bool rtn = false;

			out_packet[CMD] = 61;
			out_packet[SUB_CMD] = 21;
			out_packet[DETAIL1] = 0;
			out_packet[DETAIL2] = joint;
			out_packet[DETAIL3] = 0;
			out_packet[DETAIL4] = 0;
			out_packet[DATA1] = (byte)(angle);
			out_packet[DATA2] = (byte)(angle >> 8);
			out_packet[DATA3] = (byte)(angle >> 16);
			out_packet[DATA4] = (byte)(angle >> 24);
			rtn = SendPacket();
			if (!rtn)
				Log.LogEntry("SetSag failed.");
			return (rtn);
		}



		private static bool SetGripper()

		{
			bool rtn = false;
			const int MIN = 1;
			const int MAX = 2;

			out_packet[CMD] = 61;
			out_packet[SUB_CMD] = 30;
			out_packet[DETAIL1] = 0;
			out_packet[DETAIL2] = 1;
			out_packet[DETAIL3] = 1;
			out_packet[DETAIL4] = 0;
			out_packet[DATA1] = 0;
			out_packet[DATA2] = 0;
			out_packet[DATA3] = 0;
			out_packet[DATA4] = 0;
			rtn = SendPacket();
			if (!rtn)
				Log.LogEntry("SetGripper mode failed.");
			else
				{
				out_packet[CMD] = 41;
				out_packet[SUB_CMD] = 22;
				out_packet[DETAIL1] = 0;
				out_packet[DETAIL2] = MAX;
				out_packet[DETAIL3] = 0;
				out_packet[DETAIL4] = 0;
				out_packet[DATA1] = GRIPPER_EXT_MAX_MM;
				out_packet[DATA2] = 0;
				out_packet[DATA3] = 0;
				out_packet[DATA4] = 0;
				rtn = SendPacket();
				if (!rtn)
					Log.LogEntry("SetGripper max failed.");
				else
					{
					out_packet[DETAIL2] = MIN;
					out_packet[DATA1] = GRIPPER_EXT_MIN_MM;
					rtn = SendPacket();
					if (!rtn)
						Log.LogEntry("SetGripper min failed.");
					}
				}
			return (rtn);
		}



		private static bool Torque(bool on)

		{
			bool rtn = false;

			out_packet[CMD] = 31;
			out_packet[SUB_CMD] = 0;
			out_packet[DETAIL1] = 0;
			out_packet[DETAIL2] = 64;
			out_packet[DETAIL3] = 0;
			out_packet[DETAIL4] = 0;
			if (on)
				out_packet[DATA1] = 1;
			else
				out_packet[DATA1] = 0;
			out_packet[DATA2] = 0;
			out_packet[DATA3] = 0;
			out_packet[DATA4] = 0;
			rtn = SendPacket(NO_SERVOS);
			if (!rtn)
				Log.LogEntry("Torque " + on + " failed.");
			return (rtn);
		}



		private static bool SetPose(byte joint,int value,bool log = true)

		{
			bool rtn = false;

			out_packet[CMD] = 61;
			out_packet[SUB_CMD] = 20;
			out_packet[DETAIL1] = 0;
			out_packet[DETAIL2] = joint;
			out_packet[DETAIL3] = 0;
			out_packet[DETAIL4] = 0;
			out_packet[DATA1] = (byte) (value);
			out_packet[DATA2] = (byte) (value >> 8);
			out_packet[DATA3] = (byte) (value >> 16);
			out_packet[DATA4] = (byte) (value >> 24);
			rtn = SendPacket();
			if (!rtn && log)
				Log.LogEntry("SetPose failed.");
			return (rtn);
		}



		private static bool ExecutePose(byte accel,int vel)

		{
			bool rtn = false;

			out_packet[CMD] = 61;
			out_packet[SUB_CMD] = 10;
			out_packet[DETAIL1] = accel;
			out_packet[DETAIL2] = 1;
			out_packet[DETAIL3] = 1;
			out_packet[DETAIL4] = 1;
			out_packet[DATA1] = (byte) (vel % 256);
			out_packet[DATA2] = (byte) (vel / 256);
			out_packet[DATA3] = 0;
			out_packet[DATA4] = 0;
			rtn = SendPacket();
			if (rtn && (in_packet[DATA1] != 1))
				rtn = false;
			if (!rtn)
				Log.LogEntry("ExecutePose failed.");
			return(rtn);
		}



		private static bool Move(int wja,int wtx,int wty,int wta,int wra,byte accel,int vel)

		{
			bool rtn = false;


			if (SetPose(1, wja))
				if (SetPose(2,wtx))
					if (SetPose(3, wty))
						if (SetPose(5, wta))
							if (SetPose(6, wra))
								rtn = ExecutePose(accel,vel);
			return(rtn);
		}



		private static bool TestPose(bool log)

		{
			bool rtn = false;

			out_packet[CMD] = 61;
			out_packet[SUB_CMD] = 10;
			out_packet[DETAIL1] = 0;
			out_packet[DETAIL2] = 0;
			out_packet[DETAIL3] = 1;
			out_packet[DETAIL4] = 1;
			out_packet[DATA1] = 0;
			out_packet[DATA2] = 0;
			out_packet[DATA3] = 0;
			out_packet[DATA4] = 0;
			rtn = SendPacket();
			if (rtn && (in_packet[DATA1] != 1))
				rtn = false;
			if (!rtn && log)
				Log.LogEntry("TestPose failed.");
			return(rtn);
		}





		private static bool MoveCheck(int wja,int wtx,int wty,int wta,bool log = true)

		{
			bool rtn = false;


			if (SetPose(1, wja,false))
				if (SetPose(2,wtx,false))
					if (SetPose(3, wty,false))
						if (SetPose(5, wta,false))
							rtn = TestPose(false);
			return(rtn);
		}



		private static bool ConvertRCtoAC(int x,int y,int z,gripper_orient go,ref ac pos,bool log = true)

		{
			bool rtn = false;
			double angle;

			if (go == gripper_orient.FLAT)
				{
				angle = Math.Atan((double)x / (z + Z_OFFSET_MM));
				pos.wtx = (int) Math.Round((Math.Sqrt(Math.Pow(x,2) + Math.Pow(z + Z_OFFSET_MM,2)) - GRIPPER_LEN_MM));
				pos.wty = y - Y_OFFSET_MM;
				pos.wja = (angle * Shared.RAD_TO_DEG) + ANGLE_CORRECT;
				rtn = true;
				}
			else if (go == gripper_orient.DOWN)
				{
				angle = Math.Atan((double)x / (z + Z_OFFSET_MM));
				pos.wtx = (int) Math.Round((z + Z_OFFSET_MM)/Math.Cos(angle));
				if (y == 0)
					pos.wty = DOWN_SURFACE_Y_MM;		//CORRECTION FOR ACTUAL VS WHAT IS SUPPOSED TO WORK
				else if (y < 0)
					pos.wty = DOWN_SURFACE_Y_MM + y;
				else
					pos.wty = y - Y_OFFSET_MM + GRIPPER_LEN_MM;
				pos.wja = (angle * Shared.RAD_TO_DEG) + ANGLE_CORRECT;
				rtn = true;
				}
			if (log)
				{
				Log.LogEntry("Convert RC to AC");
				Log.LogEntry("  RC (mm): " + x + ", " + y + ", " + z);
				Log.LogEntry("  AC: " + pos.wtx + " mm, " + pos.wty + " mm, " + pos.wja.ToString("F2") + " °");
				}
			return (rtn);
		}



		public static bool InitArm()

		{
			bool rtn = false;

			if (ArmControl.Open())
				{
				if (ArmControl.SetExpBoard())
					{
					if (ArmControl.SetArmModel())
						{
						if (ArmControl.Torque(true))
							{
							if (ArmControl.SetSag(SHOULDER, (int) shoulder_sag_angle * 10) && ArmControl.SetSag(ELBOW,(int) elbow_sag_angle * 10) && (ArmControl.SetSag(WRIST_TILT,(int) wrist_sag_angle * 10)))
								{
								if (SetGripper())
									{
									Shared.current_rc_mm.x = 0;
									Shared.current_rc_mm.y = 76;
									Shared.current_rc_mm.z = -51;
									rtn = true;
									}
								else
									{
									ArmControl.Close();
									Log.KeyLogEntry("Could not set gripper min or max.");
									}
								}
							else
								{
								ArmControl.Close();
								Log.LogEntry("Could not set elbow or wrist sag.");
								}
							}
						else
							{
							ArmControl.Close();
							Log.LogEntry("Could not set arm servo torque on");
							}
						}
					else
						{
						ArmControl.Close();
						Log.LogEntry("Could not set arm model.");
						}
					}
				else
					{
					Log.LogEntry("Could not set expansion board.");
					ArmControl.Close();
					}
				}
			else
				Log.LogEntry("Could not open arm control.");
			return (rtn);
		}



		public static bool MoveJoint(byte joint,double angle)

		{
			bool rtn = false;
			int jangle;

			out_packet[CMD] = 51;
			out_packet[SUB_CMD] = 20;
			out_packet[DETAIL1] = joint;
			out_packet[DETAIL2] = 0;
			out_packet[DETAIL3] = 0;
			out_packet[DETAIL4] = 1;
			jangle = (int) Math.Round(angle * 10);
			out_packet[DATA1] = (byte)(jangle);
			out_packet[DATA2] = (byte)(jangle >> 8);
			out_packet[DATA3] = (byte)(jangle >> 16);
			out_packet[DATA4] = (byte)(jangle >> 24);
			rtn = SendPacket();
			if (!rtn)
				Log.LogEntry("MoveJoint failed.");
			return (rtn);
		}



		public static bool ReadJoint(byte joint,ref double angle)

		{
			int jangle;
			bool rtn;

			out_packet[CMD] = 82;
			out_packet[SUB_CMD] = 20;
			out_packet[DETAIL1] = joint;
			out_packet[DETAIL2] = 0;
			out_packet[DETAIL3] = 0;
			out_packet[DETAIL4] = 1;
			out_packet[DATA1] = 0;
			out_packet[DATA2] = 0;
			out_packet[DATA3] = 0;
			out_packet[DATA4] = 0;
			rtn = SendPacket();
			if (!rtn)
				Log.LogEntry("ExecutePose failed.");
			else
				{
				jangle = (in_packet[DATA4] << 24) + (in_packet[DATA3] << 16) + (in_packet[DATA2] << 8) + in_packet[DATA1];
				angle = ((double) jangle/10);
				}
			return(rtn);
		}



		public static bool GHome()

		{
			bool rtn = false;
			double dist,angle;
			int time;
			Shared.space_3d_mm ap;

			if (!Shared.parked && !Shared.homed)
				{
				dist = Math.Sqrt(Math.Pow(Shared.current_rc_mm.x, 2) + Math.Pow(Shared.current_rc_mm.y - 150, 2) + Math.Pow(Shared.current_rc_mm.z - 120, 2));
				time = (int)Math.Round(dist * 6);
				if (time < MIN_MOV_TIME_MS)
					time = MIN_MOV_TIME_MS;
				else if (time > MAX_MOVE_TIME_MS)
					time = MAX_MOVE_TIME_MS;
				Log.LogEntry("dist (mm): " + dist.ToString("F2"));
				Log.LogEntry("move time (ms): " + time);
				if (Move((int) Math.Round(Shared.current_ac.wja * 10), 120, 150, 900, 0, ACCEL, time))
					{
					rtn = true;
					Shared.parked = false;
					Shared.homed = false;
					Shared.go = gripper_orient.DOWN;
					angle = Shared.current_ac.wja;
					ap = new Shared.space_3d_mm(0, 0, -ArmControl.Z_OFFSET_MM);
					Shared.current_rc_mm.x = ap.x + (int) Math.Round(123 * Math.Cos(angle) * Shared.RAD_TO_DEG);
					Shared.current_rc_mm.z = ap.z + (int) Math.Round(123 * Math.Sin(angle) * Shared.RAD_TO_DEG);
					Shared.current_rc_mm.y = 114;
					Shared.current_ac.wtx = 120;
					Shared.current_ac.wty = 150;
					}
				}
			return (rtn);
		}



		public static bool Home()

		{
			bool rtn = false;
			double dist;
			int time;

			if (Shared.parked)
				time = 3000;
			else
				{
				dist = Math.Sqrt(Math.Pow(Shared.current_rc_mm.x, 2) + Math.Pow(Shared.current_rc_mm.y - 150, 2) + Math.Pow(Shared.current_rc_mm.z - 120, 2));
				time = (int)Math.Round(dist * 6);
				if (time < MIN_MOV_TIME_MS)
					time = MIN_MOV_TIME_MS;
				else if (time > MAX_MOVE_TIME_MS)
					time = MAX_MOVE_TIME_MS;
				Log.LogEntry("dist (mm): " + dist.ToString("F2"));
				Log.LogEntry("move time (ms): " + time);
				}
			if (Move(0,120,150,900,0,ACCEL,time))
				{
				rtn = true;
				Shared.parked = false;
				Shared.homed = true;
				Shared.go = gripper_orient.DOWN;
				Shared.current_rc_mm.x = 0;
				Shared.current_rc_mm.z = -55;
				Shared.current_rc_mm.y = 114;
				Shared.current_ac.wja = 0;
				Shared.current_ac.wtx = 120;
				Shared.current_ac.wty = 150;
				}
			return(rtn);
		}



		public static Shared.space_3d_mm HomeLoc()

		{
			Shared.space_3d_mm loc = new RobotArm.Shared.space_3d_mm();

			loc.x = 0;
			loc.z = -55;
			loc.y = 114;
			return(loc);
		}



		public static bool Park()

		{
			bool rtn = false,x_posed = false;
			double dist;
			int time;

			if (Shared.parked)
				rtn = true;
			else 
				{
				if (Shared.current_rc_mm.x != 0)
					x_posed = MoveJoint(WAIST,0);
				else
					x_posed = true;
				dist = Math.Sqrt(Math.Pow(Shared.current_rc_mm.x, 2) + Math.Pow(Shared.current_rc_mm.y - 40, 2) + Math.Pow(Shared.current_rc_mm.z + 13, 2));
				time = (int)Math.Round(dist * 6);
				if (time < MIN_MOV_TIME_MS)
					time = MIN_MOV_TIME_MS;
				else if (time > MAX_MOVE_TIME_MS)
					time = MAX_MOVE_TIME_MS;
				Log.LogEntry("dist (mm): " + dist.ToString("F2"));
				Log.LogEntry("move time (ms): " + time);
				if (x_posed && Move(0,-13,40,0,0,ACCEL,time))
					{
					Shared.current_rc_mm.x = 0;
					Shared.current_rc_mm.y =  40;
					Shared.current_rc_mm.z = GRIPPER_LEN_MM;
					if (MoveJoint(WRIST_TILT,50) && MoveJoint(ELBOW,92))
						{
						rtn = true;
						Shared.parked = true;
						Shared.homed = false;
						}
					}
				}
			return(rtn);
		}



		public static void EmergencyPark()

		{
			Move(0, -13, 40, 0, 0, ACCEL,4000);
			MoveJoint(WRIST_TILT, 50);
			MoveJoint(ELBOW, 92);
			Shared.parked = true;
		}



		public static void CheckGripperHardwareError()

		{
			bool rtn;

			Log.LogEntry("Check gripper hardware.");
			out_packet[CMD] = 12;
			out_packet[SUB_CMD] = 0;
			out_packet[DETAIL1] = 8;
			out_packet[DETAIL2] = 70;
			out_packet[DETAIL3] = 0;
			out_packet[DETAIL4] = 0;
			out_packet[DATA1] = 0;
			out_packet[DATA2] = 0;
			out_packet[DATA3] = 0;
			out_packet[DATA4] = 0;
			rtn = SendPacket();
		}



		public static bool Move(int x,int y,int z,gripper_orient go,double wangle = 0)
		
		{
			bool rtn = false;
			ac pos = new RobotArm.ArmControl.ac();
			double dist;
			int time,wt;

			Log.LogEntry("ArmControl.Move to (" + x + "  " + y + "  " + z +")");
			if (ConvertRCtoAC(x,y,z,go,ref pos))
				{
				dist = Math.Sqrt(Math.Pow(Shared.current_rc_mm.x - x,2) + Math.Pow(Shared.current_rc_mm.y - y, 2) + Math.Pow(Shared.current_rc_mm.z - z, 2));
				time = (int) Math.Round(dist * 6);
				if (time < MIN_MOV_TIME_MS)
					time = MIN_MOV_TIME_MS;
				else if (time > MAX_MOVE_TIME_MS)
					time = MAX_MOVE_TIME_MS;
				Log.LogEntry("dist (mm): " + dist.ToString("F2"));
				Log.LogEntry("move time (ms): " + time);
				if (go == gripper_orient.FLAT)
					wt = 0;
				else
					wt = 900;
				rtn = Move((int) Math.Round(pos.wja * 10), pos.wtx, pos.wty,wt,(int) Math.Round(wangle * 10), ACCEL,time);
				if (rtn)
					{
					Shared.parked = false;
					Shared.homed = false;
					Shared.go = go;
					Shared.current_rc_mm.x = x;
					Shared.current_rc_mm.y = y;
					Shared.current_rc_mm.z = z;
					Shared.current_ac = pos;
					}
				}
			return(rtn);
		}



		public static bool MoveCheck(int x, int y, int z, gripper_orient go,bool log = true)

		{
			bool rtn = false;
			ac pos = new RobotArm.ArmControl.ac();
			int wt;

			if (log)
				Log.LogEntry("ArmControl.MoveCheck to (" + x + "  " + y + "  " + z +")");
			if (ConvertRCtoAC(x,y,z,go,ref pos,log))
				{
				if (go == gripper_orient.FLAT)
					wt = 0;
				else
					wt = 900;
				rtn = MoveCheck((int)Math.Round(pos.wja * 10), pos.wtx, pos.wty, wt,log);
				}
			return (rtn);
		}


		public static bool ReadGrip(ref int gwidth)

		{
			bool rtn = false;

			out_packet[CMD] = 82;
			out_packet[SUB_CMD] = 20;
			out_packet[DETAIL1] = 5;
			out_packet[DETAIL2] = 0;
			out_packet[DETAIL3] = 0;
			out_packet[DETAIL4] = 0;
			out_packet[DATA1] = 0;
			out_packet[DATA2] = 0;
			out_packet[DATA3] = 0;
			out_packet[DATA4] = 0;
			rtn = SendPacket();
			if (rtn)
				{
				out_packet[CMD] = 82;
				out_packet[SUB_CMD] = 50;
				out_packet[DETAIL1] = 0;
				out_packet[DETAIL2] = 2;
				out_packet[DETAIL3] = 0;
				out_packet[DETAIL4] = 0;
				out_packet[DATA1] = in_packet[DATA1];
				out_packet[DATA2] = in_packet[DATA2];
				out_packet[DATA3] = in_packet[DATA3];
				out_packet[DATA4] = in_packet[DATA4];
				rtn = SendPacket();
				if (rtn)
					{
					gwidth = in_packet[DATA1];
					}
				}
			return (rtn);
		}



		public static bool Gripper(int ext_mm)

		{
			bool rtn = false;

			out_packet[CMD] = 61;
			out_packet[SUB_CMD] = 31;
			out_packet[DETAIL1] = 0;
			out_packet[DETAIL2] = 1;
			out_packet[DETAIL3] = 0;
			out_packet[DETAIL4] = 0;
			if ((ext_mm <= GRIPPER_EXT_MAX_MM) && (ext_mm >= GRIPPER_EXT_MIN_MM))
				{
				out_packet[DATA1] = (byte) ext_mm;
				out_packet[DATA2] = 0;
				out_packet[DATA3] = 0;
				out_packet[DATA4] = 0;
				rtn = SendPacket();
				if (!rtn)
					{
					Log.LogEntry("Gripper failed.");
					CheckGripperHardwareError();
					}
				}
			else
				Log.LogEntry("Gripper setting outside of range.");
			return(rtn);
		}



		public static bool GripperRotate(double angle)

		{
			bool rtn = false;

			if (Math.Abs(angle) <= 180)
				{
				if (MoveJoint(WRIST_ROTATE,angle))
					{
					Shared.gripper_rot = angle;
					rtn = true;
					}
				}
			else
				Log.LogEntry("Gripper rotation outside of range.");
			return(rtn);
		}



		public static bool WristTilt(double angle)

		{
			double cangle = 0;
			bool rtn = false;

			if (ArmControl.ReadJoint(ArmControl.WRIST_TILT, ref cangle))
				{
				rtn =ArmControl.MoveJoint(ArmControl.WRIST_TILT,cangle + angle);
				}
			return(rtn);
		}



		public static void Close()

		{
			if (csp.IsOpen)
				{
				if (!Shared.parked)
					{
					Park();
					Thread.Sleep(1000);
					}
				Torque(false);
				csp.Close();
				}
		}



		public static void CloseGrip(int min = 0,System.Windows.Forms.TextBox tb = null)

		{
			int grip_val = 0;
			int fsr_val = 0;
			const double BACKOFF_FACTOR = 1.15;

			if (ArmControl.ReadGrip(ref grip_val) && (grip_val > ArmControl.GRIPPER_EXT_MIN_MM + GRIPPER_INC_MM) && (grip_val > min + GRIPPER_INC_MM))
				{
				do
					{
					grip_val -= GRIPPER_INC_MM;
					if (ArmControl.Gripper(grip_val))
						{
						Thread.Sleep(DELAY);
						fsr_val = CameraPanTilt.ReadAnalogInput();
						if (tb != null)
							tb.AppendText(fsr_val + " ");
						}
					else
						break;
					}
				while((fsr_val < MAX_GRIP_VALUE) && (grip_val > ArmControl.GRIPPER_EXT_MIN_MM + GRIPPER_INC_MM) && (grip_val > min + GRIPPER_INC_MM));
				Log.LogEntry("Max FSR value: " + fsr_val);
				if (fsr_val > (int) Math.Round(MAX_GRIP_VALUE * BACKOFF_FACTOR))
					{
					grip_val += GRIPPER_INC_MM;
					ArmControl.Gripper(grip_val);
					fsr_val = CameraPanTilt.ReadAnalogInput();
					}
				Log.LogEntry("Final FSR value: " + fsr_val);
				}
		}



		public static int ReadGripFSR()

		{
			return(CameraPanTilt.ReadAnalogInput());
		}



		public static bool GripperFull()

		{
			bool full = false;

			if (CameraPanTilt.ReadAnalogInput() >= MAX_GRIP_VALUE)
				full = true;
			return(full);
		}


		}

	}
