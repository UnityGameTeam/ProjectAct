using System;
using System.Collections;
using System.Collections.Generic;
using UGCore;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic.Components
{
    public class AssetCachePool
    {
        private bool m_CheckingExpiredAsset;     
        private Dictionary<string,int> m_AssetNodeIndexMap = new Dictionary<string, int>(); 
        private List<AssetNode>        m_AssetNodeList = new List<AssetNode>(); 
        private List<int>              m_FreeIndexList = new List<int>();

        //这里不要使用UnityEvent来处理，UnityEvent是序列化的事件系统，如果
        //UnityEvent发送的事件带有参数，并且这些参数传送的是这里管理的资源
        //这些资源将会被UnityEvent内部引用，无法释放掉
        public Action m_CheckAssetDoneEvent;
        public Action<string, Object> m_RecycleAssetEvent;

        public void RegisterCheckAssetDoneEvent(Action action)
        {
            m_CheckAssetDoneEvent += action;
        }

        public void RegisterRecycleAssetEvent(Action<string, Object> action)
        {
            m_RecycleAssetEvent += action;
        }

        public void AddAssetNode(string assetName, AssetNode assetNode)
        {
            if (!m_AssetNodeIndexMap.ContainsKey(assetName))
            {
                assetNode.LastAccessTime = Time.unscaledTime;
                assetNode.Name = assetName;

                if (m_FreeIndexList.Count > 0)
                {
                    var index = m_FreeIndexList[m_FreeIndexList.Count - 1];
                    m_FreeIndexList.RemoveAt(m_FreeIndexList.Count - 1);
                    m_AssetNodeList[index] = assetNode;
                    m_AssetNodeIndexMap.Add(assetName, index);
                }
                else
                {
                    m_AssetNodeList.Add(assetNode);
                    m_AssetNodeIndexMap.Add(assetName, m_AssetNodeList.Count - 1);
                }
            }
        }

        public AssetNode GetAssetNode(string assetName)
        {
            if (m_AssetNodeIndexMap.ContainsKey(assetName))
            {
                var assetNode = m_AssetNodeList[m_AssetNodeIndexMap[assetName]];
                if (assetNode != null)
                {
                    assetNode.LastAccessTime = Time.unscaledTime;
                    return assetNode;
                }
            }
            return null;
        }

        protected bool RemoveAssetNode(string assetName)
        {
            if (m_AssetNodeIndexMap.ContainsKey(assetName))
            {
                var assetNode = m_AssetNodeList[m_AssetNodeIndexMap[assetName]];
                if (assetNode != null)
                {
                    var index = m_AssetNodeIndexMap[assetName];
                    m_AssetNodeIndexMap.Remove(assetName);
                    m_AssetNodeList[index] = null;
                    m_FreeIndexList.Add(index);
                    return true;
                }
            }
            return false;
        }

        public void CheckExpiredAsset()
        {
            if (!m_CheckingExpiredAsset)
            {
                m_CheckingExpiredAsset = true;
                GameCore.Instance.StartCoroutine(WaitCheckExpiredAssetDone());
            }
        }

        IEnumerator WaitCheckExpiredAssetDone()
        {
            var checkNum = 0;
            for (int i = 0; i < m_AssetNodeList.Count; ++i)
            {
                if (m_AssetNodeList[i] != null)
                {
                    var deltaTime = Time.unscaledTime - m_AssetNodeList[i].LastAccessTime;
                    if (deltaTime >= AssetLoadConfig.AssetExpirationTime)
                    {
                        --m_AssetNodeList[i].ReferenceCount;
                        if (deltaTime >= AssetLoadConfig.AssetReleaseExpirationTime)
                        {
                            m_AssetNodeList[i].ReferenceCount = 0;
                        }
                    }

                    if (m_AssetNodeList[i].ReferenceCount <= 0)
                    {
                        var assetNode = m_AssetNodeList[i];
                        RemoveAssetNode(assetNode.Name);
                        m_RecycleAssetEvent.Invoke(assetNode.Name, assetNode.Target);
                        AssetNode.ReleaseAssetNode(assetNode);
                    }

                    ++checkNum;
                    if (checkNum >= AssetLoadConfig.CheckAssetNumPerFrame)
                    {
                        checkNum = 0;
                        yield return null;
                    }
                }
            }

            m_CheckingExpiredAsset = false;
            m_CheckAssetDoneEvent.Invoke();
        } 
    }
}