using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using Logging;


namespace PvServer
	{
	public class Connection
		{

		public const string NETWORK_ADDRESS = "127.0.0.1";

		private UdpClient servr = null;
		private bool connected = false;


		public Connection(int port_no)

		{
			servr = new UdpClient(NETWORK_ADDRESS,port_no);
			connected = servr.Connected();
		}



		public bool Connected()

		{
			return(connected);
		}



		public string ReceiveResponse(int timeout_count)

		{
			byte[] rsp = new byte[UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			int len = 0;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			int count = 0;
			string rtn = "";

			do
				{
				len = servr.Receive(rsp.Length,rsp,ref ep);
				if (len > 0)
					rtn = encode.GetString(rsp,0,len);
				else
					{
					count += 1;
					if (count < timeout_count)
						Thread.Sleep(10);
					}
				}
			while ((len == 0) && (count < timeout_count));
			if (count == timeout_count)
				rtn = "fail UDP receive timed out";
			return(rtn);
		}



		public bool SendResponse(string rsp,IPEndPoint rcvr,bool log = false)

		{
			byte[] buf = new byte[UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			bool rtn = false;

			if ((servr != null) && (servr.Connected()))
				{
				buf = encode.GetBytes(rsp);
				rtn = servr.Send(buf.Length, buf,rcvr);
				if (log)
					{
					if (rtn)
						Log.LogEntry(rsp);
					else
						Log.LogEntry("Send failed for " + rsp);
					}
				}
			else
				Log.LogEntry("Send failed, UDP not open");
			return(rtn);
		}



		public string SendCommand(string command,int timeout_count,IPEndPoint rcvr)

		{
			string rtn = "";
			byte[] cmd;
			byte[] rsp = new byte [UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();

			if (servr != null)
				{
				servr.ClearReceive();
				if (timeout_count < 20)
					timeout_count = 20;
				cmd = encode.GetBytes(command);
				if (servr.Send(cmd.Length,cmd,rcvr))
					rtn = ReceiveResponse(timeout_count);
				else
					rtn = "fail UDP send failure";
				}
			else
				rtn = "fail UDP not open.";
			return(rtn);
		}



		public string ReceiveCmd(ref IPEndPoint ep)

		{
			string msg = "";
			byte[] buf = new byte[UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			int len;

			if ((len = servr.Receive(buf.Length, buf, ref ep)) > 0)
				{
				msg = encode.GetString(buf, 0, len);
				Log.LogEntry(msg);
				}
			return (msg);
		}



		public void Close()

		{
			if ((servr != null) && servr.Connected())
				{
				servr.Close();
				servr = null;
				connected = false;
				}
		}

		}
	}
