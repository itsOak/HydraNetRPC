using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hydra.Net.RPC
{
	public static class HydraLog
	{
		private static int s_ReserveLogDays = 3;

		private static DateTime s_LastLogTime = DateTime.Now;

		private static string s_LogFileName = "";

		private static bool s_bInitSuccess = false;

		private static LogLevelType _logFileLevel = LogLevelType.INFO;

		private static object _lockObj = new object();

		public static void InitLogFile(string fileNameWithoutExtentsion, LogLevelType level = LogLevelType.INFO)
		{
			string directoryName = Path.GetDirectoryName(fileNameWithoutExtentsion);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			s_LogFileName = fileNameWithoutExtentsion;
			CreateLogFile();
			Bakup(DateTime.Now);
			_logFileLevel = level;
			s_bInitSuccess = true;
		}

		private static void CreateLogFile()
		{
			string fileName = string.Format("{0}_{1}-{2}.log", s_LogFileName, Process.GetCurrentProcess().Id, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
			try
			{
				IEnumerable<TextWriterTraceListener> enumerable = Trace.Listeners.OfType<TextWriterTraceListener>();
				foreach (TextWriterTraceListener textWriterTraceListener in enumerable)
				{
					textWriterTraceListener.Close();
					textWriterTraceListener.Dispose();
				}
				Trace.Listeners.Clear();
			}
			catch (Exception)
			{
			}
			Trace.AutoFlush = true;
			TextWriterTraceListener listener = new TextWriterTraceListener(fileName);
			Trace.Listeners.Add(listener);
		}

		private static void Bakup(DateTime baseTime)
		{
			if (!string.IsNullOrWhiteSpace(HydraLog.s_LogFileName))
			{
				string directoryName = Path.GetDirectoryName(HydraLog.s_LogFileName);
				string[] files = Directory.GetFiles(directoryName, "*.log");
				foreach (string text in files)
				{
					int length = text.Length;
					if (length > 23)
					{
						string arg = text.Substring(length - 23, 10);
						string text2 = text.Substring(length - 12, 8);
						text2 = text2.Replace('-', ':');
						DateTime dateTime;
						if (DateTime.TryParse(string.Format("{0} {1}", arg, text2), out dateTime))
						{
							bool flag4 = (baseTime.Date - dateTime.Date).Days > HydraLog.s_ReserveLogDays;
							if (flag4)
							{
								try
								{
									File.Delete(text);
								}
								catch (Exception)
								{
								}
							}
						}
					}
				}
			}
		}

		public static void SetLogLevel(LogLevelType level)
		{
			_logFileLevel = level;
		}

		public static void WriteLine(string msg, LogLevelType level = LogLevelType.INFO, int skipFrames = 1)
		{
			if (s_bInitSuccess && level >= _logFileLevel)
			{
				DateTime now = DateTime.Now;
				if (now.Date != s_LastLogTime.Date)
				{
					lock (_lockObj)
					{
						if (now.Date != s_LastLogTime.Date)
						{
							CreateLogFile();
							Bakup(now);
							s_LastLogTime = now;
						}
					}
				}
				s_LastLogTime = now;
				string fileName = Path.GetFileName(new StackFrame(skipFrames, true).GetFileName());
				int fileLineNumber = new StackFrame(skipFrames, true).GetFileLineNumber();
				MethodBase method = new StackFrame(skipFrames).GetMethod();
				msg = string.Format("{0} [{1}] [{2}:{3}] @{4}.{5}(): {6}", new object[]
				{
					now,
					level,
					fileName,
					fileLineNumber,
					method.ReflectedType,
					method.Name,
					msg
				});
				Trace.WriteLine(msg);
			}
		}

		public static void WriteLine(string msg, string reason)
		{
			WriteLine(string.Format("{0}, 原因： {1} ", msg, reason), LogLevelType.WARN, 2);
		}

		public static void WriteLine(string msg, Exception e)
		{
			WriteLine(string.Format("{0}, 原因： {1} ", msg, e.Message), LogLevelType.ERR, 2);
		}

		public static void Error(string format, params object[] args)
		{
			WriteLine(string.Format(format, args), LogLevelType.ERR, 2);
		}

		public static void Warn(string format, params object[] args)
		{
			WriteLine(string.Format(format, args), LogLevelType.WARN, 2);
		}

		public static void Info(string format, params object[] args)
		{
			WriteLine(string.Format(format, args), LogLevelType.INFO, 2);
		}

		public static void Test(string format, params object[] args)
		{
			WriteLine(string.Format(format, args), LogLevelType.TEST, 2);
		}

		public static void Throw(string format, params object[] args)
		{
			string text = string.Format(format, args);
			WriteLine(text, LogLevelType.ERR, 3);
			throw new InvalidOperationException(text);
		}
	}
}
