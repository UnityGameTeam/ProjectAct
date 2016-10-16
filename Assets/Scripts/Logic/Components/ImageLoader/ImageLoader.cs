using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Components
{
    public class ImageLoader
    {
        private Dictionary<RawImage, string> m_UIToUrlMap = new Dictionary<RawImage, string>();
        private ITextureCache                m_TextureCache;
 
        public ImageLoader(int maxCacheCount = 15, int maxDownloadNum = 5)
        {
            var memoryCache = new MemoryCache(null, maxCacheCount);        
            var diskCache   = new DiskCache(memoryCache);
            m_TextureCache  = new NetworkCache(diskCache, maxDownloadNum);
        }

        public void SetMaxCacheCount(int count)
        {
            m_TextureCache.SetMaxCacheCount(count);
        }

        public void ClearCache()
        {
            m_TextureCache.ClearCache();
        }

        /// <summary>
        /// 加载图片
        /// </summary>
        /// <param name="url">图片在网络上的url路径</param>
        /// <param name="savePath">图片需要缓存在本地的路径，相对DiskCache的SaveDir的路径</param>
        /// <param name="ownerUI"></param>
        /// <param name="resetUI"></param>
        public void LoadImage(string url, string savePath,RawImage ownerUI, bool resetUI = true, Action loadDoneAction = null)
        {
            if (ownerUI == null || string.IsNullOrEmpty(url))
            {
                Debug.LogError("加载图片 RawImage可能为null或者url为空");
                return;
            }

            if (string.IsNullOrEmpty(savePath))
            {
                savePath = GetSavePathByUrl(url);
            }

            //添加ui到url的映射
            if (m_UIToUrlMap.ContainsKey(ownerUI))
            {
                m_UIToUrlMap[ownerUI] = url;
            }
            else
            {
                m_UIToUrlMap.Add(ownerUI, url);
            }

            if (resetUI)
            {
                ownerUI.texture = null;
            }

            m_TextureCache.GetTexture(url, savePath, ownerUI, m_UIToUrlMap, resetUI, loadDoneAction);
        }

        public string GetSavePathByUrl(string url)
        {
            ASCIIEncoding enc = new ASCIIEncoding();
            byte[] buffer = enc.GetBytes(url);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(buffer);

            StringBuilder sb = new StringBuilder();
            var halfLength = hash.Length / 2;
            var sumBefore = 0;
            var sumAfter = 0;
            for (int i = 0; i < hash.Length; i++)
            {
                sb.AppendFormat("{0:x2}", hash[i]);
                if (i < halfLength)
                {
                    sumBefore += hash[i];
                }
                else
                {
                    sumAfter += hash[i];
                }
            }

            return string.Format("group{0}/M{1:x2}/{2:X2}/{3:X2}/{4}.jpg", (sumBefore + sumAfter) % 8, (sumBefore + sumAfter) % 10, sumBefore % 255, sumAfter % 255,sb);
        }
    }
}