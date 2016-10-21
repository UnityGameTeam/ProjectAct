//******************************
//
// 模块名   : WrapChildrenFitter
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : 用于ListView的Item的设计类
//
//******************************
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UguiExtensions
{
    /// <summary>
    /// WrapChildrenFitter主要用于设计ListView的Item，主要有两个功能
    /// 1、根据配置，利用子孩子的信息来决定自身的宽和高，不对子孩子进行布局
    /// 2、根据配置，将自身的宽和高限制在父对象或者固定宽高的范围内
    /// </summary>
    [AddComponentMenu("Layout/Wrap Children Fitter", 141)]
    [ExecuteInEditMode]
    [RequireComponent(typeof (RectTransform))]
    public class WrapChildrenFitter : LayoutGroup, ILayoutSelfController
    {
        public enum FitMode
        {
            Unconstrained,
            AllChildrenTotalSize,
            MaxChildrenSize,
        }

        public enum SelfSizeRatioMode  //尺寸比例模式，当过高或过宽的时候可以选择，父对象或者固定尺寸的最大，最小的比例尺寸
        {
            Unconstrained,
            RelativeParent,
            FixedSize,
        }

        [SerializeField]
        protected RectOffset m_LimitPadding = new RectOffset();  //用于设置当自身被父对象或者固定距离限制时候的padding
        public RectOffset limitPadding { get { return m_LimitPadding; } set { SetProperty(ref m_LimitPadding, value); } }


        [SerializeField] protected FitMode m_HorizontalFit = FitMode.Unconstrained;
        public FitMode horizontalFit
        {
            get { return m_HorizontalFit; }
            set { if (SetPropertyUtility.SetStruct(ref m_HorizontalFit, value)) SetDirty(); }
        }

        [SerializeField] protected FitMode m_VerticalFit = FitMode.Unconstrained;
        public FitMode verticalFit
        {
            get { return m_VerticalFit; }
            set { if (SetPropertyUtility.SetStruct(ref m_VerticalFit, value)) SetDirty(); }
        }

        [SerializeField]
        protected SelfSizeRatioMode m_SelfSizeWidthRatio = SelfSizeRatioMode.Unconstrained;
        public SelfSizeRatioMode selfSizeWidthRatio
        {
            get { return m_SelfSizeWidthRatio; }
            set { if (SetPropertyUtility.SetStruct(ref m_SelfSizeWidthRatio, value)) SetDirty(); }
        }

        [SerializeField]
        protected SelfSizeRatioMode m_SelfSizeHeightRatio = SelfSizeRatioMode.Unconstrained;
        public SelfSizeRatioMode selfSizeHeightRatio
        {
            get { return m_SelfSizeHeightRatio; }
            set { if (SetPropertyUtility.SetStruct(ref m_SelfSizeHeightRatio, value)) SetDirty(); }
        }

        [SerializeField]
        protected Vector2 m_FixedSize;    //当m_ChildSizeRatio = ChildSizeRatioMode.FixedSize时候有效，指定固定宽和高(x和y)
        public Vector2 fixedSize
        {
            get { return m_FixedSize; }
            set { if (SetPropertyUtility.SetStruct(ref m_FixedSize, value)) SetDirty(); }
        }

        [SerializeField]
        protected float m_MaxWidthRatio = 1;      //子对象的最大宽度比例 0 到 1 取值
        public float maxWidthRatio
        {
            get { return m_MaxWidthRatio; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_MaxWidthRatio, value) && m_SelfSizeWidthRatio != SelfSizeRatioMode.Unconstrained)
                    SetDirty();

                m_MaxWidthRatio = Mathf.Max(m_MinWidthRatio, m_MaxWidthRatio);
            }
        }

        [SerializeField]
        protected float m_MinWidthRatio = 0;    //子对象的最小宽度比例 0 到 1 取值
        public float minWidthRatio
        {
            get { return m_MinWidthRatio; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_MinWidthRatio, value) && m_SelfSizeWidthRatio != SelfSizeRatioMode.Unconstrained)
                    SetDirty();

                m_MinWidthRatio = Mathf.Min(m_MinWidthRatio, m_MaxWidthRatio);
            }
        }

        [SerializeField]
        protected float m_MaxHeightRatio = 1;
        public float maxHeightRatio
        {
            get { return m_MaxHeightRatio; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_MaxHeightRatio, value) && m_SelfSizeHeightRatio != SelfSizeRatioMode.Unconstrained)
                    SetDirty();

                m_MaxHeightRatio = Mathf.Max(m_MinHeightRatio, m_MaxHeightRatio);
            }
        }

        [SerializeField]
        protected float m_MinHeightRatio = 0;
        public float minHeightRatio
        {
            get { return m_MinHeightRatio; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_MinHeightRatio, value) && m_SelfSizeHeightRatio != SelfSizeRatioMode.Unconstrained)
                    SetDirty();

                m_MinHeightRatio = Mathf.Min(m_MinHeightRatio, m_MaxHeightRatio);
            }
        }

        [SerializeField]
        private UnityEvent m_LayoutGroupComplete = new UnityEvent();
        public UnityEvent layoutGroupComplete { get { return m_LayoutGroupComplete; } set { m_LayoutGroupComplete = value; } }

        [SerializeField]
        protected bool m_NeedRecalculate;
        public bool needRecalculate
        {
            get { return m_NeedRecalculate; }
            set { m_NeedRecalculate = value; }
        }

        [NonSerialized] private bool m_ProcessLayoutVertical;

        protected WrapChildrenFitter() {}

        [NonSerialized] private int m_NeedRebuildCurrentFrame;
        private void HandleSelfFittingAndChildrenSizeAlongAxis(int axis)
        {
            FitMode fitting = (axis == 0 ? horizontalFit : verticalFit);
            if (fitting != FitMode.Unconstrained)
            {
               m_Tracker.Add(this, rectTransform,
               (axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY));
                rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, GetTotalPreferredSize(axis));
            }

            SelfSizeRatioMode sizeRatioMode = (axis == 0 ? selfSizeWidthRatio : selfSizeHeightRatio);
            if (sizeRatioMode != SelfSizeRatioMode.Unconstrained)
            {
                bool needRebuild = false;

                if (sizeRatioMode == SelfSizeRatioMode.RelativeParent && rectTransform.parent != null &&
                    rectTransform.parent is RectTransform)
                {
                    //如果是限制在父对象内部，则限制的区域为
                    //父对象的宽和高减去limitPadding的水平和竖直间隔范围内
                    float preferred = LayoutUtility.GetPreferredSize(rectTransform.parent as RectTransform, axis) - (axis == 0 ? limitPadding.horizontal : limitPadding.vertical);
                    preferred = Mathf.Max(0, preferred);
                    preferred = axis == 0
                        ? Mathf.Clamp(GetTotalPreferredSize(axis), m_MinWidthRatio*preferred, m_MaxWidthRatio*preferred)
                        : Mathf.Clamp(GetTotalPreferredSize(axis), m_MinHeightRatio*preferred, m_MaxHeightRatio*preferred);

                    m_Tracker.Add(this, rectTransform,
                            (axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY));

                    needRebuild = axis == 1 && (m_NeedRebuildCurrentFrame != Time.frameCount) && !Mathf.Approximately(preferred,GetTotalPreferredSize(1));

                    rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis) axis, preferred);
                    SetLayoutInputForAxis(preferred, preferred, -1, axis);
                }
                else if (sizeRatioMode == SelfSizeRatioMode.FixedSize)
                {
                    float preferred = (axis == 0 ? fixedSize.x: fixedSize.y) - (axis == 0 ? limitPadding.horizontal : limitPadding.vertical);
                    preferred = axis == 0
                                ? Mathf.Clamp(GetTotalPreferredSize(axis), m_MinWidthRatio * preferred, m_MaxWidthRatio * preferred)
                                : Mathf.Clamp(GetTotalPreferredSize(axis), m_MinHeightRatio * preferred, m_MaxHeightRatio * preferred);

                    m_Tracker.Add(this, rectTransform,
                            (axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY));

                    needRebuild = axis == 1 && (m_NeedRebuildCurrentFrame != Time.frameCount) && !Mathf.Approximately(preferred, GetTotalPreferredSize(1));

                    rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, preferred);
                    SetLayoutInputForAxis(preferred, preferred, -1, axis);
                }

                if (needRebuild)
                {               
                    //高度约束导致高度变化的情况下，需要重新计算布局，因为可能宽度也变了，但是宽度是提前算的，不重新算，会导致得到错误的值
                    m_NeedRebuildCurrentFrame = Time.frameCount;
                    LayoutRebuilderUtility.MarkLayoutForImmediateRebuild(rectTransform);
                    return;
                }
            }
        }

        public override void SetLayoutHorizontal()
        {
            HandleSelfFittingAndChildrenSizeAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            HandleSelfFittingAndChildrenSizeAlongAxis(1);
            if (m_ProcessLayoutVertical)
                return;

            //对于高度限制的Item的设计，不要立刻派发布局完成，可能导致改变了宽度，需要重新计算
            //一般不要设置这个属性，会导致两遍计算
            if (m_NeedRecalculate)
            {
                m_ProcessLayoutVertical = true;
                LayoutRebuilderUtility.ForceRebuildLayoutImmediate(rectTransform);
            }

            if (m_LayoutGroupComplete != null)
            {
                m_ProcessLayoutVertical = false;
                m_LayoutGroupComplete.Invoke();
            }
        }


        protected override void OnRectTransformDimensionsChange()
        {
            //防止ListView移动WrapChildrenFitter的时候又布局
        }

        protected void CalcAlongAxis(int axis)
        {
            float   combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical);
            float   size            = rectTransform.rect.size[axis];
            FitMode fitting         = (axis == 0 ? horizontalFit : verticalFit);
            float   totalPreferred  = combinedPadding;

            if (fitting == FitMode.Unconstrained)
            {
                //不约束的情况下，不能添加padding，否则会导致ListView在设置的时候，导致栈溢出
                SetLayoutInputForAxis(size, size, -1, axis);
                return;
            }

            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                float preferred = LayoutUtility.GetPreferredSize(child, axis);

                if (fitting == FitMode.MaxChildrenSize)
                {
                    totalPreferred = Mathf.Max(preferred + combinedPadding, totalPreferred);
                }
                else
                {
                    totalPreferred += preferred;
                }
            }
            SetLayoutInputForAxis(totalPreferred, totalPreferred, -1, axis);
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(0);
        }

        public override void CalculateLayoutInputVertical()
        {
            CalcAlongAxis(1); 
        }
    }
}
