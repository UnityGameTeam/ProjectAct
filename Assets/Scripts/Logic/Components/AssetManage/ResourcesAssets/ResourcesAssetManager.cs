using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace GameLogic.Components
{
    public class ResourcesAssetManager : AssetManager
    {
        private AssetCachePool  m_AssetCachePool;
        private AssetRecycleBin m_AssetRecycleBin;
        private AssetLoader     m_AssetLoader;

        private ResourcesAssetManager()
        {
            m_AssetCachePool  = new AssetCachePool();
            m_AssetRecycleBin = new AssetRecycleBin();
            m_AssetLoader     = new ResourcesAssetLoader();

            m_AssetCachePool.RegisterRecycleAssetEvent(m_AssetRecycleBin.RecycleAsset);
            m_AssetCachePool.RegisterCheckAssetDoneEvent(m_AssetRecycleBin.UnloadUnuesdAssets);
            m_AssetLoader.RegisterAssetLoadDoneEvent(AddAssetToCachePool);
        }

        public static void InitializeAssetManager()
        {
            if (_instance == null)
            {
                _instance = new ResourcesAssetManager();
            }           
        }

        public override void LoadAssetSync(string assetName, UnityAction<Object> loadedCallback)
        {
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
                    Debug.LogError("Asset doesn't exist at ResourcesAssetManager.LoadAssetSync,Asset name is <color=#ff0000>" + assetName + "</color>");
                }
            }
        }

        public override AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, AssetLoadPriority priority = AssetLoadPriority.Normal)
        {
            return LoadAssetAsync(assetName, loadedCallback, null,priority);
        }

        public override AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, UnityAction<float> progressCallback, AssetLoadPriority priority = AssetLoadPriority.Normal)
        {
            var assetNode = LoadAsset(assetName, loadedCallback);
            if (assetNode == null)
            {
                return m_AssetLoader.LoadAssetAsync(assetName, loadedCallback, progressCallback, priority);
            }
            return null;
        }

        public override void AddAssetReference(string assetName)
        {
            var assetNode = m_AssetCachePool.GetAssetNode(assetName);
            if (assetNode != null)
            {
                ++assetNode.ReferenceCount;
            }
        }

        public override void SubAssetReference(string assetName)
        {
            var assetNode = m_AssetCachePool.GetAssetNode(assetName);
            if (assetNode != null)
            {
                --assetNode.ReferenceCount;
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
            
        }

        protected AssetNode LoadAsset(string assetName, UnityAction<Object> loadedCallback)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Debug.LogError("Asset doesn't exist at ResourcesAssetManager.LoadAssetSync,Asset name is <color=#ff0000>" + assetName + "</color>");
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

        protected void SafeInvokeDoneAction(UnityAction<Object> loadedCallback,Object target)
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

        protected void AddAssetToCachePool(string assetName,Object targetObj)
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
                Debug.LogError("Asset doesn't exist at ResourcesAssetManager.LoadAssetAsync,Asset name is <color=#ff0000>" + assetName + "</color>");
            }
        }
    }
}
