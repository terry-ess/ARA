using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using RobotArm;


namespace DynamicWorkAssist
	{
	class ContainersDAO
		{

		private const string CONTAINERS_DB = "containers." + Shared.DB_FILE_EXT;

		private static SQLiteConnection connect = null;


		public bool Open(string dir)

		{
			bool rtn = false;

			if (connect == null)
				{
				Log.LogEntry("ContainersDAO start");
				if (!File.Exists(dir + CONTAINERS_DB))
					{
					if (!Directory.Exists(dir))
						Directory.CreateDirectory(dir);
					SQLiteConnection.CreateFile(dir + CONTAINERS_DB);
					connect = new SQLiteConnection("DATA SOURCE=" + dir + CONTAINERS_DB);
					connect.Open();
					CreateTable();
					}
				else
					{
					connect = new SQLiteConnection("DATA SOURCE=" + dir + CONTAINERS_DB);
					connect.Open();
					}
				}
			if (connect != null)
				rtn = true;
			return(rtn);
		}



		public bool ConnectionOpen()

		{
			bool rtn = false;

			if (connect != null)
				rtn = true;
			return(rtn);
		}




		private void CreateTable()

		{
			SQLiteCommand cmd;
			
			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "CREATE TABLE ODmodels (name TEXT,min_score REAL)";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "CREATE TABLE Containers (name TEXT,od_model TEXT,od_id INTEGER,is_model_id INTEGER,width INTEGER,length INTEGER,side_height INTEGER,min_od_score REAL,top INTEGER)";
			cmd.ExecuteNonQuery();
			}
			
			catch(Exception ex)
			
			{
			Log.LogEntry("CreateTable exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

		}



		public DataTable ODModelData(int id)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT name,min_score FROM ODmodels WHERE ROWID=" + id;
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "name";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "min_score";
			col.DataType = System.Type.GetType("System.Double");
			dt.Columns.Add(col);
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



		public DataTable ContainerData(int model_id,int od_id)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,name,is_model,width,length,side_height,top FROM Containers WHERE od_model_id=" + model_id + " AND od_id=" + od_id;
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
			col.ColumnName = "side_height";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "top";
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



		public DataTable ODModelsList()

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,name,min_score FROM ODmodels";
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



		public DataTable ContainerList()

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,name,od_model_id,od_id,is_model,width,length,side_height,top FROM Containers";
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
			col.ColumnName = "od_model_id";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "od_id";
			col.DataType = System.Type.GetType("System.Int32");
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
			col.ColumnName = "side_height";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "top";
			col.DataType = System.Type.GetType("System.Int32");
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
