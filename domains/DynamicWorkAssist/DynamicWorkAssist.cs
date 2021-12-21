using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using RobotArm;
using Speech;

namespace DynamicWorkAssist
	{
	class DynamicWorkAssist:DomainInterface
		{

		private const int POSITION_OFFSET = 50;
		private const int ARM_INC_MOVE_MM = 25;
		private const int GRIPPER_INC_ROT = 5;
		private const int WRIST_INC_TILT = 5;
		private const double PALM_WIDTH_MIN = (int) (2.7 * Shared.IN_TO_MM); //this varies depending on hand size, based on NASA study using 5th percentile female
		private const string IS_MODELS_FILE = "ismodels.csv";
		private const string OD_MODELS_FILE = "odmodels.csv";
		private const string OV_MODELS_FILE = "ovmodels.csv";


		private SortedList containers = new SortedList();
		private SortedList parts;
		private Thread vprocess = null;

		private bool moving = false;
		private string object_in_grippper = "";
		private string last_repeat_cmd = "";
		private int stop_msg = 0;
		private int spur_msg = 0;
		private int ignor_msg = 0;

		private bool od_server_open = false;
		private bool is_server_open = false;

		private PnP pnp = new PnP();
		private PnP.Context cntxt;
		private int pnp_tries;

		public static AutoOpInterface aoi = null;



		public bool Open(AutoOpInterface naoi)

		{
			bool rtn = false;
			int i;
			string outp;
			Parts.Part prt = new Parts.Part();
			Containers.Container con = new Containers.Container();
			string[] files;

			Log.KeyLogEntry("Running dynamic work assist");
			Speech.PvSpeech.SpeakAsync("Starting the dynamic work assist scenario.  This may take a minute or so.");
			aoi = naoi;
			Shared.domain_path = Shared.base_path + Shared.DOMAIN_DIR + "DynamicWorkAssist\\";
			spur_msg = 0;
			TextOutput("Loading object detection and image segmentation models");
			if (OpenObjectDetection() && OpenImageSegmentation())
				{
				D415Camera.CaptureImages();
				DisplayVideoImage((Image)Shared.vimg.Clone());
				TextOutput("Loading containers information.");
				containers = Containers.ContainerList();
				if (containers.Count > 0)
					{
					DomainShared.AvailContainers();
					if (DomainShared.avail_containers.Count > 0)
						{
						outp = "The following containers are available";
						for (i = 0; i < DomainShared.avail_containers.Count; i++)
							{
							con = (Containers.Container) DomainShared.avail_containers.GetByIndex(i);
							outp += ", " + con.name;
							}
						TextOutput(outp);
						}
					else
						TextOutput("No usable containers detected.");
					}
				else
					TextOutput("No container is available.");
				TextOutput("Loading parts information.");
				parts = Parts.PartsList();
				if (parts.Count > 0)
					{
					outp = "The following parts are supported ";
					for (i = 0; i < parts.Count; i++)
						{
						prt = (Parts.Part)parts.GetByIndex(i);
						outp += ", " + prt.name;
						}
					TextOutput(outp);
					rtn = true;
					TextOutput("Initializing speech context.");
					files = Directory.GetFiles(Shared.domain_path + Shared.PV_CONTEXT_DIR,"*.rhn");
					if (PvSpeech.SetContext(files[0]) && PvSpeech.RegisterHandler(SpeechHandler))
						{
						ArmControl.Gripper(ArmControl.GRIPPER_FULL_OPEN);
						if (HomeArm())
							{
							TextOutput("Dynamic work assist scenario initialized.");
							rtn = true;
							}
						else
							TextOutput("Homing failed. The scenario is not initialized.");
						}
					else
						TextOutput("Failed to load scenario's NLP. The scenario is not initialized");
					}
				else
					TextOutput("No parts information was available. The scenario is not initialized");
				}
			else
				TextOutput("The dynamic work assist scenario requires visual object detection and image segmentation which are not available. The scenario is not initialized.");
			if (!rtn)
				Close();
			return (rtn);
		}



		public bool OpenObjectDetection()

		{
			bool rtn = false;
			string fname,line,dir,nline;
			TextReader tr;
			string[] data;

			if (Shared.TENSORFLOW)
				{
				dir = Shared.domain_path + Shared.OD_MODEL_DIR;
				fname =  dir + OD_MODELS_FILE;
				}
			else
				{
#pragma warning disable CS0162 // Unreachable code detected
				dir = Shared.domain_path + Shared.OV_MODEL_DIR;
#pragma warning restore CS0162 // Unreachable code detected
				fname =  dir + OV_MODELS_FILE;
				}

			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				while ((line = tr.ReadLine()) != null)
					{
					data = line.Split(',');
					if (data.Length == 3)
						{
						nline = data[0] + "," + dir + "," + data[1] + "," + data[2];
						if (!VisualObjectDetection.Load(nline))
							{
							TextOutput("Attempt to load " + line + " failed.");
							VisualObjectDetection.Close();
							break;
							}
						}
					else
						{
						TextOutput("Detected format error in " + fname);
						VisualObjectDetection.Close();
						break;
						}
					}
				if (line == null)
					{
					od_server_open = true;
					rtn = true;
					}
				}

			return (rtn);
		}



		public void Stop()

		{
			DomainShared.stop_action = true;
		}



		public bool OpenImageSegmentation()

		{
			bool rtn = false;
			string fname, line,dir,nline;
			TextReader tr;
			string[] data;

			dir = Shared.domain_path + Shared.IS_MODEL_DIR;
			fname = dir + IS_MODELS_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				while ((line = tr.ReadLine()) != null)
					{
					data = line.Split(',');
					if (data.Length == 4)
						{
						nline = data[0] + "," + dir + "," + data[1] + "," + data[2] + "," + data[3];
						if (!ImageSegmentation.Load(nline))
							{
							TextOutput("Attempt to load " + line + " failed.");
							ImageSegmentation.Close();
							break;
							}
						}
					else
						{
						TextOutput("Detected format error in " + IS_MODELS_FILE);
						ImageSegmentation.Close();
						break;
						}
					}
				if (line == null)
					{
					is_server_open = true;
					rtn = true;
					}
				}
			return (rtn);
		}



		public void Close()

		{
			if (!Shared.parked)
				ArmControl.Park();
			PvSpeech.UnRegisterHandler();
			if (od_server_open)
				VisualObjectDetection.Unload();
			if (is_server_open)
				ImageSegmentation.Unload();
			Shared.domain_path = "";
			DomainShared.avail_containers.Clear();
			TextOutput("Dynamic work assist scenario closed.");
			Log.LogEntry("Stop vocal messages: " + stop_msg + "  spurious vocal messages: " + spur_msg + "  ignored vocal messages: " + ignor_msg);
		}



		public bool HomeArm()

		{
			bool rtn = false;

			if (ArmControl.Home() && D415Camera.CaptureImages() && Mapping.MapWorkSpace(0) && Mapping.RemoveShadows(Mapping.base_ws_map) && Mapping.SaveCurrentMapAsRef())
				{
				DisplayVideoImage((Image)Shared.vimg.Clone());
				rtn = true;
				}
			return(rtn);
		}



		public static void TextOutput(string msg,bool verbal = true)

		{
			if (aoi != null)
				aoi.TextOutput(msg);
			else
				Log.LogEntry(msg);
			if (verbal)
				PvSpeech.SpeakAsync(msg);
		}



		public static void DisplayVideoImage(Image img)

		{
			if (aoi != null)
				aoi.VideoOutput(img);
		}






		private bool SaveImages()

		{
			bool rtn = true;

			try 
			{
			Shared.SaveVideoPic(Shared.vimg);
			Shared.SaveDeptBin();
			}

			catch(Exception ex)
			{
			TextOutput("SaveButton exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			rtn = false;
			}

			return(rtn);
		}



		private void DoPnP(string msg,Containers.Container con)

		{
			ArrayList objs = new ArrayList();
			string obj_name = "";
			int i;
			PnP.OpReturn rtn;
			string reply;

			DomainShared.stop_action = false;
			if (D415Camera.CaptureImages())
				{
				for (i = 0; i < parts.Count; i++)
					{
					if (msg.Contains(((Parts.Part)parts.GetByIndex(i)).name))
						{
						obj_name = ((Parts.Part)parts.GetByIndex(i)).name;
						objs = Parts.AvailableSurfaceParts(DomainShared.avail_containers, obj_name);
						break;
						}
					}
				DisplayVideoImage((Image)Shared.vimg.Clone());
				if (objs.Count > 0)
					{
					cntxt = new PnP.Context();
					cntxt.con = con;
					cntxt.objs = objs;
					cntxt.current_op = -1;
					cntxt.current_obj_indx = 0;
					cntxt.completed_parts = 0;
					cntxt.object_in_gripper = "";
					cntxt.skipped_parts = 0;
					if (msg.Contains("parts"))
						cntxt.parts_to_move = objs.Count;
					else
						cntxt.parts_to_move = 1;
					do
						{
						rtn = pnp.DoPnP(ref cntxt);
						if (rtn == PnP.OpReturn.STOPPED)
							{
							reply = PvSpeech.Conversation("The operation is stopped.  Do you want me to continue?", 10000);
							do
								{
								if (reply == "")
									reply = PvSpeech.Conversation("The operation is stopped.  Do you want me to continue?", 10000);
								else if ((reply != "affirmative") && (reply != "negative"))
									reply = PvSpeech.Conversation("I did not understand your reply.  Do you want me to continue?", 10000);
								}
							while ((reply != "affirmative") && (reply != "negative"));
							if (reply == "negative")
								rtn = PnP.OpReturn.FAIL;
							DomainShared.stop_action = false;
							}
						}
					while((rtn != PnP.OpReturn.FAIL) && (rtn != PnP.OpReturn.DONE));
					if (rtn == PnP.OpReturn.DONE)
						{
						if (cntxt.skipped_parts > 0)
							{
							if (pnp_tries == 0)
								{
								TextOutput("Partially completed pick and place.  Attempting to locate skipped parts.");
								DoPnP(msg, con);
								pnp_tries += 1;
								}
							else
								TextOutput("Could not complete the pick and place.");
							}
						else if (cntxt.completed_parts < cntxt.parts_to_move)
							TextOutput("Could not complete the pick and place.");
						else
							TextOutput("Pick and place completed.");
						}
					else if (rtn == PnP.OpReturn.FAIL)
						TextOutput("Could not complete the pick and place.");
					}
				else if (ArmControl.GripperFull())
					PutCommand(msg);
				else
					TextOutput("Found no " + obj_name);
				}
			else
				TextOutput("Could not capture a workplace image.");
	}



		private void PnPCommand(string msg)

		{
			int i,tries = 0,r;
			Containers.Container con;

			try
			{
			for (r = 0;r < 1;r++)
				{
				for (i = 0; i < DomainShared.avail_containers.Count; i++)
					{
					con = (Containers.Container) DomainShared.avail_containers.GetByIndex(i);
					if (msg.Contains(con.name))
						{
						if (D415Camera.CaptureImages())
							{
							if (Containers.ConfirmContainer(con))
								{
								if (!Containers.ContainerTopOpen(con))
									TextOutput("The container's top is not open.");
								else
									{
									pnp_tries = 0;
									DoPnP(msg,con);
									}
								break;
								}
							else
								{
								tries += 1;
								if (tries < 2)
									{
									TextOutput("The container " + con.name + " could not be confirmed.  Please wait while I recheck available containers.");
									DomainShared.AvailContainers();
									i = -1;
									}
								else
									{
									TextOutput("The container is not available.");
									break;
									}
								}
							}
						else
							{
							TextOutput("Image capture failed.");
							break;
							}
						}
					}
				if (i == DomainShared.avail_containers.Count)
					{
					if (r == 0)
						{
						TextOutput("The container is not in the available list. Please wait while I recheck available containers");
						DomainShared.AvailContainers();
						}
					else
						TextOutput("The container is not available.");
					}
				else
					break;
				}
			}

			catch(Exception ex)
			{
			Log.LogEntry("Exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

		}



		private void WhatConainers()

		{
			string outp;
			int i;
			Containers.Container con;

			if (containers.Count > 0)
				{
				TextOutput("Checking.");
				DomainShared.AvailContainers();
				if (DomainShared.avail_containers.Count > 0)
					{
					outp = "The following containers are available";
					for (i = 0; i < DomainShared.avail_containers.Count; i++)
						{
						con = (Containers.Container)DomainShared.avail_containers.GetByIndex(i);
						outp += ", " + con.name;
						}
					TextOutput(outp);
					}
				else
					TextOutput("No usable containers detected.");
				}
			else
				TextOutput("No container is available.");
			}



			private void WhatPartsCommand()

		{
			int i,count,id;
			ArrayList al;
			string outp;
			Bitmap bm;
			Graphics g;
			Rectangle rect;
			VisualObjectDetection.visual_detected_object vdo;
			Parts.AvailPart ap;
			SortedList aparts = new SortedList();

			TextOutput("Checking.");
			outp = "I can see ";
			if (D415Camera.CaptureImages())
				{
				bm = (Bitmap)Shared.vimg.Clone();
				g = Graphics.FromImage(bm);
				al = Parts.AvailableSurfaceParts(DomainShared.avail_containers,true);
				if (al.Count > 0)
					{
					for (i = 0; i < al.Count; i++)
						{
						ap = (Parts.AvailPart) al[i];
						vdo = ap.vdo;
						rect = new Rectangle();
						rect.X = vdo.x;
						rect.Y = vdo.y;
						rect.Width = vdo.width;
						rect.Height = vdo.height;
						g.DrawRectangle(Pens.Red, rect);
						if (aparts.ContainsKey(ap.name))
							{
							count = (int) aparts[ap.name];
							count += 1;
							id = aparts.IndexOfKey(ap.name);
							aparts.SetByIndex(id,count);
							}
						else
							{
							aparts.Add(ap.name,1);
							}
						}
					DisplayVideoImage(bm);
					for (i = 0;i < aparts.Count;i++)
						{
						aparts.GetKey(i);
						outp += " " + aparts.GetByIndex(i) + " " + aparts.GetKey(i);
						if (((int) aparts.GetByIndex(i)) > 1)
							outp += "s;";
						else
							outp += ";";
						}
					TextOutput(outp + " For a total of " + al.Count + " parts in the workspace.  Usable parts are shown in the video.");
					}
				else
					TextOutput(outp + " no usable parts.");
				}
			else
				TextOutput("Could not capture a workplace image.");
		}



		private bool GripObject(DomainShared.ObjData od)

		{
			bool rtn = false;
			string reply;
			double wa;

			wa = -od.planeorient;
			if (ObstacleAvoid.Move(od.pick.x,od.pick.y,od.minz - POSITION_OFFSET, ArmControl.gripper_orient.FLAT,wa))
				{
				if (ArmControl.Move(od.pick.x, od.pick.y, od.pick.z + 2, ArmControl.gripper_orient.FLAT,wa))
					{
					ArmControl.CloseGrip();
					if (ArmControl.GripperFull())
						{
						do
							{
							reply = PvSpeech.Conversation("I have the object. Have you released it?", 10000);
							TextOutput(reply,false);
							}
						while (reply != "affirmative");
						if (!ArmControl.Move(od.pick.x, od.pick.y, od.minz - POSITION_OFFSET, ArmControl.gripper_orient.FLAT,wa))
							{
							TextOutput("Attempt to move from the pick point failed. Attempting an emergency park");
							ArmControl.CheckGripperHardwareError();
							ArmControl.EmergencyPark();
							}
						else
							rtn = true;
						}
					else
						{
						TextOutput("Attempt to grip the object failed.");
						ArmControl.Gripper(ArmControl.GRIPPER_FULL_OPEN);
						Thread.Sleep(100);
						ArmControl.Move(od.pick.x, od.pick.y, od.minz - POSITION_OFFSET, ArmControl.gripper_orient.FLAT, wa);
						}
					}
				else
					TextOutput("Attempt to move to the pick point failed.");
				}
			else
				TextOutput("Attempt to approach pick point failed.");
			return (rtn);
		}



		private bool TakeThisCommand()

		{
			bool rtn = false;
			HandDetect.HandData hd = new HandDetect.HandData();
			Rectangle rect;
			Bitmap bm,obm;
			string fname,obj_name = "",is_model = "";
			const int ADD_WIDTH = 70,ADD_HEIGHT = 20;
			int x,y,w,h,dif,i;
			ArrayList objs;
			VisualObjectDetection.visual_detected_object vdo;
			DomainShared.ObjData od = new DomainShared.ObjData();
			Parts.Part prt;

			if (!ArmControl.GripperFull())
				{
				if (HdHandDetect.WorkHandDetect(ref hd) && (Math.Abs(hd.center.x) < Mapping.MAP_WIDTH_MM/2) && (hd.center.z < Mapping.MAP_LENGTH_MM))
					{
					bm = (Bitmap)Shared.vimg.Clone();
					rect = new Rectangle();
					x = rect.X = hd.vdo.x;
					y = rect.Y = hd.vdo.y;
					w = rect.Width = hd.vdo.width;
					h = rect.Height = hd.vdo.height;
					obm = bm.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
					Shared.SaveVideoPic(obm);
					if (x >= ADD_WIDTH)
						{
						rect.X = x - ADD_WIDTH;
						dif = ADD_WIDTH;
						}
					else
						{
						dif = x;
						rect.X = 0;
						}
					if (rect.X + rect.Width + dif + ADD_WIDTH < bm.Width)
						rect.Width = w + dif + ADD_WIDTH;
					else
						rect.Width = w + dif + (bm.Width - (rect.X + w + dif));
					if (y >= ADD_HEIGHT)
						{
						rect.Y = y - ADD_HEIGHT;
						dif = ADD_HEIGHT;
						}
					else
						{
						dif = y;
						rect.Y = 0;
						}
					if (rect.Y + h + dif + ADD_HEIGHT< bm.Height)
						rect.Height = h + dif + ADD_HEIGHT;
					else
						rect.Height = h + dif + (bm.Height - (rect.Y + h + dif));
					obm = bm.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
					Shared.SaveVideoPic(obm);
					objs = Shared.Detect("objects in hand",obm,.6,0);
					if (objs.Count > 0)
						{
						for (i = 0;i < parts.Count;i++)
							{
							prt = (Parts.Part) parts.GetByIndex(i);
							if (((VisualObjectDetection.visual_detected_object)objs[0]).object_id == prt.nhand_od_id)
								{
								obj_name = prt.name;
								is_model = prt.is_model;
								break;
								}
							}
						if (is_model.Length > 0)
							{
							vdo = (VisualObjectDetection.visual_detected_object) objs[0];
							rect.X = vdo.x + rect.X;
							rect.Y = vdo.y + rect.Y;
							if (vdo.width < 74)
								rect.Width = 74;
							else
								rect.Width = vdo.width;
							if (vdo.height < 59)
								rect.Height = 59;
							else
								rect.Height = vdo.height;
							obm = bm.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
							fname = Shared.SaveVideoPic(obm);
							fname = ImageSegmentation.Infer(fname,is_model);
							if (fname.Length > 0)
								{
								obm = new Bitmap(fname);
								Shared.SaveVideoPic(obm);
								if  (Parts.PartData(obj_name,obm,rect,false,ref od))
									{
									od.name = obj_name;
									object_in_grippper = obj_name;
									GripObject(od);
									DomainShared.HomeArmOA();
									rtn = true;
									}
								else
									TextOutput("Could not determine object data.");
								obm.Dispose();
								File.Delete(fname);
								}
							else
								TextOutput("Could not obtain detail data.");
							}
						else
							TextOutput("No known object detected in your hand.");
						}
					else
						TextOutput("No object detected in your hand.");
					}	
				else
					TextOutput("No hand detected.");
				}
			else
				TextOutput("I already have a " + object_in_grippper + " in my gripper.");
			return(rtn);
		}



		private void GiveMeThat()

		{
			HdHandDetect.HandPts hpts = new HdHandDetect.HandPts();

			if (HdHandDetect.WorkHandDetect(ref hpts))
				{
				if (hpts.width > PALM_WIDTH_MIN)
					{
					if (Shared.InReach(hpts.center,false))
						{
						if (DomainShared.PlaceInHandOA(hpts, object_in_grippper))
							object_in_grippper = "";
						if (!DomainShared.HomeArmOA())
							TextOutput("Attempt to home the arm failed.");
						}
					else
						TextOutput("Hand out of reach.");
					}
				else
					{
					Log.LogEntry("Palm width of " + hpts.width + " millimeters indicates hand orientation is wrong.");
					PvSpeech.SpeakAsync("Your palm width indicates your hand orientation is wrong.");
					}
				}
			else
				TextOutput("No hand detected.");
		}



		private void GiveMe(string msg)

		{

			if (ArmControl.GripperFull())
				{
				if (msg.Contains(object_in_grippper) || !msg.Contains("part"))
					GiveMeThat();
				else
					TextOutput("I am holding a " + object_in_grippper);
				}
			else
				{
				if (PickCommand(msg,false))
					GiveMeThat();
				}
		}



		private void PositionCommand()

		{
			HdHandDetect.HandPts hpts = new HdHandDetect.HandPts();
			double reach,x,z,angle,hp;

			if (HdHandDetect.WorkHandDetect(ref hpts))
				{
				reach = Math.Sqrt(Math.Pow(hpts.ft.x, 2) + Math.Pow(hpts.ft.z + ArmControl.Z_OFFSET_MM, 2) + Math.Pow(hpts.ft.y,2));
				if (reach < ArmControl.MAX_REACH_FLAT_MM)
					{
					angle = Math.Atan((double) hpts.ft.x/(hpts.ft.z + ArmControl.Z_OFFSET_MM));
					hp = Math.Sqrt(Math.Pow(hpts.ft.z + ArmControl.Z_OFFSET_MM,2) + Math.Pow(hpts.ft.x,2)) - POSITION_OFFSET;
					z = (hp * Math.Cos(angle)) - ArmControl.Z_OFFSET_MM;
					x = hp * Math.Sin(angle);
					if (!ObstacleAvoid.Move((int) Math.Round(x),hpts.ft.y,(int) Math.Round(z), ArmControl.gripper_orient.FLAT))
						TextOutput("Attempt to position failed.");
					}
				else
					TextOutput("Position out of reach.");
				}
			else
				TextOutput("No hand detected.");
		}



		private void PutCommand(string msg)
	
		{
			int i;
			Containers.Container con;

			if (ArmControl.GripperFull())
				{
				for (i = 0;i < DomainShared.avail_containers.Count;i++)
					{
					con = (Containers.Container) DomainShared.avail_containers.GetByIndex(i);
					if (msg.Contains(con.name))
						{
						if (D415Camera.CaptureImages())
							{
							if (Containers.ConfirmContainer(con))
								{
								if (!Containers.ContainerTopOpen(con))
									TextOutput("The container's top is not open.");
								else
									{
									DomainShared.HandCheck();
									if (Containers.PlaceInContainerOA(con) == Containers.place_reply.OK)
										{
										object_in_grippper = "";
										}
									if (!DomainShared.HomeArmOA())
										TextOutput("Homing failed.");
									}
								break;
								}
							else
								{
								TextOutput("The container " + con.name + " could not be confirmed.  Please wait while I check on available containers.");
								DomainShared.AvailContainers();
								i = -1;
								}
							}
						else
							{
							TextOutput("Could not capture image.");
							break;
							}
						}
					}
				if (i == DomainShared.avail_containers.Count)
					TextOutput("The container is not available.");
				}
			else
				TextOutput("My gripper is empty.");
		}



		private bool PickCommand(string msg,bool handchk = true)

		{
			bool rtn = false;
			ArrayList objs = new ArrayList();
			string obj_name = "";
			int i;

			if (msg.Contains("part"))
				{
				if (D415Camera.CaptureImages())
					{
					for (i = 0; i < parts.Count;i++)
						{
						if (msg.Contains(((Parts.Part) parts.GetByIndex(i)).name))
							{
							obj_name = ((Parts.Part)parts.GetByIndex(i)).name;
							objs = Parts.AvailableSurfaceParts(DomainShared.avail_containers, obj_name);
							break;
							}
						}
					if (objs.Count > 0)
						{
						if (handchk)
							DomainShared.HandCheck();
						if (DomainShared.PickSurfacePartOA((DomainShared.PickData) objs[0],0))
							{
							object_in_grippper = obj_name;
							rtn = true;
							}
						if (!DomainShared.HomeArmOA(handchk))
							TextOutput("Homing failed.");
						}
					else
						TextOutput("No " + obj_name + " found.");
					}
				else
					TextOutput("Could not capture a workplace image.");
				}
			else
				TextOutput("Improper command.");
			return (rtn);
		}



		private bool ArmXMove(bool right)

		{
			bool rtn = false;
			int dx;

			DomainShared.HandCheck();
			if (right)
				dx = ARM_INC_MOVE_MM;
			else
				dx = -ARM_INC_MOVE_MM;
			if (ArmControl.Move(Shared.current_rc_mm.x + dx,Shared.current_rc_mm.y,Shared.current_rc_mm.z,Shared.go))
				{
				if (right)
					last_repeat_cmd = "right";
				else
					last_repeat_cmd = "left";
				rtn = true;
				}
			return (rtn);
		}



		private bool ArmYMove(bool up)

		{
			bool rtn = false;
			int dy;

			DomainShared.HandCheck();
			if (up)
				dy = ARM_INC_MOVE_MM;
			else
				dy = -ARM_INC_MOVE_MM;
			if (ArmControl.Move(Shared.current_rc_mm.x,Shared.current_rc_mm.y + dy,Shared.current_rc_mm.z,Shared.go))
				{
				if (up)
					last_repeat_cmd = "up";
				else
					last_repeat_cmd = "down";
				rtn = true;
				}
			return (rtn);
		}



		private bool ArmZMove(bool forward)

		{
			bool rtn = false;
			int dz;

			DomainShared.HandCheck();
			if (forward)
				dz = ARM_INC_MOVE_MM;
			else
				dz = -ARM_INC_MOVE_MM;
			if (ArmControl.Move(Shared.current_rc_mm.x,Shared.current_rc_mm.y,Shared.current_rc_mm.z + dz,Shared.go))
				{
				if (forward)
					last_repeat_cmd = "forward";
				else
					last_repeat_cmd = "back";
				rtn = true;
				}
			return (rtn);
		}



		private bool GripperRotate(bool right)

		{
			bool rtn = false;
			double angle;

			if (right)
				angle = Shared.gripper_rot - GRIPPER_INC_ROT;
			else
				angle = Shared.gripper_rot + GRIPPER_INC_ROT; 
			if (ArmControl.GripperRotate(angle))
				{
				if (right)
					last_repeat_cmd = "rotate right";
				else
					last_repeat_cmd = "rotate left";
				rtn = true;
				}
			else
				last_repeat_cmd = "";
			return(rtn);
		}



		private bool WristTilt(bool down)

		{
			bool rtn = false;
			double angle = WRIST_INC_TILT;

			if (!down)
				angle *= -1;
			if (ArmControl.WristTilt(angle))
				{
				if (down)
					last_repeat_cmd = "wrist down";
				else
					last_repeat_cmd = "wrist up";
				}
			return(rtn);
		}



		private bool More()

		{
			bool rtn = false;

			if (last_repeat_cmd.Length > 0)
				{
				switch(last_repeat_cmd)
					{
					case "right":
						ArmXMove(true);
						break;

					case "left":
						ArmXMove(false);
						break;

					case "up":
						ArmYMove(true);
						break;

					case "down":
						ArmYMove(false);
						break;

					case "forward":
						ArmZMove(true);
						break;

					case "back":
						ArmZMove(false);
						break;

					case "rotate right":
						GripperRotate(true);
						break;

					case "rotate left":
						GripperRotate(false);
						break;

					case "wrist down":
						WristTilt(true);
						break;

					case "wrist up":
						WristTilt(false);
						break;

					default:
						PvSpeech.SpeakAsync("The command to repeat, " + last_repeat_cmd + ", can not be executed.");
						last_repeat_cmd = "";
						break;
					}
				}
			else
				PvSpeech.SpeakAsync("There is no command to repeat.");
			return(rtn);
		}



		private void HandleSpeech(object msgo)

		{
			string msg = (string) msgo;
			string[] values;

			try
			{
			values = msg.Split(',');
			Log.BoldLogEntry(msg);
			if (msg == "manualMode")
				{
				PvSpeech.StartNlpOnly();
				PvSpeech.SpeakAsync("okay");
				}
			else if (msg == "manualDone")
				{
				PvSpeech.SpeakAsync("okay");
				PvSpeech.StopNlpOnly();
				}
			else if (values[0] == "whatParts")
				{
				last_repeat_cmd = "";
				WhatPartsCommand();
				}
			else if (values[0] == "whatContainers")
				{
				last_repeat_cmd = "";
				WhatConainers();
				}
			else if (values[0] == "saveImages")
				{
				last_repeat_cmd = "";
				if (!SaveImages())
					TextOutput("attempt to save images failed");
				else
					TextOutput("images saved");
				}
			else if (values[0] == "captureImages")
				{
				last_repeat_cmd = "";
				if (D415Camera.CaptureImages())
					{
					TextOutput("Images captured.");
					DisplayVideoImage((Image)Shared.vimg.Clone());
					}
				else
					TextOutput("Attempt to capture images failed.");
				}
			else if (values[0] == "done")
				{
				PvSpeech.SpeakAsync("okay");
				last_repeat_cmd = "";
				if (aoi != null)
					aoi.OpDone();
				}
			else if (values[0] == "openGripper")
				{
				last_repeat_cmd = "";
				if (ArmControl.Gripper(ArmControl.GRIPPER_FULL_OPEN))
					{
					object_in_grippper = "";
					TextOutput("Gripper opened.");
					}
				else
					TextOutput("Attempt to open gripper failed.");
				}
			else if (values[0] == "closeGripper")
				{
				PvSpeech.SpeakAsync("okay");
				last_repeat_cmd = "";
				ArmControl.CloseGrip();
				}
			else if (values[0] == "home")
				{
				moving = true;
				PvSpeech.SpeakAsync("okay");
				last_repeat_cmd = "";
				HomeArm();
				moving = false;
				}
			else if (values[0] == "park")
				{
				moving = true;
				PvSpeech.SpeakAsync("okay");
				last_repeat_cmd = "";
				if (ArmControl.Park())
					TextOutput("Arm parked.");
				else
					TextOutput("Attempt to park the arm failed.");
				moving = false;
				}
			else if (values[0] == "pnp")
				{
				moving = true;
				last_repeat_cmd = "";
				PvSpeech.SpeakAsync("okay");
				PnPCommand(msg);
				moving = false;
				}
			else if (values[0] == "pick")
				{
				moving = true;
				last_repeat_cmd = "";
				PvSpeech.SpeakAsync("okay");
				PickCommand(msg);
				moving = false;
				}
			else if (values[0] == "place")
				{
				moving = true;
				last_repeat_cmd = "";
				PvSpeech.SpeakAsync("okay");
				PutCommand(msg);
				moving = false;
				}
			else if (values[0] == "takeThis")
				{
				moving = true;
				last_repeat_cmd = "";
				PvSpeech.SpeakAsync("okay");
				TakeThisCommand();
				moving = false;
				}
			else if (values[0] == "giveMe")
				{
				moving = true;
				last_repeat_cmd = "";
				PvSpeech.SpeakAsync("okay");
				GiveMe(msg);
				moving = false;
				}
			else if (values[0] == "positionArm")
				{
				moving = true;
				PvSpeech.SpeakAsync("okay");
				last_repeat_cmd = "";
				PositionCommand();
				moving = false;
				}
			else if ((values[0] == "moveArm") && !Shared.parked && !Shared.homed)
				{
				if (msg.Contains( "right"))
					{
					moving = true;
					last_repeat_cmd = "";
					ArmXMove(true);
					moving = false;
					}
				else if (msg.Contains("left"))
					{
					moving = true;
					last_repeat_cmd = "";
					ArmXMove(false);
					moving = false;
					}
				else if (msg.Contains("up"))
					{
					moving = true;
					last_repeat_cmd = "";
					ArmYMove(true);
					moving = false;
					}
				else if (msg.Contains("down"))
					{
					moving = true;
					last_repeat_cmd = "";
					ArmYMove(false);
					moving = false;
					}
				else if (msg.Contains("forward"))
					{
					moving = true;
					last_repeat_cmd = "";
					ArmZMove(true);
					moving = false;
					}
				else if (msg.Contains("back"))
					{
					moving = true;
					last_repeat_cmd = "";
					ArmZMove(false);
					moving = false;
					}
				}
			else if ((values[0] == "rotateWrist") && !Shared.parked && !Shared.homed)
				{
				moving = true;
				last_repeat_cmd = "";
				if (msg.Contains("right"))
					GripperRotate(true);
				else
					GripperRotate(false);
				moving = false;
				}
			else if ((values[0] == "moveWrist") && !Shared.parked && !Shared.homed)
				{
				moving = true;
				last_repeat_cmd = "";
				if (msg.Contains("down"))
					WristTilt(true);
				else
					WristTilt(false);
				moving = false;
				}
			else if (values[0] == "repeatLast")
				{
				moving = true;
				More();
				moving = false;
				}
			else if ((values[0] == "affirmative") || (values[0] == "negative"))
				spur_msg += 1;
			else
				{
				last_repeat_cmd = "";
				TextOutput("The intent " + values[0] + " is not supported.");
				}
			}

			catch(Exception ex)
			{
			TextOutput("Exception occurred while processing " + msg);
			Log.LogEntry(msg + " exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			moving = false;
			}

			vprocess = null;
		}



		private void SpeechHandler(string msg)

		{
			if ((msg == "stop") && moving)
				{
				DomainShared.stop_action = true;
				stop_msg += 1;
				}
			else if (!moving && (msg != PvSpeech.NOT_UNDERSTOOD) && ((vprocess == null) || !vprocess.IsAlive))
				{
				DomainShared.stop_action = false;
				vprocess = new Thread(HandleSpeech);
				vprocess.Start(msg);
				}
		}


		}
	}
