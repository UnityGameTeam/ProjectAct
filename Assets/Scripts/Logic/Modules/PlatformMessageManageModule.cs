using System;
using System.Collections;
using System.Collections.Generic;
using UGCore;

namespace GameLogic.LogicModules
{
    public class PlatformMessageManageModule : GameModule
    {
        public const string Name = "PlatformMessageManageModule";

        public const string LBS_RegisterListenerError = "LBS_RegisterListenerError";
        public const string LBS_StatusUpdate = "LBS_StatusUpdate";
        public const string LBS_LocationChange = "LBS_LocationChange";
        public const string QRCode_ScanQRCodeResult = "QRCode_ScanQRCodeResult";
        public const string Network_NetworkChanged = "Network_NetworkChanged";
        public const string Photo_SelectPhotoDone = "LBS_LocationChange";
        public const string PoiSearch_SearchSuccess = "QRCode_ScanQRCodeResult";
        public const string PoiSearch_SearchFailure = "Network_NetworkChanged";
        public const string HalfScreen_CommitInput = "HalfScreen_CommitInput";
        public const string HalfScreen_DialogHide = "HalfScreen_DialogHide";

        private Dictionary<string, Action<string>> m_PlatformMessageMap;

        public override IEnumerator LoadModuleAsync()
        {
            m_PlatformMessageMap = new Dictionary<string, Action<string>>();
            yield break;
        }

        public void AddPlatformMessageListener(string msg, Action<string> callback)
        {
            if (m_PlatformMessageMap.ContainsKey(msg))
            {
                m_PlatformMessageMap[msg] += callback;
            }
            else
            {
                m_PlatformMessageMap.Add(msg, callback);
            }
        }

        public void RemovePlatformMessageListener(string msg, Action<string> callback)
        {
            if (m_PlatformMessageMap.ContainsKey(msg))
            {
                m_PlatformMessageMap[msg] -= callback;
            }
        }

        public void TriggerPlatformMessage(string msg, string param)
        {
            if (m_PlatformMessageMap.ContainsKey(msg) && m_PlatformMessageMap[msg] != null)
            {
                m_PlatformMessageMap[msg](param);
            }
        }
    }
}