using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using RobotArm;

namespace DynamicWorkAssist
	{
	class PartsDAO
		{
		private const string PARTS_DB = "parts." + Shared.DB_FILE_EXT;

		private static SQLiteConnection connect = null;


		public bool Open(string dir)

		{
			bool rtn = true;

			Log.LogEntry("PartsDAO open");

			try
			{
			if (!File.Exists(dir + PARTS_DB))
				{
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				SQLiteConnection.CreateFile(dir + PARTS_DB);
				connect = new SQLiteConnection("DATA SOURCE=" + dir + PARTS_DB);
				connect.Open();
				rtn = CreateTable();
				if (!rtn)
					{
					connect.Close();
					File.Delete(dir + PARTS_DB);
					}
				}
			else
				{
				connect = new SQLiteConnection("DATA SOURCE=" + dir + PARTS_DB);
				connect.Open();
				}
			}

			catch(Exception ex)
			{
			rtn = false;
			Log.LogEntry("PartsDAO open exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return(rtn);
		}



		private bool CreateTable()

		{
			SQLiteCommand cmd;
			bool rtn = true;
			
			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "CREATE TABLE SurfaceModels (name TEXT,min_od_score REAL)";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "CREATE TABLE Parts (name TEXT,surface_od_model TEXT,surface_od_id INTEGER,surface_min_od_score REAL,nhand_od_model TEXT,nhand_od_id INTEGER,nhand_surface_min_od_score REAL,is_model TEXT,width INTEGER,length INTEGER,height INTEGER,max_dim INTEGER,min_dim INTEGER)";
			cmd.ExecuteNonQuery();
			}
			
			catch(Exception ex)
			
			{
			Log.LogEntry("CreateTable exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			rtn = false;
			}

			return(rtn);
		}


		public DataTable PartsList()

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,name,surface_od_model,surface_od_id,surface_min_od_score,nhand_od_model,nhand_od_id,nhand_min_od_score,is_model,width,length,height,max_dim,min_dim FROM Parts";
			reader = cmd.ExecuteReader();
			dt = new DataTable();

			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			dt.Columns.Add(col);
			key[0] = col;

			col = new DataColumn();
			col.ColumnName = "name";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "surface_od_model";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "surface_od_id";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "surface_min_od_score";
			col.DataType = System.Type.GetType("System.Double");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "nhand_od_model";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "nhand_od_id";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "nhand_min_od_score";
			col.DataType = System.Type.GetType("System.Double");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "is_model";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "width";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "length";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "height";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "max_dim";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);

			col = new DataColumn();
			col.ColumnName = "min_dim";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);

			dt.PrimaryKey = key; 
			dt.Load(reader);
			reader.Close();
			}

			catch(Exception ex)
			{
			Log.LogEntry("ContainerData exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			if (reader != null)
				reader.Close();
			}

			return(dt);
		}



		public DataTable SurfaceModelsList()

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,name,min_score FROM SurfaceModels";
			reader = cmd.ExecuteReader();
			dt = new DataTable();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			dt.Columns.Add(col);
			key[0] = col;
			col = new DataColumn();
			col.ColumnName = "name";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "min_score";
			col.DataType = System.Type.GetType("System.Double");
			dt.Columns.Add(col);
			dt.PrimaryKey = key;
			dt.Load(reader);
			reader.Close();
			}

			catch(Exception ex)
			{
			Log.LogEntry("ContainerList exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			if (reader != null)
				reader.Close();
			}

			return(dt);
		}


		}
	}
