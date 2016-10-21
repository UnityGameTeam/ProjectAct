using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UGCore
{
    public class LoadingUI : MonoBehaviour
    {
        public enum ConfirmResultType
        {
            None,
            Ok,
            Cancel,
        }

        protected class LaodProgressTask
        {
            public float ProgressDelta;
            public float CurrentProgress;
        }

        public static LoadingUI Instance { get; set; }

        private Text m_LoadingBarTip;
        private Text m_HealthTip;
        private RectTransform m_LoadingBarTransform;
        private float m_LoadingBarWidth;

        private GameObject m_ConfirmPanel;
        private Text m_ConfirmPanelContent;

        private Button m_CancelButton;
        private Text m_CancelButtonText;
        private Button m_OkButton;
        private Text m_OkButtonText;

        private Stack<LaodProgressTask> m_loadTaskProgressDelta = new Stack<LaodProgressTask>();

        private ConfirmResultType m_ConfirmResult = ConfirmResultType.None;

        public ConfirmResultType ConfirmResult
        {
            get { return m_ConfirmResult; }
        }

        private LoadingUI()
        {
        }

        private void Awake()
        {
            Instance = this;
            m_LoadingBarTip = transform.Find("LoadingBar/LoadingBarTip").GetComponent<Text>();
            m_HealthTip = transform.Find("LoadingBottom/LoadingBottomTip").GetComponent<Text>();
            m_LoadingBarTransform = transform.Find("LoadingBar/LoadingBarFg") as RectTransform;

            m_HealthTip.text = LoadingLanguageData.Instance.GetString(2);
            m_LoadingBarTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);

            var loadingBarTransform = transform.Find("LoadingBar") as RectTransform;
            m_LoadingBarWidth = loadingBarTransform.rect.width;

            m_ConfirmPanel = transform.Find("ConfirmPanel").gameObject;
            m_ConfirmPanelContent = transform.Find("ConfirmPanel/Bg/Fg/Content").GetComponent<Text>();

            m_CancelButton = transform.Find("ConfirmPanel/Bg/CancelBtn").GetComponent<Button>();
            m_CancelButtonText = transform.Find("ConfirmPanel/Bg/CancelBtn/CancelBtnText").GetComponent<Text>();
            m_OkButton = transform.Find("ConfirmPanel/Bg/OkBtn").GetComponent<Button>();
            m_OkButtonText = transform.Find("ConfirmPanel/Bg/OkBtn/OkBtnText").GetComponent<Text>();
        }

        public void ShowLoadingTip(string tip)
        {
            m_LoadingBarTip.text = tip;
        }

        public void SetLoadingBarProgressDelta(float progress)
        {
            var progressTask = m_loadTaskProgressDelta.Peek();
            progressTask.CurrentProgress += progress * m_LoadingBarWidth * progressTask.ProgressDelta;
            m_LoadingBarTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Clamp(progressTask.CurrentProgress, 0, m_LoadingBarWidth));
        }

        public void SetLoadingBarProgress(float progress)
        {
            var progressTask = m_loadTaskProgressDelta.Peek();
            progressTask.CurrentProgress = progress * m_LoadingBarWidth * progressTask.ProgressDelta;
            m_LoadingBarTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Clamp(progressTask.CurrentProgress, 0, m_LoadingBarWidth));
        }

        public IEnumerator ShowConfirmPanel(string content, string cancelText, Action cancelAction, string okText, Action okAction,bool cancelClose = true,bool okClose = true)
        {
            m_ConfirmPanel.SetActive(false);
            m_ConfirmResult = ConfirmResultType.None;

            bool hasCancelBtn = true;
            if (string.IsNullOrEmpty(cancelText) || cancelAction == null)
            {
                hasCancelBtn = false;
                m_CancelButton.gameObject.SetActive(false);
            }
            else
            {
                m_CancelButton.gameObject.SetActive(true);
            }

            bool hasOkBtn = true;
            if (string.IsNullOrEmpty(okText) || okAction == null)
            {
                hasOkBtn = false;
                m_OkButton.gameObject.SetActive(false);
            }
            else
            { 
                m_OkButton.gameObject.SetActive(true);
            }

            if (hasCancelBtn && hasOkBtn)
            {
                m_CancelButton.transform.localPosition = new Vector3(-128.7f, -116.9f, 0);
                m_OkButton.transform.localPosition = new Vector3(128.7f, -116.9f, 0);
            }
            else
            {
                m_CancelButton.transform.localPosition = new Vector3(0, -116.9f, 0);
                m_OkButton.transform.localPosition = new Vector3(0, -116.9f, 0);
            }

            m_CancelButtonText.text = cancelText;
            m_OkButtonText.text = okText;
            m_ConfirmPanelContent.text = content == null ? "" : content;
            m_CancelButton.onClick.RemoveAllListeners();
            m_OkButton.onClick.RemoveAllListeners();

            m_CancelButton.onClick.AddListener(() =>
            {
                if (cancelAction != null)
                {
                    cancelAction();
                }

                if (cancelClose)
                {
                    m_ConfirmResult = ConfirmResultType.Cancel;
                    m_ConfirmPanel.SetActive(false);
                }
            });
            m_OkButton.onClick.AddListener(() =>
            {
                if (okAction != null)
                {
                    okAction();
                }

                if (okClose)
                {
                    m_ConfirmResult = ConfirmResultType.Ok;
                    m_ConfirmPanel.SetActive(false);
                }
            });
            m_ConfirmPanel.SetActive(true);

            while (m_ConfirmResult == ConfirmResultType.None)
            {
                yield return null;
            }
        }

        public void PushLoadTaskProgressDelta(float delta)
        {
            var progressTask = new LaodProgressTask();
            progressTask.CurrentProgress = 0;
            progressTask.ProgressDelta   = delta;
            m_loadTaskProgressDelta.Push(progressTask);
        }

        public void PopLoadTaskProgressDelta()
        {
            m_loadTaskProgressDelta.Pop();
        }
    }
}