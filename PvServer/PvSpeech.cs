using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Logging;
using OpenTK.Audio.OpenAL;
using Pv;


namespace Speech
	{

	public static class PvSpeech   //speech recognition/NLP performed by picovoice, speech synthesis by Microsoft
		{

		private const string NOT_UNDERSTOOD = "unknown";
		public const string CONVERSATION = "CONVERSATION";
		public const string CONTINUAL = "CONTINUAL";

		private enum loop_type {KEYWORD,NLP_ONLY};
		private enum free_mode {CONTINUAL,CONVERSATION};

		public delegate void SpeechHandler(string message);
		private static SpeechHandler sh = null;
		private static ALCaptureDevice audo_in;
		private static Rhino rhino = null;
		private static Porcupine porcupine = null;
		private static Thread speechthread;
		private static bool run = false;
		private static int infer_time_out = 7;
		private static loop_type lt = loop_type.KEYWORD;
		private static free_mode mode = free_mode.CONVERSATION;
		private static string access_key = "";


		static PvSpeech()

		{
		}


		public static int InferTimeOut

		{
			get
				{
				return(infer_time_out);
				}

			set
				{
				infer_time_out = value;
				}
		}



		private static void KeywordLoop()

		{
			short[] buffer = new short[rhino.FrameLength];
			int samples,result;
			Inference infer;
			string msg;
			bool final;
			Stopwatch sw = new Stopwatch();

			Log.LogEntry("Keyword loop");
			while (run)
				{
				samples = ALC.GetAvailableSamples(audo_in);
				if (samples > porcupine.FrameLength)
					{
					ALC.CaptureSamples(audo_in,ref buffer[0],porcupine.FrameLength);
					result = porcupine.Process(buffer);
					if (result >= 0)
						{
						Log.LogEntry("\tkeyword detected");
						sw.Restart();
						while(run)
							{
							samples = ALC.GetAvailableSamples(audo_in);
							if (samples > rhino.FrameLength)
								{
								ALC.CaptureSamples(audo_in, ref buffer[0],rhino.FrameLength);
								final = rhino.Process(buffer);
								if (final)
									{
									infer = rhino.GetInference();
									if (infer.IsUnderstood)
										{
										msg = infer.Intent;
										foreach(KeyValuePair<string,string> slot in infer.Slots)
											msg += "," +slot.Key + "," + slot.Value;
										if (sh != null)
											sh(msg);
										Log.LogEntry("\t" + msg);
										}
									else
										{
										sh(NOT_UNDERSTOOD);
										Log.LogEntry("did not understand command");
										}
									break;
									}
								else
									{
									if ((lt != loop_type.KEYWORD) || !run)
										{
										Log.LogEntry("Keyword loop break (rhino)");
										rhino.Reset();
										break;
										}
									else if (sw.Elapsed.TotalSeconds > infer_time_out)
										{
										Log.LogEntry("\ttimed out");
										rhino.Reset();
										if (sh != null)
											sh(NOT_UNDERSTOOD);
										break;
										}
									}
								}
							}
						sw.Stop();
						}
					else if((lt != loop_type.KEYWORD) || !run)
						{
						Log.LogEntry("Keyword loop break (porcupine)");
						break;
						}
					}
				}
		}



		private static void OpenMicLoop()

		{
			short[] buffer = new short[rhino.FrameLength];
			int samples;
			Inference infer;
			string msg = "";
			bool final;

			Log.LogEntry("Open mic loop");
			while(run)
				{
				samples = ALC.GetAvailableSamples(audo_in);
				if (samples > rhino.FrameLength)
					{
					ALC.CaptureSamples(audo_in, ref buffer[0], rhino.FrameLength);
					final = rhino.Process(buffer);
					if (final)
						{
						infer = rhino.GetInference();
						if (infer.IsUnderstood)
							{
							msg = infer.Intent;
							foreach (KeyValuePair<string, string> slot in infer.Slots)
								msg += "," + slot.Key + "," + slot.Value;
							if (sh != null)
								sh(msg);
							Log.LogEntry("\t" + msg);
							}
						else
							{
							sh(NOT_UNDERSTOOD);
							Log.LogEntry(NOT_UNDERSTOOD);
							}
						if (mode == free_mode.CONVERSATION)
							{
							lt = loop_type.KEYWORD;
							break;
							}
						}
					else
						{
						if ((lt != loop_type.NLP_ONLY) || !run)
							{
							Log.LogEntry("Open mic loop break");
							rhino.Reset();
							break;
							}
						}
					}
				}
		}



		private static void NLPLoop()

		{
			Log.LogEntry("NLP interface running");
			while(run)
				{
				if (lt == loop_type.KEYWORD)
					KeywordLoop();
				else
					OpenMicLoop();
				}
			Log.LogEntry("NLP interface closed.");
		}



		public static bool StartSpeechRecognition(double keyword_sensitivity,string accesskey)

		{
			bool rtn = false;
			List <Pv.BuiltInKeyword> list = new List<Pv.BuiltInKeyword>();
			List <float> flist = new List<float>();

			access_key = accesskey;
			list.Add(Pv.BuiltInKeyword.COMPUTER);
			flist.Add((float) keyword_sensitivity);
			porcupine = Porcupine.FromBuiltInKeywords(access_key, (IEnumerable<BuiltInKeyword>)list,null,flist);
			if (porcupine != null)
				{
				audo_in = ALC.CaptureOpenDevice("", 16000, ALFormat.Mono16, porcupine.FrameLength * 4);
				rtn = true;
				}
			return (rtn);
		}



		public static bool SetContext(string cfile,double nlp_sensitivity)

		{
			bool rtn = false;

			if ((speechthread != null) && (speechthread.IsAlive))
				{
				run = false;
				speechthread.Join();
				ALC.CaptureStop(audo_in);
				}
			if (rhino != null)
				{
				rhino.Dispose();
				}
			rhino = Rhino.Create(access_key,cfile, null, (float) nlp_sensitivity);
			if (rhino != null)
				{
				ALC.CaptureStart(audo_in);
				speechthread = new Thread(NLPLoop);
				run = true;
				speechthread.Start();
				rtn = true;
				}
			return (rtn);
		}



		public static void StopSpeechRecognition()

		{
			if ((speechthread != null) && (speechthread.IsAlive))
				{
				run = false;
				speechthread.Join();
				ALC.CaptureStop(audo_in);
				ALC.CaptureCloseDevice(audo_in);
				}
			if (porcupine != null)
				porcupine.Dispose();
			if (rhino != null)
				rhino.Dispose();
		}



		public static bool StartNlpOnly(string mode_stg)

		{
			if (mode_stg == CONTINUAL)
				mode = free_mode.CONTINUAL;
			else if (mode_stg == CONVERSATION)
				mode  = free_mode.CONVERSATION;
			lt = loop_type.NLP_ONLY;
			return(true);
		}



		public static bool StopNlpOnly()

		{
			lt = loop_type.KEYWORD;
			return (true);
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
