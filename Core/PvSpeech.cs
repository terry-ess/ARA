#define MS_SPEECH

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Globalization;
using System.Net;
using RobotArm;
#if MS_SPEECH
using System.Speech.Synthesis;
#endif


namespace Speech
	{	//speech recognition/NLP performed in a picovoice "server", speech synthesis uses Microsoft Speech or coqui-ai TTS server

	public static class PvSpeech
		{

		private const int CNTL_PORT_NO = 20000;
		private const int NLP_FEED_PORT_NO = 25000;
#if !MS_SPEECH
		private const int TTS_PORT_NO = 30000;
#endif
		private const string IP_ADDRESS = "127.0.0.1";
		private const double KEYWORD_SENSITIVITY = .85;
		private const double NLP_SENSITIVITY = .5;
		public const string OK = "ok";
		public const string FAIL = "fail";
		public const string HELLO = "HELLO";
		public const string EXIT = "EXIT";
		public const string START = "START";
		public const string KEYWORD = "KEYWORD";
		public const string NLP_ONLY = "NLP_ONLY";
		public const string INFER_TIME_OUT = "INFER_TIME_OUT";
		public const string CONVERSATION = "CONVERSATION";
		public const string CONTINUAL = "CONTINUAL";
		public const string CONTEXT = "CONTEXT";
		public const string NOT_UNDERSTOOD = "unknown";
		private const string ak_file = "accesskey.txt";


		public delegate void SpeechHandler(string message);

#if MS_SPEECH
		private static SpeechSynthesizer ss = null;
#else
		private static PvConnection tts = null;
		private static IPEndPoint tts_server = new IPEndPoint(IPAddress.Parse(IP_ADDRESS), TTS_PORT_NO);
#endif

		public static SpeechHandler sh = null;
		private static AutoResetEvent reply_ready = new AutoResetEvent(false);
			private static Process process = null;
		private static PvConnection nlp_feed = null;
		private static bool feed_run = false;
		private static Thread nlp_rcv = null;
		private static PvConnection nlp_ctl = null;
		private static IPEndPoint nlp_server = new IPEndPoint(IPAddress.Parse(IP_ADDRESS),CNTL_PORT_NO);
		private static bool conversation = false;
		private static string reply = "";
		private static string access_key = "";



		static PvSpeech()

		{
			string fname;
			TextReader tr;

			fname = Application.StartupPath + Shared.CAL_SUB_DIR + ak_file;
			tr = File.OpenText(fname);
			if (tr != null)
				{
				access_key = tr.ReadLine();
				tr.Close();
				}
		}



		public static bool MsTTS()

		{
#if MS_SPEECH
			return(true);
#else
			return(false);
#endif
		}


		public static bool StartTTS()

		{
			bool rtn = false;

#if MS_SPEECH
			ss = new SpeechSynthesizer();
			if (ss != null)
				rtn = true;
#else
			string msg;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			ProcessStartInfo psi = new ProcessStartInfo();

			try
			{
			psi.FileName = Shared.base_path + Shared.TTS_SERVER_DIR + "RunServer.bat";
			psi.WindowStyle = ProcessWindowStyle.Minimized;
			psi.WorkingDirectory = Shared.base_path + Shared.TTS_SERVER_DIR;
			process = Process.Start(psi);
			if (process != null)
				{
				Thread.Sleep(20000);
				tts = new PvConnection(IP_ADDRESS, TTS_PORT_NO + 1);
				if (tts.Connected())
					{
					tts.Send(HELLO, tts_server);
					msg = tts.Receive(100, ref ep);
					if (msg == OK)
						rtn = true;
					}
				}
			}

			catch (Exception e)
			{
			Log.LogEntry("TTS start exception: " + e.Message);
			Log.LogEntry("Stack trace: " + e.StackTrace);
			}
#endif
			return (rtn);
		}


		private static void NlpReceiver()

		{
			string msg;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

			Log.LogEntry("NLPReceiver running");
			while (feed_run)
				{

				try
				{
				msg = nlp_feed.Receive(100,ref ep);
				if ((msg.Length > 0) && !msg.StartsWith("fail"))
					{
					Log.LogEntry(msg);
					if (conversation)
						{
						reply = msg;
						reply_ready.Set();
						}
					else if (sh != null)
						sh(msg);
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("NlpReceiver exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				feed_run = false;
				}

				}
		}




		public static bool StartSpeechRecognition()

		{
			bool rtn = false;
			string msg;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			ProcessStartInfo psi = new ProcessStartInfo();

			try
			{
			psi.FileName = Shared.base_path + Shared.PV_SERVER_DIR + "PvServer.exe";
			psi.WindowStyle = ProcessWindowStyle.Minimized;
			process = Process.Start(psi);
			if (process != null)
				{
				Thread.Sleep(5000);
				nlp_ctl = new PvConnection(IP_ADDRESS,CNTL_PORT_NO + 1);
				if (nlp_ctl.Connected())
					{
					nlp_ctl.Send(HELLO, nlp_server);
					msg = nlp_ctl.Receive(100, ref ep);
					if (msg == OK)
						{
						nlp_ctl.Send(START + "," + access_key + "," + KEYWORD_SENSITIVITY,nlp_server );
						msg = nlp_ctl.Receive(300, ref ep);
						if (msg == OK)
							{
							nlp_feed = new PvConnection(IP_ADDRESS,NLP_FEED_PORT_NO + 1);
							if (nlp_feed.Connected())
								{
								feed_run = true;
								nlp_rcv = new Thread(NlpReceiver);
								nlp_rcv.Start();
								rtn = true;
								}
							else
								Log.LogEntry("Could not start NLP feed.");
							}
						else
							Log.LogEntry("Could not connect with receiver.");
						}
					else
						Log.LogEntry("Could not open control loop");
					}
				}
			else
				Log.LogEntry("Could not start NLP process.");
			}

			catch(Exception e)
			{
			Log.LogEntry("PvSpeech recognition start exception: " + e.Message);
			Log.LogEntry("Stack trace: " + e.StackTrace);
			}
			
			return(rtn);
		}



		public static bool SetContext(string context)

		{
			bool rtn = false;
			string msg;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

			nlp_ctl.Send(CONTEXT + "," + context + "," + NLP_SENSITIVITY, nlp_server);
			msg = nlp_ctl.Receive(100, ref ep);
			if (msg == OK)
					rtn = true;
			return (rtn);
		}



		public static void StopSpeechRecognition()

		{
			if (feed_run && (nlp_rcv != null) && nlp_rcv.IsAlive)
				{
				feed_run = false;
				nlp_rcv.Join();
				}
			if ((process != null) && (nlp_ctl != null))
				{
				nlp_ctl.Send(EXIT,nlp_server);
				process = null;
				}
			if (nlp_ctl != null)
				{
				nlp_ctl.Close();
				nlp_ctl = null;
				}
			if (nlp_feed != null)
				{
				nlp_feed.Close();
				nlp_feed = null;
				}
		}



		public static bool StartNlpOnly()

		{
			bool rtn = false;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg;

			nlp_ctl.Send(NLP_ONLY + "," + CONTINUAL,nlp_server);
			msg = nlp_ctl.Receive(100, ref ep);
			if (msg == OK)
				rtn = true;
			return (rtn);
		}



		public static bool StopNlpOnly()

		{
			bool rtn = false;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg;

			nlp_ctl.Send(KEYWORD, nlp_server);
			msg = nlp_ctl.Receive(100, ref ep);
			if (msg == OK)
				rtn = true;
			return (rtn);
		}



		public static bool InferTimeOut(int val)

		{
			bool rtn = false;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg;

			nlp_ctl.Send(INFER_TIME_OUT + "," + val,nlp_server);
			msg = nlp_ctl.Receive(100, ref ep);
			if (msg == OK)
				rtn = true;
			return (rtn);
		}



		public static string Conversation(string msg,int wait_time)

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

			reply = "";
			conversation = true;
			Speak(msg);
			nlp_ctl.Send(NLP_ONLY + "," + CONVERSATION,nlp_server);
			nlp_ctl.Receive(10,ref ep);
			reply_ready.WaitOne(wait_time);
			conversation = false;
			return (reply);
		}



		public static void Speak(string message)

		{
#if MS_SPEECH
			if (ss != null)
				{
				ss.Speak(message);
				}
#else
			string msg;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

			tts.Send(message, tts_server);
			msg= tts.Receive(100, ref ep);
#endif
		}



		private static void SpeakAsyncThread(object message)

		{
			Speak((string) message);
		}



		public static void SpeakAsync(string message)

		{
			Thread sat;
			
			sat = new Thread(SpeakAsyncThread);
			sat.Start(message);
		}



		public static bool RegisterHandler(SpeechHandler sh)

		{
			PvSpeech.sh = sh;
			return(true);
		}



		public static void UnRegisterHandler()

		{
			PvSpeech.sh = null;
		}


		}
	}
