using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using RobotArm;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;


namespace DynamicWorkAssist
	{
	public static class Containers
		{

		private const int MIN_BLOB_AREA = 5000;
		public const int Y_PICK_OFFSET_MM = 25;
		private const int SAFE_CONTAINER_X_MM = -200;
		private const int MAX_CON_DIST_PIX = 15;
		private const int LOW_HIGH_THRESHOLD = 5;
		private const int MAX_PLAN_ANGLE = 10;


		public enum place_reply {NONE,OK,OBSTACLE,FAIL};

		public struct Container

		{
			public string name;
			public int od_model_id;
			public int od_id;
			public string is_model;
			public int width;
			public int length;
			public int side_height;
			public bool top;
			public Shared.space_3d_mm mprcloc;
			public Rectangle rect;
		};


		public struct ContainerXZ

		{
			public Shared.space_3d_mm center;
			public double plane_angle;
			public int maxx;
			public int minx;
			public int maxz;
			public int minz;
			public int maxy;
			public int miny;
			public int edge_dist;
			public bool top_open;
			public Shared.space_3d_mm top_center;
		}

		public struct ContainerModel

		{
			public long model_id;
			public string name;
			public double min_score;
		};

		private static DataTable containers = null;
		private static DataTable models = null;
		private static ContainersDAO con = new ContainersDAO();



		static Containers()

		{

		}



		public static SortedList ContainerList()

		{
			SortedList cl = new SortedList();
			int i;
			Container contn = new Container();

			if (con.ConnectionOpen() || con.Open(Shared.domain_path + Shared.DB_DIR))
				{
				containers = con.ContainerList();
				for (i = 0;i < containers.Rows.Count;i++)
					{
					contn.name = (string)containers.Rows[i][1];
					contn.od_model_id = (int)containers.Rows[i][2];
					contn.od_id = (int) containers.Rows[i][3];
					contn.is_model = (string) containers.Rows[i][4];
					contn.width = (int) containers.Rows[i][5];
					contn.length = (int)containers.Rows[i][6];
					contn.side_height = (int)containers.Rows[i][7];
					contn.top = Convert.ToBoolean((int)containers.Rows[i][8]);
					cl.Add(contn.name,contn);
					}
				}
			return(cl);
		}



		public static ArrayList ConatinerODModelList()

		{
			ArrayList cml = new ArrayList();
			int i;
			ContainerModel cm = new ContainerModel();

			if (con.ConnectionOpen() || con.Open(Shared.domain_path + Shared.DB_DIR))
				{
				models = con.ODModelsList();
				for (i = 0;i < models.Rows.Count;i++)
					{
					cm.model_id = (long)models.Rows[i][0];
					cm.name = (string) models.Rows[i][1];
					cm.min_score = (double) models.Rows[i][2];
					cml.Add(cm);
					}
				}
			return(cml);
		}



		public static ArrayList ContainerList(int model_id,int od_id)

		{
			ArrayList al = new ArrayList();
			Container contn = new Container();
			DataTable containers;
			int i;

			containers = con.ContainerData(model_id,od_id);
			for (i = 0; i < containers.Rows.Count; i++)
				{
				contn.name = (string)containers.Rows[i][1];
				contn.od_model_id = model_id;
				contn.od_id = od_id;
				contn.is_model = (string)containers.Rows[i][2];
				contn.width = (int)containers.Rows[i][3];
				contn.length = (int)containers.Rows[i][4];
				contn.side_height = (int)containers.Rows[i][5];
				contn.top = Convert.ToBoolean((int)containers.Rows[i][6]);
				al.Add(contn);
				}
			return (al);
		}



		public static bool ConfirmContainer(Container cont)

		{
			bool rtn = false;
			ArrayList al;
			Stopwatch sw = new Stopwatch();
			Point midpt = new Point(),mpt = new Point();
			VisualObjectDetection.visual_detected_object vdo;
			int dist = -1;
			DataTable dt;

			sw.Start();
			midpt.X = cont.rect.X + (int) Math.Round((double) cont.rect.Width/2);
			midpt.Y = cont.rect.Y + (int) Math.Round((double) cont.rect.Height/2);
			dt = con.ODModelData(cont.od_model_id);
			al = Shared.Detect((string) dt.Rows[0][0],(Bitmap) Shared.vimg,(double) dt.Rows[0][1],cont.od_id);
			if (al.Count > 0)			//CASES OF MULTIPLE WHEN THERE IS ONLY ONE, DO NOT SEE IN MODEL TEST & EVAL SETS
				{
				vdo = (VisualObjectDetection.visual_detected_object) al[0];
				mpt.X = vdo.x + (int)Math.Round((double)vdo.width / 2);
				mpt.Y = vdo.y + (int)Math.Round((double)vdo.height / 2);
				dist = Shared.DistancePtToPt(midpt, mpt);
				if (dist < MAX_CON_DIST_PIX)
					rtn = true;
				}
			sw.Stop();
			Log.LogEntry("Container confirmation of " + rtn + " took " + sw.ElapsedMilliseconds + " ms, distance (pix) " + dist + ", detects " + al.Count);
			return (rtn);
		}



		public static bool ContainerTopOpen(Container cont)

		{
			bool rtn = false;
			Shared.space_3d_mm ccloc = new Shared.space_3d_mm(),rcloc;
			Point pt = new Point();
			int i,j,tminh;

			if (cont.top)
				{
				rcloc = cont.mprcloc;
				rcloc.y = 0;
				tminh = cont.side_height + Mapping.fmr.minh;
				if (Shared.ConvertRC_to_CC(rcloc, ref ccloc, CameraPanTilt.tilt_deg))
					{
					Shared.ConvertCC_to_RC(ccloc, ref rcloc, CameraPanTilt.tilt_deg);
					if (rcloc.y < Mapping.fmr.maxh)
						{
						D415Camera.DetermineVid(ccloc,CameraPanTilt.tilt_deg, ref pt);
						rtn = true;
						for (i = -10;i < 10;i++)
							{
							for (j = -10;j < 10;j++)
								{
								D415Camera.DetermineLocCC(pt.Y + i, pt.X + j, CameraPanTilt.tilt_deg, ref ccloc, false);
								Shared.ConvertCC_to_RC(ccloc, ref rcloc, CameraPanTilt.tilt_deg, false);
								if (rcloc.y > tminh)
									break;
								}
							if (rcloc.y > tminh)
								{
								rtn = false;
								break;
								}
							}
						}
					}
				}
			else
				rtn = true;
			Log.LogEntry("Container top open " + rtn);
			return (rtn);
		}



		private static bool TopAnalyze(Bitmap bm, VisualObjectDetection.visual_detected_object vdo, ref ContainerXZ cxz)

		{
			bool rtn = false;
			Rectangle rect = new Rectangle();
			Bitmap obm,ibm;
			string fname;
			Stopwatch sw = new Stopwatch();
			CvBlobs blobs = new CvBlobs();
			CvBlob b;

			rect.X = vdo.x;
			rect.Y = vdo.y;
			rect.Width = vdo.width;
			rect.Height = vdo.height;
			obm = bm.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			fname = Shared.SaveVideoPic(obm);
			sw.Start();
			fname = ImageSegmentation.Infer(fname,"top");
			sw.Stop();
			Log.LogEntry("SEG inference time (ms): " + sw.ElapsedMilliseconds);
			obm.Dispose();
			if (fname.Length > 0)
				{
				ibm = new Bitmap(fname);
				Shared.SaveVideoPic(ibm);
				IplImage pic = new IplImage(ibm.Width,ibm.Height, BitDepth.U8, 3);
				IplImage gs = new IplImage(pic.Size, BitDepth.U8, 1);
				IplImage img = new IplImage(pic.Size, BitDepth.F32, 1);
				pic = ibm.ToIplImage();
				Cv.CvtColor(pic, gs, ColorConversion.BgrToGray);
				blobs.Label(gs, img);
				gs.Dispose();
				img.Dispose();
				pic.Dispose();
				if (blobs.Count > 0)
					{
					b = blobs[blobs.GreaterBlob()];
					if (b.Area >= MIN_BLOB_AREA)
						{
						Log.LogEntry("Segmentation blob analysis");
						Log.LogEntry("  object detection box: " + vdo.x + ", " + vdo.y + ", " + vdo.height + ", " + vdo.width);
						Log.LogEntry("  blob box: " + b.Rect.X + ", " + b.Rect.Y + ", " + b.Rect.Height + ", " + b.Rect.Width);
						Log.LogEntry("  blob area: " + b.Area);
						rect.X = b.Rect.X;
						rect.Y = b.Rect.Y;
						rect.Height = b.Rect.Height;
						rect.Width = b.Rect.Width;
						obm = ibm.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
						Shared.SaveVideoPic(obm);
						rect.X += vdo.x;
						rect.Y += vdo.y;
						if (DetermineTop2D(obm, rect,ref cxz))
							{
							Log.LogEntry("Top center point: " + cxz.top_center);
							}
						else
							Log.LogEntry("DetermineTop2D failed.");
						obm.Dispose();
						ibm.Dispose();
						}
					}
				}
			return (rtn);
		}


		public static bool SurfaceObjectAnalyze(string model,Bitmap bm,VisualObjectDetection.visual_detected_object vdo,int side_height,Container con,ref ContainerXZ cxz)

		{
			bool rtn = false;
			Rectangle rect;
			Bitmap obm,ibm;
			string fname;
			Stopwatch sw = new Stopwatch();
			CvBlobs blobs = new CvBlobs();
			CvBlob b;

			rect = new Rectangle();
			rect.X = vdo.x;
			rect.Y = vdo.y;
			rect.Width = vdo.width;
			rect.Height = vdo.height;
			obm = bm.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			fname = Shared.SaveVideoPic(obm);
			sw.Start();
			fname = ImageSegmentation.Infer(fname,model);
			sw.Stop();
			Log.LogEntry("SEG inference time (ms): " + sw.ElapsedMilliseconds);
			if (fname.Length > 0)
				{
				ibm = new Bitmap(fname);
				Shared.SaveVideoPic(ibm);
				IplImage pic = new IplImage(ibm.Width, ibm.Height, BitDepth.U8, 3);
				IplImage gs = new IplImage(pic.Size, BitDepth.U8, 1);
				IplImage img = new IplImage(pic.Size, BitDepth.F32, 1);
				pic = ibm.ToIplImage();
				Cv.CvtColor(pic, gs, ColorConversion.BgrToGray);
				blobs.Label(gs, img);
				gs.Dispose();
				img.Dispose();
				pic.Dispose();
				if (blobs.Count > 0)
					{
					b = blobs[blobs.GreaterBlob()];
					if (b.Area >= MIN_BLOB_AREA)
						{
						Log.LogEntry("Box segmentation blob analysis");
						Log.LogEntry("  object detection box: " + vdo.x + ", " + vdo.y + ", " + vdo.height + ", " + vdo.width);
						Log.LogEntry("  blob box: " + b.Rect.X + ", " + b.Rect.Y + ", " + b.Rect.Height + ", " + b.Rect.Width);
						Log.LogEntry("  blob area: " + b.Area);
						rect.X = b.Rect.X;
						rect.Y = b.Rect.Y;
						rect.Height = b.Rect.Height;
						rect.Width = b.Rect.Width;
						obm = ibm.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
						Shared.SaveVideoPic(obm);
						rect.X += vdo.x;
						rect.Y += vdo.y;
						if (DetermineContainer2D(obm,rect,con,ref cxz))
							{
							obm.Dispose();
							ibm.Dispose();
							Shared.rcloc.x = cxz.center.x;
							Shared.rcloc.z = cxz.center.z;
							Shared.rcloc.y = side_height + Y_PICK_OFFSET_MM;
							Shared.gripper_rot = 0;
							Log.LogEntry("container center point: " + cxz.center);
							Log.LogEntry("workspace edge distance (mm): " + cxz.edge_dist);
							Log.LogEntry("height (mm): " + (cxz.maxz - cxz.minz));
							Log.LogEntry("width (mm): " + (cxz.maxx - cxz.minx));
							Log.LogEntry("plane angle (°): " + cxz.plane_angle);
							Log.LogEntry("top is open: " + cxz.top_open);
							if (cxz.top_open)
								{
								TopAnalyze(bm,vdo,ref cxz);
								}
							rtn = true;
							}
						else
							{
							Log.LogEntry("DetermineContainer2D failed.");
							obm.Dispose();
							}
						}
					else
						{
						Log.LogEntry("Max blob's area of " + b.Area + " did not meet min requirement.");
						}
					}
				else
					{
					Log.LogEntry("Image segmentation inference detected nothing.");
					}
				File.Delete(fname);
				}
			else
				Log.LogEntry("Image segmentation inference failed.");
			return(rtn);
		}



		public static bool ContainerUseable(string name,string is_model, Bitmap bm, VisualObjectDetection.visual_detected_object vdo, int side_height,ref Container con)

		{
			bool rtn = false;
			ContainerXZ cxz = new ContainerXZ();
			int w,h;

			if (SurfaceObjectAnalyze(is_model, bm, vdo,side_height,con,ref cxz))
				{
				if (cxz.edge_dist > 0)
					{
					w = (cxz.maxx - cxz.minx);
					h = (cxz.maxz - cxz.minz);
					if ((h > w) == (con.length < con.width))
						{
						if (Math.Abs(cxz.plane_angle) <= MAX_PLAN_ANGLE)
							{
							if (cxz.top_open)
								{
								if (((cxz.center.x < 0) && ((cxz.center.x > cxz.top_center.x))) || ((cxz.center.x > 0) && (cxz.center.x < cxz.top_center.x)))
									{
									if (Shared.InReach(Shared.rcloc,true))
										{
										con.mprcloc = Shared.rcloc;
										rtn = true;
										}
									else
										DynamicWorkAssist.TextOutput("The " + name + " is not within reach.");
									}
								else
									DynamicWorkAssist.TextOutput("The " + name + "'s top obstructs access to the box.");
								}
							else
								DynamicWorkAssist.TextOutput("The " + name + "'s top is not open.");
							}
						else
							DynamicWorkAssist.TextOutput("The " + name + " is too skewed.");
						}
					else
						DynamicWorkAssist.TextOutput("The " + name + "'s orientation is wrong.");
					}
				else
					DynamicWorkAssist.TextOutput("The " + name + " is within the workspace.");
				}
			else
				DynamicWorkAssist.TextOutput("The analysis of the " + name + " failed.");
			return (rtn);
		}



		public static bool PlaceInContainer(Container cont,bool single_time = true)

		{
			bool rtn = false;

			if (ArmControl.Move(cont.mprcloc.x, cont.mprcloc.y, cont.mprcloc.z, ArmControl.gripper_orient.DOWN))
				{
				if (ArmControl.Move(cont.mprcloc.x,cont.mprcloc.y - Y_PICK_OFFSET_MM, cont.mprcloc.z, ArmControl.gripper_orient.DOWN))
					{
					ArmControl.Gripper(ArmControl.GRIPPER_FULL_OPEN);
					Thread.Sleep(100);
					if (ArmControl.Move(cont.mprcloc.x, cont.mprcloc.y, cont.mprcloc.z, ArmControl.gripper_orient.DOWN))
						{
						if (single_time)
							if (ArmControl.Home())
								rtn = true;
							else
								DynamicWorkAssist.TextOutput("Attempt to home failed.");
						else
							rtn = true;
						}
					else
						DynamicWorkAssist.TextOutput("Attempt to move up from drop point failed.");
					}
				else
					DynamicWorkAssist.TextOutput("Attempt to move to drop point failed.");
				}
			else
				DynamicWorkAssist.TextOutput("Attempt to move to red box location failed.");
			return (rtn);
		}



		public static place_reply PlaceInContainerOA(Container cont)

		{
			place_reply rtn;

			if (ObstacleAvoid.Move(SAFE_CONTAINER_X_MM, cont.mprcloc.y, cont.mprcloc.z, ArmControl.gripper_orient.DOWN))
				{
				if (ArmControl.Move(cont.mprcloc.x, cont.mprcloc.y, cont.mprcloc.z, ArmControl.gripper_orient.DOWN))
					{
					if (ArmControl.Move(cont.mprcloc.x,cont.mprcloc.y - Y_PICK_OFFSET_MM, cont.mprcloc.z, ArmControl.gripper_orient.DOWN))
						{
						ArmControl.Gripper(ArmControl.GRIPPER_FULL_OPEN);
						Thread.Sleep(100);
						if (ArmControl.Move(cont.mprcloc.x, cont.mprcloc.y, cont.mprcloc.z, ArmControl.gripper_orient.DOWN))
							{
							rtn = place_reply.OK;
							}
						else
							{
							DynamicWorkAssist.TextOutput("Attempt to move up from drop point failed.");
							rtn = place_reply.FAIL;
							}
						}
					else
						{
						DynamicWorkAssist.TextOutput("Attempt to move to drop point failed.");
						rtn = place_reply.FAIL;
						}
					}
				else
					{
					DynamicWorkAssist.TextOutput("Attempt to move to container location failed.");
					rtn = place_reply.FAIL;
					}
				}
			else
				{
				DynamicWorkAssist.TextOutput("Attempt to move to container's approach location failed.");
				rtn = ObstacleAvoid.oa_move_status;
				}
			return (rtn);
		}



		static bool DetermineTop2D(Bitmap bm, Rectangle rect,ref ContainerXZ cont)
		// assumes that the box is at the left edge of the work space
		{
			bool rtn = false;
			int i, j,count = 0, minh, too_low = 0;
			long sz = 0, sx = 0;
			Shared.space_3d_mm rcloc = new RobotArm.Shared.space_3d_mm(), ccloc = new RobotArm.Shared.space_3d_mm();
			TextWriter tw = null;
			DateTime now = DateTime.Now;
			string fname;
			Color color;
			Stopwatch sw = new Stopwatch();

			sw.Start();
			fname = Log.LogDir() + "Top 2D analysis " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + Shared.GetUFileNo() + ".csv";
			minh = Mapping.fmr.minh;
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine("Top data set");
				tw.WriteLine(now.ToShortDateString() + " " + now.ToShortTimeString());
				tw.WriteLine("min height (mm) :" + minh);
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
							if (tw != null)
								tw.WriteLine(rcloc.x + "," + rcloc.z + "," + rcloc.y);
							count += 1;
							sz += rcloc.z;
							sx += rcloc.x;
							}
						else if (rcloc.z > 0)
							too_low += 1;
						}
					}
				}
			if (count > MIN_BLOB_AREA)
				{
				rtn = true;
				cont.top_center.x = (int)Math.Round((double)sx / count);
				cont.top_center.z = (int)Math.Round((double)sz / count);
				cont.top_center.y = 0;
				if (tw != null)
					{
					tw.WriteLine();
					tw.WriteLine("Top center point" + cont.top_center.ToCsvString());
					tw.Close();
					}
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
			sw.Stop();
			Log.LogEntry("DetermineTop2D took: " + sw.ElapsedMilliseconds + " ms");
			return (rtn);
		}



		static bool DetermineContainer2D(Bitmap bm, Rectangle rect,Container con, ref ContainerXZ cont)
		// assumes that the box is at the left edge of the work space
		{
			bool rtn = false;
			int i, j, maxx = 0, minz = 0,minx = 0,maxz = 0, miny = 0,maxy = 0,count = 0,minh,too_low = 0;
			long sxz = 0, sz = 0, sx = 0,sx2 = 0;
			Shared.space_3d_mm rcloc = new RobotArm.Shared.space_3d_mm(), ccloc = new RobotArm.Shared.space_3d_mm();
			TextWriter tw = null;
			DateTime now = DateTime.Now;
			string fname;
			Point pt = new Point();
			Color color;
			ArrayList data = new ArrayList();
			double m;
			Stopwatch sw = new Stopwatch();

			sw.Start();
			fname = Log.LogDir() + "Container 2D analysis " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + Shared.GetUFileNo() + ".csv";
			minh = Mapping.fmr.minh;
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine("Container data set");
				tw.WriteLine(now.ToShortDateString() + " " + now.ToShortTimeString());
				tw.WriteLine("container name: " + con.name);
				tw.WriteLine("min height (mm) :" + minh);
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
								minz = rcloc.z + 1;
								maxz = rcloc.z - 1;
								maxy = rcloc.y - 1;
								miny = rcloc.y + 1;
								}
							if (tw != null)
								tw.WriteLine(rcloc.x + "," + rcloc.z + "," + rcloc.y);
							count += 1;
							data.Add(rcloc);
							if (rcloc.x > maxx)
								maxx = rcloc.x;
							if (rcloc.z < minz)
								minz = rcloc.z;
							if (rcloc.x < minx)
								minx = rcloc.x;
							if (rcloc.z > maxz)
								maxz = rcloc.z;
							if (rcloc.y > maxy)
								maxy = rcloc.y;
							if (rcloc.y < miny)
								miny = rcloc.y;
							sz += rcloc.z;
							sx2 += rcloc.x * rcloc.x;
							sx += rcloc.x;
							sxz += rcloc.x * rcloc.z;
							}
						else if (rcloc.z > 0)
							too_low += 1;
						}
					}
				}
			if (count > MIN_BLOB_AREA)
				{
				rtn = true;
				cont.center.x = (int)Math.Round((double)sx / count);
				cont.center.z = (int)Math.Round((double)sz / count);
				cont.center.y = 0;
				cont.maxy = maxy;
				cont.miny = miny;
				cont.maxx = maxx;
				cont.minx = minx;
				cont.maxz = maxz;
				cont.minz = minz;
				m = ((count * sxz) - (sx * sz))/((count * sx2)- Math.Pow(sx,2));
				cont.plane_angle = Math.Atan(m) * Shared.RAD_TO_DEG;
				cont.edge_dist = (Math.Abs(cont.maxx) - (Mapping.MAP_WIDTH_MM / 2));
				Shared.ConvertRC_to_CC(cont.center, ref ccloc, CameraPanTilt.tilt_deg);
				D415Camera.DetermineVid(ccloc, CameraPanTilt.tilt_deg, ref pt);
				if (tw != null)
					{
					tw.WriteLine();
					tw.WriteLine();
					tw.WriteLine("center point" + cont.center.ToCsvString());
					tw.WriteLine("container Z-X plane angle: " + cont.plane_angle);
					tw.WriteLine("max x: " + maxx + "   min x: " + minx + "  max z: " + maxz + "   min z: " + minz + "   max y: " + maxy +  "   min y: " + miny );
					tw.WriteLine("Width (mm): " + (maxx - minx) + "    Height (mm): " + (maxz - minz));
					tw.WriteLine("Container - workspace distance (mm): " + cont.edge_dist);
					}
				Shared.ConvertRC_to_CC(cont.center, ref ccloc, CameraPanTilt.tilt_deg);
				D415Camera.DetermineVid(ccloc, CameraPanTilt.tilt_deg, ref pt);
				int tminh = con.side_height + Mapping.fmr.minh;
				if (tw != null)
					{
					tw.WriteLine();
					tw.WriteLine("top open test,X,Z,Y");
					}
				cont.top_open = true;
				for (i = -10; i < 10; i++)
					{
					for (j = -10; j < 10; j++)
						{
						D415Camera.DetermineLocCC(pt.Y + i, pt.X + j, CameraPanTilt.tilt_deg, ref ccloc, false);
						Shared.ConvertCC_to_RC(ccloc, ref rcloc, CameraPanTilt.tilt_deg, false);
						if (tw != null)
							tw.WriteLine("," + rcloc.x + "," + rcloc.z + "," + rcloc.y);
						if (rcloc.y > tminh)
							{
							cont.top_open = false;
							break;
							}
						}
					if (rcloc.y > tminh)
						{
						cont.top_open = false;
						break;
						}
					}
				if (tw != null)
					tw.WriteLine("Top open: " + cont.top_open);
				if (tw != null)
					{
					tw.WriteLine();
					tw.Close();
					Log.LogEntry("Saved: " + fname);
					}
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
			sw.Stop();
			Log.LogEntry("DetermineContainer2D took: " + sw.ElapsedMilliseconds + " ms");
			return (rtn);
		} 

		}
	}
