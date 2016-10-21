using System;
using System.Collections;
using UGCore.Components;
using UGCore.Utility;
using UnityEngine;

namespace UGCore.Modules
{
    public class DeviceLaunchModule : GameModule
    {
        protected Action m_ExitAction;

        public override IEnumerator LoadModuleAsync()
        {
            GameRuntimeInfo.IsRunning = true;
            yield return StartCoroutine(CheckNetwork());
        }

        protected void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                if (m_ExitAction == null)
                {
                    AndroidUtility.ShowExitDialog(LoadingLanguageData.Instance.GetString(26),
                        LoadingLanguageData.Instance.GetString(27), LoadingLanguageData.Instance.GetString(12),
                        LoadingLanguageData.Instance.GetString(11));
                }
                else
                {
                    m_ExitAction();
                }
            }
        }

        protected override void OnApplicationQuit()
        {
            GameRuntimeInfo.IsRunning = false;
        }

        protected override void OnApplicationPause()
        {
            AndroidUtility.HideExitDialog();
        }

        public void SetExitAction(Action exitAction)
        {
            m_ExitAction = exitAction;
        }

        protected IEnumerator CheckNetwork()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    yield return StartCoroutine(LoadingUI.Instance.
                        ShowConfirmPanel(LoadingLanguageData.Instance.GetString(18),
                            LoadingLanguageData.Instance.GetString(19),
                            () =>
                            {
                                AndroidUtility.GotoNetworkSetting();
                            },
                            LoadingLanguageData.Instance.GetString(20),
                            () =>
                            {

                            },false));
                }
                else
                {
                    yield return StartCoroutine(LoadingUI.Instance.
                        ShowConfirmPanel(LoadingLanguageData.Instance.GetString(18),
                            LoadingLanguageData.Instance.GetString(10),
                            () =>
                            {
                                GameUtility.QuitGame();
                            },
                            LoadingLanguageData.Instance.GetString(20),
                            () =>
                            {
                                
                            }));
                }
            }
        }
    }
}