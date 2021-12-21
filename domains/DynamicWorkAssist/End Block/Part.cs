using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RobotArm;
using DynamicWorkAssist;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;

namespace end_block
{
    public class Part
    {

		const int ORIENT_CORRECT = 32;
		const int CLOSE = 10;
		const int ORIENT_LIMIT = 5;	//this reflects the fact that user is to the right of the arm...

		Parts.Part part;


		public void Open(Parts.Part prt)

		{
			part = prt;
			Parts.RegisterHandler(part.name, DeterminePartData2D);
		}



		bool DeterminePartData2D(Bitmap bm, Rectangle rect, bool surface, ref DomainShared.ObjData od)

		{
			bool rtn = false,not_clip;
			int i, j, maxy = 0, maxx = 0, maxz = 0, minz = 0, count = 0, minx = 0, miny = 0, maxw = 0, clipped = 0,minh,maxh = 0,too_low = 0,x,y,w,h,screen_size,pixel,outbd = 0;
			long sy = 0, sz = 0, sx = 0;
			Shared.space_3d_mm rcloc = new RobotArm.Shared.space_3d_mm(), ccloc = new RobotArm.Shared.space_3d_mm();
			TextWriter tw = null;
			DateTime now = DateTime.Now;
			string fname;
			Point pt = new Point();
			Color color;
			ArrayList data = new ArrayList();
			byte[] bdata;
			System.Drawing.Imaging.BitmapData bmd = new System.Drawing.Imaging.BitmapData();
			CvBlobs blobs = new CvBlobs();
			IplImage pic, gs, img;
			CvBlob b;
			double angle = 0, dist = 0;

			fname = Log.LogDir() + "Object analysis " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + Shared.GetUFileNo() + ".csv";
			if (surface)
				minh = (int)-Math.Round(Mapping.fmr.avg_abs_err);
			else
				minh = HandDetect.TOP_HEIGHT_CLEAR_MM;
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine("Object data set");
				tw.WriteLine(now.ToShortDateString() + " " + now.ToShortTimeString());
				tw.WriteLine("min height (mm): " + minh);
				tw.WriteLine("object name: " + part.name);
				tw.WriteLine("surface: " + surface);
				tw.WriteLine();
				tw.WriteLine("X,Z,Y");
				}
			for (i = 0; i < rect.Width; i++)
				{
				for (j = 0; j < rect.Height; j++)
					{
					color = bm.GetPixel(i, j);
					if ((color.R == 255) && (color.G == 255) & (color.B == 255))
						{
						D415Camera.DetermineLocCC(j + rect.Y, i + rect.X, CameraPanTilt.tilt_deg, ref ccloc, false);
						Shared.ConvertCC_to_RC(ccloc, ref rcloc, CameraPanTilt.tilt_deg, false);
						if ((rcloc.y > minh) && (rcloc.z > 0))
							{
							if (count == 0)
								{
								minx = rcloc.x + 1;
								maxx = rcloc.x - 1;
								miny = rcloc.y + 1;
								maxy = rcloc.y - 1;
								maxz = rcloc.z - 1;
								minz = rcloc.z + 1;
								}
							maxw = minx + part.max_dim_mm;
							if (surface)
								{
								maxh = minz + part.max_dim_mm;
								not_clip = (rcloc.x <= maxw) && (rcloc.z <= maxh);
								}
							else
								{
								maxh = miny + part.max_dim_mm;
								not_clip = (rcloc.x <= maxw) && (rcloc.y <= maxh);
								}
							if (not_clip)
								{
								if (tw != null)
									tw.WriteLine(rcloc.x + "," + rcloc.z + "," + rcloc.y);
								count += 1;
								data.Add(rcloc);
								if (rcloc.y > maxy)
									{
									maxy = rcloc.y;
									}
								if (rcloc.y < miny)
									{
									miny = rcloc.y;
									}
								if (rcloc.x < minx)
									{
									minx = rcloc.x;
									}
								if (rcloc.x > maxx)
									{
									maxx = rcloc.x;
									}
								if (rcloc.z > maxz)
									{
									maxz = rcloc.z;
									}
								if (rcloc.z < minz)
									{
									minz = rcloc.z;
									}
								sy += rcloc.y;
								sz += rcloc.z;
								sx += rcloc.x;
								}
							else
								clipped += 1;
							}
						else if (rcloc.z > 0)
							too_low += 1;
						}
					}
				}
			if (count > Parts.MIN_BLOB_AREA)
				{
				if (tw != null)
					{
					tw.WriteLine();
					tw.WriteLine();
					}
				od.maxy = maxy;
				od.minz = minz;
				od.center.x = (int)Math.Round((double)sx / count);
				od.center.y = (int)Math.Round((double)sy / count);
				od.center.z = (int)Math.Round((double)sz / count);
				w = maxx - minx;
				while (((w * 3) % 4) != 0)
					w += 1;
				if (surface)
					h = maxz - minz + 1;
				else
					h = maxy - miny + 1;
				screen_size = w * h;
				bdata = new byte[screen_size * 3];
				bm = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				rect = new Rectangle(0, 0, w, h);
				for (i = 0;i < data.Count;i++)
					{
					rcloc = (Shared.space_3d_mm) data[i];
					x = rcloc.x - minx;
					if (surface)
						y = maxz - rcloc.z;
					else
						y = maxy - rcloc.y;
					pixel = (y * w) + x;
					if (pixel < screen_size)
						{
						bdata[3 * pixel] = 255;
						bdata[(3 * pixel) + 1] = 255;
						bdata[(3 * pixel) + 2] = 255;
						}
					else
						outbd += 1;
					}
				bmd = bm.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				System.Runtime.InteropServices.Marshal.Copy(bdata, 0, bmd.Scan0, w * h * 3);
				bm.UnlockBits(bmd);
				pic = new IplImage(w, h, BitDepth.U8, 3);
				gs = new IplImage(pic.Size, BitDepth.U8, 1);
				img = new IplImage(pic.Size, BitDepth.F32, 1);
				pic = bm.ToIplImage();
				Cv.CvtColor(pic, gs, ColorConversion.BgrToGray);
				blobs.Label(gs, img);
				b = blobs[blobs.GreaterBlob()];
				if (b.Area >= Parts.MIN_BLOB_AREA)
					{
					b.CalcCentralMoments(img);
					od.planeorient = (90 + (b.CalcAngle() * Shared.RAD_TO_DEG));
					if (Math.Abs(od.planeorient) > 90)
						if (od.planeorient > 0)
							od.planeorient -= 180;
						else
							od.planeorient += 180;
					if (surface)
						od.pick = od.center;
					else
						{
						dist = .9 * (part.max_dim_mm/2);
						if (od.planeorient >= ORIENT_LIMIT)
							angle = od.planeorient - 180;
						else
							angle = od.planeorient;
						od.end.x = (int) Math.Round(od.center.x + (dist * Math.Sin(angle * Shared.DEG_TO_RAD)));
						od.end.y = (int) Math.Round(od.center.y + ((dist * (Math.Cos(angle * Shared.DEG_TO_RAD)))));
						od.end.z = od.center.z;
						od.pick.x = (int)Math.Round(((double)od.center.x + od.end.x) / 2);
						od.pick.y = (int)Math.Round(((double)od.center.y + od.end.y) / 2);
						od.pick.z = (int)Math.Round(((double)od.center.z + od.end.z) / 2);
						}
					if (tw != null)
						{
						tw.WriteLine("center point," + od.center.ToCsvString());
						tw.WriteLine("end point," + od.end.ToCsvString());
						tw.WriteLine("pick point," + od.pick.ToCsvString());
						tw.WriteLine("gripper orientation," + od.planeorient);
						if (!surface)
							{
							tw.WriteLine("end direction," + angle.ToString("F1"));
							tw.WriteLine("end dist," + dist.ToString("F1"));
							}
						tw.WriteLine();
						tw.WriteLine("clipped pts: " + clipped);
						tw.WriteLine("too low pts: " + too_low);
						tw.WriteLine("out of bound pts: " + outbd);
						tw.WriteLine("no pts: " + count + "  " + od.ToString());
						tw.WriteLine("maxx: " + maxx + "   minx: " + minx + "   maxy: " + maxy + "   miny: " + miny + "  maxz: " + maxz + "   minz: " + minz);
						tw.Close();
						Log.LogEntry("Saved: " + fname);
						}
					Shared.ConvertRC_to_CC(od.pick, ref ccloc, CameraPanTilt.tilt_deg);
					D415Camera.DetermineVid(ccloc, CameraPanTilt.tilt_deg, ref pt);
					Bitmap nbm = (Bitmap)Shared.vimg.Clone();
					Graphics g = Graphics.FromImage(nbm);
					rect.X = pt.X - 2;
					rect.Y = pt.Y - 2;
					rect.Width = 4;
					rect.Height = 4;
					g.FillRectangle(Brushes.Red, rect);
					if ((od.center.x != od.pick.x) || (od.center.y != od.pick.y) || (od.center.z != od.pick.z))
						{
						Shared.ConvertRC_to_CC(od.center, ref ccloc, CameraPanTilt.tilt_deg);
						D415Camera.DetermineVid(ccloc, CameraPanTilt.tilt_deg, ref pt);
						rect.X = pt.X - 2;
						rect.Y = pt.Y - 2;
						rect.Width = 4;
						rect.Height = 4;
						g.FillRectangle(Brushes.Green, rect);
						}
					if ((od.end.x != 0) || (od.end.y != 0) || (od.end.z != 0))
						{
						Shared.ConvertRC_to_CC(od.end, ref ccloc, CameraPanTilt.tilt_deg);
						D415Camera.DetermineVid(ccloc, CameraPanTilt.tilt_deg, ref pt);
						rect.X = pt.X - 2;
						rect.Y = pt.Y - 2;
						rect.Width = 4;
						rect.Height = 4;
						g.FillRectangle(Brushes.Yellow, rect);
						}
					Shared.SaveVideoPic(nbm);
					Shared.SaveDeptBin();
					rtn = true;
					}
				else if (tw != null)
					{
					tw.WriteLine();
					tw.WriteLine(b.Area + " points is insufficient data for analysis");
					tw.Close();
					Log.LogEntry("Saved: " + fname);
					}
				else
					Log.LogEntry(b.Area + " points is insufficient data for analysis");
				}
			else if (tw != null)
				{
				tw.WriteLine();
				tw.WriteLine(count + " points is insufficient data for analysis");
				tw.Close();
				Log.LogEntry("Saved: " + fname);
				}
			else
				Log.LogEntry(count + " points is insufficient data for analysis");
			return (rtn);
		} 

		}

		}
