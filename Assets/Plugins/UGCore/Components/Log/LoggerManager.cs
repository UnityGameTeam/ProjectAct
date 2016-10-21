using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace UGCore.Components
{
    [Flags]
    public enum LogLevel
    {
        None = 0,
        Debug = 1,
        Info = 2,
        Warning = 4,
        Except = 8,
        Error = 16,
        Fatal = 32,
    }

    public struct LogData
    {
        public string Msg;
        public LogLevel Level;
        public bool WriteEditorLog;
    }

    public interface ILogger
    {
        void Log(string message, LogLevel logLevel, bool writeEditorLog);
        void Release();
    }

    public class LoggerManager
    {
        public static LogLevel CurrentLogLevels = LogLevel.Debug | LogLevel.Info | LogLevel.Warning | LogLevel.Except | LogLevel.Error | LogLevel.Fatal;

        private static Dictionary<string, int> m_LoggerMap = new Dictionary<string, int>();
        private static List<ILogger> m_LoggerList = new List<ILogger>();

        static LoggerManager()
        {
            Application.logMessageReceived += ProcessExceptionReport;
        }

        public static void Release()
        {
            for (int i = 0; i < m_LoggerList.Count; ++i)
            {
                m_LoggerList[i].Release();
            }
            m_LoggerList.Clear();
            m_LoggerMap.Clear();
        }

        public static bool AddLogger(string name, ILogger logger)
        {
            if (m_LoggerMap.ContainsKey(name))
                return false;

            m_LoggerList.Add(logger);
            m_LoggerMap.Add(name, m_LoggerList.Count - 1);
            return true;
        }

        public static bool Remove(string name, ILogger logger)
        {
            if (m_LoggerMap.ContainsKey(name))
            {
                var index = m_LoggerMap[name];
                m_LoggerMap.Remove(name);
                m_LoggerList.RemoveAt(index);
                return true;
            }
            return false;
        }

        private static void Log(string message, LogLevel level, bool writeEditorLog = true)
        {
            var msg = string.Concat(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff"), message);
            for (int i = 0; i < m_LoggerList.Count; ++i)
            {
                m_LoggerList[i].Log(msg, level, writeEditorLog);
            }
        }

        public static void Debug(string message, bool isShowStack = true)
        {
            if (LogLevel.Debug == (CurrentLogLevels & LogLevel.Debug))
            {
                Log(string.Concat(" [DEBUG]: ", message, "\n", isShowStack ? GetStackTraceInfo() : ""), LogLevel.Debug);
            }
        }

        public static void Info(string message, bool isShowStack = true)
        {
            if (LogLevel.Info == (CurrentLogLevels & LogLevel.Info))
            {
                Log(string.Concat(" [INFO]: ", message, "\n", isShowStack ? GetStackTraceInfo() : ""), LogLevel.Info);
            }
        }

        public static void Warning(string message, bool isShowStack = true)
        {
            if (LogLevel.Warning == (CurrentLogLevels & LogLevel.Warning))
            {
                Log(string.Concat(" [WARNING]: ", message, "\n", isShowStack ? GetStackTraceInfo() : ""), LogLevel.Warning);
            }
        }

        public static void Except(Exception ex, string message = null)
        {
            if (LogLevel.Except == (CurrentLogLevels & LogLevel.Except))
            {
                Exception innerException = ex;
                while (innerException.InnerException != null)
                {
                    innerException = innerException.InnerException;
                }
                Log(string.Concat(" [EXCEPT]: ", message == null ? "" : message + "\n", ex.Message, innerException.StackTrace), LogLevel.Except);
            }
        }

        public static void Error(string message, bool isShowStack = true)
        {
            if (LogLevel.Error == (CurrentLogLevels & LogLevel.Error))
            {
                Log(string.Concat(" [ERROR]: ", message, '\n', isShowStack ? GetStackTraceInfo() : ""), LogLevel.Error);
            }
        }

        public static void Fatal(string message, bool isShowStack = true)
        {
            if (LogLevel.Fatal == (CurrentLogLevels & LogLevel.Fatal))
            {
                Log(string.Concat(" [FATAL]: ", message, '\n', isShowStack ? GetStackTraceInfo() : ""), LogLevel.Fatal);
            }
        }

        private static string GetStackTraceInfo()
        {
#if UNITY_IPHONE
        return "";
#endif
            StringBuilder sb = new StringBuilder();
            StackTrace st = new StackTrace();
            var sf = st.GetFrames();
            for (int i = 2; i < sf.Length; i++)
            {
                sb.AppendLine(sf[i].ToString());
            }
            return sb.ToString();
        }

        private static void ProcessExceptionReport(string message, string stackTrace, LogType type)
        {
            var logLevel = LogLevel.Debug;
            switch (type)
            {
                case LogType.Assert:
                    logLevel = LogLevel.Debug;
                    break;
                case LogType.Error:
                    logLevel = LogLevel.Error;
                    break;
                case LogType.Exception:
                    logLevel = LogLevel.Except;
                    break;
                case LogType.Log:
                    logLevel = LogLevel.Debug;
                    break;
                case LogType.Warning:
                    logLevel = LogLevel.Warning;
                    break;
                default:
                    break;
            }
            if (logLevel == (CurrentLogLevels & logLevel))
                Log(string.Concat(" [UNITY_", logLevel, "]: ", message, '\n', stackTrace), logLevel, false);
        }
    }
}
