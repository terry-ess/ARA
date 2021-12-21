using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Reflection;


namespace Logging
	{

	public static class Log
		{

		public const string DATA_SUB_DIR = "\\data\\";

		private static TextWriter ltw = null;
		private static int no_writers = 0;
		private static Stopwatch sw = new Stopwatch();
		private static string log_dir = "";
		private static bool ts;
		private static object lock_obj = new object();


		public static void OpenLog(string file,bool timestamps)

		{
			string dt_stg;
			DateTime now = DateTime.Now;
			string path;

			if (ltw == null)
				{
				ts = timestamps;
				dt_stg = now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second;
				path = Assembly.GetExecutingAssembly().Location;
				log_dir = path.Substring(0,path.LastIndexOf("\\")) + DATA_SUB_DIR + dt_stg + "\\";
				if (!Directory.Exists(log_dir))
					Directory.CreateDirectory(log_dir);
				ltw = File.CreateText(log_dir + file);
				if (ltw != null)
					{
					ltw.WriteLine(file);
					ltw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() );
					ltw.WriteLine();
					ltw.Flush();
					sw.Restart();
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
				sw.Stop();
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
					ltw.Write(sw.ElapsedMilliseconds.ToString() + " ");
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



		public static void KeyLogEntry(string entry)

		{
			lock(lock_obj)
			{
			if (ltw != null)
				{
				ltw.WriteLine();
				if (ts)
					ltw.Write(sw.ElapsedMilliseconds.ToString() + " ");
				ltw.WriteLine(entry.ToUpper());
				ltw.WriteLine();
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



		public static  string LogDir()

		{
			return(log_dir);
		}

		}
	}
