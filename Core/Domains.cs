using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace RobotArm
	{
	static public class Domains
		{

		public static ArrayList ListDomains()

		{
			ArrayList al = new ArrayList();
			string dname;
			string[] dirs;
			int i;

			dname = Shared.base_path + Shared.DOMAIN_DIR;
			dirs = Directory.GetDirectories(dname);
			for (i = 0;i < dirs.Length;i++)
				al.Add(dirs[i]);
			return (al);
		}



		public static DomainInterface OpenDomain(string dir_name,string type_name,AutoOpInterface aoi)

		{
			DomainInterface di = null;
			string fname = "";

			Log.LogEntry("OpenDomain " + dir_name + " " + type_name);

			try
			{
			dir_name = dir_name.Substring(0,dir_name.Length - 1);
			fname = dir_name.Substring(dir_name.LastIndexOf('\\') + 1);
			fname += ".dll";
			Assembly DLL = Assembly.LoadFrom(dir_name + "\\" + fname);
			Type ctype = DLL.GetType(type_name);
			dynamic c = Activator.CreateInstance(ctype);
			di = (DomainInterface) c.Open();
			if (!di.Open(aoi))
				{
				Log.LogEntry("Domain open failed.");
				di.Close();
				di = null;
				}
			}

			catch (Exception ex)
			{
			Log.LogEntry("Domain open exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			di = null;
			}

			return (di);
		}

		}
	}
