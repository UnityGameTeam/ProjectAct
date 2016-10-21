using System;
using System.IO;
using System.Net;
using System.Threading;
using UGCore.Components;
using UnityEngine;

namespace UGCore.Utility
{
    public static class HttpDownloadUtility
    {
        public static void DownloadFileAsync(string url, string localPath, Action<float> progess, Action doneAction, Action errorAction)
        {
            ThreadPool.QueueUserWorkItem((param) =>
            {
                DownloadFileBreakPoint(url, localPath, progess, doneAction, errorAction);
            });
        }

        private static void DownloadFileBreakPoint(string address, string fileName, Action<float> progess, Action doneAction, Action errorAction)
        {
            HttpWebRequest httpRequest = null;
            HttpWebResponse httpResponse = null;
            try
            {
                var resquestUrl = new Uri(address);
                httpRequest = (HttpWebRequest) WebRequest.Create(resquestUrl);
                httpResponse = (HttpWebResponse) httpRequest.GetResponse();

                var contentLength = httpResponse.ContentLength;
                httpResponse.Close();
                httpResponse = null;

                httpRequest.Abort();
                httpRequest = null;

                //剩余文件长度
                var leftSize = contentLength;
                //开始读写的位置
                long position = 0;
                if (File.Exists(fileName))
                {
                    using (
                        var sw = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                            FileShare.ReadWrite))
                    {
                        leftSize = contentLength - sw.Length;
                        position = sw.Length;
                    }
                }

                httpRequest = (HttpWebRequest) WebRequest.Create(resquestUrl);
                if (leftSize > 0)
                {
                    httpRequest.AddRange((int) position, (int) (position + leftSize));
                    httpResponse = (HttpWebResponse) httpRequest.GetResponse();
                    //从response中读取字节流
                    if (ReadBytesFromResponse(httpResponse, position, contentLength, fileName, progess) > 0)
                    {
                        errorAction();
                        return;
                    }
                    httpResponse.Close();
                    httpResponse = null;
                }
                httpRequest.Abort();
                httpRequest = null;

                doneAction();
            }
            catch (Exception e)
            {
                Debug.LogError("Download Http File Error:" + e);
                errorAction();
            }
            finally
            {
                if (httpResponse != null)
                {
                    httpResponse.Close();
                }

                if (httpRequest != null)
                {
                    httpRequest.Abort();
                }
            }
        }

        private static int ReadBytesFromResponse(WebResponse response, long allFilePointer, long totalSize, string fileName, Action<float> progessAction)
        {
            FileStream fs = null;
            Stream respStream = Stream.Null;

            try
            {
                var buffer = new byte[4096];
                fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                respStream = response.GetResponseStream();
                int receivedBytesCount;
                while ((receivedBytesCount = respStream.Read(buffer, 0, buffer.Length)) > 0 && GameRuntimeInfo.IsRunning)
                {
                    fs.Position = allFilePointer;
                    fs.Write(buffer, 0, receivedBytesCount);

                    allFilePointer += receivedBytesCount; //整个文件的位置指针 
                    var progress = allFilePointer/(float) totalSize;
                    progessAction(progress*100);
                }

                fs.Flush();
                fs.Close();
                respStream.Dispose();
                return 0;
            }
            catch (Exception e)
            {
                Debug.LogError("Read Bytes From Response Error:" + e);

                if (respStream != null)
                {
                    respStream.Dispose();
                }

                if (fs != null)
                {
                    fs.Flush();
                    fs.Close();
                }
            }
            return 1;
        }
    }
}