using System.Collections;
using System.Collections.Generic;
using UGCore;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using AssetLoadList = UGFoundation.Collections.Generic.LinkedDictionary<string, GameLogic.Components.AssetLoadTask>;

namespace GameLogic.Components
{   
    public class ResourcesAssetLoader : AssetLoader
    {
        private List<AssetLoadList>  m_AssetLoadList;
        private AssetLoadTask        m_CurrentLoadTask;

        public ResourcesAssetLoader()
        {
            var priorityEnum = System.Enum.GetValues(typeof(AssetLoadPriority)); 
            m_AssetLoadList = new List<AssetLoadList> (priorityEnum.Length);
            for (int i = 0; i < priorityEnum.Length; ++i)
            {
                m_AssetLoadList.Add(new AssetLoadList());
            }
        }

        public override Object LoadAssetSync(string assetName)
        {
            return Resources.Load(assetName);
        }

        public override AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, AssetLoadPriority priority = AssetLoadPriority.Normal)
        {
            return LoadAssetAsync(assetName, loadedCallback, null,priority);
        }

        public override AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, UnityAction<float> progressCallback, AssetLoadPriority priority = AssetLoadPriority.Normal)
        {
            if (m_CurrentLoadTask != null)
            {
                if (m_AssetLoadList[(int)priority].ContainsKey(assetName))
                {
                    var loadTask = m_AssetLoadList[(int)priority][assetName];
                    loadTask.AssetName = assetName;
                    var asyncLoad = new AssetAsyncLoad(assetName);
                    loadTask.AddCallback(asyncLoad, loadedCallback, progressCallback);
                    return asyncLoad;
                }
                else
                {
                    var loadTask = AssetLoadTask.GetAssetLoadTask();
                    loadTask.AssetName = assetName;
                    var asyncLoad = new AssetAsyncLoad(assetName);
                    loadTask.AddCallback(asyncLoad, loadedCallback, progressCallback);
                    m_AssetLoadList[(int)priority].Add(assetName, loadTask);
                    return asyncLoad;
                }               
            }
            else
            {
                var loadTask = AssetLoadTask.GetAssetLoadTask();
                loadTask.AssetName = assetName;
                var asyncLoad = new AssetAsyncLoad(assetName);
                loadTask.AddCallback(asyncLoad, loadedCallback, progressCallback);
                m_CurrentLoadTask = loadTask;
                StartLoadAsset();
                return asyncLoad;
            }
        }

        public override void RemoveLoadTask(string assetName)
        {
            for (int i = 0; i < m_AssetLoadList.Count; ++i)
            {
                if (m_AssetLoadList[i].ContainsKey(assetName))
                {
                    m_AssetLoadList[i].Remove(assetName);
                }
            }
        }

        public override void RemoveLoadRequest(AssetAsyncLoad asyncRequest)
        {
            if (asyncRequest != null)
            {
                for (int i = 0; i < m_AssetLoadList.Count; ++i)
                {
                    if (m_AssetLoadList[i].ContainsKey(asyncRequest.Name))
                    {
                        m_AssetLoadList[i][asyncRequest.Name].RemoveCallback(asyncRequest);
                    }
                }
            }
        }

        protected void StartLoadAsset()
        {
            if (m_CurrentLoadTask != null)
            {
                GameCore.Instance.StartCoroutine(LoadAsset(m_CurrentLoadTask.AssetName));
            }
        }

        IEnumerator LoadAsset(string assetName)
        {
            var loadRequest = Resources.LoadAsync(assetName);

            if (m_CurrentLoadTask.HasProgressCallback)
            {
                while (!loadRequest.isDone)
                {
                    m_CurrentLoadTask.SafeInvokeAllProgressCallback(loadRequest.progress);
                    yield return null;
                }
            }
            else
            {
                yield return loadRequest;
            }


            m_AssetLoadDoneEvent.Invoke(assetName, loadRequest.asset);
            m_CurrentLoadTask.SafeInvokeAllCallback(loadRequest.asset);

            AssetLoadTask.ReleaseAssetLoadTask(m_CurrentLoadTask);
            m_CurrentLoadTask = PopAssetLoadTask();

            while (m_CurrentLoadTask != null)
            {
                var asset = AssetManager.Instance.ContainAsset(m_CurrentLoadTask.AssetName);
                if (asset == null)
                {
                    break;
                }

                m_CurrentLoadTask.SafeInvokeAllCallback(asset);
                m_CurrentLoadTask = PopAssetLoadTask();
            }

            if (m_CurrentLoadTask != null)
            {
                StartLoadAsset();
            }
        }

        AssetLoadTask PopAssetLoadTask()
        {
            var index = -1;
            var taskKey = "";
            for (int i = 0; i < m_AssetLoadList.Count; ++i)
            {
                var enumerator = m_AssetLoadList[i].Keys.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    index = i;
                    taskKey = enumerator.Current;
                    break;
                }

                if (index > -1)
                {
                    var loakTask = m_AssetLoadList[i][taskKey];
                    m_AssetLoadList[i].Remove(taskKey);
                    return loakTask;
                }
            }
            return null;
        }
    }
}