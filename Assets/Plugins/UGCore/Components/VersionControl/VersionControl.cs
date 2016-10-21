using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Pathfinding.Serialization.JsonFx;
using UGCore.Utility;
using UGFoundation.Utility;
using UnityEngine;

namespace UGCore.Components
{
    public class VersionControl
    {
        protected enum VersionCheckState
        {
            Checking,
            WaitDownloadApk,
            DownloadApk,
            DownloadingApk,
            DownloadPatch,
            DownloadingPatch,
            UncompressPatch,
        }

        protected string m_RemoteConfigPath;
        protected CoroutineWorkflow m_CoroutineWorkflow = new CoroutineWorkflow();

        protected Dictionary<string, PatchInfoConfig> m_PatchesMap;
        protected List<string> m_PatchKeyList;

        protected float m_VersionCheckProgress = 0;
        protected VersionCheckState m_VersionCheckState = VersionCheckState.Checking;

        protected float m_NeedDownloadPathcesSize;
        protected int m_HasDownloadDoneCount;

        protected int m_DotNum = 3;
        protected Coroutine m_ShowTipCoroutine;

        public virtual IEnumerator CheckVersion()
        {
            yield return null;
        }

        protected void SetVersionCheckState(VersionCheckState state)
        {
            m_DotNum = 3;
            m_VersionCheckState = state;
            SetCheckTip();
        }

        protected IEnumerator ShowCheckTip()
        {
            while (true)
            {
                SetCheckTip();
                yield return new WaitForSeconds(0.5f);
            }
        }

        protected void SetCheckTip()
        {
            var suffix = new StringBuilder();
            for (int i = 0; i < m_DotNum; ++i)
            {
                suffix.Append(".");
            }

            if (m_VersionCheckState == VersionCheckState.Checking)
            {
                LoadingUI.Instance.ShowLoadingTip(LoadingLanguageData.Instance.GetString(3) + suffix);
            }
            else if (m_VersionCheckState == VersionCheckState.WaitDownloadApk)
            {
                LoadingUI.Instance.ShowLoadingTip(LoadingLanguageData.Instance.GetString(4) + suffix);
            }
            else if (m_VersionCheckState == VersionCheckState.DownloadApk)
            {
                LoadingUI.Instance.ShowLoadingTip(LoadingLanguageData.Instance.GetString(5) + suffix);
            }
            else if (m_VersionCheckState == VersionCheckState.DownloadPatch)
            {
                LoadingUI.Instance.ShowLoadingTip(LoadingLanguageData.Instance.GetString(6) + suffix);
            }

            --m_DotNum;
            if (m_DotNum < 1)
            {
                m_DotNum = 3;
            }
        }

        #region 本地版本检查

        protected IEnumerator CheckLocalVersionInfo()
        {
            //获取本地保存的程序和资源版本
            LoadVersionInfo();
            var localProgramVn = VersionNumber.ParseString(GameRuntimeInfo.ProgramVersion);
            var localResourceVn = VersionNumber.ParseString(GameRuntimeInfo.ResourceVersion);

            //获取打包时候配置的程序和资源版本信息
            var versionInfo = Resources.Load("ReadonlyData/VersionConfig") as TextAsset;
            var versionJsonObj = JsonReader.Deserialize<Dictionary<string, string>>(versionInfo.text);
            Resources.UnloadAsset(versionInfo);
            var readonlyProgramVersion = versionJsonObj[UGCoreConfig.ProgramVersion];
            var readonlyResourceVersion = versionJsonObj[UGCoreConfig.ResourceVersion];
            var readonlyProgramVn = VersionNumber.ParseString(readonlyProgramVersion);
            var readonlyResourceVn = VersionNumber.ParseString(readonlyResourceVersion);

            if (readonlyProgramVn == null || readonlyResourceVn == null)
            {
                Debug.LogError("version control - check error : check local version info error");
                m_CoroutineWorkflow.AddFirst("CheckLocalVersionInfo", CheckLocalVersionErrorNotify);
                yield break;
            }

            if (localProgramVn == null || localResourceVn == null)
            {
                SaveVersionInfo(readonlyProgramVersion, readonlyResourceVersion);
            }
            else
            {
                if (VersionNumber.CompareTo(readonlyProgramVn, localProgramVn) == 0)
                {
                    if (VersionNumber.CompareTo(readonlyResourceVn, localResourceVn) > 0)
                    {
                        SaveVersionInfo(readonlyProgramVersion, readonlyResourceVersion);
                    }
                }
                else
                {
                    SaveVersionInfo(readonlyProgramVersion, readonlyResourceVersion);
                }
            }
        }

        protected void LoadVersionInfo()
        {
            var versionFilePath = Path.Combine(UGCoreConfig.GetExternalConfigFolder(), "Version.config");
            if (!File.Exists(versionFilePath))
            {
                GameRuntimeInfo.ProgramVersion  = "";
                GameRuntimeInfo.ResourceVersion = "";
                return;
            }

            var versionText = File.ReadAllText(versionFilePath);
            try
            {
                var versionData = Convert.FromBase64String(versionText);
                var startIndex = 0;
                var endIndex = 0;
                for (int i = 0; i < versionData.Length; ++i)
                {
                    if (versionData[i] == 0)
                    {
                        endIndex = i;
                        break;
                    }
                }

                GameRuntimeInfo.ProgramVersion = Encoding.UTF8.GetString(versionData, startIndex, endIndex);
                GameRuntimeInfo.ResourceVersion = Encoding.UTF8.GetString(versionData, endIndex + 1, versionData.Length - endIndex - 1);
            }
            catch (Exception e)
            {
                Debug.LogError("version control - parse local version info error :" + e);
                GameRuntimeInfo.ProgramVersion = "";
                GameRuntimeInfo.ResourceVersion = "";
            }
        }

        protected void SaveVersionInfo(string programVersion, string resourceVersion, bool needClearResources = true)
        {
            GameRuntimeInfo.ProgramVersion = programVersion;
            GameRuntimeInfo.ResourceVersion = resourceVersion;

            var programData = Encoding.UTF8.GetBytes(programVersion);
            var resourceData = Encoding.UTF8.GetBytes(resourceVersion);

            var data = new byte[programData.Length + resourceData.Length + 1];

            for (int i = 0; i < programData.Length; i++)
            {
                data[i] = programData[i];
            }
            data[programData.Length] = 0;
            for (int i = 0; i < resourceData.Length; i++)
            {
                data[programData.Length + i + 1] = resourceData[i];
            }

            var versionFileDir = UGCoreConfig.GetExternalConfigFolder();
            var versionFilePath = Path.Combine(versionFileDir, "Version.config");
            if (!Directory.Exists(versionFileDir))
            {
                Directory.CreateDirectory(versionFileDir);
            }
            StreamWriter sw = new StreamWriter(new FileStream(versionFilePath, FileMode.Create));
            sw.Write(Convert.ToBase64String(data));
            sw.Flush();
            sw.Close();

            if (needClearResources)
            {
                FileSystemUtility.ClearDirectory(UGCoreConfig.GetExternalResourceFolder());
            }
        }

        protected IEnumerator CheckLocalVersionErrorNotify()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(7), LoadingLanguageData.Instance.GetString(10),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(9), () =>
                    {
                        m_CoroutineWorkflow.AddFirst("CheckLocalVersionInfo", CheckLocalVersionInfo);
                    }));
        }

        #endregion

        #region 检查远程配置

        protected virtual IEnumerator CheckRemoteConfig()
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

        protected IEnumerator LoadRemoteErrorNotify()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(7), LoadingLanguageData.Instance.GetString(10),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(9), () =>
                    {
                        m_CoroutineWorkflow.AddFirst("CheckRemoteConfig", CheckRemoteConfig);
                    }));
        }

        protected IEnumerator ParseRemoteErrorNotify()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(7), LoadingLanguageData.Instance.GetString(10),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(9), () =>
                    {
                        m_CoroutineWorkflow.AddFirst("CheckRemoteConfig", CheckRemoteConfig);
                    }));
        }

        #endregion

        #region 加载远程配置

        protected IEnumerator LoadRemoteControlConfig()
        {
            WWW www = new WWW(UrlUtility.GetRandomParametersUrl(m_RemoteConfigPath));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("version control - download remote config error:" + www.error);
                m_CoroutineWorkflow.AddFirst("DownloadRemoteControlErrorNotify", DownloadRemoteControlErrorNotify);
            }
            else
            {
                var parseExcepetion = false;
                try
                {
                    GameRuntimeInfo.RemoteControlConfig = JsonReader.Deserialize<RemoteControlConfig>(www.text);
                }
                catch (Exception e)
                {
                    parseExcepetion = true;
                    Debug.LogError("version control - parse remote config error:" + e);
                }

                if (parseExcepetion)
                {
                    m_CoroutineWorkflow.AddFirst("ParseRemoteControlErrorNotify", ParseRemoteControlErrorNotify);
                    yield break;
                }
            }
        }

        protected IEnumerator DownloadRemoteControlErrorNotify()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(7), LoadingLanguageData.Instance.GetString(10),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(9), () =>
                    {
                        m_CoroutineWorkflow.AddFirst("LoadRemoteControlConfig", LoadRemoteControlConfig);
                    }));
        }

        protected IEnumerator ParseRemoteControlErrorNotify()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(7), LoadingLanguageData.Instance.GetString(10),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(9), () =>
                    {
                        m_CoroutineWorkflow.AddFirst("LoadRemoteControlConfig", LoadRemoteControlConfig);
                    }));
        }

        #endregion

        #region 检查资源版本

        protected IEnumerator CheckResourceVersion()
        {
            var localResourceVn = VersionNumber.ParseString(GameRuntimeInfo.ResourceVersion);
            var remoteResourceVn = VersionNumber.ParseString(GameRuntimeInfo.RemoteControlConfig.ResourceVersion);

            if (VersionNumber.CompareTo(remoteResourceVn, localResourceVn) > 0)
            {
                var www = new WWW(UrlUtility.GetRandomParametersUrl(GameRuntimeInfo.RemoteControlConfig.PatchListPath));
                yield return www;

                if (www.error != null)
                {
                    Debug.LogError("version control - download patch list failed:" + www.error);
                    m_CoroutineWorkflow.AddFirst("ShowDownloadPatchListFail", DownloadPatchListErrorNotify);
                    yield break;
                }
                else
                {
                    var parseExcepetion = false;
                    try
                    {
                        m_PatchesMap = JsonReader.Deserialize<Dictionary<string, PatchInfoConfig>>(www.text);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("version control - parse patch list error:" + e);
                        parseExcepetion = true;
                    }

                    if (parseExcepetion)
                    {
                        m_CoroutineWorkflow.AddFirst("ParsePatchListErrorNotify", ParsePatchListErrorNotify);
                    }
                    else
                    {
                        m_CoroutineWorkflow.AddFirst("CheckPatchList", CheckPatchList);
                    }
                    yield break;
                }
            }
        }

        protected IEnumerator DownloadPatchListErrorNotify()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(7), LoadingLanguageData.Instance.GetString(10),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(9), () =>
                    {
                        m_CoroutineWorkflow.AddFirst("CheckResourceVersion", CheckResourceVersion);
                    }));
        }

        protected IEnumerator ParsePatchListErrorNotify()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(7), LoadingLanguageData.Instance.GetString(10),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(9), () =>
                    {
                        m_CoroutineWorkflow.AddFirst("CheckResourceVersion", CheckResourceVersion);
                    }));
        }

        #endregion

        #region 检查补丁下载

        protected IEnumerator CheckPatchList()
        {
            var localResourceVn = VersionNumber.ParseString(GameRuntimeInfo.ResourceVersion);
            m_PatchKeyList = new List<string>();

            var enumerator = m_PatchesMap.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var index = enumerator.Current.Key.IndexOf("-");
                var versionNumber = VersionNumber.ParseString(enumerator.Current.Key.Substring(0, index));
                if (VersionNumber.CompareTo(versionNumber, localResourceVn) >= 0)
                {
                    m_NeedDownloadPathcesSize += enumerator.Current.Value.PatchSize;
                    m_PatchKeyList.Add(enumerator.Current.Key);
                }
            }

            m_PatchKeyList.Sort((obj1, obj2) =>
            {
                var index = obj1.IndexOf("-");
                var vn1 = VersionNumber.ParseString(obj1.Substring(0, index));

                index = obj2.IndexOf("-");
                var vn2 = VersionNumber.ParseString(obj2.Substring(0, index));

                return VersionNumber.CompareTo(vn1, vn2);
            });

            if (m_PatchKeyList.Count > 0)
            {
                SetVersionCheckState(VersionCheckState.DownloadPatch);
                m_CoroutineWorkflow.AddFirst("QueryDownlaodPatches", QueryDownlaodPatches);
            }
            yield break;
        }

        protected IEnumerator QueryDownlaodPatches()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(string.Format(LoadingLanguageData.Instance.GetString(13), m_NeedDownloadPathcesSize),
                    LoadingLanguageData.Instance.GetString(11),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(12),
                    () =>
                    {
                        m_HasDownloadDoneCount = 0;
                        m_CoroutineWorkflow.AddFirst("DownloadPatches", DownloadPatches);
                    }));
        }

        #endregion

        #region 下载补丁

        protected IEnumerator DownloadPatches()
        {
            m_VersionCheckState = VersionCheckState.DownloadingPatch;
            for (int i = m_HasDownloadDoneCount; i < m_PatchKeyList.Count; ++i)
            {
                var result = 0;
                var downloadProgress = 0f;
                var url = m_PatchesMap[m_PatchKeyList[i]].PatchPath;
                var downloadDir = UGCoreConfig.GetExternalDownloadFolder() + "/Patches";
                var localPath = Path.Combine(downloadDir, m_PatchKeyList[i]);
                if (!Directory.Exists(downloadDir))
                {
                    Directory.CreateDirectory(downloadDir);
                }

                LoadingUI.Instance.ShowLoadingTip(string.Format(LoadingLanguageData.Instance.GetString(14), (i + 1), m_PatchKeyList.Count, 0));
                LoadingUI.Instance.PushLoadTaskProgressDelta(1);
                LoadingUI.Instance.SetLoadingBarProgress(0);
                HttpDownloadUtility.DownloadFileAsync(url, localPath, (progress) =>
                {
                    downloadProgress = progress;
                },
                    () =>
                    {
                        ++m_HasDownloadDoneCount;
                        result = 1;
                    },
                    () =>
                    {
                        result = 2;
                    });

                while (result == 0)
                {
                    LoadingUI.Instance.ShowLoadingTip(string.Format(LoadingLanguageData.Instance.GetString(14), (i + 1),
                        m_PatchKeyList.Count, (int)downloadProgress));
                    LoadingUI.Instance.SetLoadingBarProgress(downloadProgress * 0.01f);
                    yield return null;
                }

                if (result == 1)
                {
                    LoadingUI.Instance.ShowLoadingTip(string.Format(LoadingLanguageData.Instance.GetString(14), (i + 1),
                        m_PatchKeyList.Count, 100));
                    LoadingUI.Instance.SetLoadingBarProgress(1);
                    LoadingUI.Instance.PopLoadTaskProgressDelta();
                }
                else if (result == 2)
                {
                    m_CoroutineWorkflow.AddFirst("ShowDownlaodPatchesError", ShowDownlaodPatchesError);
                    yield break;
                }
                yield return null;
            }

            m_HasDownloadDoneCount = 0;
            m_CoroutineWorkflow.AddFirst("UncompressPatches", UncompressPatches);
        }

        protected IEnumerator ShowDownlaodPatchesError()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(15),
                    LoadingLanguageData.Instance.GetString(10),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(9),
                    () =>
                    {
                        m_CoroutineWorkflow.AddFirst("DownloadPatches", DownloadPatches);
                    }));
        }

        #endregion

        #region 解压补丁

        protected IEnumerator UncompressPatches()
        {
            m_VersionCheckState = VersionCheckState.UncompressPatch;

            for (int i = m_HasDownloadDoneCount; i < m_PatchKeyList.Count; ++i)
            {
                var result = 0;
                var downloadProgress = 0f;
                var localPath = Path.Combine(UGCoreConfig.GetExternalDownloadFolder() + "/Patches", m_PatchKeyList[i]);

                LoadingUI.Instance.ShowLoadingTip(string.Format(LoadingLanguageData.Instance.GetString(16), (i + 1), m_PatchKeyList.Count, 0));
                LoadingUI.Instance.PushLoadTaskProgressDelta(1);
                LoadingUI.Instance.SetLoadingBarProgress(0);

                ZipUtility.UnzipDirectoryAsync(localPath, UGCoreConfig.GetExternalResourceFolder(),
                    (progress) =>
                    {
                        downloadProgress = progress;
                    },
                    () =>
                    {
                        ++m_HasDownloadDoneCount;
                        result = 1;
                    },
                    () =>
                    {
                        result = 2;
                    });

                while (result == 0)
                {
                    LoadingUI.Instance.ShowLoadingTip(string.Format(LoadingLanguageData.Instance.GetString(16), (i + 1),
                        m_PatchKeyList.Count, (int)downloadProgress));
                    LoadingUI.Instance.SetLoadingBarProgress(downloadProgress * 0.01f);
                    yield return null;
                }

                if (result == 1)
                {
                    LoadingUI.Instance.ShowLoadingTip(string.Format(LoadingLanguageData.Instance.GetString(16), (i + 1),
                        m_PatchKeyList.Count, 100));
                    LoadingUI.Instance.SetLoadingBarProgress(1);
                    LoadingUI.Instance.PopLoadTaskProgressDelta();
                }
                else if (result == 2)
                {
                    m_CoroutineWorkflow.AddFirst("ShowUncompressPatchesError", ShowUncompressPatchesError);
                    yield break;
                }
                yield return null;
            }

            //保存资源版本号
            GameRuntimeInfo.ResourceVersion = GameRuntimeInfo.RemoteControlConfig.ResourceVersion;
            SaveVersionInfo(GameRuntimeInfo.ProgramVersion, GameRuntimeInfo.ResourceVersion, false);
        }

        protected IEnumerator ShowUncompressPatchesError()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                ShowConfirmPanel(LoadingLanguageData.Instance.GetString(17),
                    LoadingLanguageData.Instance.GetString(10),
                    () =>
                    {
                        GameUtility.QuitGame();
                    },
                    LoadingLanguageData.Instance.GetString(9),
                    () =>
                    {
                        m_CoroutineWorkflow.AddFirst("UncompressPatches", UncompressPatches);
                    }));
        }

        #endregion
    }
}
