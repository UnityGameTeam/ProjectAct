using GameLogic.LogicModules;
using UguiExtensions;
using UGCore;
using UGCore.Components;
using UnityEngine;

namespace GameLogic.PlatformSpecific
{
    public class PlatformMessageReceiver : MonoBehaviour
    {
        private PlatformMessageManageModule m_PlatformMsgManageModule;

        void Awake()
        {
            m_PlatformMsgManageModule = ModuleManager.Instance.GetGameModule(PlatformMessageManageModule.Name) as PlatformMessageManageModule;
        }

        #region LBS - 地图定位

        public void LBS_RegisterListenerError(string error)
        {
            LoggerManager.Error(error);
            m_PlatformMsgManageModule.TriggerPlatformMessage(PlatformMessageManageModule.LBS_RegisterListenerError,error);
        }

        public void LBS_StatusUpdate(string jsonResult)
        {
            LoggerManager.Info(jsonResult);
            m_PlatformMsgManageModule.TriggerPlatformMessage(PlatformMessageManageModule.LBS_StatusUpdate, jsonResult);
        }

        public void LBS_LocationChange(string jsonResult)
        {
            LoggerManager.Info(jsonResult);
            //定位如果成功了，一般可以停止定位
            m_PlatformMsgManageModule.TriggerPlatformMessage(PlatformMessageManageModule.LBS_LocationChange, jsonResult);
        }

        #endregion

        #region QRCode - 二维码扫描相关

        public void QRCode_ScanQRCodeResult(string result)
        {
            LoggerManager.Info(result);
            m_PlatformMsgManageModule.TriggerPlatformMessage(PlatformMessageManageModule.QRCode_ScanQRCodeResult, result);
        }

        #endregion

        #region 网络相关

        /// <summary>
        /// 当android端检查到网络变化的时候，会发送通知，networkName为网络连接类型名,有四种不同的类型名
        /// wifi : WiFi网络  
        /// ethernet : 有线网络
        /// mobile : 移动网络
        /// unavailable : 不可用的网络连接
        /// </summary>
        /// <param name="networkName"></param>
        public void Network_NetworkChanged(string networkName)
        {
            LoggerManager.Debug(networkName);
            m_PlatformMsgManageModule.TriggerPlatformMessage(PlatformMessageManageModule.Network_NetworkChanged, networkName);
        }

        #endregion

        #region 照片相关

        public void Photo_SelectPhotoDone(string path)
        {
            LoggerManager.Debug(path);
            m_PlatformMsgManageModule.TriggerPlatformMessage(PlatformMessageManageModule.Photo_SelectPhotoDone, path);
        }

        #endregion

        #region Poi搜索相关

        public void PoiSearch_SearchSuccess(string jsonResult)
        {
            LoggerManager.Debug(jsonResult);
            m_PlatformMsgManageModule.TriggerPlatformMessage(PlatformMessageManageModule.PoiSearch_SearchSuccess, jsonResult);
        }

        public void PoiSearch_SearchFailure(string errorInfo)
        {
            LoggerManager.Debug(errorInfo);
            m_PlatformMsgManageModule.TriggerPlatformMessage(PlatformMessageManageModule.PoiSearch_SearchFailure, errorInfo);
        }

        #endregion

        #region 半屏聊天

        public void HalfScreen_CommitInput(string text)
        {
            m_PlatformMsgManageModule.TriggerPlatformMessage(PlatformMessageManageModule.HalfScreen_CommitInput, text);          
        }

        public void HalfScreen_DialogHide(string result)
        {
            m_PlatformMsgManageModule.TriggerPlatformMessage(PlatformMessageManageModule.HalfScreen_DialogHide, result);      
        }

        #endregion
    }
}