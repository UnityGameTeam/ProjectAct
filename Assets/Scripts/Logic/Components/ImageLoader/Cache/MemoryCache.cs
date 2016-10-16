using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameLogic.Components
{
    public class MemoryCache : ITextureCache
    {
        private Dictionary<string, WeakReference> m_ImageCache = new Dictionary<string, WeakReference>();

        private int                               m_MaxCacheCount = 15;
        private ITextureCache                     m_TextureCache;
        private List<string>                      m_WaitRemoveAssetList = new List<string>();  

        public MemoryCache(ITextureCache textureCache, int maxCacheCount = 15)
        {
            m_MaxCacheCount = maxCacheCount;
            m_TextureCache = textureCache;
        }

        public void SetMaxCacheCount(int maxCacheCount)
        {
            if (m_TextureCache != null)
            {
                m_TextureCache.SetMaxCacheCount(maxCacheCount);
            }
            m_MaxCacheCount = maxCacheCount;
        }

        public bool GetTexture(string key, string savePath, RawImage ownerUI, Dictionary<RawImage, string> uiToUrlMap, bool resetUI, Action loadDoneAction)
        {
            if (m_TextureCache != null)
            {
                if (m_TextureCache.GetTexture(key, savePath, ownerUI, uiToUrlMap, resetUI, loadDoneAction))
                    return true;
            }

            if (m_ImageCache.ContainsKey(key) && m_ImageCache[key].IsAlive)
            {
                var texture = m_ImageCache[key].Target as Texture2D;
                if (texture != null && ownerUI != null)
                {
                    ownerUI.texture = texture;
                    uiToUrlMap.Remove(ownerUI);
                    if (loadDoneAction != null)
                    {
                        loadDoneAction();
                    }
                    return true;
                }
            }
            else
            {
                m_ImageCache.Remove(key);
            }

            if (resetUI && ownerUI)
            {
                ownerUI.texture = null;
            }
            return false;
        }

        public void ClearCache()
        {
            if (m_TextureCache != null)
            {
                m_TextureCache.ClearCache();
            }

            var enumerator = m_ImageCache.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var texture = enumerator.Current.Value.Target as Texture2D;
                if (texture != null)
                {
                    Object.Destroy(texture);
                }
            }
            m_ImageCache.Clear();
        }

        public bool CacheTexture(string key, Texture2D texture)
        {
            if (m_TextureCache == null)
            {
                if (!m_ImageCache.ContainsKey(key))
                {
                    var weakReference = new WeakReference(texture);
                    m_ImageCache.Add(key, weakReference);
                }
                else
                {
                    if (!m_ImageCache[key].IsAlive)
                    {
                        m_ImageCache[key].Target = texture;
                    }
                }

                if (m_ImageCache.Count > m_MaxCacheCount)
                {
                    Resources.UnloadUnusedAssets();
                }

                CheckExpiredAsset();
                return true;
            }

            if (m_TextureCache.CacheTexture(key, texture))
                return true;

            return false;
        }
 
        private void CheckExpiredAsset()
        {
            if (m_ImageCache.Count > m_MaxCacheCount * 3)
            {
                m_WaitRemoveAssetList.Clear();
                var enumerator = m_ImageCache.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (!enumerator.Current.Value.IsAlive)
                    {
                        m_WaitRemoveAssetList.Add(enumerator.Current.Key);
                    }
                }

                for (int i = 0, count = m_WaitRemoveAssetList.Count; i < count; ++i)
                {
                    m_ImageCache.Remove(m_WaitRemoveAssetList[i]);
                }
            }
        }
    }
}