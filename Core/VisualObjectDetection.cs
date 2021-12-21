using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;


namespace RobotArm
	{
	public static class VisualObjectDetection
		{

		private const string SERVER_IP = "127.0.0.1";
		private const int SERVER_PORT = 60000;
		private const string CLIENT_IP = "127.0.0.1";
		private const double DEFAULT_SCORE_LIMIT = .6;
		private const int WAIT_TIME = 80000;	//the initial load of python is really slow

		public struct visual_detected_object
			{
			public int object_id;
			public int prob;
			public int x;
			public int y;
			public int width;
			public int height;
			};

		private static bool initialized = false;
		private static DeviceComm dc = new DeviceComm();
		private static Mutex od_mutex = new Mutex();
		private static int temp_fn = 0;
		private static Process process = null;
		private static string temp_image_file_name;
		private static string server_dir;
		private static System.Drawing.Imaging.ImageFormat image_format;

		public static bool Open()

		{
			string rsp;
			ProcessStartInfo psi = new ProcessStartInfo();
			Stopwatch sw = new Stopwatch();

			if (Shared.TENSORFLOW)
				{
				temp_image_file_name = "temp.jpg";
				server_dir = Shared.OD_SERVER_DIR;
				image_format = System.Drawing.Imaging.ImageFormat.Jpeg;
				}
			else
				{
#pragma warning disable CS0162 // Unreachable code detected
				temp_image_file_name = "temp.png";
#pragma warning restore CS0162 // Unreachable code detected
				server_dir = Shared.OD_SERVER_DIR;
				image_format = System.Drawing.Imaging.ImageFormat.Png;
				}

			try
			{
			psi.FileName = Shared.base_path + server_dir + "RunServer.bat";
			psi.WindowStyle = ProcessWindowStyle.Minimized;
			psi.WorkingDirectory = Shared.base_path + server_dir;
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
					while (!initialized && (sw.ElapsedMilliseconds < WAIT_TIME));
					sw.Stop();
					}
				else
					Log.LogEntry("Could not open connection.");
				}
			else
				Log.LogEntry("Could not start process");
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



		public static ArrayList DetectObject(Bitmap bm,string model,int obj_id,double score_limit = DEFAULT_SCORE_LIMIT, bool log = true)

		{
			ArrayList al = new ArrayList();
			string rsp;
			string[] boxes, values;
			visual_detected_object vdo = new visual_detected_object();
			int i;

			rsp = Detect(bm,score_limit,model,obj_id,log);
			if (rsp.StartsWith("OK"))
				{
				boxes = rsp.Split('[');
				if (boxes.Length > 1)
					for (i = 1;i < boxes.Length;i++)
						{
						values = boxes[i].Split(',');
						if (values.Length == 5)
							{
							values[4] = values[4].Substring(0, values[4].Length - 1);
							vdo.object_id = obj_id;
							vdo.prob = int.Parse(values[0]);
							vdo.x = int.Parse(values[1]);
							vdo.y = int.Parse(values[2]);
							vdo.width = int.Parse(values[3]);
							vdo.height = int.Parse(values[4]);
							al.Add(vdo);
							}
						else if (values.Length == 6)
							{
							values[5] = values[5].Substring(0, values[5].Length - 1);
							vdo.prob = int.Parse(values[0]);
							vdo.object_id = int.Parse(values[1]);
							vdo.x = int.Parse(values[2]);
							vdo.y = int.Parse(values[3]);
							vdo.width = int.Parse(values[4]);
							vdo.height = int.Parse(values[5]);
							al.Add(vdo);
							}
						}
				}
			else if (log)
				{
				Log.LogEntry("Visual object detection failed.");
				Shared.SaveVideoPic(bm);
				}
			return (al);
		}



		public static VisualObjectDetection.visual_detected_object UnCropVDO(VisualObjectDetection.visual_detected_object vdo,int crop_size,int shift)

		{
			VisualObjectDetection.visual_detected_object rvdo = new VisualObjectDetection.visual_detected_object();

			rvdo.x= vdo.x + shift + (int) Math.Round(((double) Shared.vimg.Width - crop_size)/2);
			rvdo.y = vdo.y + (Shared.vimg.Height - crop_size);
			rvdo.width = vdo.width;
			rvdo.height = vdo.height;
			rvdo.object_id = vdo.object_id;
			rvdo.prob = vdo.prob;
			return(rvdo);
		}



		public static Bitmap CropPicture(Bitmap bm,int crop_size, int shift)

		{
			Rectangle rect = new Rectangle();

			rect.Height = rect.Width = crop_size;
			rect.X = ((int)(Math.Round(((double)bm.Width - rect.Width) / 2)) + shift);
			rect.Y = bm.Height - rect.Height;
			return(bm.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb));
		}



		private static string Detect(Bitmap bm,double score_limit,string model,int obj_id,bool log = true)

		{
			string rsp,cmd,fname;
			Stopwatch sw = new Stopwatch();

			if (initialized)
				{
				od_mutex.WaitOne();
				fname = Application.StartupPath + "\\" + model + temp_fn + temp_image_file_name;
				temp_fn += 1;
				bm.Save(fname,image_format);
				if (File.Exists(fname))
					{
					cmd = model + "," + fname + "," + score_limit + "," + obj_id;
					sw.Start();
					rsp = dc.SendCommand(cmd,100,false);
					sw.Stop();
					od_mutex.ReleaseMutex();
					Log.LogEntry("OD base infer time (ms):" + sw.ElapsedMilliseconds);
					if (log)
						{
						Log.LogEntry(cmd);
						Log.LogEntry(rsp);
						}
					File.Delete(fname);
					}
				else
					{
					od_mutex.ReleaseMutex();
					rsp = "FAIL could not save image";
					}
				}
			else
				rsp = "FAIL connection not open";
			return(rsp);
		}


		}
	}
