using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using RobotArm;

namespace DynamicWorkAssist
	{
	class PnP
		{

		private const int NO_OPS = 3;

		public enum OpReturn {FAIL,STOPPED,OK,DONE};

		public struct Context
		{
			public int current_op;
			public int current_obj_indx;
			public ArrayList objs;
			public string object_in_gripper;
			public Containers.Container con;
			public int skipped_parts;
			public int completed_parts;
			public int parts_to_move;
		};


		private bool scan = false;
		private bool last_scan = false;
		private bool long_term = false;
		private Thread HandScan = null;
		private short[] depthdata = new short[D415Camera.WIDTH * D415Camera.HEIGHT];
		private byte[] videodata = new byte[D415Camera.WIDTH * D415Camera.HEIGHT * D415Camera.BYTES_PER_PIXEL];


		private OpReturn ExecuteNextOp(ref Context cntxt,bool confirm)

		{
			OpReturn rtn = OpReturn.FAIL;
			int nxt_op;
			Containers.place_reply pr;
			bool part_confirmed,con_confirmed;

			if (!DomainShared.stop_action)
				{
				nxt_op = (cntxt.current_op + 1) % NO_OPS;
				switch(nxt_op)
					{
					case 0:
						if (last_scan)
							DomainShared.HandCheck();
						if (confirm)
							{
							if (long_term == true)
								{
								part_confirmed = Parts.ConfirmPart((DomainShared.PickData) cntxt.objs[cntxt.current_obj_indx]);
								}
							else
								part_confirmed = true;
							con_confirmed = ConfirmConatainer(cntxt.con);
							}
						else
							{
							part_confirmed = true;
							con_confirmed = true;
							}
						if (part_confirmed && con_confirmed)
							{
							if (DomainShared.PickSurfacePartOA((DomainShared.PickData) cntxt.objs[cntxt.current_obj_indx], cntxt.con.side_height))
								{
								cntxt.object_in_gripper = ((DomainShared.PickData) cntxt.objs[cntxt.current_obj_indx]).part.name;
								rtn = OpReturn.OK;
								cntxt.current_obj_indx += 1;
								cntxt.current_op = 0;
								}
							else
								{
								DynamicWorkAssist.TextOutput("Part " + (cntxt.current_obj_indx + 1) + " could not be picked.  Skipping the part.");
								cntxt.skipped_parts += 1;
								if (cntxt.current_obj_indx == cntxt.parts_to_move)
									rtn = OpReturn.DONE;
								else
									{
									rtn = OpReturn.OK;
									cntxt.current_obj_indx += 1;
									cntxt.current_op = 2;
									}
								}
							}
						else if (!part_confirmed && con_confirmed)
							{
							DynamicWorkAssist.TextOutput("Part " + (cntxt.current_obj_indx + 1) + " could not be confirmed.  Skipping the part.");
							cntxt.skipped_parts += 1;
							if (cntxt.current_obj_indx == cntxt.parts_to_move)
								rtn = OpReturn.DONE;
							else
								{
								rtn = OpReturn.OK;
								cntxt.current_obj_indx += 1;
								cntxt.current_op = 2;
								}
							}
						break;

					case 1:
						if (last_scan)
							DomainShared.HandCheck();
						pr = Containers.PlaceInContainerOA(cntxt.con);
						if (pr == Containers.place_reply.OBSTACLE)
							{
							DynamicWorkAssist.TextOutput("attempting work around.");
							if (DomainShared.HomeArmOA())
								pr = Containers.PlaceInContainerOA(cntxt.con);
							}
						if (pr == Containers.place_reply.OK)
							{
							cntxt.object_in_gripper = "";
							cntxt.completed_parts += 1;
							cntxt.current_op = 1;
							rtn = OpReturn.OK;
							}
						else
							DynamicWorkAssist.TextOutput("Placement failed.");
						break;

					case 2:
						if (DomainShared.HomeArmOA())
							{
							if (cntxt.current_obj_indx == cntxt.parts_to_move)
								rtn = OpReturn.DONE;
							else
								rtn = OpReturn.OK;
							cntxt.current_op = 2;
							}
						else
							DynamicWorkAssist.TextOutput("Homing failed.");
						break;
					}
				}
			else
				rtn = OpReturn.STOPPED;
			return (rtn);
		}



		private bool ConfirmConatainer(Containers.Container con)

		{
			bool rtn = false;

			if (D415Camera.CaptureImages())
				{
				if (Containers.ConfirmContainer(con))
					{
					if (!Containers.ContainerTopOpen(con))
						DynamicWorkAssist.TextOutput("The container's top is not open.");
					else
						rtn = true;
					}
				else
					{
					DynamicWorkAssist.TextOutput("The container " + con.name + " could not be confirmed.  Please wait while I recheck available containers.");
					DomainShared.AvailContainers();
					if (Containers.ConfirmContainer(con))
						{
						if (!Containers.ContainerTopOpen(con))
							DynamicWorkAssist.TextOutput("The container's top is not open.");
						else
							rtn = true;
						}
					else
						DynamicWorkAssist.TextOutput("The container is not available.");
					}
				}
			else
				DynamicWorkAssist.TextOutput("Image capture failed.");
			return(rtn);
		}



		public OpReturn DoPnP(ref Context cntxt)

		{
			OpReturn rtn;
			bool confirm = false;

			try
			{
			long_term = false;
			last_scan = false;
			scan = true;
			DomainShared.stop_action = false;
			HandScan = new Thread(HandScanThread);
			HandScan.Start();
			do
				{
				rtn = ExecuteNextOp(ref cntxt,confirm);
				confirm = true;
				}
			while (rtn == OpReturn.OK);
			}

			catch(Exception ex)
			{
			DynamicWorkAssist.TextOutput("Exception occurred in pick and place.");
			Log.LogEntry("DoPnp exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			rtn = OpReturn.FAIL;
			}

			if ((HandScan != null) && (HandScan.IsAlive))
				{
				scan = false;
				HandScan.Join();
				HandScan = null;
				}
			return(rtn);
		}



		private void HandScanThread()

		{
			const int WAIT_TIME = 200;
			Stopwatch sw = new Stopwatch();
			int wait_time;

			while (scan == true)
				{
				sw.Restart();
				if (HandDetect.ScanHandsInWorkspace(depthdata,videodata))
					{
					last_scan = true;
					long_term = true;
					}
				else
					last_scan = false;
				sw.Stop();
				wait_time = WAIT_TIME - (int) sw.ElapsedMilliseconds;
				if ((wait_time > 0) && (scan == true))
					Thread.Sleep(wait_time);
				}
			HandScan = null;
		}


		}
	}
