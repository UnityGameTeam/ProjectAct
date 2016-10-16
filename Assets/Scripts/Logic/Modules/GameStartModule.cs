using System.Collections;
using GameLogic.PlatformSpecific;
using UGCore;
using UGCore.Components;

namespace GameLogic.LogicModules
{
    public class GameStartModule : GameModule
    {
        public override IEnumerator LoadModuleAsync()
        {
            GameCore.Instance.gameObject.AddComponent<PlatformMessageReceiver>();

            //AndroidUtility.RequestLocation(); 地图定位

            LoadingUI.Instance.PopLoadTaskProgressDelta();
            LoadingUI.Instance.PushLoadTaskProgressDelta(1);
            LoadingUI.Instance.SetLoadingBarProgress(1);
            LoadingUI.Instance.PopLoadTaskProgressDelta();

            GameRuntimeInfo.RemoteConfigInfo = null;
            GameRuntimeInfo.RemoteControlConfig = null;

            yield return GameCore.Instance.StartCoroutine(SceneManager.Instance.LoadSceneAsync("TestBall"));


            yield break;
        }
    }
}
