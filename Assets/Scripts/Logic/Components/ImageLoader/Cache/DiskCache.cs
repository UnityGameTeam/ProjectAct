using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Components
{
    public class DiskCache : ITextureCache
    {
        public static string  SaveDir = Path.Combine(Application.temporaryCachePath, "Photo");
        private ITextureCache m_TextureCache;

        public DiskCache(ITextureCache textureCache)
        {
            m_TextureCache = textureCache;
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

            var filePath = Path.Combine(SaveDir, savePath);
            if (File.Exists(filePath))
            {
                ActionShcheduler.Instance.RunAsync(() =>
                {
                    var data = ReadFile(filePath);
                    ActionShcheduler.Instance.QueueOnMainThread(() =>
                    {
                        if (data != null && ownerUI != null && uiToUrlMap.ContainsKey(ownerUI) &&
                            uiToUrlMap[ownerUI] == url)
                        {
                            Texture2D textured = new Texture2D(2, 2, TextureFormat.RGB24, false);
                            textured.LoadImage(data);
                            textured.Apply(false);

                            ownerUI.texture = textured;
                            uiToUrlMap.Remove(ownerUI);
                            CacheTexture(url, textured);

                            if (loadDoneAction != null)
                            {
                                loadDoneAction();
                            }
                        }
                    });
                });
                return true;
            }
            return false;
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

        private static byte[] ReadFile(string fileName)
        {
            byte[] buffer;
            try
            {
                buffer = File.ReadAllBytes(fileName);
            }
            catch (Exception e)
            {
                buffer = null;
                Debug.LogError(e);
            }
            return buffer;
        }
    }
}