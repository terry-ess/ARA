using System;
using System.Net;
using System.Reflection;
using System.Threading;
using Logging;
using Speech;

namespace PvServer
	{
	class Program
		{

		public const string OK = "ok"; 
		public const string FAIL = "fail";
		public const string HELLO = "HELLO";
		public const string EXIT = "EXIT";
		public const string START = "START";
		public const string KEYWORD = "KEYWORD";
		public const string NLP_ONLY = "NLP_ONLY";
		public const string INFER_TIME_OUT = "INFER_TIME_OUT";
		public const string CONTEXT = "CONTEXT";

		public const int CNTL_PORT_NO = 20000;
		public const int NLP_FEED_PORT_NO = 25000;

		private static Connection cntl = null,feed = null;
		private static IPEndPoint nlp_rcvr = null;
		private static bool run = false;



		public static void NlpFeed(string msg)

		{
			if (feed != null)
				feed.SendResponse(msg,nlp_rcvr);
		}



		private static void CntlServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg,rsp = "";
			string[] words;
			int value = 0;

			Log.LogEntry("Control server started.");
			Console.WriteLine("Picovoice server started.");
			while (run)
				{
				rsp = FAIL;
				msg = cntl.ReceiveCmd(ref ep);
				if (msg.Length > 0)
					{
					if (msg == HELLO)
						{
						nlp_rcvr = new IPEndPoint(ep.Address, NLP_FEED_PORT_NO + 1);
						feed = new Connection(NLP_FEED_PORT_NO);
						if (feed.Connected())
							rsp = OK;
						}
					else if (msg == EXIT)
						{
						PvSpeech.UnRegisterHandler();
						PvSpeech.StopSpeechRecognition();
						rsp = OK;
						run = false;
						}
					else if (msg.StartsWith(START))
						{
						words = msg.Split(',');
						if (words.Length == 3)
							{
							if (PvSpeech.StartSpeechRecognition(double.Parse(words[2]),words[1]) && PvSpeech.RegisterHandler(NlpFeed))
									rsp = OK;
							}
						}
					else if (msg.StartsWith(CONTEXT))
						{
						words = msg.Split(',');
						if (words.Length == 3)
							{
							if (PvSpeech.SetContext(words[1],double.Parse(words[2])))
								rsp = OK;
							}
						}
					else if (msg == KEYWORD)
						{
						if (PvSpeech.StopNlpOnly())
							rsp = OK;
						}
					else if (msg.StartsWith(NLP_ONLY))
						{
						words = msg.Split(',');
						if ((words.Length == 2) && (PvSpeech.StartNlpOnly(words[1])))
							rsp = OK;
						}
					else if (msg.StartsWith(INFER_TIME_OUT))
						{
						words = msg.Split(',');
						if ((words.Length == 2) && (int.TryParse(words[1],out value )))
							{
							PvSpeech.InferTimeOut = value;
							rsp = OK;
							}
						}
					else
						rsp += ",unknown command";
					if (rsp.Length > 0)
						cntl.SendResponse(rsp, ep,true);
					}
				else
					Thread.Sleep(10);
				}
			Log.LogEntry("Control server stopped.");
			Console.WriteLine("Picovoice server closed.");
		}



		static void Main(string[] args)

		{
			try
			{
			Console.Title = "Picovoice Server";
			Log.OpenLog("Picovoice Server.log",true);
			cntl = new Connection(CNTL_PORT_NO);
			if (cntl.Connected())
				{
				run = true;
				CntlServer();
				Log.LogEntry("PVServer closed.");
				Log.CloseLog();
				}
			else
				Log.LogEntry("Could not open control connection.");
			}

			catch(Exception ex)
			{
				Log.LogEntry("Exception: " + ex.Message);
				Console.WriteLine("Exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				Console.WriteLine("Stack trace: " + ex.StackTrace);
				Console.Write("Press any key to exit.");
				Console.ReadKey(true);
			}

			}

		}
	}
