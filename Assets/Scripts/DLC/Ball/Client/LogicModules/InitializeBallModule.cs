using System.Collections;
using DLC.Ball.Config;
using GameLogic.Components;
using UGCore;
using UnityEngine;

namespace GameLogic.LogicModules
{
    public class InitializeBallModule : GameModule
    {
        public override IEnumerator LoadModuleAsync()
        {
            //这里可以考虑分帧加载，暂时性能影响不大,使用ActionScheduler
            DynamicRenderData.Instance.CreateMeshCache();

            //将球球的本地配置保存到本地存储模块中
            LocalStorage.Instance.AddLocalData(BallConfig.BallLocalStorageConfig);
            if (Application.platform != RuntimePlatform.Android)
            {
                QualitySettings.antiAliasing = BallConfig.BallQuality << 2;
            }

            yield break;
        }
    }
}
