using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using RobotArm;
using Speech;

namespace DynamicWorkAssist
	{
	public static class HdHandDetect
		{

		public const double MIN_HDHAND_OD_SCORE = .6;
		public const string HDHAND_OD_MODEL = "hd hand";
		public const int HDHAND_OD_ID = 1;

		public struct HandPts
		{
			public Shared.space_3d_mm center;
			public Shared.space_3d_mm ft;
			public int maxh;
			public int minz;
			public int width;
		}


		private static bool DetermineHandKeyPoints(HandDetect.HandData hdata,ref HandPts hp)

		{
			bool rtn = true;
			int i,j,pixel,odist,maxy = 0;
			long sx = 0,sy = 0,sz = 0;
			int count = 0,minx = 0,minz = ArmControl.MAX_REACH_FLAT_MM;
			Shared.space_3d_mm rcloc = new RobotArm.Shared.space_3d_mm(),ccloc = new RobotArm.Shared.space_3d_mm(),minxpt = new Shared.space_3d_mm(),toploc = new Shared.space_3d_mm(),bottomloc = new Shared.space_3d_mm();
			TextWriter tw = null;
			DateTime now = DateTime.Now;
			string fname;
			Point pt = new Point();
			ArrayList minxpts = new ArrayList();

			fname = Log.LogDir() + "Hand analysis " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + Shared.GetUFileNo() + ".csv";
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine("Hand analysis data set");
				tw.WriteLine(now.ToShortDateString() + " " + now.ToShortTimeString());
				tw.WriteLine();
				tw.WriteLine("X,Z,Y");
				}
			for (i = hdata.vdo.x; i < hdata.vdo.x + hdata.vdo.width; i++)
				{
				for (j = hdata.vdo.y; j < hdata.vdo.y + hdata.vdo.height; j++)
					{
					pixel = (j * D415Camera.WIDTH) + i;
					odist = Shared.depthdata[pixel];
					if (odist > D415Camera.MIN_Z)
						{
						D415Camera.DetermineLocCC(j, i, CameraPanTilt.tilt_deg, ref ccloc, false);
						Shared.ConvertCC_to_RC(ccloc, ref rcloc, CameraPanTilt.tilt_deg, false);
						if (rcloc.y > HandDetect.TOP_HEIGHT_CLEAR_MM)
							{
							if (count == 0)
								minx = rcloc.x + 1;
							if (tw != null)
								tw.WriteLine(rcloc.x + "," + rcloc.z + "," + rcloc.y);
							count += 1;
							sx += rcloc.x;
							sy += rcloc.y;
							sz += rcloc.z;
							if (rcloc.z < minz)
								minz = rcloc.z;
							if (rcloc.y > maxy )
								maxy = rcloc.y;
							if (rcloc.x < minx)
								{
								minxpts.Clear();
								minx = rcloc.x;
								minxpt.x = rcloc.x;
								minxpt.y = rcloc.y;
								minxpt.z = rcloc.z;
								minxpts.Add(minxpt);
								}
							else if (rcloc.x == minx)
								{
								minxpt.x = rcloc.x;
								minxpt.y = rcloc.y;
								minxpt.z = rcloc.z;
								minxpts.Add(minxpt);
								}
							}
						}
					}
				}
			hp.center.x = (int) Math.Round(((double) sx) / count);
			hp.center.z = (int) Math.Round(((double) sz) / count);
			hp.center.y = (int) Math.Round(((double) sy) / count);
			hp.maxh = maxy;
			hp.ft.x = minx;
			hp.minz = minz;
			sy = sz = 0;
			for (i = 0;i < minxpts.Count;i++)
				{
				minxpt = (Shared.space_3d_mm) minxpts[i];
				sy += minxpt.y;
				sz += minxpt.z;
				}
			hp.ft.z = (int) Math.Round((double) sz/minxpts.Count);
			hp.ft.y = (int) Math.Round((double) sy/minxpts.Count);
			Point mpt = new Point(hp.center.x,hp.center.z),fpt = new Point(hp.ft.x,hp.ft.z);
			double angle = Shared.DetermineOrient(fpt,mpt);
			angle = (angle + 90) % 360;
			double dc = Math.Sin(angle * Shared.DEG_TO_RAD);
			double dr = Math.Cos(angle * Shared.DEG_TO_RAD);
			if (tw != null)
				{
				tw.WriteLine();
				tw.WriteLine("Width angle: " + angle.ToString("F1") + "  dcol: " + dc.ToString("F2") + "  drow: " + dr.ToString("F2"));
				}
			Point mrc = new Point(),start,e1,e2;
			Shared.ConvertRC_to_CC(hp.center, ref ccloc, CameraPanTilt.tilt_deg);
			if (D415Camera.DetermineVid(ccloc,CameraPanTilt.tilt_deg,ref mrc,true))
				{
				start = mrc;
				if (tw != null)
					tw.Write("width scan (RC):  [mid] " + hp.center.ToString());
				for (i = 1;;i++)
					{
					mrc.X = (int) Math.Round(start.X - (i * dc));
					mrc.Y = (int) Math.Round(start.Y + (i * dr));
					if ((D415Camera.DetermineLocCC(mrc.Y,mrc.X,CameraPanTilt.tilt_deg,ref ccloc,false)) && Shared.ConvertCC_to_RC(ccloc, ref rcloc, CameraPanTilt.tilt_deg,false))
						{
						if (rcloc.y < HandDetect.TOP_HEIGHT_CLEAR_MM)
							break;
						else
							bottomloc = rcloc;
						}
					}
				mrc = start;
				for (i = 1; ; i++)
					{
					mrc.X = (int)Math.Round(start.X + (i * dc));
					mrc.Y = (int)Math.Round(start.Y - (i * dr));
					if ((D415Camera.DetermineLocCC(mrc.Y, mrc.X, CameraPanTilt.tilt_deg, ref ccloc, false)) && Shared.ConvertCC_to_RC(ccloc, ref rcloc, CameraPanTilt.tilt_deg,false))
						{
						if (rcloc.y < HandDetect.TOP_HEIGHT_CLEAR_MM)
							break;
						else
							toploc = rcloc;
						}
					}
				e1 = new Point(toploc.x,toploc.z);
				e2 = new Point(bottomloc.x,bottomloc.z);
				hp.width = Shared.DistancePtToPt(e1,e2);
				if (tw != null)
					tw.Write("; [top] " + toploc.ToString() + "; [bottom] " + bottomloc.ToString());
				}
			else
				hp.width = 0;
			if (count > HandDetect.MIN_BLOB_AREA)
				rtn = true;
			if (tw != null)
				{
				tw.WriteLine();
				tw.WriteLine("no pts: " + count + "    mid-pt (mm): " + hp.center + "    ft-pt (mm): " + hp.ft  + "    width (mm): " + hp.width + "    max height(mm): " + maxy + "   min z (mm): " + minz);
				tw.Close();
				Log.LogEntry("Saved " + fname);
				}
			Shared.ConvertRC_to_CC(hp.center, ref ccloc, CameraPanTilt.tilt_deg);
			D415Camera.DetermineVid(ccloc, CameraPanTilt.tilt_deg, ref pt);
			Bitmap bm = (Bitmap)Shared.vimg.Clone();
			Graphics g = Graphics.FromImage(bm);
			Rectangle rect = new Rectangle();
			rect.X = hdata.vdo.x;
			rect.Y = hdata.vdo.y;
			rect.Width = hdata.vdo.width;
			rect.Height = hdata.vdo.height;
			g.DrawRectangle(Pens.Red,rect);
			rect.X = pt.X - 2;
			rect.Y = pt.Y - 2;
			rect.Width = 4;
			rect.Height = 4;
			g.FillRectangle(Brushes.Red,rect);
			Shared.ConvertRC_to_CC(hp.ft, ref ccloc, CameraPanTilt.tilt_deg);
			D415Camera.DetermineVid(ccloc, CameraPanTilt.tilt_deg, ref pt);
			rect.X = pt.X - 2;
			rect.Y = pt.Y - 2;
			rect.Width = 4;
			rect.Height = 4;
			g.FillRectangle(Brushes.Yellow, rect);
			Shared.SaveVideoPic(bm);
			Shared.SaveDeptBin();
			DynamicWorkAssist.DisplayVideoImage((Image) bm.Clone());
			return (rtn);
		}



		public static ArrayList DetectHands(Bitmap bm,short[] depthdata,bool log = true)

		{
			ArrayList hdlist = new ArrayList();
			ArrayList rlist = new ArrayList();
			HandDetect.HandData hdata = new HandDetect.HandData();
			int i,j,k,basey,basex,dist = 0;
			Shared.space_3d_mm ccloc = new RobotArm.Shared.space_3d_mm();

			hdlist = Shared.Detect(HDHAND_OD_MODEL, bm, MIN_HDHAND_OD_SCORE,HDHAND_OD_ID,null,log);
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



		public static bool WorkHandDetect(ref HandDetect.HandData hd)

		{	//selection criteria: hand closest to the center line, above the surface, within reach
			bool rtn = false;
			ArrayList hands = new ArrayList();
			int i, idx = 0;
			int min_dist,dist;
			double reach;

			Log.LogEntry("WorkHandDetect");
			hands = DetectHands();
			if (hands.Count > 0)
				{
				if (hands.Count > 1)
					{
					min_dist = 10000;
					idx = 0;
					for (i = 0; i < hands.Count; i++)
						{
						if (((HandDetect.HandData)hands[i]).center.y > HandDetect.TOP_HEIGHT_CLEAR_MM)
							{
							dist = Math.Abs(((HandDetect.HandData)hands[i]).center.x);
							if (dist < min_dist)
								{
								min_dist = dist;
								idx = i;
								}
							}
						}
					hd = (HandDetect.HandData)hands[idx];
					}
				else 
					hd = (HandDetect.HandData)hands[0];
				reach = Math.Sqrt(Math.Pow(hd.center.x, 2) + Math.Pow(hd.center.y, 2) + Math.Pow(hd.center.z + ArmControl.Z_OFFSET_MM, 2));
				if ((reach < ArmControl.MAX_REACH_FLAT_MM) && (hd.center.y > HandDetect.TOP_HEIGHT_CLEAR_MM))
					{
					rtn = true;
					Log.LogEntry("Selected hand " + idx);
					}
				else
					Log.LogEntry("Detected hand (" + idx + ") was not above the surface or out of reach.");
				}
			else
				Log.LogEntry("No hands detected.");
			return (rtn);
		}



		public static bool WorkHandDetect(ref HandPts hpts)	//ignoring possible robot arm - hand overlap in this implementation

		{
			ArrayList hands = new ArrayList();
			bool pos_deter = false;
			HandDetect.HandData hd = new HandDetect.HandData();
			Stopwatch sw = new Stopwatch();

			sw.Start();
			if (WorkHandDetect(ref hd))
				{
				if (DetermineHandKeyPoints( hd,ref hpts))
					pos_deter = true;
				else
					{
					Log.LogEntry("Could not determine key hand points.");
					PvSpeech.SpeakAsync("Could not determine key hand points.");
					}
				}
			sw.Stop();
			Log.LogEntry("execution time (ms): " + sw.ElapsedMilliseconds);
			return (pos_deter);
		}

		}
	}
