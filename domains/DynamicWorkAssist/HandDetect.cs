using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using RobotArm;


namespace DynamicWorkAssist
	{
	public static class HandDetect
		{

		public const double MIN_HAND_OD_SCORE = .6;
		public const string HAND_OD_MODEL = "hand";
		public const int HAND_OD_ID = 1;

		private const int DIST_DELTA = 102;
		public const int TOP_HEIGHT_CLEAR_MM = 64;
		public const int MIN_BLOB_AREA = 2000;


		public struct HandData
		{
			public VisualObjectDetection.visual_detected_object vdo;
			public int dist;
			public double angle;
			public Shared.space_3d_mm center;
		};


		public enum hand_pt {PALM,FINGERTIP };

		private static Bitmap bm = new Bitmap(D415Camera.WIDTH, D415Camera.HEIGHT, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		private static Rectangle rect = new Rectangle(0, 0, D415Camera.WIDTH, D415Camera.HEIGHT);


		public static ArrayList DetectHands(Bitmap bm,short[] depthdata,bool log = true)

		{
			ArrayList hdlist = new ArrayList();
			ArrayList rlist = new ArrayList();
			HandData hdata = new HandData();
			int i,j,k,basey,basex,dist = 0;
			Shared.space_3d_mm ccloc = new RobotArm.Shared.space_3d_mm();

			hdlist = Shared.Detect(HAND_OD_MODEL, bm, MIN_HAND_OD_SCORE,HAND_OD_ID,null,log);
			for (k = 0;k < hdlist.Count;k++)
				{ 
				hdata.vdo = (VisualObjectDetection.visual_detected_object) hdlist[k];
				hdata.angle = (int)Math.Round(D415Camera.VideoHorDegrees((int)Math.Round((hdata.vdo.x + ((double) hdata.vdo.width / 2)) - ((double) hdata.vdo.width / 2))));
				hdata.dist = -1;
				basey = hdata.vdo.y + (hdata.vdo.height / 2);
				basex = hdata.vdo.x + (hdata.vdo.width / 2);
				for (i = -5; i < 5; i++)
					{
					for (j = -5; j < 5; j++)
						{
						dist = depthdata[((basey + i) * D415Camera.WIDTH) +(basex + j)];
						if (dist > D415Camera.MIN_Z)
							{
							hdata.dist = dist;
							D415Camera.DetermineLocCC(basey + i, basex + j,depthdata, CameraPanTilt.tilt_deg, ref ccloc, false);
							Shared.ConvertCC_to_RC(ccloc, ref hdata.center, CameraPanTilt.tilt_deg, false);
							rlist.Add(hdata);
							break;
							}
						}
					if (dist > D415Camera.MIN_Z)
						break;
					}
				}
			return (rlist);
		}



		private static ArrayList DetectHands(bool log = true)

		{
			ArrayList rlist = new ArrayList();

			if (D415Camera.CaptureImages())
				{
				rlist = DetectHands((Bitmap) Shared.vimg,Shared.depthdata,log);
				}
			return(rlist);
		}



		private static ArrayList DetectHands(short[] depthdata, byte[] videodata)

		{
			ArrayList rlist = new ArrayList();

			if (D415Camera.ScanCaptureImages(depthdata,videodata))
				{
				System.Drawing.Imaging.BitmapData bmd = bm.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				System.Runtime.InteropServices.Marshal.Copy(videodata, 0, bmd.Scan0, D415Camera.WIDTH * D415Camera.HEIGHT * D415Camera.BYTES_PER_PIXEL);
				bm.UnlockBits(bmd);
				rlist = DetectHands(bm,depthdata,false);
				}
			return (rlist);
		}



		public static bool HandsInWorkspace()

		{
			bool rtn = false;
			ArrayList hands = new ArrayList();
			int i;
			Stopwatch sw = new Stopwatch();

			sw.Start();
			hands = DetectHands(false);
			if (hands.Count > 0)
				{
				for (i = 0; i < hands.Count; i++)
					{
					if (Mapping.WithinWorkSpace(((HandData)hands[i]).center))
						{
						rtn = true;
						break;
						}
					}
				}
			sw.Stop();
			Log.LogEntry("HandsInWorkspace " + rtn + " in " + sw.ElapsedMilliseconds + " ms.");
			return (rtn);
		}



		public static bool ScanHandsInWorkspace(short[] depthdata,byte[] videodata)
		{
			bool rtn = false;
			ArrayList hands = new ArrayList();
			int i;
			Stopwatch sw = new Stopwatch();

			sw.Start();
			hands = DetectHands(depthdata,videodata);
			if (hands.Count > 0)
				{
				for (i = 0; i < hands.Count; i++)
					{
					Log.LogEntry("Hand center: " + ((HandData)hands[i]).center);
					if (Mapping.WithinWorkSpace(((HandData)hands[i]).center))
						{
						rtn = true;
						break;
						}
					} 
				}
			sw.Stop();
			Log.LogEntry("ScanHandsInWorkspace " + rtn + " in " + sw.ElapsedMilliseconds + " ms.");
			return (rtn);
		}


		}
	}
