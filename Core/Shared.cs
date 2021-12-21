using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using MathNet.Numerics.LinearAlgebra.Double;


namespace RobotArm
	{
	static public class Shared
		{

		public const string CAL_SUB_DIR = "\\cal\\";
		public const string DATA_SUB_DIR = "\\data\\";
		public const string DB_DIR = "\\database\\";
		public const string DOMAIN_DIR = "\\domains\\";
		public const string OD_SERVER_DIR = "\\odserver\\";
		public const string OV_SERVER_DIR ="\\ovserver\\";
		public const string IS_SERVER_DIR = "\\isserver\\";
		public const string PV_SERVER_DIR = "\\pvserver\\netcoreapp3.1\\";
		public const string TTS_SERVER_DIR = "\\ttsserver\\";
		public const string OD_MODEL_DIR = "\\ODmodels\\";
		public const string OV_MODEL_DIR = "\\OVmodels\\";
		public const string IS_MODEL_DIR = "\\ISmodels\\";
		public const string PV_CONTEXT_DIR = "\\Pvcontext\\";

		public const string DB_FILE_EXT = "db";
		public const string TEXT_TILE_EXT = ".txt";

		public const double RAD_TO_DEG = 180 / Math.PI;
		public const double DEG_TO_RAD = Math.PI / 180;
		public const double MM_TO_IN = 0.0393701;
		public const double IN_TO_MM = 25.4;

		public const double CAM_ROBOT_Z_OFFSET = 7.0 - (D415Camera.FRONT_OFFSET + D415Camera.AC_Z_OFFSET);
		public const double CAL_RCX_IN = 0.0;
		public const double CAL_RCY_IN = 0.0;
		public const double CAL_RCZ_IN = 9.0;

		public const int MIN_BLOB_AREA = 120;

		public const bool TENSORFLOW = true;

		public struct space_3d_mm
		{
			public int x;
			public int y;
			public int z; 

			public space_3d_mm(int x,int y,int z)

			{
				this.x = x;
				this.y = y;
				this.z = z;
			}

			public override string ToString()

			{
				string rtn;

				rtn = "(" + this.x + "  " + this.y + "  " + this.z + ")";
				return(rtn);
			}

			public  string ToCsvString()

			{
				string rtn;
				rtn = "," + this.x + "," + this.y + "," + this.z;
				return (rtn);
			}

		};

		public struct PartInfo
		{
			public int no_pts;
			public Point avg_xz_pt_mm;
			public double orient;
			public int maxh_mm;

			public override string ToString()
			
			{
				string rtn;

				rtn = "no pts: " + no_pts + "  xz pt (mm): (" + avg_xz_pt_mm.X + "  " + avg_xz_pt_mm.Y + ")  orient (°): " + orient.ToString("F2") + "  max height (mm): " + maxh_mm;
				return(rtn);
			}

		}


		public static Stopwatch app_time = new Stopwatch();
		public static short[] depthdata;
		public static byte[] videodata;
		public static Image vimg;
		public static double x_correct_in = 0;
		public static double y_correct_in = 0;
		public static space_3d_mm rcloc = new Shared.space_3d_mm();
		public static bool vision_calibrated = false;

		public static string base_path = "";
		public static string domain_path = "";

		public static space_3d_mm current_rc_mm;			//current arm state
		public static ArmControl.ac current_ac;
		public static ArmControl.gripper_orient go;
		public static double gripper_rot = 0;
		public static bool parked = true;
		public static bool homed = false;

		private static uint ufile_no = 0;
		private static Mutex ufm = new Mutex();


		static Shared()

		{
			base_path = Application.StartupPath;
		}



		public static string ToCsvString(this Point pt)

		{
			string rtn;

			rtn = pt.ToString();
			return(rtn.Replace(',',' '));
		}


		public static uint GetUFileNo()

		{
			uint rtn;

			ufm.WaitOne();
			rtn = ufile_no;
			ufile_no += 1;
			ufm.ReleaseMutex();
			return (rtn);
		}


		public static string SaveVideoPic(Image img)

		{
			string fname;
			DateTime now = DateTime.Now;
			string dtstg = now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + "-" + Shared.GetUFileNo();

			fname = Log.LogDir() + "Robot arm video " + dtstg + ".jpg";
			img.Save(fname, System.Drawing.Imaging.ImageFormat.Jpeg);
			Log.LogEntry("Saved " + fname);
			return(fname);
		}



		public static void SaveDeptBin()

		{
			string fname;
			DateTime now = DateTime.Now;
			BinaryWriter bw;
			int i;

			fname = Log.LogDir() + "Robot arm depth data " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + "-" + Shared.GetUFileNo() + ".bin";
			bw = new BinaryWriter(File.Open(fname, FileMode.Create));
			if (bw != null)
				{
				for (i = 0; i < depthdata.Length; i++)
					bw.Write((short) depthdata[i]);
				}
			bw.Close();
			Log.LogEntry("Saved " + fname);
		}



		public static bool ConvertCC_to_RC(Shared.space_3d_mm cc,ref Shared.space_3d_mm rc,double tilt,bool log = true)
		// RC (0,0,0) IS AT THE FRONT CENTER BASE BOARD SURFACE)
		{
			bool rtn = true;
			DenseMatrix mat;
			DenseVector result, vec;
			int cfdd;
			double ch;

			rc.x = cc.x - D415Camera.x_offset_mm;
			cfdd =  D415Camera.CameraForwardDist(tilt);
			ch = D415Camera.CameraHeight(tilt);
			mat = DenseMatrix.OfArray(new[,] { { Math.Cos(tilt * Shared.DEG_TO_RAD), -Math.Sin(tilt * Shared.DEG_TO_RAD) }, { Math.Sin(tilt * Shared.DEG_TO_RAD), Math.Cos(tilt * Shared.DEG_TO_RAD) } });
			vec = new DenseVector(new[] { (double) cc.z, (double) cc.y });
			result = vec * mat;
			rc.z =  (int) Math.Round(result.Values[0] + cfdd - (CAM_ROBOT_Z_OFFSET * Shared.IN_TO_MM));
			rc.y = (int) Math.Round(result.Values[1] + (ch * Shared.IN_TO_MM));
			rc.x -= (int) Math.Round(x_correct_in * Shared.IN_TO_MM);
			rc.y -= (int) Math.Round(y_correct_in * Shared.IN_TO_MM);
			if (log)
				{
				Log.LogEntry("Convert CC to RC");
				Log.LogEntry("  CC: " + (cc.x * Shared.MM_TO_IN).ToString("F2") + ", " + (cc.y * Shared.MM_TO_IN) + ", " + (cc.z * Shared.MM_TO_IN));
				Log.LogEntry("  Camera tilt (°): " + tilt);
				Log.LogEntry("  Camera forward dist (in): " + (cfdd * Shared.MM_TO_IN).ToString("F2"));
				Log.LogEntry("  Camera height (in): " + ch);
				Log.LogEntry("  Cal x correct (in): " + x_correct_in);
				Log.LogEntry("  Cal y correct (in): " + y_correct_in);
				Log.LogEntry("  RC: " + (rc.x * Shared.MM_TO_IN).ToString("F2") + ", " + (rc.y * Shared.MM_TO_IN) + ", " + (rc.z * Shared.MM_TO_IN));
				}
			return (rtn);
		}



		public static bool ConvertRC_to_CC(Shared.space_3d_mm rc,ref Shared.space_3d_mm cc,double tilt,bool log = true)

		{
			bool rtn = true;
			DenseMatrix mat;
			DenseVector result, vec;
			int cfdd;
			double ch;

			cc.x = rc.x + D415Camera.x_offset_mm + (int) Math.Round(x_correct_in * Shared.IN_TO_MM);
			cfdd =  D415Camera.CameraForwardDist(tilt);
			ch = D415Camera.CameraHeight(tilt);
			mat = DenseMatrix.OfArray(new[,] { { Math.Cos(-tilt * Shared.DEG_TO_RAD), -Math.Sin(-tilt * Shared.DEG_TO_RAD) }, { Math.Sin(-tilt * Shared.DEG_TO_RAD), Math.Cos(-tilt * Shared.DEG_TO_RAD) } });
			vec = new DenseVector(new[] { (double)rc.z - cfdd + (CAM_ROBOT_Z_OFFSET * Shared.IN_TO_MM) + (x_correct_in * Shared.IN_TO_MM), (double)rc.y - (ch * Shared.IN_TO_MM) + (y_correct_in * Shared.IN_TO_MM) });
			result = vec * mat;
			cc.z = (int)Math.Round(result.Values[0]);
			cc.y = (int)Math.Round(result.Values[1]);

			if (log)
				{
				Log.LogEntry("Convert RC to CC");
				Log.LogEntry("  RC: " + (rc.x * Shared.MM_TO_IN).ToString("F2") + ", " + (rc.y * Shared.MM_TO_IN) + ", " + (rc.z * Shared.MM_TO_IN));
				Log.LogEntry("  Camera tilt (°): " + tilt);
				Log.LogEntry("  Camera forward dist (in): " + (cfdd * Shared.MM_TO_IN).ToString("F2"));
				Log.LogEntry("  Camera height (in): " + ch);
				Log.LogEntry("  Cal x correct (in): " + x_correct_in);
				Log.LogEntry("  Cal y correct (in): " + y_correct_in);
				Log.LogEntry("  CC: " + (cc.x * Shared.MM_TO_IN).ToString("F2") + ", " + (cc.y * Shared.MM_TO_IN) + ", " + (cc.z * Shared.MM_TO_IN));
				}
			return (rtn);
		}






		public static double DetermineOrient(Point to_pt,Point from_pt)

		{
			double orient,dx,dy,ra;

			dy = to_pt.Y - from_pt.Y;
			dx = to_pt.X - from_pt.X;
			Log.LogEntry("Determine orient: dx " + dx + "   dy: " + dy);
			if (dy == 0)
				{
				if (dx > 0)
					orient = 90;
				else
					orient = -90;
				}
			else
				{
				ra = (int)Math.Round(Math.Atan((double)dx / dy) * RobotArm.Shared.RAD_TO_DEG);
				if (dy > 0)
					orient = ra;
				else
					{
					if (dx > 0)
						orient = 180 + ra;
					else
						orient= ra - 180;
					}
				}
			return (orient);
		}



		public static void Output(string line,TextBox tb = null)

		{
			if (tb != null)
				tb.AppendText(line + "\r\n");
			Log.LogEntry(line);
		}



		public static ArrayList Detect(string model,Bitmap bm,double score,int obj_id,TextBox tb = null,bool log = true)

		{
			ArrayList objs;
			Stopwatch sw = new Stopwatch();

			sw.Start();
			objs = VisualObjectDetection.DetectObject(bm,model,obj_id,score,log);
			sw.Stop();
			if (log)
				Output("OD inference time (msec): " + sw.ElapsedMilliseconds,tb);
			return(objs);
		}



		public static bool InReach(space_3d_mm loc,bool down)

		{
			bool rtn;

			if (down)
				rtn = ArmControl.MoveCheck(loc.x,loc.y,loc.z,ArmControl.gripper_orient.DOWN,false);
			else
				rtn = ArmControl.MoveCheck(loc.x, loc.y, loc.z, ArmControl.gripper_orient.FLAT,false);
			if (!rtn)
				{
				if (ArmControl.eno != -990000)		//-990000 means out of reach, due to arm controller firmware problem? other values do not
					rtn = true;
				}
			Log.LogEntry("InReach (" + loc.x + "," + loc.y + "," + loc.z + ")  " + rtn);
			return (rtn);
		}



		public static bool RectOverlap(Rectangle r1, Rectangle r2)

		{
			bool rtn = true;

			if ((r1.X + r1.Width < r2.X) || (r1.X > r2.X + r2.Width) || (r1.Y + r1.Height < r2.Y) || (r1.Y > r2.Y + r2.Height))
				rtn = false;
			return (rtn);
		}



		public static int DistancePtToPt(Point p1,Point p2)

		{
			return ((int) Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)));
		}


		}
	}
