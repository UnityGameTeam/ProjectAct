using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameLogic.Components;
using GameLogic.Modules;
using Pathfinding.Serialization.JsonFx;
using UGCore;
using UGCore.Components;
using UGCore.Utility;
using UGFoundation.Utility;

namespace GameLogic.LogicModules
{
    public class GameStartConfigModule : GameModule
    {
        protected CoroutineWorkflow m_CoroutineWorkflow = new CoroutineWorkflow();

        public override IEnumerator LoadModuleAsync()
        {
            //1 下载服务器信息
            m_CoroutineWorkflow.AddLast("DownloadServeInfo", DownloadServeInfo);
            LoadingUI.Instance.SetLoadingBarProgressDelta(0.25f);

            //2 下载服务器组信息
            m_CoroutineWorkflow.AddLast("DownloadServerGroupData", DownloadServerGroupData);
            LoadingUI.Instance.SetLoadingBarProgressDelta(0.25f);

            //3 下载游戏公告信息
            m_CoroutineWorkflow.AddLast("DownloadNotifyData", DownloadNotifyData);
            LoadingUI.Instance.SetLoadingBarProgressDelta(0.25f);

            //4 下载游戏控制参数信息
            m_CoroutineWorkflow.AddLast("DownloadGameControlInfo", DownloadGameControlInfo);
            LoadingUI.Instance.SetLoadingBarProgressDelta(0.25f);

            yield return GameCore.Instance.StartCoroutine(m_CoroutineWorkflow.ExecuteTasksAsync());

            SetRuntimeConfigInfo();
        }

        #region 下载服务器信息

        protected IEnumerator DownloadServeInfo()
        {
            var www = new WWW(UrlUtility.GetRandomParametersUrl(RuntimeInfo.RemoteControlConfig.ServerInfoPath));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("GameStartConfig - download serverInfo list failed:" + www.error);
                m_CoroutineWorkflow.AddFirst("DownloadServerInfoError", DownloadServerInfoError);
                yield break;
            }
            else
            {
                var parseExcepetion = false;
                try
                {
                    RuntimeInfo.ServerInfoList = JsonReader.Deserialize<List<ServerInfo>>(www.text);
                }
                catch (Exception e)
                {
                    Debug.LogError("GameStartConfig - parse server info list error:" + e);
                    parseExcepetion = true;
                }

                if (parseExcepetion)
                {
                    m_CoroutineWorkflow.AddFirst("DownloadServerInfoError", DownloadServerInfoError);
                    yield break;
                }
            }
        }

        protected IEnumerator DownloadServerInfoError()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(21), LoadingLanguageData.Instance.GetString(10), () =>
                {
                    GameUtility.QuitGame();
                },
                LoadingLanguageData.Instance.GetString(9), () =>
                {
                    m_CoroutineWorkflow.AddFirst("DownloadServeInfo", DownloadServeInfo);
                }));
        }

        protected IEnumerator DownloadServerGroupData()
        {
            var www = new WWW(UrlUtility.GetRandomParametersUrl(RuntimeInfo.RemoteControlConfig.ServerGroupPath));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("GameStartConfig - download server group data list failed:" + www.error);
                m_CoroutineWorkflow.AddFirst("DownloadServerGroupDataError", DownloadServerGroupDataError);
                yield break;
            }
            else
            {
                var parseExcepetion = false;
                try
                {
                    RuntimeInfo.ServerGroupList = JsonReader.Deserialize<List<ServerGroup>>(www.text);
                }
                catch (Exception e)
                {
                    Debug.LogError("GameStartConfig - parse server group data list error:" + e);
                    parseExcepetion = true;
                }

                if (parseExcepetion)
                {
                    m_CoroutineWorkflow.AddFirst("DownloadServerGroupDataError", DownloadServerGroupDataError);
                    yield break;
                }
            }
        }

        protected IEnumerator DownloadServerGroupDataError()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(21), LoadingLanguageData.Instance.GetString(10), () =>
                {
                    GameUtility.QuitGame();
                },
                LoadingLanguageData.Instance.GetString(9), () =>
                {
                    m_CoroutineWorkflow.AddFirst("DownloadServerGroupData", DownloadServerGroupData);
                }));
        }

        #endregion

        #region 下载游戏公告信息

        protected IEnumerator DownloadNotifyData()
        {
            var www = new WWW(UrlUtility.GetRandomParametersUrl(RuntimeInfo.RemoteControlConfig.NoticeDataPath));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("GameStartConfig - download notice data list failed:" + www.error);
                m_CoroutineWorkflow.AddFirst("DownloadNotifyDataError", DownloadNotifyDataError);
            }
            else
            {
                var parseExcepetion = false;
                try
                {
                    RuntimeInfo.GameNoticeList = JsonReader.Deserialize<List<GameNoticeData>>(www.text);
                }
                catch (Exception e)
                {
                    Debug.LogError("GameStartConfig - parse notice data list error:" + e);
                    parseExcepetion = true;
                }

                if (parseExcepetion)
                {
                    m_CoroutineWorkflow.AddFirst("DownloadNotifyDataError", DownloadNotifyDataError);
                }
            }
        }

        protected IEnumerator DownloadNotifyDataError()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(21), LoadingLanguageData.Instance.GetString(10), () =>
                {
                    GameUtility.QuitGame();
                },
                LoadingLanguageData.Instance.GetString(9), () =>
                {
                    m_CoroutineWorkflow.AddFirst("DownloadNotifyData", DownloadNotifyData);
                }));
        }

        #endregion

        #region 下载游戏控制配置信息

        protected IEnumerator DownloadGameControlInfo()
        {
            var www = new WWW(UrlUtility.GetRandomParametersUrl(RuntimeInfo.RemoteControlConfig.GameControlConfigPath));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("GameStartConfig - download control info list failed:" + www.error);
                m_CoroutineWorkflow.AddFirst("DownloadGameControlInfoError", DownloadGameControlInfoError);
                yield break;
            }
            else
            {
                var parseExcepetion = false;
                try
                {
                    SetGameControlInfo(www.text);
                }
                catch (Exception e)
                {
                    Debug.LogError("GameStartConfig - parse control info list error:" + e);
                    parseExcepetion = true;
                }

                if (parseExcepetion)
                {
                    m_CoroutineWorkflow.AddFirst("DownloadGameControlInfoError", DownloadGameControlInfoError);
                    yield break;
                }
            }
        }

        protected void SetGameControlInfo(string text)
        {
            var controlConfig = JsonReader.Deserialize<RemoteGameControlConfig>(text);
            if (!controlConfig.IsUseRemoteConfig)
            {
                var localControlAsset = Resources.Load("ReadonlyData/LogicReadonlyConfig") as TextAsset;
                controlConfig = JsonReader.Deserialize<RemoteGameControlConfig>(localControlAsset.text);
                Resources.UnloadAsset(localControlAsset);
            }

            AssetLoadModule.UseAssetBundle = controlConfig.UseAssetBundle;

            LoggerManager.CurrentLogLevels = LogLevel.None;
            var logLevelNames = Enum.GetNames(typeof (LogLevel));
            var logLevelConfigNames = controlConfig.LogOutputLevel.Split('|');
            for (int i = 0; i < logLevelConfigNames.Length; ++i)
            {
                for (int j = 0; j < logLevelNames.Length; ++j)
                {
                    if (logLevelConfigNames[i] == logLevelNames[j])
                    {
                        LoggerManager.CurrentLogLevels |= (LogLevel)Enum.Parse(typeof (LogLevel), logLevelNames[j]);
                        break;
                    }
                }
            }
        }

        protected IEnumerator DownloadGameControlInfoError()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(21), LoadingLanguageData.Instance.GetString(10), () =>
                {
                    GameUtility.QuitGame();
                },
                LoadingLanguageData.Instance.GetString(9), () =>
                {
                    m_CoroutineWorkflow.AddFirst("DownloadGameControlInfo", DownloadGameControlInfo);
                }));
        }

        #endregion

        #region 设置运行信息

        protected void SetRuntimeConfigInfo()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                //设置目标的帧速率和屏幕不休眠
                Application.targetFrameRate = 60;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                AndroidUtility.KeepScreenNeverSleep();
            }
        }

        #endregion
    }
}