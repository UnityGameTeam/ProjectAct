using System;
using System.Collections;
using System.Collections.Generic;
using UGCore;
using UGCore.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic.Components
{
    public class AssetRecycleBin
    {
        protected class AssetWeakReference
        {
            public string Name { get; set; }
            protected WeakReference AssetReference { get; set; }

            public AssetWeakReference()
            {
                AssetReference = new WeakReference(null);
            }

            public bool IsAlive()
            {
                return AssetReference.IsAlive;
            }

            public void SetTarget(Object assetObject)
            {
                AssetReference.Target = assetObject;
            }

            public object GetTarget()
            {
                return AssetReference.Target;
            }
        }

        protected static ObjectCachePool<AssetWeakReference> s_AssetReferences = new ObjectCachePool<AssetWeakReference>(null, null);

        protected bool m_ReleasingAsset;
        protected Dictionary<string, int>  m_RecycleAssetIndexMap = new Dictionary<string, int>();
        protected List<AssetWeakReference> m_RecycleAssetList     = new List<AssetWeakReference>();
        protected List<int>                m_FreeIndexList        = new List<int>();
 
        public void RecycleAsset(string assetName, Object assetObject)
        {
            if (!m_RecycleAssetIndexMap.ContainsKey(assetName))
            {
                if (m_FreeIndexList.Count > 0)
                {
                    var index = m_FreeIndexList[m_FreeIndexList.Count - 1];
                    m_FreeIndexList.RemoveAt(m_FreeIndexList.Count - 1);
                    var assetReference = s_AssetReferences.Get();
                    assetReference.SetTarget(assetObject);
                    assetReference.Name = assetName;
                    m_RecycleAssetList[index] = assetReference;
                    m_RecycleAssetIndexMap.Add(assetName, index);
                }
                else
                {
                    var assetReference = s_AssetReferences.Get();
                    assetReference.SetTarget(assetObject);
                    assetReference.Name = assetName;
                    m_RecycleAssetList.Add(assetReference);
                    m_RecycleAssetIndexMap.Add(assetName, m_RecycleAssetList.Count - 1);
                }
            }
            else
            {
                var assetReference = m_RecycleAssetList[m_RecycleAssetIndexMap[assetName]];
                if (!assetReference.IsAlive())
                {
                    assetReference.Name = assetName;
                    assetReference.SetTarget(assetObject);
                }
            }
        }

        public AssetNode GetAssetNode(string assetName)
        {
            if (m_RecycleAssetIndexMap.ContainsKey(assetName))
            {
                var assetReference = m_RecycleAssetList[m_RecycleAssetIndexMap[assetName]];
                if (assetReference.IsAlive())
                {
                    var target = assetReference.GetTarget() as Object;
                    if (target == null)
                    {
                        RemoveAsset(assetName);
                        return null;
                    }

                    var assetNode = AssetNode.GetAssetNode();               
                    assetNode.Target = assetReference.GetTarget() as Object;
                    assetNode.LastAccessTime = Time.unscaledTime;
                    return assetNode;
                }
                RemoveAsset(assetName);
            }
            return null;
        }

        public bool RemoveAsset(string assetName)
        {
            if (m_RecycleAssetIndexMap.ContainsKey(assetName))
            {
                var assetReference = m_RecycleAssetList[m_RecycleAssetIndexMap[assetName]];
                if (assetReference != null)
                {
                    var index = m_RecycleAssetIndexMap[assetName];
                    s_AssetReferences.Release(m_RecycleAssetList[index]);
                    m_RecycleAssetList[index] = null;
                    m_FreeIndexList.Add(index);
                }
                m_RecycleAssetIndexMap.Remove(assetName);
                return true;
            }
            return false;
        }

        public void UnloadUnuesdAssets()
        {
            if (!m_ReleasingAsset)
            {
                m_ReleasingAsset = true;
                GameCore.Instance.StartCoroutine(WaitUnloadDone());
            }
        }

        protected virtual IEnumerator WaitUnloadDone()
        {
            yield return Resources.UnloadUnusedAssets();

            var checkNum = 0;
            for (int i = 0; i < m_RecycleAssetList.Count; ++i)
            {
                if (m_RecycleAssetList[i] != null && !m_RecycleAssetList[i].IsAlive())
                {
                    RemoveAsset(m_RecycleAssetList[i].Name);

                    ++checkNum;
                    if (checkNum >= AssetLoadConfig.CheckAssetNumPerFrame)
                    {
                        checkNum = 0;
                        yield return null;
                    }
                }
            }

            m_ReleasingAsset = false;
        }
    }
}
