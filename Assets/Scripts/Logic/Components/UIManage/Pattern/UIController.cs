namespace GameLogic.Components
{
    public abstract class UIController
    {
        protected UIView  m_UIView;
        protected UIModel m_UIModel;

        public virtual bool EnableUpdate     { get; protected set; }
        public virtual bool EnableLateUpdate { get; protected set; }
        public virtual bool AutoRelease      { get; protected set; }                        //能否被UI管理器自动释放,释放暂时没有处理
        public virtual bool UILoadDone       { get { return m_UIView.UIRootObj != null; } }
        public virtual bool UIActive         { get { return m_UIView.UIRootObj != null && m_UIView.ActiveInHierarchy; } }

        public virtual void Update()
        {
            m_UIView.Update();
        }

        public virtual void LateUpdate()
        {
            m_UIView.LateUpdate();
        }

        public virtual void OpenUI(Bundle bundle, bool needActive = true)
        {
            m_UIView.OpenUI(bundle, needActive);
        }

        public virtual void CloseUI(Bundle bundle)
        {
            m_UIView.CloseUI(bundle);
        }
    }
}
