using System;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;


namespace RobotArm
	{

	public static class Log
		{

		private static TextWriter ltw = null;
		private static int no_writers = 0;
		private static string log_dir = "";
		private static bool ts;
		private static object lock_obj = new object();


		public static void OpenLog(string file,bool timestamps)

		{
			string dt_stg;
			DateTime now = DateTime.Now;

			if (ltw == null)
				{
				ts = timestamps;
				dt_stg = now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second;
				log_dir = Application.StartupPath + Shared.DATA_SUB_DIR + dt_stg + "\\";
				if (!Directory.Exists(log_dir))
					Directory.CreateDirectory(log_dir);
				ltw = File.CreateText(log_dir + file);
				if (ltw != null)
					{
					ltw.WriteLine(file);
					ltw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
					ltw.WriteLine();
					ltw.Flush();
					no_writers = 1;
					}
				}
			else
				no_writers += 1;
		}



		public static void CloseLog()

		{
			lock(lock_obj)
			{
			if ((ltw != null) && (no_writers == 1))
				{
				ltw.Close();
				ltw = null;
				no_writers = 0;
				}
			else if (ltw != null)
				no_writers -= 1;
			}
		}



		public static void LogEntry(string entry)

		{
			lock(lock_obj)
			{
			if (ltw != null)
				{
				if (ts)
					ltw.Write(Shared.app_time.ElapsedMilliseconds.ToString() + " ");
				ltw.WriteLine(entry);
				ltw.Flush();
				}
			}
		}



		public static void LogArrayList(string title,ArrayList al)

		{
			int i;

			lock(lock_obj)
			{
			if (ltw != null)
				{
				LogEntry(title);
				for (i = 0;i < al.Count;i++)
					{
					ltw.Write("\t" + i);
					ltw.WriteLine("\t" + al[i].ToString());
					}
				ltw.Flush();
				}
			}
		}



		public static void LogStack(string title,Stack stk)

		{
			int i = 0;

			lock(lock_obj)
			{
			if (ltw != null)
				{
				LogEntry(title);
				foreach (Object obj in stk)
					{
					i += 1;
					ltw.Write("\t" + i);
					ltw.WriteLine("\t" + obj);
					}
				ltw.Flush();

				}
			}
		}


		public static void KeyLogEntry(string entry)

		{
			lock(lock_obj)
			{
			if (ltw != null)
				{
				ltw.WriteLine();
				if (ts)
					ltw.Write(Shared.app_time.ElapsedMilliseconds.ToString() + " ");
				ltw.WriteLine(entry.ToUpper());
				ltw.WriteLine();
				ltw.Flush();
				}
			}
		}



		public static void BoldLogEntry(string entry)

		{
			lock(lock_obj)
			{
			if (ltw != null)
				{

				if (ts)
					ltw.Write(Shared.app_time.ElapsedMilliseconds.ToString() + " ");
				ltw.WriteLine(entry.ToUpper());
				ltw.Flush();
				}
			}
		}



		public static bool LogOpen()

		{
			bool rtn = false;

			if (ltw != null)
				rtn = true;
			return(rtn);
		}



		public static string LogDir()

		{
			return(log_dir);
		}

		}
	}
