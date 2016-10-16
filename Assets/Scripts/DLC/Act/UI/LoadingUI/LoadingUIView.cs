using DG.Tweening;
using GameLogic.Components;
using UnityEngine;

namespace Game.UI
{
    public class LoadingUIView : UIView
    {
        public override string LayerName { get { return UILayer.HighLayer.ToString(); } }
        protected override bool ShowLoadingTip { get { return false; } }

        public override string PrefabPath
        {
            get { return "DLC/Act/UI/LoadingUI/LoadingUI"; }
        }

        private Transform m_DotAnchor;
        private Tweener   m_DotTweener;

        protected override void InitUI()
        {
            m_DotAnchor = m_UIRootObj.transform.FindChild("DotAnchor");
            m_DotTweener = m_DotAnchor.DORotate(new Vector3(0, 0, -360), 1.5f, RotateMode.FastBeyond360);
            m_DotTweener.SetLoops(-1);
            m_DotTweener.SetEase(Ease.Linear);
        }

        protected override void OnEnable()
        {
            m_DotTweener.Play();
        }

        protected override void OnDisable()
        {
            m_DotTweener.Pause();
        }
    }
}
