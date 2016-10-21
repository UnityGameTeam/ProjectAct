using System;
using UnityEngine;
using System.IO;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using UGCore.Components;

namespace UGCore.Utility
{
    public static class ZipUtility
    {
        public static void UnzipDirectoryAsync(string filePath, string unZipDirecotyPath, Action<float> progressAction, Action doneAction, Action errorAction)
        {
            ThreadPool.QueueUserWorkItem((param) =>
            {
                UnzipFileToDirectory(filePath, unZipDirecotyPath, progressAction, doneAction, errorAction);
            });     
        }

        private static void UnzipFileToDirectory(string filePath, string unZipDirecotyPath, Action<float> progressAction, Action doneAction, Action errorAction)
        {
            ZipInputStream zipStream = null;
            FileStream stream = null;
            ZipFile zipFile = null;
            try
            {
                zipFile = new ZipFile(filePath);
                var entryCountDelta = 1f / zipFile.Count;
                zipFile.Close();
                zipFile = null;

                var currentEntryCount = 0;
                var currentProgress = 0f;

                zipStream = new ZipInputStream(File.OpenRead(filePath));
                ZipEntry zipEntry = null;
              
                if ((zipEntry = zipStream.GetNextEntry()) != null)
                {
                    var buffer = new byte[4096];
                    while (zipEntry != null && GameRuntimeInfo.IsRunning)
                    {
                        string fileName = Path.GetFileName(zipEntry.Name);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            if (zipEntry.CompressedSize != 0)
                            {
                                var path = Path.Combine(unZipDirecotyPath, zipEntry.Name);
                                var dirPath = Path.GetDirectoryName(path);
                                if (!Directory.Exists(dirPath))
                                {
                                    Directory.CreateDirectory(dirPath);
                                }

                                var fileSizeBase = 1f / zipEntry.Size;
                                var processSize = 0;
                                stream = File.Create(path);
                                while (GameRuntimeInfo.IsRunning)
                                {
                                    int size = zipStream.Read(buffer, 0, buffer.Length);
                                    if (size > 0)
                                    {
                                        stream.Write(buffer, 0, size);
                                        processSize += size;
                                        progressAction((currentProgress + Mathf.Max(fileSizeBase * processSize * entryCountDelta,0))*100);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                stream.Flush();
                                stream.Close();
                                stream = null;
                            }
                        }

                        ++currentEntryCount;
                        currentProgress = currentEntryCount * entryCountDelta;
                        //progressAction(currentProgress * 100);
                        zipEntry = zipStream.GetNextEntry();
                    }
                }
                doneAction();
            }
            catch (Exception e)
            {
                Debug.LogError("unzip file to directory error:" + e);
                errorAction();
            }
            finally
            {
                if (stream != null)
                {
                    stream.Flush();
                    stream.Close();
                }

                if (zipStream != null)
                {
                    zipStream.Flush();
                    zipStream.Close();
                }

                if (zipFile != null)
                {
                    zipFile.Close();
                }
            }          
        }
    }
}