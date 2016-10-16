using System.Collections;
using GameLogic.Components;
using UGCore;
using UnityEngine;

namespace GameLogic.Modules
{
    public class AssetLoadModule : GameModule
    {
        public static bool UseAssetBundle { get; set; }

        public override IEnumerator LoadModuleAsync()
        {
            if (UseAssetBundle)
            {
                var www = new WWW(UGCoreConfig.GetAssetBundlePath("AssetsDependences.pkg"));
                yield return www;

                var assetbundleRequest = www.assetBundle.LoadAssetAsync("Assets/Resources/AssetsDependences",typeof(Object));
                yield return assetbundleRequest;

                var assetDependences = assetbundleRequest.asset as TextAsset;
                if (assetDependences != null)
                {
                    AssetBundleManager.InitializeAssetManager(assetDependences.bytes);
                }
                else
                {
                    Debug.LogError("Dont have asset dependences config");
                }

                www.assetBundle.Unload(true);
            }
            else
            {
                ResourcesAssetManager.InitializeAssetManager();
            }

            TimerHelper.AddTimer((uint)AssetLoadConfig.CheckAssetReferenceDeltaTime, AssetLoadConfig.CheckAssetReferenceDeltaTime, AssetManager.Instance.CheckExpiredAssets);
            LoadingUI.Instance.SetLoadingBarProgressDelta(1);
        }
    }
}
