//******************************
//
// 模块名   : ListView
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : 具体的ListView实现
//
//******************************

using System;
using UnityEngine;
using UnityEngine.UI;

namespace UguiExtensions
{
    /// <summary>
    /// 具体的ListView实现,派生自AbstractListView，主要对Item
    /// 布局相关进行处理，集成一些常用的策划功能，比如箭头，类
    /// 梦幻西游聊天的锁定功能
    /// </summary>
    public class ListView : AbstractListView
    {
        [NonSerialized] protected bool m_NotifyUpOrLeftArrow;
        [NonSerialized] protected bool m_NotifyBottomOrRightArrow;
        [NonSerialized] protected bool m_NotifyCanLock;
        [NonSerialized] protected bool m_OldNotifyUpOrLeftArrow;
        [NonSerialized] protected bool m_OldNotifyBottomOrRightArrow;
        [NonSerialized] protected bool m_OldNotifyCanLock;

        [NonSerialized] protected bool m_ImmediateScrollToTop;
        [NonSerialized] protected int m_ScrollToTopFrameCount;

        [SerializeField] protected ScrollUnityEvent m_UpOrLeftArrow;

        public ScrollUnityEvent upOrLeftArrow
        {
            get { return m_UpOrLeftArrow; }
            set { m_UpOrLeftArrow = value; }
        }

        [SerializeField] protected ScrollUnityEvent m_BottomOrRightArrow;

        public ScrollUnityEvent bottomOrRightArrow
        {
            get { return m_BottomOrRightArrow; }
            set { m_BottomOrRightArrow = value; }
        }

        [SerializeField] protected ScrollUnityEvent m_CanLock;

        public ScrollUnityEvent canLock
        {
            get { return m_CanLock; }
            set { m_CanLock = value; }
        }

        protected bool SetChildrenAlongAxis(bool isVertical)
        {
            for (int axis = 0; axis < 2; ++axis)
            {
                float size = rectTransform.rect.size[axis];
                float startOffset = -m_TopStartPosition;
                bool alongOtherAxis = (isVertical ^ (axis == 1));
                if (alongOtherAxis)
                {
                    float innerSize = size;
                    startOffset = 0;
                    for (int i = 0; i < itemViewChildren.Count; i++)
                    {
                        var child = itemViewChildren[i].ItemRectTransform;
                        var itemSize = LayoutUtility.GetPreferredSize(child, axis);
                        SetChildItemAlongAxis(child, axis, startOffset, innerSize);

                        if (!Mathf.Approximately(itemSize, innerSize))
                        {
                            LayoutRebuilderUtility.ForceRebuildLayoutImmediate(child);
                            return true;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < itemViewChildren.Count; i++)
                    {
                        var child = itemViewChildren[i].ItemRectTransform;
                        var itemSize = LayoutUtility.GetPreferredSize(child, axis);
                        SetChildItemAlongAxis(child, axis, startOffset, itemSize);
                        startOffset += itemSize + m_Divider;
                    }
                }
            }
            return false;
        }

        protected void SetChildItemAlongAxis(RectTransform rect, int axis, float pos, float size)
        {
            if (rect == null)
                return;

            m_Tracker.Add(this, rect,
                DrivenTransformProperties.Anchors |
                DrivenTransformProperties.AnchoredPosition |
                //DrivenTransformProperties.SizeDelta |    //不能控制这个属性，可能导致子对象的WrapChildrenFitter包裹获取到错误的值，原因未知
                DrivenTransformProperties.Pivot);

            rect.pivot = new Vector2(0, 1);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.SetInsetAndSizeFromParentEdge(axis == 0 ? RectTransform.Edge.Left : RectTransform.Edge.Top, pos, size);
        }

        public override void ChildrenLayoutGroupComplete()
        {
            m_Tracker.Clear();
            if (m_FillItemUp)
            {
                var itemSize = LayoutUtility.GetPreferredSize(itemViewChildren[0].ItemRectTransform, isVerticalLayout ? 1 : 0);
                if (!Mathf.Approximately(itemSize,0))  //如果第一个等于0，很可能是布局中才添加的还没计算布局，这时等待重新计算布局，不修改m_FillItemUp的属性
                {
                    m_FillItemUp = false;
                }
                m_TopStartPosition += itemSize + m_Divider;
            }
 
            if (m_ImmediateScrollToTop && m_FirstPosition == 0 && m_MotionEvent == MotionEvent.ActionNone && m_TopStartPosition < 0)
            {
                m_TopStartPosition = 0;
            }

            if (!SetChildrenAlongAxis(isVerticalLayout))
            {
                CheckFillItemViewInLayoutComplete();
                if (m_MotionEvent == MotionEvent.ActionNone && !m_ImmediateScrollToTop)
                {
                    m_MotionEvent = MotionEvent.ActionScroll;
                }
                CheckArrowAndLock();
            }
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (m_OldNotifyUpOrLeftArrow != m_NotifyUpOrLeftArrow)
            {
                m_NotifyUpOrLeftArrow = m_OldNotifyUpOrLeftArrow;
                m_UpOrLeftArrow.Invoke(m_NotifyUpOrLeftArrow);
            }

            if (m_OldNotifyBottomOrRightArrow != m_NotifyBottomOrRightArrow)
            {
                m_NotifyBottomOrRightArrow = m_OldNotifyBottomOrRightArrow;
                m_BottomOrRightArrow.Invoke(m_NotifyBottomOrRightArrow);
            }

            if (m_OldNotifyCanLock != m_NotifyCanLock)
            {
                m_NotifyCanLock = m_OldNotifyCanLock;
                m_CanLock.Invoke(m_NotifyCanLock);
            }

            if (m_ScrollToTopFrameCount != Time.frameCount)
            {
                m_ImmediateScrollToTop = false;
            }
        }

        public override void SetAdapter(IAdapter adapter)
        {
            base.SetAdapter(adapter);

            m_UpOrLeftArrow.Invoke(false);
            m_BottomOrRightArrow.Invoke(false);
            m_CanLock.Invoke(false);

            m_NotifyUpOrLeftArrow = false;
            m_NotifyBottomOrRightArrow = false;
            m_NotifyCanLock = false;

            m_OldNotifyUpOrLeftArrow = false;
            m_OldNotifyBottomOrRightArrow = false;
            m_OldNotifyCanLock = false;
        }

        protected override void CheckArrowAndLock()
        {
            if (itemViewChildren.Count == 0)
            {
                m_UpOrLeftArrow.Invoke(false);
                m_BottomOrRightArrow.Invoke(false);
                m_CanLock.Invoke(false);
                return;
            }

            int axis = isVerticalLayout ? 1 : 0;
            int deltaDir = isVerticalLayout ? 1 : -1;

            if (m_FirstPosition != 0)
            {
                m_OldNotifyUpOrLeftArrow = true;
                m_OldNotifyCanLock = true;
            }
            else
            {
                if (GetChildItemViewTop(0, axis)*deltaDir > GetViewRectTop(axis) + 0.01f)
                {
                    m_OldNotifyUpOrLeftArrow = true;
                    m_OldNotifyCanLock = true;
                    if (
                        !CheckAllItemOverViewSize(GetChildItemViewTop(0, axis),
                            GetChildItemViewBottom(GetShowItemCount() - 1, axis), axis))
                    {
                        m_OldNotifyCanLock = false;
                    }
                }
                else
                {
                    m_OldNotifyUpOrLeftArrow = false;
                    m_OldNotifyCanLock = false;
                }
            }

            if (m_FirstPosition + GetShowItemCount() < m_Adapter.GetCount())
            {
                m_OldNotifyBottomOrRightArrow = true;
            }
            else
            {
                if (GetChildItemViewBottom(GetShowItemCount() - 1, axis)*deltaDir > GetViewRectBottom(axis) - 0.01f)
                {
                    m_OldNotifyBottomOrRightArrow = false;
                }
                else
                {
                    m_OldNotifyBottomOrRightArrow = true;
                }
            }
        }

        //设置布局的时候，如果第0个高度低于ListView上部，如果不是滚动模式下，可以立即滚动到最上方
        public void SetImmediateScrollToTop(bool isStop)
        {
            m_ImmediateScrollToTop = isStop;
            m_ScrollToTopFrameCount = Time.frameCount;
        }
    }
}

