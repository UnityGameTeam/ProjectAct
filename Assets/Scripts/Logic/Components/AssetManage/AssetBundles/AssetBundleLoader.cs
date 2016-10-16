using System.Collections;
using System.Collections.Generic;
using UGCore;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using AssetLoadList = UGFoundation.Collections.Generic.LinkedDictionary<string, GameLogic.Components.AssetLoadTask>;

namespace GameLogic.Components
{
    public class AssetBundleLoader : AssetLoader
    {
        private List<AssetLoadList>           m_AssetLoadList;
        private AssetLoadTask                 m_CurrentLoadTask;
        private Dictionary<string, AssetInfo> m_AssetDependences;
        private AssetBundleCollector          m_AssetBundleCollector;

        private List<string>                  m_AssetLoadOrder;
        private HashSet<string>               m_AssetLoadUniqueSet;
        private List<string>                  m_AssetAsyncLoadOrder;
        private HashSet<string>               m_AssetAsyncLoadUniqueSet;
        public AssetBundleLoader(Dictionary<string, AssetInfo> dependences, AssetBundleCollector assetBundleCollector)
        {
            var priorityEnum = System.Enum.GetValues(typeof(AssetLoadPriority));
            m_AssetLoadList = new List<AssetLoadList>(priorityEnum.Length);
            for (int i = 0; i < priorityEnum.Length; ++i)
            {
                m_AssetLoadList.Add(new AssetLoadList());
            }
            m_AssetDependences = dependences;

            m_AssetLoadOrder = new List<string>();
            m_AssetLoadUniqueSet = new HashSet<string>();
            m_AssetAsyncLoadOrder = new List<string>();
            m_AssetAsyncLoadUniqueSet = new HashSet<string>();

            m_AssetBundleCollector = assetBundleCollector;
        }

        public override Object LoadAssetSync(string assetName)
        {
            GetAssetDependences(assetName);
            return LoadAssetsSync();
        }

        public override AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, AssetLoadPriority priority = AssetLoadPriority.Normal)
        {
            return LoadAssetAsync(assetName, loadedCallback, null, priority);
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
            GetAssetDependencesForAysnc(assetName);
            Object asset = null;

            var progress = 0f;
            var count = m_AssetAsyncLoadOrder.Count;
            for (int i = 0; i < count; ++i)
            {
                var loadAssetName = m_AssetAsyncLoadOrder[i];
                asset = AssetManager.Instance.ContainAsset(loadAssetName);
                if (asset != null)
                {
                    continue;
                }

                var assetBundlePath = m_AssetDependences[loadAssetName].AssetBundlePath;
                var assetBundle = m_AssetBundleCollector.GetAssetBundle(assetBundlePath);
                AssetBundleRequest assetbundleRequest = null;
                if (assetBundle != null)
                {
                    if (m_CurrentLoadTask.HasProgressCallback)
                    {
                        var deltaProgress = 1f / count;
                        assetbundleRequest = assetBundle.LoadAssetAsync(loadAssetName, typeof(Object));
                        while (!assetbundleRequest.isDone)
                        {
                            var tempProgress = progress + deltaProgress * assetbundleRequest.progress;
                            m_CurrentLoadTask.SafeInvokeAllProgressCallback(tempProgress);
                            yield return null;
                        }

                        progress += deltaProgress;
                        m_CurrentLoadTask.SafeInvokeAllProgressCallback(progress);
                    }
                    else
                    {
                        assetbundleRequest = assetBundle.LoadAssetAsync(loadAssetName, typeof(Object));
                        yield return assetbundleRequest;
                    }

                    asset = assetbundleRequest.asset;
                    m_AssetLoadDoneEvent(loadAssetName, asset);
                    m_AssetBundleCollector.AddAssetBundleReference(loadAssetName, assetBundlePath);
                    continue;
                }

                WWW www = null;
                if (m_CurrentLoadTask.HasProgressCallback)
                {
                    var deltaProgress = 1f / count;
                    www = new WWW(UGCoreConfig.GetAssetBundlePath(assetBundlePath));
                    while (!www.isDone)
                    {
                        var tempProgress = progress + deltaProgress * 0.5f * www.progress;
                        m_CurrentLoadTask.SafeInvokeAllProgressCallback(tempProgress);
                        yield return null;
                    }

                    assetBundle = m_AssetBundleCollector.GetAssetBundle(assetBundlePath);
                    if (assetBundle == null)
                    {
                        assetBundle = www.assetBundle;
                    }
                    assetbundleRequest = assetBundle.LoadAssetAsync(loadAssetName, typeof(Object));

                    while (!assetbundleRequest.isDone)
                    {
                        var tempProgress = progress + deltaProgress * 0.5f * (1 + assetbundleRequest.progress);
                        m_CurrentLoadTask.SafeInvokeAllProgressCallback(tempProgress);
                        yield return null;
                    }

                    progress += deltaProgress;
                    m_CurrentLoadTask.SafeInvokeAllProgressCallback(progress);
                }
                else
                {
                    www = new WWW(UGCoreConfig.GetAssetBundlePath(assetBundlePath));
                    yield return www;

                    assetBundle = m_AssetBundleCollector.GetAssetBundle(assetBundlePath);
                    if (assetBundle == null)
                    {
                        assetBundle = www.assetBundle;
                    }
                    assetbundleRequest = assetBundle.LoadAssetAsync(loadAssetName, typeof(Object));
                    yield return assetbundleRequest;
                }

                asset = assetbundleRequest.asset;
                m_AssetLoadDoneEvent(loadAssetName, asset);
      
                m_AssetBundleCollector.AddAssetBundle(assetBundlePath, assetBundle);
                m_AssetBundleCollector.AddAssetBundleReference(loadAssetName, assetBundlePath);
            }

            m_AssetLoadDoneEvent.Invoke(assetName, asset);
            m_CurrentLoadTask.SafeInvokeAllCallback(asset);

            AssetLoadTask.ReleaseAssetLoadTask(m_CurrentLoadTask);
            m_CurrentLoadTask = PopAssetLoadTask();

            while (m_CurrentLoadTask != null)
            {
                asset = AssetManager.Instance.ContainAsset(m_CurrentLoadTask.AssetName);
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

        private void GetAssetDependencesForAysnc(string assetName)
        {
            m_AssetAsyncLoadOrder.Clear();
            m_AssetAsyncLoadUniqueSet.Clear();
            if (!m_AssetDependences.ContainsKey(assetName))
            {
                Debug.LogError("AssetBundles dont have asset,AssetName : " + assetName);
                return;
            }
            GetAssetDeepDependencesForAysnc(assetName);
        }

        private void GetAssetDeepDependencesForAysnc(string finalName)
        {
            var childDependencesList = m_AssetDependences[finalName].DependencesPath;
            for (int i = 0; i < childDependencesList.Count; ++i)
            {
                GetAssetDeepDependencesForAysnc(childDependencesList[i]);
            }

            if (!m_AssetAsyncLoadUniqueSet.Contains(finalName))
            {
                m_AssetAsyncLoadOrder.Add(finalName);
                m_AssetAsyncLoadUniqueSet.Add(finalName);
            }
        }

        private void GetAssetDependences(string assetName)
        {
            m_AssetLoadOrder.Clear();
            m_AssetLoadUniqueSet.Clear();
            if (!m_AssetDependences.ContainsKey(assetName))
            {
                Debug.LogError("AssetBundles dont have asset,AssetName : "+assetName);
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

        private Object LoadAssetsSync()
        {
            Object asset = null;
            for (int i = 0; i < m_AssetLoadOrder.Count; ++i)
            {
                var assetName = m_AssetLoadOrder[i];
                if (AssetManager.Instance.ContainAsset(assetName))
                {
                    continue;
                }

                var assetBundlePath = m_AssetDependences[assetName].AssetBundlePath;
                var assetBundle = m_AssetBundleCollector.GetAssetBundle(assetBundlePath);
                if (assetBundle != null)
                {
                    asset = assetBundle.LoadAsset(assetName);
                    m_AssetLoadDoneEvent(assetName, asset);
                    m_AssetBundleCollector.AddAssetBundleReference(assetName, assetBundlePath);
                    continue;
                }

                var www = new WWW(UGCoreConfig.GetAssetBundlePath(assetBundlePath));
                assetBundle = www.assetBundle;
                asset = www.assetBundle.LoadAsset(assetName);
                m_AssetLoadDoneEvent(assetName, asset);
                m_AssetBundleCollector.AddAssetBundle(assetBundlePath,assetBundle);
                m_AssetBundleCollector.AddAssetBundleReference(assetName,assetBundlePath);
            }
            return asset;
        }
    }
}