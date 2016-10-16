using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace GameLogic.Components
{
    public class AssetBundleManager : AssetManager
    {
        private AssetCachePool  m_AssetCachePool;
        private AssetRecycleBin m_AssetRecycleBin;
        private AssetLoader     m_AssetLoader;
        private Dictionary<string, AssetInfo> m_AssetDependences;
        private AssetBundleCollector m_AssetBundleCollector;

        private List<string>    m_AssetLoadOrder;
        private HashSet<string> m_AssetLoadUniqueSet;

        private AssetBundleManager(byte[] assetDependencesBytes)
        {
            m_AssetDependences     = new AssetDependencesParser().ParseAssetDependences(assetDependencesBytes);
            m_AssetCachePool       = new AssetCachePool();

            m_AssetBundleCollector = new AssetBundleCollector(m_AssetDependences);
            m_AssetRecycleBin      = new AssetBundleRecycleBin(m_AssetBundleCollector);

            m_AssetCachePool.RegisterRecycleAssetEvent(m_AssetRecycleBin.RecycleAsset);
            m_AssetCachePool.RegisterCheckAssetDoneEvent(m_AssetRecycleBin.UnloadUnuesdAssets);

            m_AssetLoader = new AssetBundleLoader(m_AssetDependences, m_AssetBundleCollector);
            m_AssetLoader.RegisterAssetLoadDoneEvent(AddAssetToCachePool);

            m_AssetLoadOrder = new List<string>();
            m_AssetLoadUniqueSet = new HashSet<string>();
        }

        public static void InitializeAssetManager(byte[] assetDependencesBytes)
        {
            if (_instance == null)
            {
                _instance = new AssetBundleManager(assetDependencesBytes);
            }
        }

        public override void LoadAssetSync(string assetName, UnityAction<Object> loadedCallback)
        {
            assetName = string.Format("Assets/Resources/{0}", assetName);
            var assetNode = LoadAsset(assetName, loadedCallback);
            if (assetNode == null)
            {
                var targetObj = m_AssetLoader.LoadAssetSync(assetName);
                if (targetObj != null)
                {
                    assetNode = AssetNode.GetAssetNode();
                    assetNode.Target = targetObj;
                    m_AssetCachePool.AddAssetNode(assetName, assetNode);
                    SafeInvokeDoneAction(loadedCallback, assetNode.Target);
                }
                else
                {
                    Debug.LogError("Asset doesn't exist at AssetBundleManager.LoadAssetSync,Asset name is <color=#ff0000>" + assetName + "</color>");
                }
            }
        }

        public override AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, AssetLoadPriority priority = AssetLoadPriority.Normal)
        {
            return LoadAssetAsync(assetName, loadedCallback, null, priority);
        }

        public override AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, UnityAction<float> progressCallback, AssetLoadPriority priority = AssetLoadPriority.Normal)
        {
            assetName = string.Format("Assets/Resources/{0}", assetName);
            var assetNode = LoadAsset(assetName, loadedCallback);
            if (assetNode == null)
            {
                return m_AssetLoader.LoadAssetAsync(assetName, loadedCallback, progressCallback, priority);
            }
            return null;
        }

        public override void AddAssetReference(string assetName)
        {
            GetAssetDependences(assetName);
            for (int i = 0; i < m_AssetLoadOrder.Count; ++i)
            {
                var assetNode = m_AssetCachePool.GetAssetNode(m_AssetLoadOrder[i]);
                if (assetNode != null)
                {
                    ++assetNode.ReferenceCount;
                }
            }
        }

        public override void SubAssetReference(string assetName)
        {
            GetAssetDependences(assetName);
            for (int i = 0; i < m_AssetLoadOrder.Count; ++i)
            {
                var assetNode = m_AssetCachePool.GetAssetNode(m_AssetLoadOrder[i]);
                if (assetNode != null)
                {
                    --assetNode.ReferenceCount;
                }
            }
        }

        public override void RemoveLoadTask(string assetName)
        {
            m_AssetLoader.RemoveLoadTask(assetName);
        }

        public override void RemoveLoadRequest(AssetAsyncLoad asyncRequest)
        {
            m_AssetLoader.RemoveLoadRequest(asyncRequest);
        }

        public override Object ContainAsset(string assetName)
        {
            var assetNode = LoadAsset(assetName, null);
            if (assetNode != null)
            {
                return assetNode.Target;
            }
            return null;
        }

        public override void UnloadUnusedAssets()
        {
            m_AssetRecycleBin.UnloadUnuesdAssets();
        }

        public override void CheckExpiredAssets()
        {
            m_AssetCachePool.CheckExpiredAsset();
        }

        public override void ReleaseAssetBundle(string assetName)
        {
            m_AssetBundleCollector.ReleaseAssetBundle(assetName);
        }

        protected AssetNode LoadAsset(string assetName, UnityAction<Object> loadedCallback)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Debug.LogError("Asset doesn't exist at AssetBundleManager.LoadAssetSync,Asset name is <color=#ff0000>" + assetName + "</color>");
                return null;
            }

            var assetNode = m_AssetCachePool.GetAssetNode(assetName);
            if (assetNode != null)
            {
                SafeInvokeDoneAction(loadedCallback, assetNode.Target);
                return assetNode;
            }

            assetNode = m_AssetRecycleBin.GetAssetNode(assetName);
            if (assetNode != null)
            {
                m_AssetRecycleBin.RemoveAsset(assetName);
                m_AssetCachePool.AddAssetNode(assetName, assetNode);
                SafeInvokeDoneAction(loadedCallback, assetNode.Target);
                return assetNode;
            }

            return null;
        }

        protected void SafeInvokeDoneAction(UnityAction<Object> loadedCallback, Object target)
        {
            try
            {
                if (loadedCallback != null)
                {
                    loadedCallback(target);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
            }
        }

        protected void AddAssetToCachePool(string assetName, Object targetObj)
        {
            if (targetObj != null)
            {
                var assetNode = LoadAsset(assetName, null);
                if (assetNode == null)
                {
                    assetNode = AssetNode.GetAssetNode();
                    assetNode.Target = targetObj;
                    m_AssetCachePool.AddAssetNode(assetName, assetNode);
                }
            }
            else
            {
                Debug.LogError("Asset doesn't exist at AssetBundleManager.LoadAssetAsync,Asset name is <color=#ff0000>" + assetName + "</color>");
            }
        }

        private void GetAssetDependences(string assetName)
        {
            assetName = string.Format("Assets/Resources/{0}", assetName);
            m_AssetLoadOrder.Clear();
            m_AssetLoadUniqueSet.Clear();
            if (!m_AssetDependences.ContainsKey(assetName))
            {
                Debug.LogError("AssetBundles dont have asset,AssetName : " + assetName);
                return;
            }
            GetAssetDeepDependences(assetName);
        }

        private void GetAssetDeepDependences(string finalName)
        {
            var childDependencesList = m_AssetDependences[finalName].DependencesPath;
            for (int i = 0; i < childDependencesList.Count; ++i)
            {
                GetAssetDeepDependences(childDependencesList[i]);
            }

            if (!m_AssetLoadUniqueSet.Contains(finalName))
            {
                m_AssetLoadOrder.Add(finalName);
                m_AssetLoadUniqueSet.Add(finalName);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            var enumerator = m_AssetDependences.GetEnumerator();
            while (enumerator.MoveNext())
            {
                sb.AppendLine(enumerator.Current.Key);
                sb.AppendLine("\t" + enumerator.Current.Value.AssetBundlePath);
                for (int i = 0; i < enumerator.Current.Value.DependencesPath.Count; ++i)
                {
                    sb.AppendLine("\t" + enumerator.Current.Value.DependencesPath[i]);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
