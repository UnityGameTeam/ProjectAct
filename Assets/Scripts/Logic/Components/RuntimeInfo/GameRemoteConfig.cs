using System.Collections.Generic;

namespace GameLogic.Components
{
    /// <summary>
    /// 远程游戏控制配置
    /// </summary>
    public class RemoteGameControlConfig
    {
        public bool IsUseRemoteConfig; //是否使用远程配置信息来配置游戏参数，如果不是，使用本地配置来控制游戏参数
        public bool UseAssetBundle;    //游戏是否使用AssetBundle资源和管理方式
        public string LogOutputLevel;  //日志的输出等级
    }

    public enum ServerState : int
    {
        Hot = 1,      //火爆
        Normal = 2,   //正常
        Close = 3,    //关闭
        Maintain = 4, //维护
        Recommend = 5 //推荐
    }

    public class ServerInfo
    {
        public int Id;        //服务器id
        public string Name;   //服务器名称
        public int Flag;      //服务器当前状态标记
        public string IP;     //服务器IP地址
        public int Port;      //服务器端口地址
        public string Detail; //服务器当前状态描述
        public int GroupId;   //服务器所属组id
    }

    public class ServerGroup
    {
        public int GroupId;            //服务器组id
        public string GroupName;       //服务器组名称
        public int SortFlag;           //服务器组排序标记
        public List<int> TimeZoneList; //服务器时区列表,可以时区列表是否包含当前时区来获取服务器组
    }

    public class GameNoticeData
    {
        public int Id;         //游戏公告id
        public string Title;   //游戏公告标题
        public string Content; //游戏公告内容
        public string Date;    //游戏公告时间
        public bool IsNew;     //是否是新的公告
    }
}