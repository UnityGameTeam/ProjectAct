using System;
using System.Collections.Generic;
using Game.UI;
using UnityEngine;

namespace GameLogic.Components
{
    public enum UILayer
    {
        LowLayer,
        MiddleLayer,
        NormalLayer,
        HighLayer,
        TopLayer,
    }

    //LoadingTipUI 添加超时功能
    //互斥处理，考虑做成配表的处理，现在先不处理了
    //释放先不处理
    //动画处理等
    public class UIManager
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UIManager();
                }
                return _instance;
            }
        }

        protected Dictionary<string,Transform>    m_UILayers = new Dictionary<string, Transform>();
        protected Dictionary<string,UIController> m_UIControllerMap = new Dictionary<string, UIController>();
        protected IEnumerator<UIController>       m_UIControllerMapEnumerator;

        protected UIManager()
        {
            m_UIControllerMapEnumerator = m_UIControllerMap.Values.GetEnumerator();
        }

        public static void Initialize(Transform canvasTransform)
        {
            if (_instance == null)
            {
                _instance = new UIManager();     
            }

            _instance.m_UILayers.Clear();
            for (int i = 0; i < canvasTransform.childCount; ++i)
            {
                var childTransform = canvasTransform.GetChild(i);
                _instance.m_UILayers.Add(childTransform.name, childTransform);
            }
        }

        /// <summary>
        /// 外部调用,支持上层ui的更新需求
        /// </summary>
        public void Update()
        {
            m_UIControllerMapEnumerator.Reset();
            while (m_UIControllerMapEnumerator.MoveNext())
            {
                var uiController = m_UIControllerMapEnumerator.Current;
                if (uiController != null && uiController.EnableUpdate && uiController.UIActive)
                {
                    uiController.Update();
                }
            }
        }

        /// <summary>
        /// 外部调用，支持上层ui的更新需求
        /// </summary>
        public void LateUpdate()
        {
            m_UIControllerMapEnumerator.Reset();
            while (m_UIControllerMapEnumerator.MoveNext())
            {
                var uiController = m_UIControllerMapEnumerator.Current;
                if (uiController != null && uiController.EnableLateUpdate && uiController.UIActive)
                {
                    uiController.LateUpdate();
                }
            }
        }

        public UIController OpenUI(string uiName, Bundle bundle = null, bool needShow = true)
        {
            if (!m_UIControllerMap.ContainsKey(uiName))
            {
                var uiController = GetType().Assembly.CreateInstance(String.Format("Game.UI.{0}", uiName)) as UIController;
                if (uiController == null)
                {
                    Debug.LogError("无法加载UIController : " + uiName);
                    return null;
                }
                m_UIControllerMap.Add(uiName, uiController);
                m_UIControllerMapEnumerator = m_UIControllerMap.Values.GetEnumerator();
            }
            m_UIControllerMap[uiName].OpenUI(bundle, needShow);
            return m_UIControllerMap[uiName];
        }
 
        public void CloseUI(string uiName, Bundle bundle = null)
        {
            if (m_UIControllerMap.ContainsKey(uiName))
            {
                m_UIControllerMap[uiName].CloseUI(bundle);
            }
        }

        public Transform GetLayerRootTransform(string layerName)
        {
            return m_UILayers[layerName];
        }

        public UIController GetUIController(string uiName, bool create = true)
        {
            if (m_UIControllerMap.ContainsKey(uiName))
            {
                return m_UIControllerMap[uiName];
            }

            if (create)
            {
                var uiController =
                    GetType().Assembly.CreateInstance(String.Format("Game.UI.{0}", uiName)) as UIController;
                return uiController;
            }
            return null;
        }

        public void ShowLoadingTipUI()
        {
            if (GetUIController(typeof(LoadingUIController).Name,false) != null)
            {
                OpenUI(typeof(LoadingUIController).Name);
            }
        }

        public void HideLoadingTipUI()
        {
            if (GetUIController(typeof (LoadingUIController).Name, false) != null)
            {
                CloseUI(typeof (LoadingUIController).Name);
            }
        }

        public void ShowMessage(string msg)
        {
            var bundle = Bundle.GetBundle();
            bundle.PutString("ShowMsg", msg);
            OpenUI(typeof (MessageUIController).Name, bundle);
        }
    }
}
