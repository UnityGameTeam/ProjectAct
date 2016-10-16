using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Components
{
    public interface ITextureCache
    {
        void SetMaxCacheCount(int maxCacheCount);
        bool GetTexture(string key, string savePath, RawImage ownerUI, Dictionary<RawImage, string> uiToUrlMap, bool resetUI, Action loadDoneAction);
        bool CacheTexture(string key, Texture2D texture);
        void ClearCache();
    }
}