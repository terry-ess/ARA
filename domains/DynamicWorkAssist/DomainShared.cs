using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RobotArm;
using Speech;


namespace DynamicWorkAssist
	{
	static public class DomainShared
		{

		private const int TIP_DIST = 50;
		private const int MIN_BLOB_AREA = 100;

		public const int Y_PICK_OFFSET_MM = 25;
		public const int HAND_PLACE_OFFSET_MM = 64;
		public const int HAND_PLACE_MIN_OFFSET_MM = 10;

		public struct ObjData
		{
			public Shared.space_3d_mm end;
			public Shared.space_3d_mm pick;
			public Shared.space_3d_mm center;
			public double planeorient;
			public int maxy;
			public int minz;
			public string name;

			public override string ToString()

			{
				string rtn = "";

				rtn = "center pt: " + center + "  end pt: " + end + "  pick pt:" + pick + "  max y (mm): " + maxy + "  min z (mm): " + minz + "  gripper direction (°): " + planeorient;
				return(rtn);
			}

		}


		public struct PickData
		{
			public Shared.space_3d_mm mp_rcloc_mm;
			public double wrist_rot;
			public VisualObjectDetection.visual_detected_object vdo;
			public Parts.Part part;
		};

		public static bool stop_action = false;
		public static SortedList avail_containers = new SortedList();


		public static bool PickSurfacePart(PickData pd,int container_side_height)

		{
			bool rtn = false;

			if (ArmControl.Move(pd.mp_rcloc_mm.x,pd.mp_rcloc_mm.y + Y_PICK_OFFSET_MM,pd.mp_rcloc_mm.z, ArmControl.gripper_orient.DOWN,pd.wrist_rot))
				{
				if (ArmControl.Move(pd.mp_rcloc_mm.x,0, pd.mp_rcloc_mm.z, ArmControl.gripper_orient.DOWN, pd.wrist_rot))
					{
					ArmControl.CloseGrip(0);
					if (ArmControl.ReadGripFSR() < ArmControl.MAX_GRIP_VALUE)
						{
						ArmControl.Gripper(ArmControl.GRIPPER_EXT_MAX_MM);
						if (ArmControl.Move(pd.mp_rcloc_mm.x, -2, pd.mp_rcloc_mm.z, ArmControl.gripper_orient.DOWN, pd.wrist_rot))
							{
							ArmControl.CloseGrip(0);
							if (ArmControl.ReadGripFSR() < ArmControl.MAX_GRIP_VALUE)
								{
								DynamicWorkAssist.TextOutput("Attempt to grip object failed.");
								ArmControl.Gripper(ArmControl.GRIPPER_EXT_MAX_MM);
								ArmControl.Home();
								}
							else if (ArmControl.Move(pd.mp_rcloc_mm.x, pd.mp_rcloc_mm.y + Y_PICK_OFFSET_MM + container_side_height, pd.mp_rcloc_mm.z, ArmControl.gripper_orient.DOWN, pd.wrist_rot))
								{
								Thread.Sleep(100);
								rtn = true;
								}
							else
								DynamicWorkAssist.TextOutput("Attempt to move up from surface failed.");
							}
						else
							DynamicWorkAssist.TextOutput("Attempt to move to pick position failed.");
						}
					else if (ArmControl.Move(pd.mp_rcloc_mm.x, pd.mp_rcloc_mm.y + Y_PICK_OFFSET_MM + container_side_height, pd.mp_rcloc_mm.z, ArmControl.gripper_orient.DOWN))
						{
						Thread.Sleep(100);
						rtn = true;
						}
					else
						DynamicWorkAssist.TextOutput("Attempt to move up from surface failed.");
					}
				else
					DynamicWorkAssist.TextOutput("Attempt to move to pick position failed.");
				}
			else
				DynamicWorkAssist.TextOutput("Attempt to move to shaft location failed.");
			return(rtn);
		}


		public static bool PickSurfacePartOA(PickData pd,int container_side_height)

		{
			bool rtn = false;

			if (ObstacleAvoid.Move(pd.mp_rcloc_mm.x,pd.mp_rcloc_mm.y + Y_PICK_OFFSET_MM,pd.mp_rcloc_mm.z, ArmControl.gripper_orient.DOWN,pd.wrist_rot))
				{
				if (ArmControl.Move(pd.mp_rcloc_mm.x,0, pd.mp_rcloc_mm.z, ArmControl.gripper_orient.DOWN, pd.wrist_rot))
					{
					ArmControl.CloseGrip(0);
					if (ArmControl.ReadGripFSR() < ArmControl.MAX_GRIP_VALUE)
						{
						ArmControl.Gripper(ArmControl.GRIPPER_EXT_MAX_MM);
						if (ArmControl.Move(pd.mp_rcloc_mm.x, -2, pd.mp_rcloc_mm.z, ArmControl.gripper_orient.DOWN, pd.wrist_rot))
							{
							ArmControl.CloseGrip(0);
							if (ArmControl.ReadGripFSR() < ArmControl.MAX_GRIP_VALUE)
								{
								DynamicWorkAssist.TextOutput("Attempt to grip object failed.");
								ArmControl.Gripper(ArmControl.GRIPPER_EXT_MAX_MM);
								ArmControl.Home();
								}
							else if (ArmControl.Move(pd.mp_rcloc_mm.x, pd.mp_rcloc_mm.y + Y_PICK_OFFSET_MM + container_side_height, pd.mp_rcloc_mm.z, ArmControl.gripper_orient.DOWN, pd.wrist_rot))
								{
								Thread.Sleep(100);
								rtn = true;
								}
							else
								DynamicWorkAssist.TextOutput("Attempt to move up from surface failed.");
							}
						else
							DynamicWorkAssist.TextOutput("Attempt to move to pick position failed.");
						}
					else if (ArmControl.Move(pd.mp_rcloc_mm.x, pd.mp_rcloc_mm.y + Y_PICK_OFFSET_MM + container_side_height, pd.mp_rcloc_mm.z, ArmControl.gripper_orient.DOWN))
						{
						Thread.Sleep(100);
						rtn = true;
						}
					else
						DynamicWorkAssist.TextOutput("Attempt to move up from surface failed.");
					}
				else
					DynamicWorkAssist.TextOutput("Attempt to move to pick position failed.");
				}
			else
				DynamicWorkAssist.TextOutput("Attempt to move to shaft location failed.");
			return(rtn);
		}


		
		public static bool PlaceInHand(HdHandDetect.HandPts hpts,string name)

		{
			bool rtn = false,positioned = false,gdown;
			string reply;

			gdown = true;
			if (ArmControl.Move(hpts.center.x, hpts.maxh + HAND_PLACE_OFFSET_MM, hpts.center.z, ArmControl.gripper_orient.DOWN))
				{
				if (ArmControl.Move(hpts.center.x, hpts.center.y + 10, hpts.center.z, ArmControl.gripper_orient.DOWN))
					positioned = true;
				else
					DynamicWorkAssist.TextOutput("Attempt to move to drop point failed.");
				}
			else
				{
				gdown = false;
				if (ArmControl.Move(hpts.center.x, hpts.maxh + HAND_PLACE_OFFSET_MM + ArmControl.GRIPPER_90_OVERHANG + TIP_DIST, hpts.center.z, ArmControl.gripper_orient.FLAT,90))
					{
					if (ArmControl.Move(hpts.center.x, hpts.center.y + 10 + ArmControl.GRIPPER_90_OVERHANG + TIP_DIST, hpts.center.z, ArmControl.gripper_orient.FLAT,90))
						positioned = true;
					else
						DynamicWorkAssist.TextOutput("Attempt to move to drop point failed.");
					}
				else
					DynamicWorkAssist.TextOutput("Attempt to move to hand location failed.");
				}
			if (positioned)
				{
				do
					reply = PvSpeech.Conversation("Are you ready to take the " + name + "?", 10000);
				while (reply != "affirmative");
				if (!gdown)
					ArmControl.WristTilt(20);
				ArmControl.Gripper(ArmControl.GRIPPER_EXT_MAX_MM);
				Thread.Sleep(100);
				if (ArmControl.ReadGripFSR() >= ArmControl.MAX_GRIP_VALUE)
					DynamicWorkAssist.TextOutput("Attempt to drop " + name + " failed.");
				if (gdown)
					ArmControl.Move(hpts.center.x, hpts.maxh + HAND_PLACE_OFFSET_MM, hpts.center.z, ArmControl.gripper_orient.DOWN);
				else
					ArmControl.Move(hpts.center.x,hpts.maxh + HAND_PLACE_OFFSET_MM +ArmControl.GRIPPER_90_OVERHANG,hpts.center.z, ArmControl.gripper_orient.FLAT,90);
				}
			return (rtn);
		}



		public static bool PlaceInHandOA(HdHandDetect.HandPts hpts,string name)

		{
			bool rtn = false,positioned = false,gdown = true;
			string reply;

			if (ArmControl.MoveCheck(hpts.center.x, hpts.maxh + HAND_PLACE_OFFSET_MM, hpts.center.z, ArmControl.gripper_orient.DOWN,false))
				{
				gdown = true;
				if (ObstacleAvoid.Move(hpts.center.x, hpts.maxh + HAND_PLACE_OFFSET_MM, hpts.minz, ArmControl.gripper_orient.DOWN))
					{
					if (ArmControl.Move(hpts.center.x, hpts.maxh + HAND_PLACE_OFFSET_MM, hpts.center.z, ArmControl.gripper_orient.DOWN))
						{
						if (ArmControl.Move(hpts.center.x, hpts.center.y + HAND_PLACE_MIN_OFFSET_MM, hpts.center.z, ArmControl.gripper_orient.DOWN))
							positioned = true;
						else
							DynamicWorkAssist.TextOutput("Attempt to move to drop point failed.");
						}
					else
						DynamicWorkAssist.TextOutput("Attempt to move to palm location failed.");
					}
				else
					DynamicWorkAssist.TextOutput("Attempt to move to hand approach location failed.");
				}
			else
				{
				gdown = false;
				if (ObstacleAvoid.Move(hpts.center.x, hpts.maxh + HAND_PLACE_OFFSET_MM + ArmControl.GRIPPER_90_OVERHANG + TIP_DIST, hpts.minz, ArmControl.gripper_orient.FLAT, 90))
					{
					if (ArmControl.Move(hpts.center.x, hpts.maxh + HAND_PLACE_OFFSET_MM + ArmControl.GRIPPER_90_OVERHANG + TIP_DIST, hpts.center.z, ArmControl.gripper_orient.FLAT,90))
						{
						if (ArmControl.Move(hpts.center.x, hpts.center.y + HAND_PLACE_MIN_OFFSET_MM + ArmControl.GRIPPER_90_OVERHANG + TIP_DIST, hpts.center.z, ArmControl.gripper_orient.FLAT,90))
							positioned = true;
						else
							DynamicWorkAssist.TextOutput("Attempt to move to drop point failed.");
						}
					else
						DynamicWorkAssist.TextOutput("Attempt to move to palm location failed.");
					}
				else
					DynamicWorkAssist.TextOutput("Attempt to move to hand approach location failed.");
				}
			if (positioned)
				{
				do
					reply = PvSpeech.Conversation("Are you ready to take the " + name + "?", 10000);
				while (reply != "affirmative");
				if (!gdown)
					ArmControl.WristTilt(20);
				ArmControl.Gripper(ArmControl.GRIPPER_EXT_MAX_MM);
				Thread.Sleep(100);
				if (ArmControl.ReadGripFSR() >= ArmControl.MAX_GRIP_VALUE)
					DynamicWorkAssist.TextOutput("Attempt to drop " + name + " failed.");
				if (gdown)
					ArmControl.Move(hpts.center.x, hpts.maxh + HAND_PLACE_OFFSET_MM, hpts.center.z, ArmControl.gripper_orient.DOWN);
				else
					ArmControl.Move(hpts.center.x,hpts.maxh + HAND_PLACE_OFFSET_MM +ArmControl.GRIPPER_90_OVERHANG,hpts.center.z, ArmControl.gripper_orient.FLAT,90);
				}
			return (rtn);
		}



		public static void HandCheck()

		{
			int no_checks = 0;

			while (HandDetect.HandsInWorkspace())
				{
				if (no_checks % 5 == 0)
					PvSpeech.Speak("There is a hand within the workspace.  I can not move until the hand is removed.");
				Thread.Sleep(1000);
				no_checks += 1;
				}
		}



		public static void HandCheck2()

		{
			int no_checks = 0;
			bool spoke = false;

			while (HandDetect.HandsInWorkspace())
				{
				if (no_checks % 10 == 0)
					{
					PvSpeech.Speak("Please remove your hand from the work space.");
					spoke = true;
					}
				Thread.Sleep(500);
				no_checks += 1;
				}
			if (spoke)
				PvSpeech.SpeakAsync("Thank you.");
		}



		public static bool HomeArmOA(bool handchk = true)

		{
			bool rtn = false;
			ObstacleAvoid.ArmCompoundMove acm = new ObstacleAvoid.ArmCompoundMove();
			ArrayList cm = new ArrayList();

			if (!Shared.homed)
				{
				acm.mt = ObstacleAvoid.compound_move_type.GHOME_BASE_TURN;
				cm.Add(acm);
				if (ObstacleAvoid.CompoundMove(cm))
					{
					if (handchk)
						{
						DomainShared.HandCheck2();
						if (D415Camera.CaptureImages() && Mapping.MapWorkSpace(0) && Mapping.RemoveShadows(Mapping.base_ws_map) && Mapping.SaveCurrentMapAsRef())
							{
							DynamicWorkAssist.DisplayVideoImage((Image)Shared.vimg.Clone());
							rtn = true;
							}
						}
					else
						rtn = true;
					}
				}
			else
				rtn = true;
			return (rtn);
		}



		public static void AvailContainers()

		{
			Bitmap bm;
			int i,j,k;
			Containers.Container contn = new Containers.Container();
			Containers.ContainerModel cm;
			ArrayList al,ml,cl;
			Stopwatch sw = new Stopwatch();
			SortedList org_avail_containers = new SortedList();

			sw.Start();
			org_avail_containers = (SortedList) avail_containers.Clone();
			avail_containers.Clear();
			if (D415Camera.CaptureImages())
				{
				bm = (Bitmap)Shared.vimg.Clone();
				ml = Containers.ConatinerODModelList();
				if (ml.Count > 0)
					Shared.SaveVideoPic(bm);
				for (i = 0;i < ml.Count;i++)
					{
					cm = (Containers.ContainerModel) ml[i];
					al = Shared.Detect(cm.name, bm, cm.min_score,0);
					for (j = 0; j < al.Count; j++)
						{
						cl = Containers.ContainerList((int) cm.model_id, ((VisualObjectDetection.visual_detected_object)al[j]).object_id);
						for (k = 0;k < cl.Count;k++)
							{
							contn = (Containers.Container) cl[k];
							if (!avail_containers.ContainsKey(contn.name))
								{
								if (Containers.ContainerUseable(contn.name, contn.is_model, bm, (VisualObjectDetection.visual_detected_object)al[j], contn.side_height, ref contn))
									{
									contn.rect.X = ((VisualObjectDetection.visual_detected_object)al[j]).x;
									contn.rect.Y = ((VisualObjectDetection.visual_detected_object)al[j]).y;
									contn.rect.Width = ((VisualObjectDetection.visual_detected_object)al[j]).width;
									contn.rect.Height = ((VisualObjectDetection.visual_detected_object)al[j]).height;
									avail_containers.Add(contn.name, contn);
									if ((org_avail_containers.Count > 0) && !org_avail_containers.ContainsKey(contn.name))
										DynamicWorkAssist.TextOutput(contn.name + " is now available.");
									}
								}
							}
						}
					}
				}
			sw.Stop();
			Log.LogEntry("AvailContainers scan took " + sw.ElapsedMilliseconds + " ms.");
		}


		}
	}
