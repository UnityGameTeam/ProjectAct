using UnityEngine;

namespace GameLogic.Components
{
    public abstract class UIView
    {
        protected GameObject m_UIRootObj;
        protected bool       m_IsLoadingUI;
        protected bool       m_ActiveUIAfterLoadDone;
        protected Bundle     m_WaitProcessBundle;

        protected virtual bool ShowLoadingTip { get { return true; }}
        public abstract string PrefabPath     { get; }

        public GameObject UIRootObj           { get { return m_UIRootObj; } }
        public virtual bool ActiveSelf        { get { return m_UIRootObj.activeSelf; } }
        public virtual bool ActiveInHierarchy { get { return m_UIRootObj.activeInHierarchy; } }
        public virtual string LayerName       { get { return UILayer.NormalLayer.ToString(); } }

        protected virtual void InitUI()
        {
            
        }

        protected virtual void OnEnable()
        {

        }

        public virtual void Update()
        {

        }

        public virtual void LateUpdate()
        {

        }

        protected virtual void OnDisable()
        {

        }

        protected virtual void OnDestroy()
        {
            
        }

        /// <summary>
        /// 如果bundle不需要保存下来，最好Cache起来，Bundle.CacheBundle(bundle);
        /// </summary>
        protected virtual void ProcessIntent(Bundle bundle)
        {
            
        }

        public virtual void OpenUI(Bundle bundle, bool needActive)
        {
            if (m_UIRootObj == null)
            {
                m_ActiveUIAfterLoadDone = needActive;
                if (!m_IsLoadingUI)
                {
                    m_IsLoadingUI = true;
                    if (ShowLoadingTip && needActive)
                    {
                        UIManager.Instance.ShowLoadingTipUI();
                    }
                    AssetManager.Instance.LoadAssetAsync(PrefabPath, LoadUIDone);
                }

                ReplaceBundle(bundle);
            }
            else
            {
                if (!ActiveSelf)
                {
                    m_UIRootObj.SetActive(true);
                    m_UIRootObj.transform.SetAsLastSibling();
                    OnEnable();
                }
                ProcessIntent(bundle);
            } 
        }

        public virtual void CloseUI(Bundle bundle)
        {
            if (m_UIRootObj == null)
            {
                if (m_IsLoadingUI)
                {
                    m_ActiveUIAfterLoadDone = false;
                }

                if (ShowLoadingTip)
                {
                    UIManager.Instance.HideLoadingTipUI();
                }
            }
            else
            {
                if (ActiveSelf)
                {
                    m_UIRootObj.SetActive(false);
                    OnDisable();
                }
                ProcessIntent(bundle);
            }        
        }

        protected virtual void LoadUIDone(Object uiObj)
        {
            if (ShowLoadingTip)
            {
                UIManager.Instance.HideLoadingTipUI();
            }

            if (uiObj == null)
            {
                //资源加载已经会报错了，这里就不报错了
                return;
            }

            m_UIRootObj = Object.Instantiate(uiObj) as GameObject;
            m_UIRootObj.transform.SetParent(UIManager.Instance.GetLayerRootTransform(LayerName));
            m_UIRootObj.transform.localRotation = Quaternion.identity;
            m_UIRootObj.transform.localScale = Vector3.one;
            var rectTransform = m_UIRootObj.transform as RectTransform;
            rectTransform.pivot = new Vector2(0.5f,0.5f);
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;

            InitUI();

            m_UIRootObj.SetActive(m_ActiveUIAfterLoadDone);
            if (m_ActiveUIAfterLoadDone)
            {
                m_UIRootObj.transform.SetAsLastSibling();
                OnEnable();
                ProcessIntent(m_WaitProcessBundle);
            }
        }

        protected void ReplaceBundle(Bundle bundle)
        {
            if (m_WaitProcessBundle == null)
            {
                m_WaitProcessBundle = bundle;
                return;
            }

            if (bundle != null)
            {
                Bundle.CacheBundle(m_WaitProcessBundle);
                m_WaitProcessBundle = bundle;
            }                 
        }
    }
}
