using System;
using System.Collections;
using System.IO;
using Pathfinding.Serialization.JsonFx;
using UGCore.Utility;
using UGFoundation.Utility;
using UnityEngine;

namespace UGCore.Components
{
    public class AndroidVersionControl : VersionControl
    {
        public override IEnumerator CheckVersion()
        {
            var progressDelta = 1/5f;
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

            //4 检查Apk下载
            m_CoroutineWorkflow.AddLast("CheckProgramVersion", CheckProgramVersion);
            LoadingUI.Instance.SetLoadingBarProgressDelta(progressDelta);

            //5 检查资源版本
            m_CoroutineWorkflow.AddLast("CheckResourceVersion", CheckResourceVersion);
            LoadingUI.Instance.SetLoadingBarProgressDelta(progressDelta);

            yield return GameCore.Instance.StartCoroutine(m_CoroutineWorkflow.ExecuteTasksAsync());

            GameCore.Instance.StopCoroutine(m_ShowTipCoroutine);
            FileSystemUtility.ClearDirectory(UGCoreConfig.GetExternalDownloadFolder() + "/Patches");
            FileSystemUtility.ClearDirectory(UGCoreConfig.GetExternalDownloadFolder() + "/Apk");
            LoadingUI.Instance.SetLoadingBarProgressDelta(0.04f);
        }

        #region 检查远程配置

        protected override IEnumerator CheckRemoteConfig()
        {
            //获取ReadonlyConfig.bytes中配置的url路径,得到远程配置信息
            var readonlyConfig = Resources.Load("ReadonlyData/ReadonlyConfig") as TextAsset;
            var readonlyContent = readonlyConfig.text;
            Resources.UnloadAsset(readonlyConfig);
            WWW www = new WWW(UrlUtility.GetRandomParametersUrl(readonlyContent));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("version control - download remote config error:" + www.error);
                m_CoroutineWorkflow.AddFirst("LoadRemoteErrorNotify", LoadRemoteErrorNotify);
                yield break;
            }
            else
            {
                var parseExcepetion = false;
                try
                {
                    GameRuntimeInfo.RemoteConfigInfo = JsonReader.Deserialize<RemoteConfig>(www.text);
                    m_RemoteConfigPath = GameRuntimeInfo.RemoteConfigInfo.DefaultConfigPath;

                    var isMatch = false;
                    var packageName = AndroidUtility.GetPackageName();
                    for (int i = 0; i < GameRuntimeInfo.RemoteConfigInfo.SpecialConfigList.Count; ++i)
                    {
                        if (GameRuntimeInfo.RemoteConfigInfo.SpecialConfigList[i].PackageName == packageName && GameRuntimeInfo.RemoteConfigInfo.SpecialConfigList[i].ProgramVerison == GameRuntimeInfo.ProgramVersion)
                        {
                            isMatch = true;
                            m_RemoteConfigPath = GameRuntimeInfo.RemoteConfigInfo.SpecialConfigList[i].ConfigPath;
                            break;
                        }
                    }

                    if (!isMatch)
                    {
                        var enumerator = GameRuntimeInfo.RemoteConfigInfo.PackageNameList.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current.Key == packageName)
                            {
                                isMatch = true;
                                m_RemoteConfigPath = enumerator.Current.Value;
                                break;
                            }
                        }
                    }

                    if (!isMatch)
                    {
                        var enumerator = GameRuntimeInfo.RemoteConfigInfo.ProgramVerisonList.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current.Key == GameRuntimeInfo.ProgramVersion)
                            {
                                m_RemoteConfigPath = enumerator.Current.Value;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    parseExcepetion = true;
                    Debug.LogError("version control - parse remote config error:" + e);
                }

                if (parseExcepetion)
                {
                    m_CoroutineWorkflow.AddFirst("ParseRemoteErrorNotify", ParseRemoteErrorNotify);
                }
            }
        }

        #endregion

        #region 检查程序版本

        protected IEnumerator CheckProgramVersion()
        {
            var localProgramVn = VersionNumber.ParseString(GameRuntimeInfo.ProgramVersion);
            var remoteProgramVn = VersionNumber.ParseString(GameRuntimeInfo.RemoteControlConfig.ProgramVersion);

            if (VersionNumber.CompareTo(remoteProgramVn, localProgramVn) > 0)
            {
                if (GameRuntimeInfo.RemoteControlConfig.IsPlatformUpdate)
                {
                    while (true)
                    {
                        SetVersionCheckState(VersionCheckState.WaitDownloadApk);
                        yield return null;
                    }
                }
                else
                {
                    SetVersionCheckState(VersionCheckState.DownloadApk);
                    m_CoroutineWorkflow.AddFirst("QueryDownlaodApk", QueryDownlaodApk);
                    yield break;
                }
            }
        }

        protected IEnumerator QueryDownlaodApk()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(
                    string.Format(LoadingLanguageData.Instance.GetString(22), GameRuntimeInfo.RemoteControlConfig.ApkInfo.ApkSize), LoadingLanguageData.Instance.GetString(11),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(12), () =>
                    {
                        m_VersionCheckState = VersionCheckState.DownloadingApk;
                        m_CoroutineWorkflow.AddFirst("DownloadApk", DownloadApk);
                    }));
        }

        protected IEnumerator DownloadApk()
        {
            var result = 0;
            var downloadProgress = 0f;
            var apkDir = UGCoreConfig.GetExternalDownloadFolder() + "/Apk/";
            var apkPath = apkDir + Path.GetFileName(GameRuntimeInfo.RemoteControlConfig.ApkInfo.ApkPath);
            if (!Directory.Exists(apkDir))
            {
                Directory.CreateDirectory(apkDir);
            }

            LoadingUI.Instance.ShowLoadingTip(string.Format(LoadingLanguageData.Instance.GetString(23), 0));
            LoadingUI.Instance.PushLoadTaskProgressDelta(1);
            LoadingUI.Instance.SetLoadingBarProgress(0);
            HttpDownloadUtility.DownloadFileAsync(GameRuntimeInfo.RemoteControlConfig.ApkInfo.ApkPath, apkPath,
                (progress) =>
                {
                    downloadProgress = progress;
                },
                () =>
                {
                    result = 1;
                },
                () =>
                {
                    result = 2;
                });

            while (result == 0)
            {
                LoadingUI.Instance.ShowLoadingTip(string.Format(LoadingLanguageData.Instance.GetString(23), (int)downloadProgress));
                LoadingUI.Instance.SetLoadingBarProgress(downloadProgress * 0.01f);
                yield return null;
            }

            if (result == 1)
            {
                LoadingUI.Instance.ShowLoadingTip(string.Format(LoadingLanguageData.Instance.GetString(23), 100));
                LoadingUI.Instance.SetLoadingBarProgress(1);
                LoadingUI.Instance.PopLoadTaskProgressDelta();
                
                AndroidUtility.InstallApk(apkPath, true);
            }
            else if (result == 2)
            {
                m_CoroutineWorkflow.AddFirst("DownloadApkError", DownloadApkError);
                yield break;
            }
            yield return null;
        }

        protected IEnumerator DownloadApkError()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(
                    LoadingLanguageData.Instance.GetString(24), LoadingLanguageData.Instance.GetString(10),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(9),
                    () =>
                    {
                        m_CoroutineWorkflow.AddFirst("DownloadApk", DownloadApk);
                    }));
        }

        #endregion

    }
}