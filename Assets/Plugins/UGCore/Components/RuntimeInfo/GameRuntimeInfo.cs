namespace UGCore.Components
{
    public class GameRuntimeInfo
    {
        protected GameRuntimeInfo()
        {

        }

        //程序版本
        public static string ProgramVersion { get; set; }

        //资源版本
        public static string ResourceVersion { get; set; }

        //游戏是否正在运行
        public static bool IsRunning { get; set; }

        //游戏的远程配置信息,主要模块加载完成后赋值为null
        public static RemoteConfig RemoteConfigInfo { get; set; }

        //游戏的远程控制配置信息,主要模块加载完成后赋值为null
        public static RemoteControlConfig RemoteControlConfig { get; set; }
    }
}
