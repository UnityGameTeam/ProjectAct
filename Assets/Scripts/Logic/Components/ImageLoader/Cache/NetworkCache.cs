using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine.UI;

namespace GameLogic.Components
{
    public struct TextureDownloadTask
    {
        public string downloadUrl;
        public RawImage ownerUI;
        public string savePath;
        public Dictionary<RawImage, string> uiToUrlMap;
        public bool resetUI;
        public Action loadDoneAction;
    }

    public class NetworkCache : ITextureCache
    {
        private ITextureCache                m_TextureCache;
        private int                          m_CurrentIdleTask;
        private Queue<TextureDownloadTask>   m_WaitDownloadTasks = new Queue<TextureDownloadTask>();
        private HashSet<string>              m_DownloadingTask = new HashSet<string>();

        public NetworkCache(ITextureCache textureCache, int maxDownloadNum = 5)
        {
            m_TextureCache    = textureCache;
            m_CurrentIdleTask = maxDownloadNum;
        }

        public void SetMaxCacheCount(int maxCacheCount)
        {
            if (m_TextureCache != null)
            {
                m_TextureCache.SetMaxCacheCount(maxCacheCount);
            }
        }

        public bool GetTexture(string url, string savePath, RawImage ownerUI, Dictionary<RawImage, string> uiToUrlMap, bool resetUI, Action loadDoneAction)
        {
            if (m_TextureCache != null)
            {
                if (m_TextureCache.GetTexture(url, savePath, ownerUI, uiToUrlMap, resetUI, loadDoneAction))
                    return true;
            }

            var loadTask = new TextureDownloadTask();
            loadTask.downloadUrl = url;
            loadTask.ownerUI = ownerUI;
            loadTask.savePath = savePath;
            loadTask.uiToUrlMap = uiToUrlMap;
            loadTask.resetUI = resetUI;
            loadTask.loadDoneAction = loadDoneAction;

            m_WaitDownloadTasks.Enqueue(loadTask);
            if (m_CurrentIdleTask > 0)
            {
                LaunchNextLoadTask();
            }
            return true;
        }

        public bool CacheTexture(string key, Texture2D texture)
        {
            if (m_TextureCache != null)
            {
                if (m_TextureCache.CacheTexture(key, texture))
                    return true;
            }
            return false;
        }

        public void ClearCache()
        {
            if (m_TextureCache != null)
            {
                m_TextureCache.ClearCache();
            }
        }

        public void CacheTextureToDisk(string key, string savePath, byte[] data)
        {
            ActionShcheduler.Instance.RunAsync(() =>
            {
                if (!Directory.Exists(DiskCache.SaveDir))
                {
                    Directory.CreateDirectory(DiskCache.SaveDir);
                }

                var filePath = Path.Combine(DiskCache.SaveDir, savePath);
                if (!File.Exists(filePath))
                {
                    var parentDir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }
                    File.WriteAllBytes(filePath, data);
                }
            });
        }

        private void LaunchNextLoadTask()
        {
            if (m_CurrentIdleTask > 0 && m_WaitDownloadTasks.Count > 0)
            {
                var tryTimes = m_WaitDownloadTasks.Count;
                TextureDownloadTask task = new TextureDownloadTask();
                bool needLaunchTask = false;
                while (tryTimes > 0)
                {
                    --tryTimes;
                    task = m_WaitDownloadTasks.Dequeue();
                    if (m_DownloadingTask.Contains(task.downloadUrl))
                    {
                        m_WaitDownloadTasks.Enqueue(task);
                    }
                    else
                    {
                        needLaunchTask = true;
                        break;
                    }
                }

                if (!needLaunchTask)
                    return;

                if (m_TextureCache != null)
                {
                    if (m_TextureCache.GetTexture(task.downloadUrl, task.savePath, task.ownerUI, task.uiToUrlMap, task.resetUI, task.loadDoneAction))
                    {
                        LaunchNextLoadTask();
                        return;
                    }
                }

                --m_CurrentIdleTask;
                m_DownloadingTask.Add(task.downloadUrl);
                ActionShcheduler.Instance.RunAsync(() =>
                {
                    var webClient = new WebClient();
                    byte[] data;
                    try
                    {
                        data = webClient.DownloadData(task.downloadUrl);
                        var contentType = webClient.ResponseHeaders["Content-Type"];
                        if (string.IsNullOrEmpty(contentType) || !contentType.StartsWith("image"))
                        {
                            data = null;
                        } 
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("load image from htpp error:" + e);
                        data = null;
                    }

                    ActionShcheduler.Instance.QueueOnMainThread(() =>
                    {
                        try
                        {
                            if (data != null)
                            {
                                CacheTextureToDisk(task.downloadUrl, task.savePath, data);
                            }

                            if (data != null && task.ownerUI != null && task.uiToUrlMap.ContainsKey(task.ownerUI) && task.uiToUrlMap[task.ownerUI] == task.downloadUrl)
                            {
                                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                                texture.LoadImage(data);
                                texture.Apply(false);

                                task.ownerUI.texture = texture;
                                task.uiToUrlMap.Remove(task.ownerUI);
                                CacheTexture(task.downloadUrl, texture);

                                if (task.loadDoneAction != null)
                                {
                                    task.loadDoneAction();
                                }
                            }
                        }
                        finally 
                        {
                            ++m_CurrentIdleTask;
                            m_DownloadingTask.Remove(task.downloadUrl);
                            LaunchNextLoadTask();
                        }
                    });
                });          
            }
        }
    }
}