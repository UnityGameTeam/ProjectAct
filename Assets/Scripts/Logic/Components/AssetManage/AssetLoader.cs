using System;
using UGCore.Components;
using UGFoundation.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace GameLogic.Components
{
    public class AssetLoadTask
    {
        private static ObjectCachePool<AssetLoadTask> s_AssetLoadTasks = new ObjectCachePool<AssetLoadTask>(null, x => x.Reset());

        protected LinkedDictionary<AssetAsyncLoad, UnityAction<Object>> m_LoadedCallbackList;
        protected LinkedDictionary<AssetAsyncLoad, UnityAction<float>>  m_ProgressCallbackList;

        public string AssetName { get; set; }

        public bool HasProgressCallback
        {
            get
            {
                if (m_ProgressCallbackList != null)
                {
                    return m_ProgressCallbackList.Count > 0;
                }
                return false;
            }
        }

        public AssetLoadTask()
        {
            m_LoadedCallbackList = new LinkedDictionary<AssetAsyncLoad, UnityAction<Object>>();
            m_ProgressCallbackList = new LinkedDictionary<AssetAsyncLoad, UnityAction<float>>();

            m_LoadedCallbackList.ReverseEnumerate   = true;
            m_ProgressCallbackList.ReverseEnumerate = true;
        }

        public static AssetLoadTask GetAssetLoadTask()
        {
            return s_AssetLoadTasks.Get();
        }

        public static void ReleaseAssetLoadTask(AssetLoadTask target)
        {
            s_AssetLoadTasks.Release(target);
        }

        protected void Reset()
        {
            m_LoadedCallbackList.Clear();
            m_ProgressCallbackList.Clear();
        }

        public void AddCallback(AssetAsyncLoad asyncLoad, UnityAction<Object> loaded, UnityAction<float> progress)
        {
            if (loaded != null)
                m_LoadedCallbackList.Add(asyncLoad, loaded);

            if(progress != null)
                m_ProgressCallbackList.Add(asyncLoad, progress);
        }

        public void RemoveCallback(AssetAsyncLoad asyncLoad)
        {
            if (m_LoadedCallbackList.ContainsKey(asyncLoad))
            {
                m_LoadedCallbackList.Remove(asyncLoad);
            }

            if (m_ProgressCallbackList.ContainsKey(asyncLoad))
            {
                m_ProgressCallbackList.Remove(asyncLoad);
            }
        }

        public void SafeInvokeAllCallback(Object target)
        {
            SafeInvokeAllProgressCallback(1f);

            var enumerator = m_LoadedCallbackList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                SafeInvokeAction(enumerator.Current.Value,target);
            }
        }

        public void SafeInvokeAllProgressCallback(float currentProgress)
        {
            var enumerator = m_ProgressCallbackList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                SafeInvokeProgressAction(enumerator.Current.Value, currentProgress);
            }
        }

        protected void SafeInvokeProgressAction(UnityAction<float> progress, float currentProgress)
        {
            try
            {
                if (progress != null)
                {
                    progress(currentProgress);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
            }
        }

        protected void SafeInvokeAction(UnityAction<Object> loaded,Object target)
        {
            try
            {
                if (loaded != null)
                {
                    loaded(target);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
            }
        }
    }

    public abstract class AssetLoader
    {
        protected Action<string, Object> m_AssetLoadDoneEvent;

        public void RegisterAssetLoadDoneEvent(Action<string, Object> action)
        {
            m_AssetLoadDoneEvent += action;
        }

        public abstract Object LoadAssetSync(string assetName);
        public abstract AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, AssetLoadPriority priority = AssetLoadPriority.Normal);
        public abstract AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, UnityAction<float> progressCallback, AssetLoadPriority priority = AssetLoadPriority.Normal);

        /// <summary>
        /// 移除某一个资源的加载任务，只能移除还未加载的任务，当前正在加载的任务不会被移除
        /// </summary>
        public abstract void RemoveLoadTask(string assetName);

        /// <summary>
        /// 移除某一个资源的加载任务中的一个回调，只能移除还未加载的任务中的回调
        /// </summary>
        public abstract void RemoveLoadRequest(AssetAsyncLoad asyncRequest);
    }
}