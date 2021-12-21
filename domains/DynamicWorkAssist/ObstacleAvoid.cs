using System;
using System.Collections;
using RobotArm;

namespace DynamicWorkAssist
	{
	class ObstacleAvoid
		{

		public enum compound_move_type { BASE_TURN_MOVE_TO_DEST, GHOME,GHOME_BASE_TURN };

		public struct ArmCompoundMove
		{
			public compound_move_type mt;
			public Shared.space_3d_mm dloc;
			public double turn;
			public ArmControl.gripper_orient go;
		};

		public static Containers.place_reply oa_move_status = Containers.place_reply.NONE;


		public static bool CompoundMove(ArrayList moves)

		{
			bool rtn = false;
			int i;
			ArmCompoundMove acm;

			if (moves.Count > 0)
				{
				for (i = 0; i < moves.Count; i++)
					{
					acm = (ArmCompoundMove) moves[i];
					if (acm.mt == compound_move_type.BASE_TURN_MOVE_TO_DEST)
						{
						Log.LogEntry("Base turn and move to destination.");
						if (ArmControl.MoveJoint(ArmControl.WAIST,acm.turn))
							{
							if (!(rtn = ArmControl.Move(acm.dloc.x, acm.dloc.y, acm.dloc.z,acm.go)))
								{
								DynamicWorkAssist.TextOutput("Move after base turn failed.");
								break;
								}
							}
						else
							{
							DynamicWorkAssist.TextOutput("Base turn failed.");
							break;
							}
						}
					else if (acm.mt == compound_move_type.GHOME)
						{
						Log.LogEntry("Ghome");
						if (!(rtn = ArmControl.GHome()))
							{
							DynamicWorkAssist.TextOutput("GHome failed.");
							break;
							}
						}
					else if (acm.mt == compound_move_type.GHOME_BASE_TURN)
						{
						Log.LogEntry("Ghome and base turn");
						if (ArmControl.GHome())
							{
							if (rtn = ArmControl.MoveJoint(ArmControl.WAIST,0))
								{
								Shared.homed = true;
								Shared.current_rc_mm = ArmControl.HomeLoc();
								Shared.current_ac.wja = 0;
								}
							else
								{
								DynamicWorkAssist.TextOutput("Base turn failed.");
								break;
								}
							}
						else
							{
							DynamicWorkAssist.TextOutput("GHome failed.");
							break;
							}
						}
					}
				rtn = i == moves.Count;
				}
			return (rtn);
		}



		public static bool Move(int x,int y,int z,ArmControl.gripper_orient go,double wangle = 0)

		{		//could improve performance by only going as far as necessary to clear obstacle
			bool rtn = false;
			Shared.space_3d_mm dloc = new RobotArm.Shared.space_3d_mm(x,y,z),btloc = new Shared.space_3d_mm(),hloc = new Shared.space_3d_mm(),ap = new Shared.space_3d_mm();
			ArrayList moves = new ArrayList();
			ArmCompoundMove acm = new ArmCompoundMove();
			double a1, a2, turn,dist;
			Shared.space_3d_mm sloc = new Shared.space_3d_mm(),eloc = new Shared.space_3d_mm();

			Log.LogEntry("OA move to " + dloc);
			oa_move_status = Containers.place_reply.OK;
			D415Camera.CaptureImages();
			Mapping.MapWorkSpace(0);
			Mapping.SaveWorkspaceMap("Move work space map");
			if (Mapping.WithinWorkSpace(Shared.current_rc_mm))
				{
				Mapping.RemoveArmFromMap(Shared.current_rc_mm,Shared.go);
				Mapping.SaveWorkspaceMap("Arm removal map");
				}
			sloc = Shared.current_rc_mm;
			eloc = dloc;
			if (go == ArmControl.gripper_orient.FLAT)
				{
				sloc.z += 10;
				eloc.z += 10;
				}
			else
				{
				sloc.z += 30;
				eloc.z += 30;
				}
			if (Mapping.ObstacleClear(sloc,eloc))
				{
				rtn = ArmControl.Move(dloc.x, dloc.y, dloc.z, go,wangle);
				if (!rtn)
					oa_move_status = Containers.place_reply.FAIL;
				}
			else
				{
				a1 = Math.Atan((double)Shared.current_rc_mm.x / (Shared.current_rc_mm.z + ArmControl.Z_OFFSET_MM));
				a2 = Math.Atan((double)acm.dloc.x / (acm.dloc.z + ArmControl.Z_OFFSET_MM));
				turn = a2 - a1;
				hloc = ArmControl.HomeLoc();
				ap = new Shared.space_3d_mm(0,0,-ArmControl.Z_OFFSET_MM);
				dist = Math.Sqrt(Math.Pow(ap.x - hloc.x,2) + Math.Pow(ap.z -hloc.z,2));
				btloc.x = ap.x + (int) Math.Round(dist * Math.Sin(turn));
				btloc.z = ap.z + (int) Math.Round(dist * Math.Cos(turn));
				btloc.y = y;
				sloc = btloc;
				if (go == ArmControl.gripper_orient.FLAT)
					sloc.z += 10;
				else
					sloc.z += 30;
				if (Mapping.ObstacleClear(sloc,eloc))
					{
					if (!Shared.homed)
						{
						acm.mt = compound_move_type.GHOME;
						moves.Add(acm);
						}
					acm.mt = compound_move_type.BASE_TURN_MOVE_TO_DEST;
					acm.turn = turn;
					acm.dloc = dloc;
					acm.go = go;
					moves.Add(acm);
					rtn = CompoundMove(moves);
					if (!rtn)
						oa_move_status = Containers.place_reply.FAIL;
					}
				else
					{
					oa_move_status = Containers.place_reply.OBSTACLE;
					DynamicWorkAssist.TextOutput("No clear path available.");
					}
				}
			return(rtn);
		}


		}
	}
