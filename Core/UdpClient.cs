using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;


namespace RobotArm
	{

	public class UdpClient
	{
	
		
		public static int MAX_DG_SIZE = 1400;

		
		private IPEndPoint ip_end;
		private Socket sock = null;
		private bool connected = false;
		private IPEndPoint server_ip_end;
		private IPEndPoint sserver_ip_end;
		private string last_error;
		private byte[] tc = new byte[UdpClient.MAX_DG_SIZE];
		


		public bool Send(int length,byte[] buff,IPEndPoint to)

		{
			bool rtn = false;
		
			if ((connected == true) && (length <= MAX_DG_SIZE))
				{
				try
					{
					if (sock.SendTo(buff,0,length,SocketFlags.None,to) == length)
						rtn = true;
					else
						{
						last_error = "UPD send failed.";
						Log.LogEntry("UDP send failure to " + to.ToString());
						}
					}
					
				catch (Exception ex)
					{
					last_error = "UDP send failed.";
					Log.LogEntry("UDP send exception: " + ex.Message);
					}
				}
			else if (connected)
				{
				last_error = "Send message too long";
				Log.LogEntry(last_error);
				}
			else
				{
				last_error = "Not connected.";
				Log.LogEntry(last_error);
				}
			return(rtn);
		}		


		
		public int Receive(int length,byte[] buff,ref IPEndPoint from)
		
		{
			int rtn = 0;
			IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			EndPoint senderep = (EndPoint)sender;
			
			if (connected)
				{
				try
					{
					if (sock.Available > 0)
						rtn = sock.ReceiveFrom(buff,length,SocketFlags.None,ref senderep);
					}
						
				catch (SocketException sex)
					{
					rtn = 0;
					connected = false;
					last_error = "UDP receive socket exception, connection closed.";
					Log.LogEntry("Socket Exception: " + sex.Message);
					Close();
					}
						
				catch
					{
					rtn = 0;
					last_error = "UDP receive exception.";
					}
				}
			if (rtn > 0)
				{
				from = (IPEndPoint) senderep;
				}
			return(rtn);
		}



		public void ClearReceive()

		{
			IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			EndPoint senderep = (EndPoint)sender;

			
			if (connected)
				{
				try
				{
				while (sock.Available > 0)
					sock.ReceiveFrom(tc,tc.Length,SocketFlags.None,ref senderep);
				}
						
						
				catch
				{
				}

				}
		}



		public bool Connected()

		{
			return(connected);
		}



		public void Close()

		{
			if (sock != null)
				{
				sock.Close();
				sock = null;
				connected = false;
				}
		}



		public IPEndPoint Server()
		
		{
			return(server_ip_end);
		}



		public IPEndPoint SServer()
		
		{
			return(sserver_ip_end);
		}



		public UdpClient(string ip_address,int port_no,string server_ip_address,int server_port_no)

		{
			IPAddress ip_addr;
			
			try
				{
				sock = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
				ip_addr = IPAddress.Parse(ip_address);
				ip_end = new IPEndPoint(ip_addr ,port_no);
				sock.Bind(ip_end);
				ip_addr = IPAddress.Parse(server_ip_address);
				server_ip_end = new IPEndPoint(ip_addr, server_port_no);
				sserver_ip_end = new IPEndPoint(ip_addr, server_port_no + 1);
				connected = true;
				}
				
			catch(Exception ex)
				{
				sock.Close();
				connected = false;
//				MessageBox.Show("Exception: " + ex.Message,"Error");
				Log.LogEntry("UdpClient exception: " + ex.Message);
				Log.LogEntry("          stack trace: " + ex.StackTrace);
				last_error = "UDP open exception " + ex.Message;
				}
		}



		public UdpClient(string ip_address,int port_no)

		{
			IPAddress ip_addr;
			
			try
				{
				sock = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
				ip_addr = IPAddress.Parse(ip_address);
				ip_end = new IPEndPoint(ip_addr ,port_no);
				sock.Bind(ip_end);
				connected = true;
				}
				
			catch(Exception ex)
				{
				sock.Close();
				connected = false;
//				MessageBox.Show("Exception: " + ex.Message,"Error");
				Log.LogEntry("UdpClient exception: " + ex.Message);
				Log.LogEntry("          stack trace: " + ex.StackTrace);
				last_error = "UDP open exception " + ex.Message;
				}
		}



		public string LastError()

		{
			return(last_error);
		}

	}
}
