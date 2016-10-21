using UGCore.Components;

namespace UGCore.Modules
{
    public class LogManageModule : GameModule
    {
        protected void Awake()
        {
#if !UNITY_WEBPLAYER
            LoggerManager.AddLogger("FileLogger", new FileLogger());
#endif
        }

        protected override void OnApplicationQuit()
        {
            LoggerManager.Release();
        }
    }
}