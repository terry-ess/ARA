using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using Usb.Events;


namespace RobotArm
	{
	static public class D415Camera
		{

		[DllImport("D415.dll")]
		private static extern bool Open();

		[DllImport("D415.dll")]
		private static extern void Close();

		[DllImport("D415.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern bool CaptureImages([MarshalAs(UnmanagedType.LPArray, SizeConst = WIDTH * HEIGHT)] short[] depth, [MarshalAs(UnmanagedType.LPArray, SizeConst = WIDTH * HEIGHT * BYTES_PER_PIXEL)] byte[] color);

		public const int WIDTH = 1280;
		public const int HEIGHT = 720;
		public const int BYTES_PER_PIXEL = 3;
		public const double VIDEO_VERT_FOV = 42.5;
		public const double VIDEO_HOR_FOV = 69.4;
		public const int MIN_Z = 450;
		public const double FRONT_OFFSET = .375;
		private const double CENTER_OFFSET = 1.75;
		public const double AC_Z_OFFSET = .375;
		public const double BASE_HEIGHT = 30.4375;
		private const double X_OFFSET = 2.0;

		private static bool connected = false;
		private static Mutex ci_mutex = new Mutex();

		public static int x_offset_mm = (int) Math.Round(X_OFFSET * Shared.IN_TO_MM);
		public static double ha, va;

		private static Bitmap bm = new Bitmap(D415Camera.WIDTH, D415Camera.HEIGHT, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		private static Rectangle rect = new Rectangle(0, 0, D415Camera.WIDTH, D415Camera.HEIGHT);


		public static bool COpen()

		{
			bool rtn = false;

			Shared.depthdata = new short[WIDTH * HEIGHT];
			Shared.videodata = new byte[WIDTH * HEIGHT * BYTES_PER_PIXEL];

			try
			{
			connected = rtn = Open();
			UsbEventWatcher usbew = new UsbEventWatcher();
			usbew.UsbDeviceRemoved += UsbEW;
			}

			catch (Exception ex)
			{
			Log.LogEntry("COpen exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return (rtn);
		}



		private static void UsbEW(object sender,UsbDevice device)

		{
			if ((device.ProductID == "0AD3") && connected)
				{
				Log.KeyLogEntry("D415 camera USB connection dropped.");
				Speech.PvSpeech.SpeakAsync("The D415 camera USB connection dropped.");
				connected = false;
				}
		}



		public static void CClose()

		{
			Close();
			connected = false;
		}



		public static bool CaptureImages()

		{
			bool rtn = false;

			if (connected)
				{
				ci_mutex.WaitOne();

				try
				{
				rtn = CaptureImages(Shared.depthdata,Shared.videodata);
				}

				catch(Exception ex)
				{
				Log.LogEntry("CaptureImage exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				ci_mutex.ReleaseMutex();
				if (rtn)
					{
					System.Drawing.Imaging.BitmapData bmd = bm.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
					System.Runtime.InteropServices.Marshal.Copy(Shared.videodata, 0, bmd.Scan0, D415Camera.WIDTH * D415Camera.HEIGHT * D415Camera.BYTES_PER_PIXEL);
					bm.UnlockBits(bmd);
					Shared.vimg = bm;   //do not clone, this leads to generic GDI+ error
					}
				}
			return (rtn);
		}



		public static bool ScanCaptureImages(short[] depthdata,byte[] videodata)

		{
			bool rtn = false;

			if (connected)
				{
				ci_mutex.WaitOne();

				try
				{
				rtn = CaptureImages(depthdata,videodata);
				}


				catch(Exception ex)
				{
				Log.LogEntry("CaptureImage exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				ci_mutex.ReleaseMutex();
				}
			return (rtn);
		}



		public static double VideoVerDegrees(int no_pixel)

		{
			double val,adj;

			adj = ((double) HEIGHT/2) / (Math.Tan((VIDEO_VERT_FOV / 2) * Shared.DEG_TO_RAD));
			val = Math.Atan(no_pixel / adj) * Shared.RAD_TO_DEG;
			return (val);
		}



		public static double VideoHorDegrees(int no_pixel)

		{
			double val,adj;

			adj = ((double) WIDTH/ 2) / (Math.Tan((VIDEO_HOR_FOV / 2) * Shared.DEG_TO_RAD));
			val = Math.Atan(no_pixel / adj) * Shared.RAD_TO_DEG;
			return (val);
		}



		private static int GetZdistMM(int row,int col,short[] depthdata)

		{
			ha = VideoHorDegrees((int)Math.Round(col - ((double)WIDTH / 2)));
			va = VideoVerDegrees((int)Math.Round(((double)HEIGHT / 2) - row));
			return(depthdata[row * WIDTH + col]);
		}



		public static bool DetermineLocCC(int row,int col,double tilt,ref Shared.space_3d_mm loc,bool log = true)

		{
			bool rtn = false;
			int zdist;

			zdist = GetZdistMM(row,col,Shared.depthdata);
			if (zdist >= MIN_Z)
				{
				loc.z = zdist;
				loc.x = (int) Math.Round(zdist * Math.Tan(ha * Shared.DEG_TO_RAD));
				loc.y = (int) Math.Round(zdist * Math.Tan(va * Shared.DEG_TO_RAD));
				if (log)
					{
					Log.LogEntry("Determine CC loc");
					Log.LogEntry("  row " + row + "  col " + col);
					Log.LogEntry("  Camera tilt (°): " + tilt);
					Log.LogEntry("  Horizontal angle (°): " + ha);
					Log.LogEntry("  Vertical angle (°): " + va);
					Log.LogEntry("  Z dist (in): " + (zdist * Shared.MM_TO_IN).ToString("F2"));
					}
				rtn = true;
				}
			return (rtn);
		}



		public static bool DetermineLocCC(int row,int col,short[] depthdata,double tilt,ref Shared.space_3d_mm loc,bool log = true)

		{
			bool rtn = false;
			int zdist;

			zdist = GetZdistMM(row,col,depthdata);
			if (zdist >= MIN_Z)
				{
				loc.z = zdist;
				loc.x = (int) Math.Round(zdist * Math.Tan(ha * Shared.DEG_TO_RAD));
				loc.y = (int) Math.Round(zdist * Math.Tan(va * Shared.DEG_TO_RAD));
				if (log)
					{
					Log.LogEntry("Determine CC loc");
					Log.LogEntry("  row " + row + "  col " + col);
					Log.LogEntry("  Camera tilt (°): " + tilt);
					Log.LogEntry("  Horizontal angle (°): " + ha);
					Log.LogEntry("  Vertical angle (°): " + va);
					Log.LogEntry("  Z dist (in): " + (zdist * Shared.MM_TO_IN).ToString("F2"));
					}
				rtn = true;
				}
			return (rtn);
		}



		public static  bool DetermineVid(Shared.space_3d_mm loc,double tilt,ref Point pt,bool log = true)

		{
			bool rtn = true;
			double ha,va,adj;
			int zdist,no_pixel,row,col;

			ha = Math.Atan((double) loc.x/loc.z) * Shared.RAD_TO_DEG;
			va = Math.Atan((double) loc.y/loc.z) * Shared.RAD_TO_DEG;
			zdist = (int) Math.Round(loc.x/Math.Tan(ha * Shared.DEG_TO_RAD));
			adj = ((double) WIDTH / 2) / Math.Tan((VIDEO_HOR_FOV / 2) * Shared.DEG_TO_RAD);
			no_pixel = (int) Math.Round(adj * Math.Tan(ha * Shared.DEG_TO_RAD));
			col = (int) Math.Round(((double) WIDTH/2) + no_pixel);
			adj = ((double)HEIGHT / 2) / Math.Tan((VIDEO_VERT_FOV / 2) * Shared.DEG_TO_RAD);
			no_pixel = (int) Math.Round(adj * Math.Tan(va * Shared.DEG_TO_RAD));
			row = (int) Math.Round(((double) HEIGHT/2) - no_pixel);
			pt.X = col;
			pt.Y = row;
			if (log)
				{
				Log.LogEntry("Determine Vid");
				Log.LogEntry("  row " + row + "  col " + col);
				Log.LogEntry("  Camera tilt (°): " + tilt);
				Log.LogEntry("  Horizontal angle (°): " + ha);
				Log.LogEntry("  Vertical angle (°): " + va);
				Log.LogEntry("  Z dist (in): " + (zdist * Shared.MM_TO_IN).ToString("F2"));
				}
			return (rtn);
		}



		public static double CameraHeight(double tilt) // tilt and cfa are down from the horizon (i.e. negative angles)

		{
			double ch,y2,cfa,cfd;

			cfa = Math.Atan(FRONT_OFFSET / CENTER_OFFSET) * Shared.RAD_TO_DEG;
			cfd = Math.Sqrt((CENTER_OFFSET * CENTER_OFFSET) + (FRONT_OFFSET * FRONT_OFFSET));
			y2 = cfd * Math.Cos((cfa + tilt) * Shared.DEG_TO_RAD);
			ch = BASE_HEIGHT + (y2 - CENTER_OFFSET);
			return(ch);
		}



		public static int CameraForwardDist(double tilt)

		{
			double cfa, cfd, x2;

			cfa = Math.Atan(FRONT_OFFSET / CENTER_OFFSET) * Shared.RAD_TO_DEG;
			cfd = Math.Sqrt((CENTER_OFFSET * CENTER_OFFSET) + (FRONT_OFFSET * FRONT_OFFSET));
			x2 = (cfd * Math.Sin((cfa + tilt) * Shared.DEG_TO_RAD)) - FRONT_OFFSET;
			return ((int) Math.Round(x2 * Shared.IN_TO_MM));
		}


		}
	}
