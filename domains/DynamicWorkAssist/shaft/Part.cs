using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RobotArm;
using DynamicWorkAssist;

namespace shaft
{
    public class Part
    {

		const double PERP_RATIO = .5;

		Parts.Part part;


		public void Open(Parts.Part prt)

		{	
			part = prt;
			Parts.RegisterHandler(part.name,DeterminePartData2D);
		}


		
		bool DeterminePartData2D(Bitmap bm, Rectangle rect, bool surface, ref DomainShared.ObjData od)

		{
			bool rtn = false,no_min = false;
			int i,j,maxy = 0, maxx = 0,maxz = 0,minz = 0,count = 0,minx = 0,miny = 0,maxw = 0,clipped = 0,minh = 0,too_low = 0,maxh = 0;
			long sy = 0, sz = 0,sx = 0;
			RobotArm.Shared.space_3d_mm rcloc = new RobotArm.Shared.space_3d_mm(),ccloc = new RobotArm.Shared.space_3d_mm(),minxpt = new RobotArm.Shared.space_3d_mm();
			RobotArm.Shared.space_3d_mm maxypt = new RobotArm.Shared.space_3d_mm(),maxzpt = new RobotArm.Shared.space_3d_mm(),maxxpt = new RobotArm.Shared.space_3d_mm();
			RobotArm.Shared.space_3d_mm minzpt = new RobotArm.Shared.space_3d_mm(), minypt = new RobotArm.Shared.space_3d_mm();
			TextWriter tw = null;
			DateTime now = DateTime.Now;
			string fname;
			Color color;
			Point pt = new Point(),tpt = new Point(),fpt = new Point();

			fname = RobotArm.Log.LogDir() + "Object analysis " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + RobotArm.Shared.GetUFileNo() + ".csv";
			if (surface)
				no_min = true;
			else
				minh = HandDetect.TOP_HEIGHT_CLEAR_MM;
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine("Object data set");
				tw.WriteLine(now.ToShortDateString() + " " + now.ToShortTimeString());
				if (no_min)
					tw.WriteLine("min height(mm): none");
				else
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
						RobotArm.D415Camera.DetermineLocCC(j + rect.Y, i + rect.X, RobotArm.CameraPanTilt.tilt_deg, ref ccloc, false);
						RobotArm.Shared.ConvertCC_to_RC(ccloc, ref rcloc, RobotArm.CameraPanTilt.tilt_deg, false);
						if ((no_min | (rcloc.y > minh)) && (rcloc.z > 0))
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
							maxh = minz + part.max_dim_mm;
							if ((rcloc.x <= maxw)  && (rcloc.z <= maxh))
								{
								if (tw != null)
									tw.WriteLine(rcloc.x + "," + rcloc.z + "," + rcloc.y);
								count += 1;
								if (rcloc.y > maxy )
									{
									maxy = rcloc.y;
									maxypt.x = rcloc.x;
									maxypt.y = rcloc.y;
									maxypt.z = rcloc.z;
									}
								if (rcloc.y < miny)
									{
									miny = rcloc.y;
									minypt.x = rcloc.x;
									minypt.y = rcloc.y;
									minypt.z = rcloc.z;
									}
								if (rcloc.x < minx)
									{
									minx = rcloc.x;
									minxpt.x = rcloc.x;
									minxpt.y = rcloc.y;
									minxpt.z = rcloc.z;
									}
								if (rcloc.x > maxx)
									{
									maxx = rcloc.x;
									maxxpt.x = rcloc.x;
									maxxpt.y = rcloc.y;
									maxxpt.z = rcloc.z;
									}
								if (rcloc.z > maxz)
									{
									maxz = rcloc.z;
									maxzpt.x = rcloc.x;
									maxzpt.y = rcloc.y;
									maxzpt.z = rcloc.z;
									}
								if (rcloc.z < minz)
									{
									minz = rcloc.z;
									minzpt.x = rcloc.x;
									minzpt.y = rcloc.y;
									minzpt.z = rcloc.z;
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
				double dnx,dx;

				if (tw != null)
					{
					tw.WriteLine();
					tw.WriteLine();
					}
				od.maxy = maxy;
				od.minz = minz;
				od.center.x = (int) Math.Round((double) sx/count);
				od.center.y = (int) Math.Round((double) sy/count);
				od.center.z = (int) Math.Round((double) sz/count);
				if (tw != null)
					{
					tw.WriteLine(",,X,Y,Z");
					tw.WriteLine("Center point," + od.center.ToCsvString());
					tw.WriteLine("Min X first pt," + minxpt.ToCsvString());
					tw.WriteLine("Max X first pt," + maxxpt.ToCsvString());
					if (surface)
						{
						tw.WriteLine("Max Z first pt," + maxzpt.ToCsvString());
						tw.WriteLine("Min Z first pt," + minzpt.ToCsvString());
						}
					else
						{
						tw.WriteLine("Max Y first pt," + maxypt.ToCsvString());
						tw.WriteLine("Min Y first pt," + minypt.ToCsvString());
						}
					}
				dx = maxxpt.x - minxpt.x;
				if (surface)
					{ 
					od.pick = od.center;
					dnx = maxzpt.z - minzpt.z;
					}
				else
					{
					dnx = maxypt.y - minypt.y;
					}
				if (dx/dnx < PERP_RATIO)
					{
					tpt.X = maxzpt.x;
					fpt.X = od.center.x;
					od.end = maxzpt;
					if (surface)
						{
						tpt.Y = maxzpt.z;
						fpt.Y = od.center.z;
						}
					else
						{
						tpt.Y = maxzpt.y;
						fpt.Y = od.center.y;
						od.pick.x = (int) Math.Round((double) (maxzpt.x + od.center.x)/2);
						od.pick.y = (int)Math.Round((double) (maxzpt.y + od.center.y)/ 2);
						od.pick.z = (int)Math.Round((double) (maxzpt.z + od.center.z)/ 2);
						}
					}
				else
					{
					tpt.X = minxpt.x;
					fpt.X = od.center.x;
					od.end = minxpt;
					if (surface)
						{
						tpt.Y = minxpt.z;
						fpt.Y = od.center.z;
						}
					else
						{
						tpt.Y = minxpt.y;
						fpt.Y = od.center.y;
						od.pick = minxpt;
						od.pick.x = (int) Math.Round((double) (minxpt.x + od.center.x)/ 2);
						od.pick.y = (int) Math.Round((double) (minxpt.y + od.center.y)/ 2);
						od.pick.z = (int) Math.Round((double) (minxpt.z + od.center.z)/ 2);
						}
					}
				od.planeorient = Shared.DetermineOrient(tpt, fpt);
				if (Math.Abs(od.planeorient) > 90)
					if (od.planeorient > 0)
						od.planeorient -= 180;
					else
						od.planeorient += 180;
				if (tw != null)
					{
					tw.WriteLine("dx/dnx," + dx/dnx);
					tw.WriteLine("center point," + od.center.ToCsvString());
					tw.WriteLine("end point," + od.end.ToCsvString());
					tw.WriteLine("pick point," + od.pick.ToCsvString());
					tw.WriteLine("gripper orientation," + od.planeorient);
					tw.WriteLine();
					tw.WriteLine();
					tw.WriteLine("final maxw: " + maxw);
					tw.WriteLine("final maxh: " + maxh);
					tw.WriteLine("clipped pts: " + clipped);
					tw.WriteLine("too low pts: " + too_low);
					tw.WriteLine("no pts: " + count + "  " + od.ToString());
					tw.WriteLine("maxx: " + maxx + "   minx: " + minx + "   maxy: " + maxy + "   miny: " + miny + "  maxz: " + maxz + "   minz: " + minz);
					tw.Close();
					RobotArm.Log.LogEntry("Saved: " + fname);
					}
				Bitmap nbm = (Bitmap)RobotArm.Shared.vimg.Clone();
				Graphics g = Graphics.FromImage(nbm);
				if (!surface)
					{
					RobotArm.Shared.ConvertRC_to_CC(od.center, ref ccloc, RobotArm.CameraPanTilt.tilt_deg);
					RobotArm.D415Camera.DetermineVid(ccloc, RobotArm.CameraPanTilt.tilt_deg, ref pt);
					g.DrawRectangle(Pens.Red, rect);
					rect.X = pt.X - 2;
					rect.Y = pt.Y - 2;
					rect.Width = 4;
					rect.Height = 4;
					g.FillRectangle(Brushes.Red, rect);
					}
				RobotArm.Shared.ConvertRC_to_CC(od.pick, ref ccloc, RobotArm.CameraPanTilt.tilt_deg);
				RobotArm.D415Camera.DetermineVid(ccloc, RobotArm.CameraPanTilt.tilt_deg, ref pt);
				g.DrawRectangle(Pens.Red,rect);
				rect.X = pt.X - 2;
				rect.Y = pt.Y - 2;
				rect.Width = 4;
				rect.Height = 4;
				g.FillRectangle(Brushes.Red,rect);
				RobotArm.Shared.ConvertRC_to_CC(od.end, ref ccloc, RobotArm.CameraPanTilt.tilt_deg);
				RobotArm.D415Camera.DetermineVid(ccloc, RobotArm.CameraPanTilt.tilt_deg, ref pt);
				rect.X = pt.X - 2;
				rect.Y = pt.Y - 2;
				rect.Width = 4;
				rect.Height = 4;
				g.FillRectangle(Brushes.Yellow, rect);
				RobotArm.Shared.SaveVideoPic(nbm);
				RobotArm.Shared.SaveDeptBin();
				rtn = true;
				}
			else if (tw != null)
				{
				tw.WriteLine();
				tw.WriteLine(count + " points is insufficent data for analysis");
				tw.Close();
				}
			else
				RobotArm.Log.LogEntry(count + " points is insufficent data for analysis");
			return (rtn);
		}

	}
}
