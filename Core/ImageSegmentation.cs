using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;


namespace RobotArm
	{

	public static class ImageSegmentation

	{
		private const string SERVER_IP = "127.0.0.1";
		private const int SERVER_PORT = 60010;
		private const string CLIENT_IP = "127.0.0.1";
		private const string TEMP_IMAGE_FILE_NAME = "temporary.jpg";
		private const int WAIT_TIME = 5000;

		private static bool initialized = false;
		private static DeviceComm dc = new DeviceComm();
		private static Process process = null;


		public static bool Open()

		{
			string rsp;
			ProcessStartInfo psi = new ProcessStartInfo();
			Stopwatch sw = new Stopwatch();

			try
			{
			psi.FileName = Shared.base_path + Shared.IS_SERVER_DIR + "RunServer.bat";
			psi.WindowStyle = ProcessWindowStyle.Minimized;
			psi.WorkingDirectory = Shared.base_path + Shared.IS_SERVER_DIR;
			process = Process.Start(psi);
			if (process != null)
				{
				if (dc.Open(CLIENT_IP,SERVER_PORT - 1, SERVER_IP,SERVER_PORT))
					{
					sw.Start();
					do
						{
						Thread.Sleep(2000);
						rsp = dc.SendCommand("hello", 100);
						if (rsp.StartsWith("OK"))
							{
							initialized = true;
							Log.LogEntry("Inference server connection established in " + sw.ElapsedMilliseconds + " ms");
							}
						else
							{
							if (rsp.Contains("connection lost"))
								if (!dc.Open(CLIENT_IP, SERVER_PORT - 1, SERVER_IP, SERVER_PORT))
									{
									Log.LogEntry("Connection lost.");
									break;
									}
							}
						}
					while(!initialized && (sw.ElapsedMilliseconds < WAIT_TIME));
					sw.Stop();
					}
				else
					Log.LogEntry("Could not open connection.");
				}
			}

			catch (Exception ex)
			{
			Log.LogEntry("Open exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return(initialized);
		}



		public static bool Load(string def)

		{
			string rsp;
			bool rtn = false;

			rsp = dc.SendCommand("load," + def,1000);
			if (rsp.StartsWith("OK"))
				rtn = true;
			return(rtn);
		}



		public static bool Unload()

		{
			string rsp;
			bool rtn = false;

			rsp = dc.SendCommand("unload",1000);
			if (rsp.StartsWith("OK"))
				rtn = true;
			return(rtn);
		}



		public static void Close()

		{
			if (initialized)
				{
				Unload();
				dc.SendCommand("exit", 100);
				dc.Close();
				initialized = false;
				}
		}



		public static bool Initialized()

		{
			return(initialized);
		}



		public static string Infer(string fname,string model)

		{
			string rtn = "",cmd,rsp;
			string[] values;

			cmd = model + "," + fname;
			rsp = dc.SendCommand(cmd,50);
			if (rsp.StartsWith("OK"))
				{
				values = rsp.Split(',');
				if (values.Length == 2)
					rtn = values[1];
				}
			return (rtn);
		}

	}



	}
