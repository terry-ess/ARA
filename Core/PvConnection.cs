using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using RobotArm;


namespace Speech
	{
	class PvConnection
		{

		public static int MAX_DG_SIZE = 1400;


		private IPEndPoint ip_end;
		private Socket sock;
		private bool connected = false;



		public bool Send(string msg,IPEndPoint to)

		{
			byte[] cmd;
			ASCIIEncoding encode = new ASCIIEncoding();
			bool rtn = false;
		
			if (connected == true)
				{
				try
					{
					cmd = encode.GetBytes(msg);
					if (cmd.Length <= MAX_DG_SIZE)
						if (sock.SendTo(cmd,0,cmd.Length,SocketFlags.None,to) == cmd.Length)
							rtn = true;
					}
					
				catch
					{
					}
				}
			return(rtn);
		}		



		private int Receive(int length,byte[] buff,ref IPEndPoint from)
		
		{
			int rtn = 0;
			IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			EndPoint senderep = sender;
			
			if (connected)
				{
				try
					{
					if (sock.Available > 0)
						rtn = sock.ReceiveFrom(buff,length,SocketFlags.None,ref senderep);
					}
						
				catch (SocketException)
					{
					rtn = 0;
					connected = false;
					}
						
				catch
					{
					rtn = 0;
					}
				}
			if (rtn > 0)
				{
				from = (IPEndPoint) senderep;
				}
			return(rtn);
		}



		public string Receive(int timeout_count,ref IPEndPoint from, bool log = false)

		{
			byte[] rsp = new byte[MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			int len = 0;
			int count = 0;
			string rtn = "";

			do
				{
				len = Receive(rsp.Length,rsp,ref from);
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
			if (log)
				Log.LogEntry(rtn);
			return(rtn);
		}



		public bool Connected()

		{
			return(connected);
		}



		public void Close()

		{
			if (connected)
				{
				sock.Close();
				connected = false;
				}
		}



		public PvConnection(string ip_address, int port_no, int input_buffer_size = 8192)

		{
			IPAddress ip_addr;

			try
				{
				sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				ip_addr = IPAddress.Parse(ip_address);
				ip_end = new IPEndPoint(ip_addr, port_no);
				sock.Bind(ip_end);
				if (sock.ReceiveBufferSize < input_buffer_size)
					sock.ReceiveBufferSize = input_buffer_size;
				connected = true;
				}

			catch (Exception ex)
				{
				sock.Close();
				connected = false;
				Log.LogEntry("Sp2Connection exception: " + ex.Message);
				Log.LogEntry("          stack trace: " + ex.StackTrace);
				}
		}

		}
	}
