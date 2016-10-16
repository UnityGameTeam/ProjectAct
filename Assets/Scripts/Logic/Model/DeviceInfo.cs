using UGCore.Utility;
using UnityEngine;

namespace GameLogic.Model
{
    public class DeviceInfo
    {
        private static DeviceInfo _instance;
        public static DeviceInfo Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new DeviceInfo();
                }
                return _instance;
            }
        }

        private DeviceInfo()
        {

        }

        public string DeviceID
        {
            get { return SystemInfo.deviceUniqueIdentifier; }
        }

        public int SystemMemorySize
        {
            get { return SystemInfo.systemMemorySize; }
        }

        //Sim卡的运营商
        public string SimProvider
        {
            get
            {

#if UNITY_ANDROID && !UNITY_EDITOR
                return AndroidUtility.GetSimProvider();
#else
                return "unknown";
#endif
            }
        }

        //网络类型2g/3g/4g/wifi
        public string NetworkType
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return AndroidUtility.GetCurrentNetworkType();
#else
                return "unknown";
#endif
            }
        }
    }
}
