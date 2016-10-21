using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace UGCore.Components
{
    public class FileLogger : ILogger
    {
        private static readonly object m_Locker = new object();

        private string m_LogFileName = "log_{0}.txt";
        private string m_LogFilePath;
        private FileStream m_FileStream;
        private StreamWriter m_StreamWriter;

        private bool m_InitSuccess;
        private Thread m_LogThread;
        private Queue<LogData> m_LogQueue = new Queue<LogData>();
        private LogData m_CurrentLogData;

        public bool DoWriteLog = true;
        public int LogQueueCount = 50;

        public FileLogger()
        {
            var logDir = UGCoreConfig.GetExternalLogFolder();
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            m_LogFilePath = Path.Combine(logDir, String.Format(m_LogFileName, DateTime.Today.ToString("yyyy_MM_dd")));
            try
            {

                m_FileStream = new FileStream(m_LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                m_StreamWriter = new StreamWriter(m_FileStream);

                if (m_LogThread == null)
                {
                    m_LogThread = new Thread(WriteLogHandler);
                    m_LogThread.Start();
                }
                m_InitSuccess = true;
            }
            catch (Exception ex)
            {
                m_InitSuccess = false;
                Debug.LogError("init file error:" + ex.Message);
            }
        }

        public void Log(string message, LogLevel logLevel, bool writeEditorLog)
        {
            lock (m_Locker)
            {
                if (m_LogQueue.Count < LogQueueCount)
                    m_LogQueue.Enqueue(new LogData {Level = logLevel, Msg = message, WriteEditorLog = writeEditorLog});
            }
        }

        public void Release()
        {
            lock (m_Locker)
            {
                if (m_StreamWriter != null)
                {
                    m_StreamWriter.Close();
                    m_StreamWriter.Dispose();
                }

                if (m_FileStream != null)
                {
                    m_FileStream.Close();
                    m_FileStream.Dispose();
                }

                DoWriteLog = false;

                if (m_LogThread != null)
                    m_LogThread.Abort();
                m_LogThread = null;
            }
        }

        private void WriteLogHandler()
        {
            while (DoWriteLog)
            {
                DoWrite();
                Thread.Sleep(10);
            }
        }

        private void DoWrite()
        {
            lock (m_Locker)
            {
                if (m_LogQueue.Count > 0)
                {
                    m_CurrentLogData = m_LogQueue.Dequeue();
                }
            }

            if (m_CurrentLogData.Level != LogLevel.None)
            {
                if (m_CurrentLogData.WriteEditorLog)
                {
                    switch (m_CurrentLogData.Level)
                    {
                        case LogLevel.Debug:
                        case LogLevel.Info:
                            Debug.Log(m_CurrentLogData.Msg);
                            break;
                        case LogLevel.Warning:
                            Debug.LogWarning(m_CurrentLogData.Msg);
                            break;
                        case LogLevel.Error:
                        case LogLevel.Except:
                        case LogLevel.Fatal:
                            Debug.LogError(m_CurrentLogData.Msg);
                            break;
                        default:
                            break;
                    }
                }

                if (m_StreamWriter != null && m_InitSuccess)
                {
                    try
                    {
                        m_StreamWriter.WriteLine(m_CurrentLogData.Msg);
                        m_StreamWriter.Flush();
                    }
                    catch (Exception ex)
                    {
                        m_InitSuccess = false;
                        Debug.LogError(ex.Message);
                    }
                }
                m_CurrentLogData.Level = LogLevel.None;
            }
        }
    }
}