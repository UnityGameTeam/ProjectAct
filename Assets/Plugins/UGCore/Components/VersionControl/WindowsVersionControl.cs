using System.Collections;
using UGFoundation.Utility;

namespace UGCore.Components
{
    public class WindowsVersionControl : VersionControl
    {
        public override IEnumerator CheckVersion()
        {
            var progressDelta = 1 / 4f;
            m_ShowTipCoroutine = GameCore.Instance.StartCoroutine(ShowCheckTip());

            //1 先检查本地版本信息,执行相关清理操作
            m_CoroutineWorkflow.AddLast("CheckLocalVersionInfo", CheckLocalVersionInfo);
            LoadingUI.Instance.SetLoadingBarProgressDelta(progressDelta);

            //2 检查远程配置信息
            m_CoroutineWorkflow.AddLast("CheckRemoteConfig", CheckRemoteConfig);
            LoadingUI.Instance.SetLoadingBarProgressDelta(progressDelta);

            //3 加载远程配置信息
            m_CoroutineWorkflow.AddLast("LoadRemoteControlConfig", LoadRemoteControlConfig);
            LoadingUI.Instance.SetLoadingBarProgressDelta(progressDelta);

            //4 检查资源版本
            m_CoroutineWorkflow.AddLast("CheckResourceVersion", CheckResourceVersion);
            LoadingUI.Instance.SetLoadingBarProgressDelta(progressDelta);

            yield return GameCore.Instance.StartCoroutine(m_CoroutineWorkflow.ExecuteTasksAsync());

            GameCore.Instance.StopCoroutine(m_ShowTipCoroutine);
            FileSystemUtility.ClearDirectory(UGCoreConfig.GetExternalDownloadFolder() + "/Patches");
            LoadingUI.Instance.SetLoadingBarProgressDelta(0.02f);
        }
    }
}
