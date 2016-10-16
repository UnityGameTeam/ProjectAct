using UnityEngine;
using UnityEngine.Events;

namespace GameLogic.Components
{
    public class AssetAsyncLoad
    {
        public string Name    { get; protected set; }

        public AssetAsyncLoad(string name)
        {
            Name = name;
  
        }
    }

    /// <summary>
    /// 优先级从高到低排列
    /// </summary>
    public enum AssetLoadPriority
    {
        High,
        Medium,
        Normal,
        Low,
    }

    public abstract class AssetManager
    {        
        protected static AssetManager _instance;
        public static AssetManager Instance
        {
            get { return _instance; }           
        }

        /// <summary>
        /// 同步加载Asset,使用和异步加载相同的接口，方便上层代码的修改
        /// releaseAssetBundle用于资源加载使用AssetBundle的时候，是不是
        /// 在资源加载出来之后，立即释放对应的AssetBundle，默认情况下是
        /// </summary>
        public abstract void LoadAssetSync(string assetName, UnityAction<Object> loadedCallback);
        public abstract AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, AssetLoadPriority priority = AssetLoadPriority.Normal);
        public abstract AssetAsyncLoad LoadAssetAsync(string assetName, UnityAction<Object> loadedCallback, UnityAction<float> progressCallback, AssetLoadPriority priority = AssetLoadPriority.Normal);

        /// <summary>
        /// AddAssetReference 增加资源的引用次数
        ///    一般程序中，如果获取了一个资源，比如GameObject的Instantiate
        ///    或者使用了非临时变量引用了资源，都应该增加对资源的引用计数
        /// 
        /// AddAssetReference一般应该和SubAssetReference成对使用，类似C++的new/delete
        /// 
        /// 默认情况下，资源管理模块会定时检查缓存的资源是不是在一段时间中没有被访问过
        /// 如果一段时间内没有被访问，将会自动的把资源的引用次数降1，防止因为上次逻辑
        /// 误用引用计数，比如只增加引用计数，没有减少引用计数，导致资源一直被引用，无法
        /// 释放
        /// 
        /// 推荐的做法应该在需要增加引用次数时调用AddAssetReference，释放引用的时候减少
        /// 引用次数调用SubAssetReference，两者配合使用，可以有效控制资源的及时回收，避免
        /// 超时检查可能带来的资源扎堆释放
        /// </summary>
        public abstract void AddAssetReference(string assetName);
        public abstract void SubAssetReference(string assetName);

        /// <summary>
        /// 移除某一个资源的加载任务，只能移除还未加载的任务，当前真正加载的任务不会被移除
        /// </summary>
        public abstract void RemoveLoadTask(string assetName);

        /// <summary>
        /// 移除某一个资源的加载任务中的一个回调，只能移除还未加载的任务中的回调
        /// </summary>
        public abstract void RemoveLoadRequest(AssetAsyncLoad asyncRequest);

        public abstract Object ContainAsset(string assetName);

        public abstract void UnloadUnusedAssets();

        /// <summary>
        /// 需要外部定时调用，以检查过期资源以便回收
        /// </summary>
        public abstract void CheckExpiredAssets();

        /// <summary>
        /// 立即释放保存了assetName的AssetBundle，谨慎使用，也会导致加载出来的asset被释放，不管资源是否在使用
        /// </summary>
        public abstract void ReleaseAssetBundle(string assetName);
    }
}
