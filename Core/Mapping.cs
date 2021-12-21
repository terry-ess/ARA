using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;


namespace RobotArm
	{
	public static class Mapping
		{  

		const int MAP_WIDTH_CM = 41;
		const int MAP_SIDE_LIMIT_CM = 20;
		public const int MAP_WIDTH_MM = 406;
		const int MAP_LENGTH_CM = 41;
		public const int MAP_LENGTH_MM = 406;
		const int MAP_SIZE_CM = MAP_WIDTH_CM * MAP_LENGTH_CM;
		const int COL_CORRECT = 20;
		const int OBS_HEIGHT_CLEAR_MM = 15;
		const int SIDE_BLOCKS = 6;
		const int SP_CORRECT_SIDE_BLOCKS = 10;
		const int SP_CORRECT_Z_CM = 10;
		const int MAX_HEIGHT_LIMIT_MM = 30;
		

		public struct MapResults
		{
			public int maxh,minh;
			public double avg_abs_err,avgh;
			public string map_file;
		}

		public struct Dpt
		{
			public double X;
			public double Y;

			public Dpt(double x,double y)

			{
				this.X = x;
				this.Y = y;
			}


			public override string ToString()

			{
				string rtn;

				rtn = "(" + this.X.ToString("F3") + ", " + this.Y.ToString("F3") + ")";
				return(rtn);
			}

		};

		public static short[] base_ws_map = null;
		private static short[] ws_map = new short[MAP_SIZE_CM];
		public static short[] ref_ws_map = null;
		public static string[] ob_check_map = new string[MAP_SIZE_CM];
		private static int check_count = 0;
		private static int remove_count = 0;
		private static int gd_remove_count = 0;
		private static int max_cal_height = 0;
		private static Point[] apcm = {new Point(16,3),new Point(16,2)};
		public static MapResults fmr = new Mapping.MapResults();



		public static MapResults CalibrateMap(double tc,bool log = true)

		{
			int row,col,ymin = 10,ymax = 0,pts = 0,max_count = 0;
			Shared.space_3d_mm ccloc = new Shared.space_3d_mm(), rcloc = new Shared.space_3d_mm(),yminloc = new Shared.space_3d_mm();
			long ysum = 0,aysum = 0;
			short[] map = null;
			int i,loc = 0;
			string fname;
			DateTime now = DateTime.Now;
			TextWriter tw;
			MapResults mr;

			if (log)
				{
				Shared.SaveVideoPic(Shared.vimg);
				map = new short[MAP_SIZE_CM];
				for (i = 0;i < MAP_SIZE_CM;i++)
					map[i] = -99;
				}
			for (row = 0; row < D415Camera.HEIGHT; row++)
				for (col = 0; col < D415Camera.WIDTH; col++)
					{
					if (D415Camera.DetermineLocCC(row,col, CameraPanTilt.tilt_deg + tc, ref ccloc, false))
						{
						Shared.ConvertCC_to_RC(ccloc, ref rcloc, CameraPanTilt.tilt_deg + tc, false);
						if ((Math.Abs(rcloc.x) <= (MAP_WIDTH_MM/2)) && (rcloc.z >= 0) && (rcloc.z <= MAP_LENGTH_MM))
							{
							if (rcloc.y >= MAX_HEIGHT_LIMIT_MM)
								max_count += 1;
							else
								{
								pts += 1;
								ysum += rcloc.y;
								aysum += Math.Abs(rcloc.y);
								if (rcloc.y > ymax)
									ymax = rcloc.y;
								if (rcloc.y < ymin)
									{
									ymin = rcloc.y;
									yminloc = rcloc;
									}
								}
							if (log)
								{
								if ((MapArrayLoc(rcloc, ref loc)) && (rcloc.y > map[loc]))
										map[loc] = (short)rcloc.y;
								}
							}
						}
					}
			mr.maxh = ymax;
			mr.minh = ymin;
			mr.avgh = ((double) ysum / pts);
			mr.avg_abs_err = ((double)aysum / pts);
			mr.map_file = "";
			max_cal_height = ymax;
			if (log)
				{
				fname = Log.LogDir() + "surface map " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + "-" + Shared.GetUFileNo() + ".csv";
				tw = File.CreateText(fname);
				if (tw != null)
					{
					tw.WriteLine("Surface map");
					tw.WriteLine(now.ToShortDateString() + "  " + now.ToShortTimeString());
					tw.WriteLine();
					tw.WriteLine(" base tilt (°): " + CameraPanTilt.tilt_deg);
					tw.WriteLine(" tilt correct (°): " + tc);
					tw.WriteLine(" max height (mm): " + ymax);
					tw.WriteLine(" min height (mm): " + ymin);
					tw.WriteLine(" min height location: " + yminloc.ToCsvString());
					tw.WriteLine(" avg absolute error (mm): " + ((double)aysum / pts).ToString("F2"));
					tw.WriteLine(" avg height (mm): " + ((double) ysum/pts).ToString("F2"));
					tw.WriteLine(" no. sample pts: " + pts);
					tw.WriteLine(" no. pts exceeding height limit : " + max_count);
					tw.WriteLine();
					for (i = -20; i < 21; i++)
						tw.Write("," + i);
					tw.WriteLine();
					for (row = 0; row < 41; row++)
						{
						for (col = 0; col < 41; col++)
							{
							if (col == 0)
								tw.Write((41 - row));
							tw.Write("," + map[(row * 41) + col]);
							}
						tw.WriteLine();
						}
					tw.Close();
					mr.map_file = fname;
				}
			}
		return(mr);
		}



		public static bool MapWorkSpace(double tc,bool remove_anamoly = true)

		{
			int row = 0, col = 0,loc = 0,i;
			Shared.space_3d_mm ccloc = new Shared.space_3d_mm(), rcloc = new Shared.space_3d_mm();
			bool rtn = true;
			const int MIN_ROW = 280;
			const int MIN_COL = 450;
			const int MAX_COL = 985;

			try
			{
			for (i = 0; i < MAP_SIZE_CM; i++)
				ws_map[i] = -99;
			for (row = MIN_ROW; row < D415Camera.HEIGHT; row++)
				for (col = MIN_COL; col < MAX_COL; col++)
					{
					if (D415Camera.DetermineLocCC(row,col, CameraPanTilt.tilt_deg + tc, ref ccloc, false))
						{
						Shared.ConvertCC_to_RC(ccloc, ref rcloc, CameraPanTilt.tilt_deg + tc, false);
						if ((MapArrayLoc(rcloc, ref loc)) && (rcloc.y > ws_map[loc]))
							ws_map[loc] = (short) rcloc.y;
						}
					}
			if (remove_anamoly)
				{
				for (i = 0;i < apcm.Length;i++)
					{
					if (MapArrayLocCM(apcm[i],ref loc))
						ws_map[loc] = 0;
					}
				}
			}

			catch(Exception ex)
			{
			Log.LogEntry("MapWorkSpace exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Log.LogEntry("Row " + row + "  Col " + col);
			rtn = false;
			}

			return(rtn);
		}



		public static bool RemoveArmFromMap(Shared.space_3d_mm aloc,ArmControl.gripper_orient go)

		{
			bool rtn = false;
			int i,dx,dy,side_blocks;
			Point ep, ap,spcm;
			double  a1,a2;
			ArrayList steps;
			string lines;
			Stopwatch sw = new Stopwatch();

			if (ref_ws_map != null)
				{

				try
				{
				sw.Start();
				remove_count = 0;
				gd_remove_count = 0;
				ep = new Point(aloc.x,aloc.z);
				Log.LogEntry("Arm removal from " + ep);
				ap = new Point(0,-ArmControl.Z_OFFSET_MM);
				dx = ep.X - ap.X;
				dy = ep.Y - ap.Y;
				if (dx == 0)
					{
					side_blocks = SIDE_BLOCKS;
					a1 = 0;
					}
				else
					{
					a1 = Math.Atan(dy / dx) * Shared.RAD_TO_DEG;
					if (dx > 0)
						a2 = 90 - a1;
					else
						a2 = -(90 + a1);
					side_blocks = (int)Math.Abs(Math.Ceiling(SIDE_BLOCKS / Math.Cos(a2 * Shared.DEG_TO_RAD)));
					}
				if ((ws_map != null) && WithinWorkSpace(aloc))
					{
					steps = DetermineSteps(ap, ep,false);
					for (i = 0; i < steps.Count; i++)
						{
						spcm = ConvertPtToCm((Point) steps[i]);
						if (spcm.Y < SP_CORRECT_Z_CM)
							rtn = RemovePts((Point)steps[i], dx, dy,SP_CORRECT_SIDE_BLOCKS);
						else
							rtn = RemovePts((Point) steps[i],dx,dy,side_blocks);
						if (!rtn)
							break;
						}
					RemoveShadows(ref_ws_map);
					if((a1 != 0) && (go != ArmControl.gripper_orient.DOWN))
						RemoveEndPts(a1,ep);
					if (go == ArmControl.gripper_orient.DOWN)
						GripperDownCorrect(ep);
					sw.Stop();
					lines = "Arm removal map\r\nArm end point: " + ep.X + " " + ep.Y + "\r\nARM origin point: " + ap.X + " " + ap.Y + "\r\nStandard removal count: " + remove_count + "\r\nGD removal count: " + gd_remove_count + "\r\nExecution time (ms): " + sw.ElapsedMilliseconds;
					SaveWorkspaceMap(lines);
					}
				}

				catch(Exception ex)
				{
				rtn = false;
				Log.LogEntry("ObstacleCheck exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				}
			else
				Log.LogEntry("Ref work space map is not available.");
			return(rtn);
		}



		public static bool RemoveShadows(short[] rmap)

		{
			bool rtn = false;
			int i;

			if (rmap != null)
				{
				for (i = 0; i < ws_map.Length; i++)
					{
					if (ws_map[i] == -99)
						ws_map[i] = rmap[i];
					}
				rtn = true;
				}
			else
				Log.LogEntry("Ref work space map is not available.");
			return(rtn);
		}




		private static bool RemoveEndPts(double a,Point ep)

		{
			ArrayList ep_p2 = new ArrayList(), p1_p2 = new ArrayList(),fp = new ArrayList();
			bool rtn = true;
			int i,loc = 0;
			Point pt;

			if (a != 0)
				{
				GenEndPts(a,ep,ref ep_p2,ref p1_p2);
				if ((ep_p2.Count > 0)  && (p1_p2.Count > 0))
					{
					for (i = 0;i < ep_p2.Count;i++)
						{
						pt = (Point) ep_p2[i];
						if (MapArrayLocCM(pt, ref loc))
							{
							ws_map[loc] = ref_ws_map[loc];
							remove_count += 1;
							}
						}
					for (i = 0;i < p1_p2.Count;i++)
						{
						pt = (Point) p1_p2[i];
						if (MapArrayLocCM(pt, ref loc))
							{
							ws_map[loc] = ref_ws_map[loc];
							remove_count += 1;
							}
						}
					GenFillPts(ep_p2, p1_p2, ref fp);
					for (i = 0;i < fp.Count;i++)
						{
						pt = (Point) fp[i];
						if (MapArrayLocCM(pt, ref loc))
							{
							ws_map[loc] = ref_ws_map[loc];
							remove_count += 1;
							}
						}
					}
				}
			else
				Log.LogEntry("Angle is 0.");
			return(rtn);
		}


		
		public static string SaveWorkspaceMap(string lines = "")

		{
			string fname;
			TextWriter tw;
			DateTime now = DateTime.Now;
			int i,row,col;

			fname = Log.LogDir() + "workspace map " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + "-" + Shared.GetUFileNo() + ".csv";
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine("Workspace map");
				tw.WriteLine(now.ToShortDateString() + "  " + now.ToShortTimeString());
				tw.WriteLine();
				if (lines.Length > 0)
					{
					tw.WriteLine(lines);
					tw.WriteLine();
					}
				for (i = -20; i < 21; i++)
					tw.Write("," + i);
				tw.WriteLine();
				for (row = 0; row < MAP_LENGTH_CM; row++)
					{
					for (col = 0; col < MAP_WIDTH_CM; col++)
						{
						if (col == 0)
							tw.Write((MAP_LENGTH_CM - row));
						tw.Write("," + ws_map[(row * MAP_WIDTH_CM) + col]);
						}
					tw.WriteLine();
					}
				tw.Close();
				Log.LogEntry("Saved " + fname);
				}
			else
				fname = "";
			return (fname);
		}



		public static bool SaveCheckMap(String lines)

		{
			bool rtn = false;
			TextWriter tw;
			int row, col,i;
			string fname;
			DateTime now = DateTime.Now;

			fname = Log.LogDir() + "Check Map " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + Shared.GetUFileNo() + ".csv";
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine(fname);
				tw.WriteLine(now.ToShortDateString() + "  " + now.ToShortTimeString());
				tw.WriteLine();
				tw.WriteLine(lines);
				tw.WriteLine();
				for (i = -20; i < 21; i++)
					tw.Write("," + i);
				tw.WriteLine();
				for (row = 0; row < MAP_LENGTH_CM; row++)
					{
					for (col = 0; col < MAP_WIDTH_CM; col++)
						{
						if (col == 0)
							tw.Write(MAP_LENGTH_CM - row);
						tw.Write("," + ob_check_map[(row * MAP_WIDTH_CM) + col]);
						}
					tw.WriteLine();
					}
				tw.Close();
				Log.LogEntry("Saved: " + fname);
				rtn = true;
				}
			return (rtn);
		}



		public static bool SaveCurrentMapAsBase()

		{
			bool rtn = false;

			if (ws_map != null)
				{
				base_ws_map = (short[]) ws_map.Clone();
				rtn = true;
				}
			return(rtn);
		}



		public static bool SaveCurrentMapAsRef()

		{
			bool rtn = false;

			if (ws_map != null)
				{
				ref_ws_map = (short[]) ws_map.Clone();
				rtn = true;
				}
			return(rtn);
		}



		public static bool WithinWorkSpace(Shared.space_3d_mm loc)

		{
			bool rtn = false;

			if ((Math.Abs(loc.x) <= (MAP_WIDTH_MM/2)) && (loc.z >= 0) && (loc.z <= MAP_LENGTH_MM))
				rtn = true;
			return(rtn);
		}



		public static bool WithinWorkSpace(Point pt)

		{
			bool rtn = false;

			if ((Math.Abs(pt.X) <= (MAP_WIDTH_MM / 2)) && (pt.Y >= 0) && (pt.Y <= MAP_LENGTH_MM))
				rtn = true;
			return (rtn);
		}



		private static bool MapArrayLoc(Shared.space_3d_mm pt, ref int loc)

		{
			bool rtn = false;
			int row,col;

			col = (int)Math.Round((pt.x + ((double)MAP_WIDTH_MM / 2)) / 10);
			row = (int)Math.Round(MAP_LENGTH_CM - ((double)pt.z / 10));
			if ((row <= MAP_LENGTH_CM) && (col < MAP_WIDTH_CM) && (col >= 0) && (row >= 0))
				{
				loc = row * MAP_WIDTH_CM + col;
				if ((loc >= 0) && (loc < MAP_SIZE_CM))
					rtn = true;
				}
			return(rtn);
		}



		public static void Calibrate(int row,int col)

		{
			double va,tilt,etilt,dif,min_dif = 1000,mtilt = 0;
			int i;
			string msg;
			Shared.space_3d_mm ccloc = new Shared.space_3d_mm(), rcloc = new Shared.space_3d_mm(),mrcloc = new Shared.space_3d_mm();
			Mapping.MapResults mr = new Mapping.MapResults(), lmr = new Mapping.MapResults();

			va = D415Camera.VideoVerDegrees((int)Math.Round(((double) D415Camera.HEIGHT / 2) - row));
			tilt = (Math.Atan((D415Camera.BASE_HEIGHT - Shared.CAL_RCY_IN)/(Shared.CAM_ROBOT_Z_OFFSET + Shared.CAL_RCZ_IN)) * Shared.RAD_TO_DEG) + va;
			for (i = 1;i <= 20;i++)
				{
				etilt = tilt + ((double) i/10);
				D415Camera.DetermineLocCC(row, col, etilt, ref ccloc,false);
				Shared.ConvertCC_to_RC(ccloc, ref rcloc, etilt,false);
				dif = rcloc.z - (Shared.CAL_RCZ_IN * Shared.IN_TO_MM);
				if (Math.Abs(dif) < min_dif)
					{
					min_dif = Math.Abs(dif);
					mtilt = etilt;
					mrcloc = rcloc;
					}
				else
					break;
				}
			CameraPanTilt.tilt_deg = mtilt;
			Shared.x_correct_in = (mrcloc.x * Shared.MM_TO_IN) - Shared.CAL_RCX_IN;
			Shared.y_correct_in = (mrcloc.y * Shared.MM_TO_IN) - Shared.CAL_RCY_IN;
			msg = "Calibration:";
			Log.LogEntry(msg);
			msg = "  matched z (in) : " + (mrcloc.z * Shared.MM_TO_IN).ToString("F3") + " in " + i + " steps";
			Log.LogEntry(msg);
			msg = "  revised tilt (°): " + CameraPanTilt.tilt_deg;
			Log.LogEntry(msg);
			msg = "  x correct (in): " + Shared.x_correct_in;
			Log.LogEntry(msg);
			msg = "  y correct (in): " + Shared.y_correct_in;
			Log.LogEntry(msg);
			msg = "Calibration workspace height optimization:";
			Log.LogEntry(msg);
			CalibrateMap(0);
			for (i = 0; i > -10; i--)
				{
				mr = Mapping.CalibrateMap((double)i / 10, false);
				if ((i != 0) && (mr.avg_abs_err > lmr.avg_abs_err))
					break;
				lmr = mr;
				}
			msg = "  iterations: " + (-i + 1);
			Log.LogEntry(msg);
			CameraPanTilt.tilt_deg += ((double)(i + 1) / 10);
			msg = "  revised tilt (°): " + CameraPanTilt.tilt_deg;
			Log.LogEntry(msg);
			msg = "  avg absolute error (mm): " + mr.avg_abs_err;
			Log.LogEntry(msg);
			fmr = Mapping.CalibrateMap(0);
			Mapping.MapWorkSpace(0);
			Mapping.SaveCurrentMapAsBase();
			Shared.vision_calibrated = true;
		}




		private static bool MapArrayLoc(Point pt,ref int loc)

		{
			bool rtn = false;

			if ((Math.Abs(pt.X) <= (MAP_WIDTH_MM / 2)) && (pt.Y >= 0) && (pt.Y <= MAP_LENGTH_MM))
				{
				loc = ((int)Math.Round(MAP_LENGTH_CM - ((double) pt.Y / 10)) * MAP_LENGTH_CM) + (int)Math.Round((double)(pt.X + (MAP_WIDTH_MM/2)) / 10);
				if ((loc >= 0) && (loc < MAP_SIZE_CM))
					rtn = true;
				}
			return(rtn);
		}



		private static bool MapArrayLocCM(Point pt,ref int loc)

		{
			bool rtn = false;

			if ((Math.Abs(pt.X) <= MAP_SIDE_LIMIT_CM) && (pt.Y >= 0) && (pt.Y <= MAP_LENGTH_CM))
				{
				loc = ((MAP_LENGTH_CM - pt.Y) * MAP_WIDTH_CM) + (pt.X + COL_CORRECT);
				if ((loc >= 0) && (loc < MAP_SIZE_CM))
					rtn = true;
				}
			return(rtn);
		}



		private static bool RemovePts(Point pt, int dx, int dy, int sblocks)

		{
			bool rtn = true;
			int loc = 0,j;
			Point cp2 = new Point();
			

			if (MapArrayLoc(pt,ref loc))
				{
				ws_map[loc] = ref_ws_map[loc];
				check_count += 1;
				}
			if (Math.Abs(dx) < Math.Abs(dy))
				{
				cp2.Y = pt.Y;
				for (j = 1;j <= sblocks * 2;j++)
					{
					cp2.X = pt.X + (j * 5);
					if (MapArrayLoc(cp2, ref loc))
						{
						ws_map[loc] = ref_ws_map[loc];
						remove_count += 1;
						}
					cp2.X = pt.X - (j * 5);
					if (MapArrayLoc(cp2, ref loc))
						{
						ws_map[loc] = ref_ws_map[loc];
						remove_count += 1;
						}
					}
				}
			else
				{
				cp2.X = pt.X;
				for (j = 1; j <= sblocks * 2; j++)
					{
					cp2.Y = pt.Y + (j * 5);
					if (MapArrayLoc(cp2, ref loc))
						{
						ws_map[loc] = ref_ws_map[loc];
						remove_count += 1;
						}
					cp2.Y = pt.Y - (j * 5);
					if (MapArrayLoc(cp2, ref loc))
						{
						ws_map[loc] = ref_ws_map[loc];
						remove_count += 1;
						}
					}
				}
			return(rtn);
		}



		private static void GripperDownCorrect(Point ep)

		{
			Point epcm,mpt = new Point();
			int i,loc = 0;
			const int MAX_ROW = 4;
			const int MAX_COL = 13;

			Log.LogEntry("Gripper down correct");
			epcm = ConvertPtToCm(ep);
			for (i = 0;i < MAX_COL * MAX_ROW;i++)
				{
				mpt.X = epcm.X + ((i % MAX_COL) - ((MAX_COL - 1)/2));
				if (i < MAX_COL)
					mpt.Y = epcm.Y + MAX_ROW;
				else if (i < MAX_COL * 2)
					mpt.Y = epcm.Y + MAX_ROW - 1;
				else if (i < MAX_COL * 3)
					mpt.Y = epcm.Y + MAX_ROW - 2;
				else
					mpt.Y = epcm.Y + 1;
				if (MapArrayLocCM(mpt,ref loc))
					{
					if (ws_map[loc] > max_cal_height)
						{
						ws_map[loc] = ref_ws_map[loc];
						gd_remove_count += 1;
						}
					}
				}
		}




		private static ArrayList DetermineSteps(Point sp,Point ep,bool mark = true)

		{
			ArrayList steps = new ArrayList();
			int dx,dy,minc,i,count,loc = 0,last_loc = -1;
			bool xmain;
			Point step;
			double oinc;

			dx = (int) Math.Round((double) (ep.X - sp.X)/5);
			dy = (int) Math.Round((double) (ep.Y - sp.Y)/5);
			if (Math.Abs(dx) >= Math.Abs(dy))
				{
				xmain = true;
				count = Math.Abs(dx);
				oinc = Math.Abs((double) dy/dx) * 5;
				if (dy < 0)
					oinc *= -1;
				if (dx > 0)
					minc = 5;
				else
					minc = -5;
				}
			else
				{
				xmain = false;
				count = Math.Abs(dy);
				oinc = Math.Abs((double) dx/dy) * 5;
				if (dx < 0)
					oinc *= -1;
				if (dy > 0)
					minc = 5;
				else
					minc = -5;
				}
			step = sp;
			for (i = 0;i < count;i++)
				{
				if (xmain)
					{
					step.X += minc;
					step.Y = sp.Y + (int) Math.Round((i + 1) * oinc);
					}
				else
					{
					step.Y += minc;
					step.X = sp.X + (int) Math.Round((i + 1) * oinc);
					}
				if (MapArrayLoc(step,ref loc))
					{
					if (loc != last_loc)
						{
						steps.Add(step);
						if (mark)
							ob_check_map[loc] += "e";
						last_loc = loc;
						}
					}
				}
			return (steps);
		}



		private static bool MapCheck(int loc,double sh,double eh)

		{
			bool rtn = true;
			double hi;

			if ((loc >= 0) && (loc < ob_check_map.Length))
				{
				if (!ob_check_map[loc].Contains("c"))
					ob_check_map[loc] += "c";
				check_count += 1;
				hi = ws_map[loc] + OBS_HEIGHT_CLEAR_MM;
				if ((hi > sh) || (hi > eh))
					{
					rtn = false;
					Log.LogEntry("Obstacle found with height of " + hi + " (with " + OBS_HEIGHT_CLEAR_MM +  " clearance) and min arm height of " + Math.Min(sh, eh).ToString("F3"));
					ob_check_map[loc] += "O";
					}
				}
			else
				Log.LogEntry("Index outside of map: " + loc);
			return(rtn);
		}



		private static bool CheckPts(Point cp,int dx,int dy,double sh,double eh,int sblocks)

		{
			bool rtn = true;
			int loc = 0,j;
			Point cp2 = new Point();

			if (MapArrayLoc(cp, ref loc))
				{
				rtn = MapCheck(loc, sh, eh);
				}
			if (rtn)
				{
				if (Math.Abs(dx) < Math.Abs(dy))
					{
					cp2.Y = cp.Y;
					for (j = 1; j <= sblocks * 2; j++)
						{
						cp2.X = cp.X + (j * 5);
						if (MapArrayLoc(cp2, ref loc))
							{
							if (!(rtn = MapCheck(loc, sh, eh)))
								break;
							}
						cp2.X = cp.X - (j * 5);
						if (MapArrayLoc(cp2, ref loc))
							{
							if (!(rtn = MapCheck(loc, sh, eh)))
								break;
							}
						}
					}
				else
					{
					cp2.X = cp.X;
					for (j = 1; j <= sblocks * 2; j++)
						{
						cp2.Y = cp.Y + (j * 5);
						if (MapArrayLoc(cp2, ref loc))
							{
							if (!(rtn = MapCheck(loc, sh, eh)))
								break;
							}
						cp2.Y = cp.Y - (j * 5);
						if (MapArrayLoc(cp2, ref loc))
							{
							if (!(rtn = MapCheck(loc, sh, eh)))
								break;
							}
						}
					}
				}
			return (rtn);
		}



		private static bool CheckLine(Point ap, int dx, int dy, double sh, double eh, int sblocks)

		{
			int minc, count, dist, i;
			bool xmain;
			Point cp;
			double oinc;
			bool rtn = false;

			dist = Math.Abs(ap.Y);
			if (Math.Abs(dx) >= Math.Abs(dy))
				{
				xmain = true;
				count = Math.Abs(dx);
				oinc = (double)dy / dx;
				if (dx > 0)
					minc = 1;
				else
					minc = -1;
				}
			else
				{
				xmain = false;
				count = Math.Abs(dy);
				oinc = (double)dx / dy;
				if (dy > 0)
					minc = 1;
				else
					minc = -1;
				}
			cp = ap;
			for (i = dist; i < count + 1; i++)
				{
				if (xmain)
					{
					cp.X = ap.X + (i * minc);
					cp.Y = ap.Y + (int)Math.Round(i * oinc);
					}
				else
					{
					cp.Y = ap.Y + (i * minc);
					cp.X = ap.X + (int)Math.Round(i * oinc);
					}
				if (!(rtn = CheckPts(cp,dx,dy,sh,eh,sblocks)))
					break;
				}
			return (rtn);
		}



		public static bool ObstacleClear(Shared.space_3d_mm loc1, Shared.space_3d_mm loc2,bool log = true)

		{
			bool rtn = true;
			int i,loc = -1,side_blocks,dx,dy;
			Point sp, ep,ap,step;
			double ih, sh, dh, eh,a1,a2;
			ArrayList steps;
			string lines;
			Stopwatch sw = new Stopwatch();

			try
			{
			Log.LogEntry("Arm obstacle check from " + loc1 + " to " + loc2);
			if ((ws_map != null) && WithinWorkSpace(loc2))
				{
				sw.Start();
				check_count = 0;
				for (i = 0; i < ob_check_map.Length; i++)
					ob_check_map[i] = "";
				sp = new Point(loc1.x,loc1.z);
				ep = new Point(loc2.x,loc2.z);
				ap = new Point(0,-ArmControl.Z_OFFSET_MM);
				if (MapArrayLoc(sp,ref loc))
					ob_check_map[loc] = "start";
				ih = sh = loc1.y;
				steps = DetermineSteps(sp, ep);
				dh = (loc2.y - loc1.y) /steps.Count;
				for (i = 0; i < steps.Count; i++)
					{
					eh = ih + ((i + 1) * dh);
					step = (Point) steps[i];
					dx = step.X - ap.X;
					dy = step.Y - ap.Y;
					if (dx == 0)
						side_blocks = SIDE_BLOCKS;
					else
						{
						a1 = Math.Atan((double) dy/dx) * Shared.RAD_TO_DEG;
						if (dx > 0)
							a2 = 90 - a1;
						else
							a2 = -(90 + a1);
						if (a2 == 90)
							side_blocks = SIDE_BLOCKS;
						else
							side_blocks = (int) Math.Abs(Math.Round(SIDE_BLOCKS/Math.Cos(a2 * Shared.DEG_TO_RAD)));
						}
					rtn = CheckLine(ap,dx,dy,sh,eh,side_blocks);
					if (!rtn)
						break;
					sh = eh;
					}
				sw.Stop();
				lines = "Start point: " + sp.X + " " + sp.Y + "\r\nEnd point: " + ep.X + " " + ep.Y + "\r\nArm origin point: " + ap.X + " " + ap.Y + "\r\ncheck count: " + check_count + "\r\nExecution time (ms): " + sw.ElapsedMilliseconds;
				if (log)
					SaveCheckMap(lines);
				}
			else if (ws_map == null)
				Log.LogEntry("No work space map available");
			else
				Log.LogEntry(loc2.ToString() + " outside of work space.");
			}

			catch(Exception ex)
			{
			rtn = false;
			Log.LogEntry("ObstacleCheck exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			Log.LogEntry("ObstacleClear " + rtn);
			return (rtn);
		}



		private static Point ConvertPtToCm(Point pt)

		{
			Point rpt = new Point();

			rpt.X = (int) Math.Round((double) pt.X/10);
			rpt.Y = (int) Math.Round((double) pt.Y/10);
			return(rpt);
		}



		private static void GenPts(Point sp,int dx,int dy,ref ArrayList pts)

		{
			double sdx,sdy;
			Point pt = new Point();
			string line = "";
			int i;

			if (dx == 0)
				{
				sdy = 1;
				sdx = 0;
				}
			else if (dy == 0)
				{
				sdy = 0;
				sdx = 1;
				}
			else
				{
				sdy = (double) dy / dx;
				if (Math.Abs(sdy) > 1)
					{
					sdy *= .5;
					sdx = .5;
					}
				else
					sdx = 1;
				}
			if (dx < 0)
				{
				sdx *= -1;
				sdy *= -1;
				}
			pts.Add(sp);
			if (sdx == 0)
				{
				pt.X = sp.X;
				for (i = 1; i < Math.Abs(dy) + 1; i++)
					{
					pt.Y = sp.Y + (int)Math.Round(i * sdy);
					pts.Add(pt);
					line += pt.ToString() + " ";
					}
				}
			else if (Math.Abs(sdx) == 1)
				{
				for (i = 1; i < Math.Abs(dx) + 1; i++)
					{
					pt.X = sp.X + (int)Math.Round(i * sdx);
					pt.Y = sp.Y + (int)Math.Round(i * sdy);
					pts.Add(pt);
					line += pt.ToString() + " ";
					}
				}
			else
				{
				for (i = 1;i < (2 * Math.Abs(dx)) + 1;i++)
					{
					pt.X = sp.X + (int)Math.Round(i * sdx);
					pt.Y = sp.Y + (int)Math.Round(i * sdy);
					pts.Add(pt);
					line += pt.ToString() + " ";
					}
				}
			Log.LogEntry(line);
		}


		private static void GenEndPts(double a,Point ep,ref ArrayList ep_p2, ref ArrayList p1_p2)

		{
			int dx,dy,r;
			Point p1 = new Point(),p2 = new Point(),epcm;

			if (a != 0)
				{
				dx = (int) Math.Round((double) 50/Math.Sin(a * Shared.DEG_TO_RAD));
				p1.X = ep.X - dx;
				p1.Y = ep.Y;
				dy = (int) Math.Round(50 * Math.Cos(a * Shared.DEG_TO_RAD));
				r = (int) Math.Round(Math.Sqrt(Math.Pow(50,2) - Math.Pow(dy,2)));
				if (a < 0)
					{
					p2.X = ep.X + r;
					p2.Y = ep.Y + dy;
					}
				else
					{
					p2.X = ep.X - r;
					p2.Y = ep.Y + dy;
					}
				epcm = ConvertPtToCm(ep);
				p1 = ConvertPtToCm(p1);
				p2 = ConvertPtToCm(p2);
				Log.LogEntry("angle: " + a);
				Log.LogEntry("ep: " + epcm.ToString());
				Log.LogEntry("p1: " + p1.ToString());
				Log.LogEntry("p2: " + p2.ToString());
				Log.LogEntry("ep to p2 points");
				dx = p2.X - epcm.X;
				dy = p2.Y - epcm.Y;
				GenPts(epcm,dx,dy,ref ep_p2);
				Log.LogEntry("p1 to p2 points");
				dx = p2.X - p1.X;
				dy = p2.Y - p1.Y;
				GenPts(p1,dx,dy,ref p1_p2);
				}
			else
				Log.LogEntry("Angle is 0.");
		}		



		private static void GenFillPts(ArrayList ep_p2,ArrayList p1_p2,ref ArrayList fp)

		{
			int sx,i,j,loc;
			Point ep,p1,pt,pt2 = new Point();
			bool done;
			string line = "";

			ep = (Point) ep_p2[0];
			p1 = (Point) p1_p2[0];
			if (ep.X < p1.X)
				sx = 1;
			else
				sx = -1;
			Log.LogEntry("fill points");
			j = 0;
			for (i = 0;i < ep_p2.Count;i++)
				{
				pt = (Point) ep_p2[i];
				if (pt.Y != pt2.Y)
					do
						{
						if (j == p1_p2.Count)
							{
							pt2 = new Point(0,0);
							break;
							}
						pt2 = (Point) p1_p2[j];
						j += 1;
						}
					while (pt.Y != pt2.Y);
				if ((!pt2.IsEmpty) && (pt.X != pt2.X))
					{
					done = false;
					do
						{
						pt.X += sx;
						if (pt.X != pt2.X)
							{
							loc = ((MAP_LENGTH_CM - pt.Y) * MAP_WIDTH_CM) + (pt.X + COL_CORRECT);
							if (ob_check_map[loc].Contains("c"))
								done = true;
							else
								{
								fp.Add(pt);
								line += pt.ToString() + " ";
								}
							}
						else
							done = true;
						}
					while(!done);
					}
				}
			Log.LogEntry(line);
		}


		}
	}
