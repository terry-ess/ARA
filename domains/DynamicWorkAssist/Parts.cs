using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using RobotArm;

namespace DynamicWorkAssist
	{
	public static class Parts
		{

		public const int MIN_BLOB_AREA = 120;
		private const int MAX_MP_DIST_PIX = 15;
		private const int CROP_SIZE = 640;	//since only concerned with parts in the work space, crop and shift picture from D415 camera to get best inference results
		private const int SHIFT = 100;

		public delegate bool PartData2D(Bitmap bm, Rectangle rect, bool surface,ref DomainShared.ObjData od);

		private const string PART_TYPE_NAME = ".Part";


		public struct Part
		{
			public string name;
			public string surface_od_model;
			public int surface_od_id;
			public double surface_min_od_score;
			public string nhand_od_model;
			public int nhand_od_id;
			public double nhand_min_od_score;
			public string is_model;
			public int width_mm;
			public int length_mm;
			public int height_mm;
			public int max_dim_mm;
			public int min_dim_mm;
		};

		public struct PartsModel

		{
			public long model_id;
			public string name;
			public double min_score;
		};



		public struct AvailPart

		{
			public string name;
			public VisualObjectDetection.visual_detected_object vdo;
		};


		private static SortedList partslist = new SortedList();
		private static PartsDAO prts = new PartsDAO();
		private static ArrayList mdls = new ArrayList();

		public static SortedList<string, Delegate> part_handlers = new SortedList<string, Delegate>();
		public static DataTable parts = null;
		public static DataTable surface_models = null;



		static Parts()

		{

		}



		public static SortedList PartsList()
		
		{
			SortedList sl = new SortedList();
			int i;
			Part prt = new Part();
			string fname,tname;

			if (partslist.Count == 0)
				{
				if (prts.Open(Shared.domain_path + Shared.DB_DIR))
					{
					parts = prts.PartsList();
					for (i = 0; i < parts.Rows.Count; i++)
						{
						prt.name = (string) parts.Rows[i][1];
						prt.surface_od_model = (string) parts.Rows[i][2];
						prt.surface_od_id = (int) parts.Rows[i][3];
						prt.surface_min_od_score = (double)parts.Rows[i][4];
						prt.nhand_od_model = (string)parts.Rows[i][5];
						prt.nhand_od_id = (int)parts.Rows[i][6];
						prt.nhand_min_od_score = (double)parts.Rows[i][7];
						prt.is_model = (string) parts.Rows[i][8];
						prt.width_mm = (int) parts.Rows[i][9];
						prt.length_mm = (int) parts.Rows[i][10];
						prt.height_mm = (int) parts.Rows[i][11];
						prt.max_dim_mm = (int) parts.Rows[i][12];
						prt.min_dim_mm = (int) parts.Rows[i][13];
						sl.Add(prt.name,prt);
						part_handlers.Add(prt.name,null);
						fname = Shared.domain_path + prt.name + ".dll";
						tname = prt.name.Replace(' ', '_') + PART_TYPE_NAME;
						LoadPartDll(fname, tname, prt);
						}
					partslist = sl;
					}
				}
			return (partslist);
		}



		private static ArrayList PartsSurfaceODModelList()

		{
			ArrayList pml = new ArrayList();
			int i;
			PartsModel pm = new PartsModel();

			if (prts.Open(Shared.domain_path + Shared.DB_DIR))
				{
				surface_models = prts.SurfaceModelsList();
				for (i = 0;i < surface_models.Rows.Count;i++)
					{
					pm.model_id = (long) surface_models.Rows[i][0];
					pm.name = (string) surface_models.Rows[i][1];
					pm.min_score = (double) surface_models.Rows[i][2];
					pml.Add(pm);
					}
				mdls = pml;
				}
			return(pml);
		}



		private static bool LoadPartDll(string fname,string type_name,Part prt)

		{
			bool rtn = false;

			if (File.Exists(fname))
				{

				try
				{
				Assembly DLL = Assembly.LoadFrom(fname);
				Type ctype = DLL.GetType(type_name);
				dynamic c = Activator.CreateInstance(ctype);
				c.Open(prt);
				}

				catch(Exception)
				{
				rtn = false;
				DynamicWorkAssist.TextOutput("Could not load " + prt.name + "'s DLL");
				}

				}
			else
				Log.LogEntry("Could not find " + fname);
			return(rtn);
		}




		public static bool RegisterHandler(string part_name, PartData2D pd)

		{
			bool rtn = false;

			try
			{
			part_handlers[part_name] = pd;
			rtn = true;
			}

			catch (Exception ex)
			{
			Log.LogEntry("Register handler exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return (rtn);
		}



		public static bool PartData(string part_name,Bitmap bm,Rectangle rect,bool surface,ref DomainShared.ObjData od)

		{
			object obj;
			PartData2D pd2d;
			bool rtn = true;

			obj = part_handlers[part_name];
			if (obj != null)
				{
				pd2d = (PartData2D)obj;
				rtn = pd2d(bm, rect, surface, ref od);
				}
			else
				rtn = false;
			return(rtn);
		}


		public static bool SurfaceObjectAnalyze(Part prt,Bitmap bm,VisualObjectDetection.visual_detected_object vdo)

		{
			bool rtn = false;
			Rectangle rect;
			Bitmap obm;
			string fname;
			Stopwatch sw = new Stopwatch();
			DomainShared.ObjData od = new DomainShared.ObjData();
			double waist_rot = 0;
			object obj;
			PartData2D pd2d;

			rect = new Rectangle();
			rect.X = vdo.x;
			rect.Y = vdo.y;
			rect.Width = vdo.width;
			rect.Height = vdo.height;
			obm = bm.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			fname = Shared.SaveVideoPic(obm);
			sw.Start();
			fname = ImageSegmentation.Infer(fname,prt.is_model);
			sw.Stop();
			Log.LogEntry("SEG inference time (msec): " + sw.ElapsedMilliseconds);
			if (fname.Length > 0)
				{
				obm = new Bitmap(fname);
				Shared.SaveVideoPic(obm);
				obj = part_handlers[prt.name];
				if (obj != null)
					{
					pd2d = (PartData2D) obj;
					if (pd2d(obm,rect,true,ref od))
						{
						if (od.pick.y <= Mapping.fmr.maxh + prt.height_mm)
							{
							Shared.rcloc = od.pick;
							Shared.rcloc.y = od.maxy;
							waist_rot = Math.Atan((double)Shared.rcloc.x / (Shared.rcloc.z + ArmControl.Z_OFFSET_MM)) * Shared.RAD_TO_DEG;
							Log.LogEntry("  Waist rotation (°): " + waist_rot.ToString("F1"));
							Log.LogEntry("  object orient (°): " + od.planeorient);
							Shared.gripper_rot = waist_rot - od.planeorient;
							Log.LogEntry("  Wrist rotation (°): " + Shared.gripper_rot.ToString("F1"));
							rtn = true;
							}
						else
							Log.LogEntry("part is not on the surface");
						}
					else
						Log.LogEntry("pd2d failed.");
					}
				else
					Log.LogEntry("No pd2d available.");
				obm.Dispose();
				File.Delete(fname);
				}
			else
				Log.LogEntry("Image segmentation inference failed.");
			return(rtn);
		} 



		public static bool ConfirmPart(DomainShared.PickData pd)

		{
			bool rtn = false;
			Bitmap bm;
			Stopwatch sw = new Stopwatch();
			ArrayList al;
			int i,dist = 0;
			Point midpt = new Point(),mpt = new Point();
			VisualObjectDetection.visual_detected_object vdo;

			sw.Start();
			if (D415Camera.CaptureImages())
				{
				bm = VisualObjectDetection.CropPicture((Bitmap) Shared.vimg,CROP_SIZE,SHIFT);
				midpt.X = pd.vdo.x + (int) Math.Round((double) pd.vdo.width/2);
				midpt.Y = pd.vdo.y + (int) Math.Round((double) pd.vdo.height/2);
				al = Shared.Detect(pd.part.surface_od_model, bm, pd.part.surface_min_od_score, pd.part.surface_od_id);
				for (i = 0;i < al.Count;i++)
					{
					vdo = (VisualObjectDetection.visual_detected_object) al[i];
					vdo = VisualObjectDetection.UnCropVDO(vdo, CROP_SIZE, SHIFT);
					mpt.X = vdo.x + (int) Math.Round((double) vdo.width / 2);
					mpt.Y = vdo.y + (int) Math.Round((double) vdo.height / 2);
					dist = Shared.DistancePtToPt(midpt, mpt);
					if (dist < MAX_MP_DIST_PIX)
						{
						rtn = true;
						break;
						}
					}
				}
			else
				Log.LogEntry("Could not capture images.");
			sw.Stop();
			Log.LogEntry("Part confirmation " + rtn + " in " + sw.ElapsedMilliseconds + " ms, distance (pix) " + dist);
			return (rtn);
		}



		public static ArrayList AvailableSurfaceParts(SortedList containers,string pname,bool verbal = false)

		{
			ArrayList al,avail_parts = new ArrayList();
			Bitmap bm;
			int i,j,out_reach = 0,in_box = 0;
			DomainShared.PickData pd;
			Rectangle rect = new Rectangle();
			bool inbox;
			Part prt;
			Object obj;
			Stopwatch sw = new Stopwatch();
			VisualObjectDetection.visual_detected_object vdo;

			sw.Start();
			bm = VisualObjectDetection.CropPicture((Bitmap) Shared.vimg, CROP_SIZE, SHIFT);
			obj = partslist[pname];
			if (obj != null)
				{
				prt = (Part) obj;
				al = Shared.Detect(prt.surface_od_model, bm,prt.surface_min_od_score,prt.surface_od_id);
				if (verbal && (al.Count == 0))
					{
					DynamicWorkAssist.TextOutput("Could detect no " + pname + "s.");
					}
				else
					{
					for (i = 0;i < al.Count;i++)
						{
						vdo = (VisualObjectDetection.visual_detected_object) al[i];
						vdo = VisualObjectDetection.UnCropVDO(vdo, CROP_SIZE, SHIFT);
						if (SurfaceObjectAnalyze(prt,(Bitmap) Shared.vimg,vdo))
							{
							if (Shared.InReach(Shared.rcloc,true))
								{
								rect.X = vdo.x;
								rect.Y = vdo.y;
								rect.Width = vdo.width;
								rect.Height = vdo.height;
								inbox = false;
								for (j = 0;j < containers.Count;j++)
									{
									if (Shared.RectOverlap(rect,((Containers.Container) containers.GetByIndex(j)).rect))
										{
										in_box += 1;
										inbox = true;
										break;
										}
									}
								if (!inbox)
									{
									pd = new DomainShared.PickData();
									pd.vdo = vdo;
									pd.mp_rcloc_mm = Shared.rcloc;
									pd.wrist_rot = Shared.gripper_rot;
									pd.part = prt;
									avail_parts.Add(pd);
									}
								}	
							else
								out_reach += 1;
							}
						else
							DynamicWorkAssist.TextOutput("Could detect no " + pname + "s.");
						}
					}
				Log.LogEntry(pname + "s: available " + avail_parts.Count + ", out of reach " + out_reach + ", in box " + in_box);
				if (verbal)
					{
					if (out_reach > 0)
						DynamicWorkAssist.TextOutput(out_reach + " " + pname + "s out of range.");
					if (in_box > 0)
						DynamicWorkAssist.TextOutput(in_box + " " + pname + "s in box.");
					}
				}
			sw.Stop();
			Log.LogEntry("AvailableSurfaceParts scan took " + sw.ElapsedMilliseconds + " ms.");
			return(avail_parts);
		}



		public static ArrayList AvailableSurfaceParts(SortedList containers,bool verbal = false)

		{
			ArrayList al,avail_parts = new ArrayList();
			Bitmap bm;
			int i,j,k,out_reach = 0,in_box = 0;
			AvailPart ap = new AvailPart();
			Rectangle rect = new Rectangle();
			bool inbox;
			Part prt;
			Stopwatch sw = new Stopwatch();
			VisualObjectDetection.visual_detected_object vdo;
			DataView dv;
			Parts.PartsModel pm;
			DataRow[] dr;

			sw.Start();
			bm = VisualObjectDetection.CropPicture((Bitmap) Shared.vimg, CROP_SIZE, SHIFT);
			dv = new DataView(Parts.parts);
			dv.Sort = "surface_od_id ASC";
			if (mdls.Count == 0)
				PartsSurfaceODModelList();
			for (i = 0; i < mdls.Count; i++)
				{
				pm = (Parts.PartsModel) mdls[i];
				al = Shared.Detect(pm.name, bm, pm.min_score, 0);
				if (verbal && al.Count == 0)
					DynamicWorkAssist.TextOutput("Could detect no " + pm.name + "s.");
				else
					{
					for (j = 0; j < al.Count; j++)
						{
						vdo = (VisualObjectDetection.visual_detected_object) al[j];
						vdo = VisualObjectDetection.UnCropVDO(vdo, CROP_SIZE, SHIFT);
						dr = Parts.parts.Select("surface_od_id = " + vdo.object_id);
						prt.name = (string) dr[0][1];
						prt.surface_od_model = (string) dr[0][2];
						prt.surface_od_id = (int) dr[0][3];
						prt.surface_min_od_score = (double)parts.Rows[0][4];
						prt.nhand_od_model = (string)parts.Rows[0][5];
						prt.nhand_od_id = (int)parts.Rows[0][6];
						prt.nhand_min_od_score = (double)parts.Rows[0][7];
						prt.is_model = (string)parts.Rows[0][8];
						prt.width_mm = (int)parts.Rows[0][9];
						prt.length_mm = (int)parts.Rows[0][10];
						prt.height_mm = (int)parts.Rows[0][11];
						prt.max_dim_mm = (int)parts.Rows[0][12];
						prt.min_dim_mm = (int)parts.Rows[0][13];
						if (SurfaceObjectAnalyze(prt, (Bitmap)Shared.vimg, vdo))
							{
							if (Shared.InReach(Shared.rcloc, true))
								{
								rect.X = vdo.x;
								rect.Y = vdo.y;
								rect.Width = vdo.width;
								rect.Height = vdo.height;
								inbox = false;
								for (k = 0; k < containers.Count; k++)
									{
									if (Shared.RectOverlap(rect, ((Containers.Container)containers.GetByIndex(k)).rect))
										{
										in_box += 1;
										inbox = true;
										break;
										}
									}
								if (!inbox)
									{
									ap.name = prt.name;
									ap.vdo = vdo;
									avail_parts.Add(ap);
									}
								}
							else
								out_reach += 1;
							}
						}
					}
				}
			sw.Stop();
			if (out_reach == 1)
				DynamicWorkAssist.TextOutput("One part is out of range.");
			else if (out_reach > 0)
				DynamicWorkAssist.TextOutput(out_reach + " parts are out of range.");
			if (in_box == 1)
				DynamicWorkAssist.TextOutput("One part is in a box.");
			else if (in_box > 0)
				DynamicWorkAssist.TextOutput(in_box + " parts are in a box.");
			Log.LogEntry("AvailableSurfaceParts scan took " + sw.ElapsedMilliseconds + " ms.");
			return(avail_parts);
		}



		 
		}
	}
