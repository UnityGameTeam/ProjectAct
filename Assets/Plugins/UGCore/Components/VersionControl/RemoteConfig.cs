using System.Collections.Generic;

namespace UGCore.Components
{
    /// <summary>
    /// 保存Resources目录下ReadonlyConfig文件中所指向的远程json文件信息用于版本控制和游戏参数配置
    /// 
    /// DefaultConfigPath : 默认游戏版本所指向的配置路径（当游戏不符合特殊配置列表中时，使用此项）
    /// </summary>
    public class RemoteConfig
    {
        public string DefaultConfigPath;
        public List<SpecialConfig> SpecialConfigList;        //特殊版本配置，当包名和程序版本匹配时优先使用
        public Dictionary<string,string> PackageNameList;    //根据游戏的包名来匹配，优先级低于SpecialConfigList
        public Dictionary<string,string> ProgramVerisonList; //根据程序版本号来匹配，优先级低于PackageNameList
    }

    public class SpecialConfig
    {
        public string PackageName;
        public string ProgramVerison;
        public string ConfigPath;
    }

    /// <summary>
    /// 具体的远程控制配置
    /// </summary>
    public class RemoteControlConfig
    {
        public string ProgramVersion;        //程序版本
        public string ResourceVersion;       //资源版本
        public RemoteApkInfo ApkInfo;        //最新的程序安装包信息
        public bool IsPlatformUpdate;        //当前如果是要更新程序，是否使用运营平台的SDK来更新
        public string GameControlConfigPath; //游戏控制参数配置路径
        public string NoticeDataPath;        //游戏公告配置路径
        public string ServerInfoPath;        //服务器信息配置路径
        public string ServerGroupPath;       //服务器组信息配置路径
        public string PatchListPath;         //补丁信息配置路径
    }

    /// <summary>
    /// 远程程序安装包的信息
    /// </summary>
    public class RemoteApkInfo
    {
        public float  ApkSize; //程序安装包的大小(mb)
        public string ApkPath; //程序安装包的路径
    }

    /// <summary>
    /// 补丁信息配置
    /// </summary>
    public class PatchInfoConfig
    {
        public float  PatchSize; //补丁大小mb
        public string PatchPath; //补丁路径
    }
}