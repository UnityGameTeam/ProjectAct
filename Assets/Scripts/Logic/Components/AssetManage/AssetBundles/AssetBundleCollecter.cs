using UnityEngine;
using System.Collections.Generic;
using UGCore.Components;

namespace GameLogic.Components
{
    public class AssetBundleCollector
    {
        protected class AssetBundleWrap
        {
            private static ObjectCachePool<AssetBundleWrap> s_AssetBundleWrap = new ObjectCachePool<AssetBundleWrap>(x => x.ReferenceCount = 0, null);

            public AssetBundle Target         { get; set; }
            public int         ReferenceCount { get; set; }

            public static AssetBundleWrap GetAssetBundleWrap()
            {
                return s_AssetBundleWrap.Get();
            }

            public static void ReleaseAssetBundleWrap(AssetBundleWrap target)
            {
                s_AssetBundleWrap.Release(target);
            }
        }

        protected Dictionary<string, AssetBundleWrap> m_AssetBundleMap        = new Dictionary<string, AssetBundleWrap>();
        protected Dictionary<string, AssetBundleWrap> m_AssetToAssetBundleMap = new Dictionary<string, AssetBundleWrap>();
        private Dictionary<string, AssetInfo> m_AssetDependences;

        public AssetBundleCollector(Dictionary<string, AssetInfo> dependences)
        {
            m_AssetDependences = dependences;
        }

        public AssetBundle GetAssetBundle(string assetBundlePath)
        {
            if (m_AssetBundleMap.ContainsKey(assetBundlePath))
                return m_AssetBundleMap[assetBundlePath].Target;
            return null;
        }

        public void AddAssetBundle(string assetBundlePath, AssetBundle assetBundle)
        {
            if (!m_AssetBundleMap.ContainsKey(assetBundlePath))
            {
                var assetBundleWarp = AssetBundleWrap.GetAssetBundleWrap();
                assetBundleWarp.Target = assetBundle;
                m_AssetBundleMap.Add(assetBundlePath,assetBundleWarp);
            }
        }

        public void AddAssetBundleReference(string assetName,string assetBundlePath)
        {
            if (!m_AssetToAssetBundleMap.ContainsKey(assetName))
            {
                if (!m_AssetBundleMap.ContainsKey(assetBundlePath))
                {
                    return;
                }
                var assetBundleWarp = m_AssetBundleMap[assetBundlePath];
                ++assetBundleWarp.ReferenceCount;
                m_AssetToAssetBundleMap.Add(assetName, assetBundleWarp);
            }
        }

        public void SubAssetBundleReference(string assetName)
        {
            if (m_AssetToAssetBundleMap.ContainsKey(assetName))
            {
                var assetBundleWarp = m_AssetToAssetBundleMap[assetName];
                m_AssetToAssetBundleMap.Remove(assetName);
                if (assetBundleWarp == null)
                {
                    return;
                }

                --assetBundleWarp.ReferenceCount;
                if (assetBundleWarp.ReferenceCount <= 0)
                {
                    m_AssetBundleMap.Remove(m_AssetDependences[assetName].AssetBundlePath);
                    if(assetBundleWarp.Target != null)
                        assetBundleWarp.Target.Unload(false);
                    AssetBundleWrap.ReleaseAssetBundleWrap(assetBundleWarp);
                }
            }
        }


        public void ReleaseAssetBundle(string assetName)
        {
            assetName = string.Format("Assets/Resources/{0}", assetName);
            if (m_AssetDependences.ContainsKey(assetName))
            {
                var assetBundlePath = m_AssetDependences[assetName].AssetBundlePath;
                if (m_AssetBundleMap.ContainsKey(assetBundlePath))
                {
                    var assetBundleWarp = m_AssetBundleMap[assetBundlePath];
                    if (assetBundleWarp == null)
                    {
                        return;
                    }

                    if (assetBundleWarp.Target != null)
                    {
                        assetBundleWarp.Target.Unload(true);
                    }
                }
            }
        }
    }
}