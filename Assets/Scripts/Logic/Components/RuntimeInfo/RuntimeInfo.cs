using System.Collections.Generic;
using UGCore.Components;
using UnityEngine;

namespace GameLogic.Components
{
    public class RuntimeInfo : GameRuntimeInfo
    {
        public static string DeviceOS
        {
            get
            {
                if (RuntimePlatform.Android == Application.platform)
                {
                    return "Android";
                }
                if (RuntimePlatform.WebGLPlayer == Application.platform)
                {
                    return "Web";
                }
                if (RuntimePlatform.WindowsPlayer == Application.platform)
                {
                    return "Windows";
                }
                if (RuntimePlatform.IPhonePlayer == Application.platform)
                {
                    return "IOS";
                }
                return "PC";
            }
        }

        //服务器信息列表
        public static List<ServerInfo> ServerInfoList { get; set; }

        //服务器组信息列表
        public static List<ServerGroup> ServerGroupList { get; set; }

        //游戏通知信息列表
        public static List<GameNoticeData> GameNoticeList { get; set; }
    }
}