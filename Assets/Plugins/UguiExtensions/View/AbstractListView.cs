//******************************
//
// 模块名   : AbstractListView
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : 抽象ListView相关的功能实现
//
//******************************
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UguiExtensions
{
    /// <summary>
    /// 抽象ListView相关的功能实现,主要包括两大功能
    /// 1、根据事件进行滑动处理
    /// 2、Item缓存处理
    /// 
    /// 子类应该派生自该类，主要对布局进行处理，比如
    /// 具体的ListView,GridView的处理
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public abstract class AbstractListView : UIBehaviour, IScrollHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public enum MovementType
        {
            Unrestricted,           // Unrestricted movement -- can scroll forever
            Elastic,                // Restricted but flexible -- can go past the edges, but springs back in place
            Clamped,                // Restricted movement where it's not possible to go past the edges
            ClampedForDistance,     // 允许移动超过边缘一段距离
        }

        public enum MotionEvent
        {
            ActionNone,
            ActionDown,
            ActionMove,
            ActionUp,
            ActionScroll,
            ActionFling,
        }

        [Serializable]
        public class ScrollUnityEvent : UnityEvent<bool>
        {
            
        }

        [SerializeField]
        protected bool m_AutoDragOrScrollView;
        public bool AutoDragOrScrollView  //当所有子item超过一屏幕的时候是否可以接受滚动和拖拽，如果为true,当所有item不满一屏的时候，不接收事件，超过一屏的时候接收事件
        {
            get { return m_AutoDragOrScrollView; }
            set { m_AutoDragOrScrollView = value; }
        }

        [SerializeField]
        protected bool m_EnableScrollView = true;
        public bool enableScrollView
        {
            get { return m_EnableScrollView; }
            set { m_EnableScrollView = value; }
        }

        [SerializeField]
        protected bool m_EnableDragView = true;
        public bool enableDragView
        {
            get { return m_EnableDragView; }
            set { m_EnableDragView = value; }
        }

        [SerializeField]
        private float m_ScrollSensitivity = 60;
        public float scrollSensitivity
        {
            get { return m_ScrollSensitivity; }
            set { m_ScrollSensitivity = value; }
        }

        [SerializeField]
        protected bool m_IsVerticalLayout = true;
        public bool isVerticalLayout
        {
            get { return m_IsVerticalLayout; }
            set
            {
                m_IsVerticalLayout = value;
            }
        }

        [SerializeField]
        protected float m_Divider;
        public float divider
        {
            get { return m_Divider; }
            set
            {
                m_Divider = value;
                SetDirty();
            }
        }

        [NonSerialized]
        private RectTransform m_Rect;
        protected RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        [SerializeField]
        protected float m_ClampedDistanceRatio = 0.35f;
        public float ClampedDistanceRatio
        {
            get { return m_ClampedDistanceRatio; }
            set { m_ClampedDistanceRatio = value; }
        }

        [SerializeField]
        protected float m_ScrollSpeed = 575;
        public float scrollSpeed
        {
            get { return m_ScrollSpeed; }
            set { m_ScrollSpeed = value; }
        }

        [SerializeField]
        protected MovementType m_MovementType =  MovementType.ClampedForDistance;//默认使用ClampedForDistance，目前MovementType.Elastic的滑动模式不太理想，后续优化
        public MovementType movementType
        {
            get { return m_MovementType; }
            set { m_MovementType = value; }
        }

        [NonSerialized]
        private List<IItemView> m_ItemViewChildren = new List<IItemView>();
        protected List<IItemView> itemViewChildren
        {
            get { return m_ItemViewChildren; }
        }

        [NonSerialized] protected MotionEvent m_MotionEvent = MotionEvent.ActionNone;
        [NonSerialized] private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        [NonSerialized] protected int m_FirstPosition;
        [NonSerialized] private bool m_FirstFling;
        [NonSerialized] private bool m_IsDragging;
        [NonSerialized] protected IAdapter m_Adapter;
        [NonSerialized] protected RecycleBin m_RecycleBin;
        [NonSerialized] protected DrivenRectTransformTracker m_Tracker;
        [NonSerialized] protected float m_TopStartPosition;
        [NonSerialized] protected float m_BottomEndPosition;
        [NonSerialized] protected bool m_ItemViewNeedLayout;
        [NonSerialized] protected bool m_FillItemUp;
        [NonSerialized] protected VelocityTracker m_VelocityTracker;
     
        //用于控制多点触摸，只有最新的拖拽才响应，比如多根手指一起拖动的问题
        [NonSerialized] protected HashSet<PointerEventData> m_DragEventMap = new HashSet<PointerEventData>();
        [NonSerialized] protected PointerEventData m_CurrentDragEvent;

        [NonSerialized] protected float m_Velocity;
        [NonSerialized] protected bool m_FlingDown;

        [NonSerialized] private PointerEventData m_PotentialDragEvent;

        //上一次添加废弃ItemView到缓存的时间
        public float LastAddScrapViewTime
        {
            get
            {
                return m_RecycleBin.lastAddScrapViewTime;
            }
        }

        [NonSerialized]
        private bool m_NeedSendLoadMoreEvent;
        protected bool needSendLoadMoreEvent
        {
            get { return m_NeedSendLoadMoreEvent; }
            set
            {
                if (m_NeedSendLoadMoreEvent != value)
                {
                    if (value && Time.unscaledTime - m_CurrentTime < m_LoadMoreCD)
                    {
                        return;
                    }
                    m_NeedSendLoadMoreEvent = value;
                    if (value)
                    {
                        m_CurrentTime = Time.unscaledTime;
                        m_LoadMoreEvent.Invoke();
                    }
                }
            }
        }

        [NonSerialized] protected float m_CurrentTime;
        [SerializeField] protected float m_LoadMoreCD;
        public float LoadMoreCD
        {

            get { return m_LoadMoreCD; }
            set
            {
                m_LoadMoreCD = value;
                m_CurrentTime = Time.unscaledTime;
            }
        }

        [SerializeField]
        protected UnityEvent m_LoadMoreEvent;
        public UnityEvent loadMoreEvent
        {

            get { return m_LoadMoreEvent; }

            set { m_LoadMoreEvent = value; }

        }

        [SerializeField]
        private float m_DecelerationRate = 0.15f;
        public float decelerationRate
        {
            get { return m_DecelerationRate; }
            set { m_DecelerationRate = value; }
        }

        #region 生命周期处理
        protected override void Awake()
        {
            base.Awake();
            m_RecycleBin = new RecycleBin(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_Adapter != null)
            {
                m_ItemViewNeedLayout = true;
            }
        }

        protected virtual void LateUpdate()
        {
            if (!m_IsDragging && m_PotentialDragEvent != null && m_PotentialDragEvent.pointerDrag != gameObject)
            {
                m_PotentialDragEvent = null;
                if (m_MotionEvent == MotionEvent.ActionDown)
                {
                    m_MotionEvent = MotionEvent.ActionScroll;
                }
            }

            if (m_MotionEvent == MotionEvent.ActionFling || m_MotionEvent == MotionEvent.ActionScroll)
                OnTouchEvent(null);

            AttachItemView();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();           
            base.OnDisable();
            ResetOnDisable();

            m_CurrentDragEvent = null;
            var enumerator = m_DragEventMap.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.pointerDrag == gameObject)
                {
                    enumerator.Current.pointerDrag = null;
                }
            }
            m_DragEventMap.Clear();
        }

        protected void ResetOnDisable()
        {
            m_MotionEvent = MotionEvent.ActionNone;
            m_PointerStartLocalCursor = Vector2.zero;
            m_FirstFling = false;
            m_IsDragging = false;
            m_FillItemUp = false;
            m_CurrentTime = Time.unscaledTime;

            int axis = isVerticalLayout ? 1 : 0;

            if (itemViewChildren.Count > 0)
            {
                if (m_FirstPosition == 0 && m_TopStartPosition < GetViewRectTop(axis))
                {
                    m_TopStartPosition = GetViewRectTop(axis);
                }
                else if (m_FirstPosition + itemViewChildren.Count == m_Adapter.GetCount()
                         && m_BottomEndPosition > GetViewRectBottom(axis))
                {
                    if (!CheckAllItemOverViewSize(m_TopStartPosition,m_BottomEndPosition,axis))
                    {
                        m_TopStartPosition = GetViewRectTop(axis);
                    }
                    else
                    {
                        m_TopStartPosition += GetViewRectBottom(axis) - m_BottomEndPosition;
                    }
                }
            }

            for (int i = 0; i < m_ItemViewChildren.Count; ++i)
            {
                m_ItemViewChildren[i].ItemRectTransform.gameObject.SetActive(false);
                m_RecycleBin.AddScrapView(m_ItemViewChildren[i]);
            }
            m_ItemViewChildren.Clear();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }
#endif

        protected void SetDirty()
        {
            if (itemViewChildren.Count > 0)
            {
                LayoutRebuilder.MarkLayoutForRebuild(itemViewChildren[0].ItemRectTransform);
            }
        }

        #endregion

        #region 输入事件滑动处理
        public virtual void OnScroll(PointerEventData eventData)
        {
            if (!IsActive() || !enableScrollView ||m_IsDragging || !isVerticalLayout)
                return;

            Vector2 delta = eventData.scrollDelta;
            delta.y *= -1;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                delta.y = delta.x;

            var oldMovementType = m_MovementType;
            m_MovementType = MovementType.Clamped;
            TrackMotionDrag(delta.y * m_ScrollSensitivity);
            m_MotionEvent = MotionEvent.ActionScroll;
            m_MovementType = oldMovementType;
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !m_EnableDragView)
                return;

            m_PotentialDragEvent = eventData;
            m_MotionEvent = MotionEvent.ActionDown;
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !m_EnableDragView)
                return;

            m_CurrentDragEvent = eventData;
            if (!m_DragEventMap.Contains(eventData))
            {
                m_DragEventMap.Add(eventData);
            }

            if (m_VelocityTracker == null)
                m_VelocityTracker = VelocityTracker.Obtain();
            m_VelocityTracker.Clear();

            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position,
                eventData.pressEventCamera, out m_PointerStartLocalCursor);
            m_VelocityTracker.AddMovement(eventData.position);

            m_IsDragging = true;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !m_EnableDragView)
                return;

            if (m_CurrentDragEvent != null && eventData != m_CurrentDragEvent)
            {
                return;
            }

            if (m_CurrentDragEvent == null)
            {
                OnBeginDrag(eventData);
            }

            m_MotionEvent = MotionEvent.ActionMove;
            OnTouchEvent(eventData);
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !m_EnableDragView)
                return;

            if (m_CurrentDragEvent != null && eventData != m_CurrentDragEvent)
            {
                return;
            }

            m_CurrentDragEvent = null;
            m_DragEventMap.Remove(eventData);

            m_IsDragging = false;
            m_MotionEvent = MotionEvent.ActionUp;
            OnTouchEvent(eventData);

            if (m_VelocityTracker != null)
            {
                m_VelocityTracker.Recycle();
                m_VelocityTracker = null;
            }

            m_PotentialDragEvent = null;
        }

        protected virtual void OnTouchEvent(PointerEventData eventData)
        {
            var pointDelta = Vector2.zero;
            if (eventData != null)
            {
                Vector2 localCursor;
                
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
                    return;

                pointDelta = localCursor - m_PointerStartLocalCursor;
                m_PointerStartLocalCursor = localCursor;

                m_VelocityTracker.AddMovement(eventData.position);
            }

            var delta = isVerticalLayout ? pointDelta.y : pointDelta.x;
            switch (m_MotionEvent)
            {
                case MotionEvent.ActionMove:
                    TrackMotionDrag(delta);
                    break;

                case MotionEvent.ActionUp:
                    m_VelocityTracker.ComputeCurrentVelocity(1000);
                    float velocity = isVerticalLayout
                        ? m_VelocityTracker.GetYVelocity()
                        : m_VelocityTracker.GetXVelocity();
                    var velocityDown = velocity < 0;
                    m_FlingDown = delta < 0;
                    m_Velocity = Mathf.Abs(velocity);
                    if (Mathf.Abs(velocity) > VelocityTracker.FLING_MIN_VELOCITY && m_FlingDown == velocityDown)
                    {
                        m_MotionEvent = MotionEvent.ActionFling;
                        m_FirstFling = true;                    
                    }
                    else
                    {
                        m_MotionEvent = MotionEvent.ActionScroll;
                    }
                    break;

                case MotionEvent.ActionScroll:
                    TrackMotionScroll();
                    break;

                case MotionEvent.ActionFling:
                    TrackMotionFling();
                    break;
            }
        }

        protected virtual void TrackMotionFling()
        {
            int childCount = GetShowItemCount();
            if (childCount == 0)
            {
                m_MotionEvent = MotionEvent.ActionScroll;
                return;
            }

            int axis = isVerticalLayout ? 1 : 0;
            var deltaDir = isVerticalLayout ? 1 : -1;
            bool dragDown = axis == 0 ? !m_FlingDown : m_FlingDown;
            var firstItemTop = GetChildItemViewTop(0, axis);
            float viewRectBottom = GetViewRectBottom(axis);
            var lastItemBottom = GetChildItemViewBottom(childCount - 1, axis);
            float viewRectTop = GetViewRectTop(axis);

            if (m_FirstFling)
            {
                m_FirstFling = false;
                if (m_FirstPosition == 0 && dragDown && firstItemTop * deltaDir < viewRectTop - 10)
                {
                    switch (m_MovementType)
                    {
                        case MovementType.ClampedForDistance:
                            var clampedDistance = m_ClampedDistanceRatio * Mathf.Abs(viewRectBottom);
                            var velocity = (1 + m_ClampedDistanceRatio) * m_ScrollSpeed * (viewRectTop - 10 - firstItemTop * deltaDir) / clampedDistance;
                            if (m_Velocity > velocity)
                                m_Velocity = velocity;
                            break;

                        case MovementType.Elastic:
                            var dragMaxDistance = 0.5f * viewRectBottom;
                            float pullDownY = firstItemTop - viewRectTop;
                            pullDownY *= deltaDir;
                            pullDownY = Mathf.Clamp(pullDownY, dragMaxDistance, 0);
                            float ratio = 1 + 15 * Mathf.Tan(1.5707f * Mathf.Abs(pullDownY) / Mathf.Abs(dragMaxDistance));
                            if (ratio < 0)
                                ratio = float.MaxValue;
                            velocity = (1 + m_ClampedDistanceRatio) * m_ScrollSpeed / ratio;
                            if (m_Velocity > velocity)
                                m_Velocity = velocity;
                            break;
                    }
                }

                if (m_FirstPosition + childCount == m_Adapter.GetCount() && !dragDown && lastItemBottom * deltaDir > viewRectBottom + 10)
                {
                    switch (m_MovementType)
                    {
                        case MovementType.ClampedForDistance:
                            var clampedDistance = m_ClampedDistanceRatio * Mathf.Abs(viewRectBottom);
                            var velocity = (1 + m_ClampedDistanceRatio) * m_ScrollSpeed * (lastItemBottom * deltaDir - viewRectBottom - 10) / clampedDistance;
                            if (!CheckAllItemOverViewSize(firstItemTop, lastItemBottom, axis))
                            {
                                if (firstItemTop * deltaDir > viewRectTop + 10)
                                {
                                    velocity = (1 + m_ClampedDistanceRatio) * m_ScrollSpeed * (firstItemTop * deltaDir - viewRectTop - 10) / clampedDistance;
                                }
                            }
                            if (m_Velocity > velocity)
                                m_Velocity = velocity;
                            break;

                        case MovementType.Elastic:
                            var dragMaxDistance = 0.5f * viewRectBottom;
                            float pullUpY = viewRectBottom - lastItemBottom * deltaDir;
                            if (!CheckAllItemOverViewSize(firstItemTop, lastItemBottom, axis))  //所有Item的显示不超过一屏
                            {
                                pullUpY = (viewRectTop - firstItemTop) * deltaDir;
                            }
                            pullUpY = Mathf.Clamp(pullUpY, viewRectBottom, 0);
                            float ratio = 1 + 15 * Mathf.Tan(1.5707f * Mathf.Abs(pullUpY) / Mathf.Abs(dragMaxDistance));
                            if (ratio < 0)
                                ratio = float.MaxValue;

                            velocity = (1 + m_ClampedDistanceRatio) * m_ScrollSpeed / ratio;
                            if (m_Velocity > velocity)
                                m_Velocity = velocity;
                            break;
                    }
                }

                if (m_Velocity > m_ScrollSpeed * 4)
                    m_Velocity = m_ScrollSpeed * 4;
                return;
            }

            int down = m_FlingDown ? -1 : 1;
            m_Velocity *= Mathf.Pow(m_DecelerationRate, Time.deltaTime);
            if (Mathf.Abs(m_Velocity) < 1)
                m_Velocity = 0;

            var velocityDelta = 0f;
            if (m_FirstPosition == 0 && dragDown && firstItemTop*deltaDir <= viewRectTop)
            {
                switch (m_MovementType)
                {
                    case MovementType.ClampedForDistance:
                        if (m_Velocity > 1.5f*m_ScrollSpeed)
                        {
                            m_Velocity = 1.5f*m_ScrollSpeed;
                        }
                        var clampedDistance = m_ClampedDistanceRatio*Mathf.Abs(viewRectBottom);
                        velocityDelta = m_ScrollSpeed*(viewRectTop - firstItemTop*deltaDir)/clampedDistance;
                        break;
                    
                    case MovementType.Elastic:
                        if (m_Velocity > 2 * m_ScrollSpeed)
                        {
                            m_Velocity = 2 * m_ScrollSpeed;
                        }
                        var dragMaxDistance = 0.5f * viewRectBottom;
                        float pullDownY = firstItemTop - viewRectTop;
                        pullDownY *= deltaDir;
                        pullDownY = Mathf.Clamp(pullDownY, dragMaxDistance, 0);
                        float ratio = 1 + 15 * Mathf.Tan(1.5707f * Mathf.Abs(pullDownY) / Mathf.Abs(dragMaxDistance));
                        if (ratio < 0)
                            ratio = float.MaxValue;
                        velocityDelta = m_ScrollSpeed / ratio;
                        break;
                }
            }

            if (m_FirstPosition + childCount == m_Adapter.GetCount() && !dragDown && lastItemBottom * deltaDir >= viewRectBottom)
            {
                switch (m_MovementType)
                {
                    case MovementType.ClampedForDistance:
                        if (m_Velocity > 1.5f * m_ScrollSpeed)
                        {
                            m_Velocity = 1.5f * m_ScrollSpeed;
                        }
                        var clampedDistance = m_ClampedDistanceRatio * Mathf.Abs(viewRectBottom);
                        velocityDelta = m_ScrollSpeed * (lastItemBottom * deltaDir - viewRectBottom) / clampedDistance;
                        if (!CheckAllItemOverViewSize(firstItemTop, lastItemBottom, axis))
                        {
                            if (firstItemTop * deltaDir >= viewRectTop)
                            {
                                velocityDelta = m_ScrollSpeed * (firstItemTop * deltaDir - viewRectTop) / clampedDistance;
                            }
                        }
                        break;

                    case MovementType.Elastic:
                        if (m_Velocity > 2 * m_ScrollSpeed)
                        {
                            m_Velocity = 2 * m_ScrollSpeed;
                        }
                        var dragMaxDistance = 0.5f * viewRectBottom;
                        float pullUpY = viewRectBottom - lastItemBottom * deltaDir;
                        if (!CheckAllItemOverViewSize(firstItemTop, lastItemBottom, axis))  //所有Item的显示不超过一屏
                        {
                            pullUpY = (viewRectTop - firstItemTop) * deltaDir;
                        }
                        pullUpY = Mathf.Clamp(pullUpY, viewRectBottom, 0);
                        float ratio = 1 + 15 * Mathf.Tan(1.5707f * Mathf.Abs(pullUpY) / Mathf.Abs(dragMaxDistance));
                        if (ratio < 0)
                            ratio = float.MaxValue;
                        velocityDelta = m_ScrollSpeed / ratio;
                        break;         
                }
            }

            float delta = (m_Velocity - velocityDelta) * Time.deltaTime * down;
            delta = TrackMotionDrag(delta);
            if (m_Velocity - velocityDelta < 0 || Mathf.Abs(delta) < 1)
            {
                m_MotionEvent = MotionEvent.ActionScroll;
            }
        }

        protected virtual float TrackMotionDrag(float delta)
        {
            int childCount = GetShowItemCount();
            if (childCount == 0)
                return 0;

            int axis = isVerticalLayout ? 1 : 0;
            float viewRectBottom = GetViewRectBottom(axis);
            float viewRectTop = GetViewRectTop(axis);

            bool dragDown = axis == 0 ? delta >= 0 : delta <= 0;
            if (dragDown)
            {
                delta = Mathf.Max(viewRectBottom + 1, delta);
            }
            else
            {
                delta = Mathf.Min(-viewRectBottom - 1, delta);
            }
            var deltaDir = isVerticalLayout ? 1 : -1;

            int firstPosition = m_FirstPosition;
            var firstItemTop = GetChildItemViewTop(0, axis);

            var lastItemIndex = childCount - 1;
            var lastItemBottom = GetChildItemViewBottom(lastItemIndex, axis);

            if ((firstPosition + lastItemIndex + 1 < m_Adapter.GetCount()) || (lastItemBottom * deltaDir <= viewRectBottom))
            {
                needSendLoadMoreEvent = false;
            }
 
            if (firstPosition == 0 && dragDown)
            {
                switch (m_MovementType)
                {
                    case MovementType.Clamped:
                        if (firstItemTop + delta * deltaDir <= viewRectTop)
                        {
                            delta = viewRectTop - firstItemTop;
                        }
                        break;

                    case MovementType.ClampedForDistance:
                        var clampedDistance = m_ClampedDistanceRatio*Mathf.Abs(viewRectBottom);
                        if ((firstItemTop + delta) * deltaDir <= viewRectTop - clampedDistance)
                        {
                            delta = viewRectTop - clampedDistance - firstItemTop * deltaDir;
                            delta *= deltaDir;
                        }
                        break;

                    case MovementType.Elastic:
                        if (firstItemTop * deltaDir <= viewRectTop)
                        {
                            var dragMaxDistance = 0.5f*viewRectBottom;
                            float pullDownY = firstItemTop - viewRectTop;
                            pullDownY *= deltaDir;
                            pullDownY = Mathf.Clamp(pullDownY, dragMaxDistance, 0);
                            float ratio = 1 + 15*Mathf.Tan(1.5707f*Mathf.Abs(pullDownY)/Mathf.Abs(dragMaxDistance));
                            if (ratio < 0)
                                ratio = float.MaxValue;
                            delta = delta/ratio;
                        }
                        break;
                }
                OffsetChildrenTopAndBottom(delta);  //第一个Item可视的情况向下移动，单纯的移动View，不做Item的移除加入处理
                return delta;
            }

            if (firstPosition + lastItemIndex + 1 == m_Adapter.GetCount() && !dragDown)
            {
                if((lastItemBottom + delta) * deltaDir > viewRectBottom)
                {
                    needSendLoadMoreEvent = true;
                }
                switch (m_MovementType)
                {
                    case MovementType.Clamped:
                        if (!CheckAllItemOverViewSize(firstItemTop, lastItemBottom, axis))
                        {
                            delta = 0;
                            break;
                        }
                        if ((lastItemBottom + delta) * deltaDir > viewRectBottom)
                        {
                            delta = viewRectBottom * deltaDir - lastItemBottom;
                        }
                        break;

                    case MovementType.ClampedForDistance:
                        var clampedDistance = m_ClampedDistanceRatio * Mathf.Abs(viewRectBottom);
                        if (!CheckAllItemOverViewSize(firstItemTop, lastItemBottom, axis))
                        {
                            if ((firstItemTop + delta) * deltaDir >= viewRectTop + clampedDistance)
                            {
                                delta = (viewRectTop + clampedDistance) * deltaDir - firstItemTop;
                            }
                            break;
                        }
                        if ((lastItemBottom + delta) * deltaDir > viewRectBottom + clampedDistance)
                        {
                            delta = (viewRectBottom + clampedDistance) * deltaDir - lastItemBottom;
                        }
                        break;

                    case MovementType.Elastic:
                        if (lastItemBottom*deltaDir >= viewRectBottom)
                        {
                            var dragMaxDistance = 0.5f*viewRectBottom;
                            float pullUpY = viewRectBottom - lastItemBottom*deltaDir;
                            if (!CheckAllItemOverViewSize(firstItemTop, lastItemBottom, axis)) //所有Item的显示不超过一屏
                            {
                                pullUpY = (viewRectTop - firstItemTop)*deltaDir;
                            }
                            pullUpY = Mathf.Clamp(pullUpY, viewRectBottom, 0);
                            float ratio = 1 + 15*Mathf.Tan(1.5707f*Mathf.Abs(pullUpY)/Mathf.Abs(dragMaxDistance));
                            if (ratio < 0)
                                ratio = float.MaxValue;
                            delta = delta/ratio;
                        }
                        break;
                }
                OffsetChildrenTopAndBottom(delta);
                CheckFillItemView(0, 0, false);
                return delta;
            }

            int start = 0;
            int count = 0;
            if (dragDown)
            {
                float bottom = viewRectBottom - delta * deltaDir;
                for (int i = childCount - 1; i >= 0; --i)
                {                   
                    if (GetChildItemViewTop(i, axis) * deltaDir >= bottom)
                    {
                        break;
                    }
                    start = i;
                    ++count;
                }
            }
            else
            {
                float top = viewRectTop - delta * deltaDir;
                for (int i = 0; i < childCount; ++i)
                {
                    if (GetChildItemViewBottom(i, axis) * deltaDir <= top)
                    {
                        break;
                    }
                    ++count;
                }
            }

            OffsetChildrenTopAndBottom(delta);
            DetachViewsFromParent(start, count, dragDown);

            if (!dragDown)
            {
                m_FirstPosition += count;
            }

            return delta;
        }

        protected virtual void TrackMotionScroll()
        {
            int childCount = GetShowItemCount();
            if (childCount == 0)
            {
                m_MotionEvent = MotionEvent.ActionNone;
                return;
            }
                

            int axis = isVerticalLayout ? 1 : 0;
            float viewRectBottom = GetViewRectBottom(axis);
            float viewRectTop = GetViewRectTop(axis);

            int firstPosition = m_FirstPosition;
            var firstItemTop = GetChildItemViewTop(0, axis);
            var deltaDir = isVerticalLayout ? 1 : -1;

            if (firstPosition == 0 && firstItemTop * deltaDir < viewRectTop - 0.01f)
            {
                if (m_MovementType != MovementType.Unrestricted)
                {
                    float delta = m_ScrollSpeed * Time.deltaTime * deltaDir;

                    if (m_MovementType == MovementType.Elastic)
                    {
                        var dragMaxDistance = 0.5f * viewRectBottom;
                        float pullDownY = firstItemTop - viewRectTop;
                        pullDownY *= deltaDir;
                        pullDownY = Mathf.Clamp(pullDownY, viewRectBottom, 0);
                        delta = m_ScrollSpeed + 4 * m_ScrollSpeed * Mathf.Tan(1.5707f * Mathf.Abs(pullDownY) / Mathf.Abs(dragMaxDistance));
                        if (delta < 0)
                            delta = float.MaxValue;
                        delta *= Time.deltaTime * deltaDir;
                    }
                    if ((firstItemTop + delta) * deltaDir > viewRectTop)
                    {
                        delta = viewRectTop * deltaDir - firstItemTop;
                        m_MotionEvent = MotionEvent.ActionNone;
                    }
                    OffsetChildrenTopAndBottom(delta);
                }
                return;
            }

            var lastItemIndex = childCount - 1;
            var lastItemBottom = GetChildItemViewBottom(lastItemIndex, axis);

            if (lastItemBottom * deltaDir > viewRectBottom)
            {
                if (firstPosition + lastItemIndex + 1 == m_Adapter.GetCount())
                {
                    needSendLoadMoreEvent = true;
                    if (firstItemTop*deltaDir > viewRectTop + 0.01f)
                    {
                        if (m_MovementType != MovementType.Unrestricted)
                        {
                            bool isSmallerViewRect = false;

                            float delta = -m_ScrollSpeed*Time.deltaTime*deltaDir;
                            if (!CheckAllItemOverViewSize(firstItemTop, lastItemBottom, axis)) //所有Item的显示不超过一屏
                            {
                                isSmallerViewRect = true;
                            }

                            if (m_MovementType == MovementType.Elastic)
                            {
                                var dragMaxDistance = 0.5f*viewRectBottom;
                                float pullUpY = viewRectBottom - lastItemBottom*deltaDir;
                                if (isSmallerViewRect)
                                {
                                    pullUpY = viewRectTop - firstItemTop;
                                    pullUpY *= deltaDir;
                                }
                                pullUpY = Mathf.Clamp(pullUpY, viewRectBottom, 0);
                                delta = m_ScrollSpeed +
                                        4*m_ScrollSpeed*Mathf.Tan(1.5707f*Mathf.Abs(pullUpY)/Mathf.Abs(dragMaxDistance));
                                if (delta < 0)
                                    delta = float.MaxValue;
                                delta *= -Time.deltaTime*deltaDir;
                            }

                            if (isSmallerViewRect)
                            {
                                if ((firstItemTop + delta)*deltaDir < viewRectTop)
                                {
                                    delta = viewRectTop*deltaDir - firstItemTop;
                                    m_MotionEvent = MotionEvent.ActionNone;
                                    ;
                                }
                            }
                            else
                            {
                                if ((lastItemBottom + delta)*deltaDir < viewRectBottom)
                                {
                                    delta = viewRectBottom*deltaDir - lastItemBottom;
                                    m_MotionEvent = MotionEvent.ActionNone;
                                }
                            }
                            OffsetChildrenTopAndBottom(delta);
                            CheckFillItemView(0, 0, false);
                            return;
                        }
                    }
                    CheckFillItemView(0, 0, false);
                }
                else if (firstPosition + lastItemIndex + 1 < m_Adapter.GetCount())
                {
                    CheckFillItemView(0, 0, false);
                    return;
                }
            }
            m_MotionEvent = MotionEvent.ActionNone;
        }
        #endregion

        #region 缓存处理

        public virtual void SetAdapter(IAdapter adapter)
        {
            for (int i = 0; i < m_ItemViewChildren.Count; ++i)
            {
                m_ItemViewChildren[i].ItemRectTransform.gameObject.SetActive(false);
                m_RecycleBin.AddScrapView(m_ItemViewChildren[i]);
            }

            m_RecycleBin.Clear();
            m_ItemViewChildren.Clear();
            m_FirstPosition = 0;
            m_Adapter = adapter;
            m_TopStartPosition = 0;
            m_BottomEndPosition = 0;
            m_FillItemUp = false;

            if (m_Adapter != null)
            {
                m_Adapter.Owner = this;
                m_RecycleBin.SetViewTypeCount(m_Adapter.GetViewTypeCount());
                var preloadItemList = m_Adapter.PreloadItem(this);
                if (preloadItemList != null)
                {
                    for (int i = 0; i < preloadItemList.Count; ++i)
                    {
                        var itemRectTransform = preloadItemList[i].ItemRectTransform;
                        itemRectTransform.SetParent(gameObject.transform);
                        itemRectTransform.gameObject.SetActive(false);
                        m_RecycleBin.AddScrapView(preloadItemList[i]);
                        CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(LayoutRebuilderUtility.GetLayoutRebuilder(itemRectTransform));
                    }
                }

                m_MotionEvent = MotionEvent.ActionNone;
                if (m_Adapter.GetCount() > 0)
                {
                    m_ItemViewNeedLayout = true;
                }
            }
        }

        public virtual void ClearScrapItemView()
        {
            if (m_RecycleBin != null)
            {
                m_RecycleBin.Clear();
            }
        }

        public virtual void RefreshAllItem()
        {
            for (int i = 0; i < m_ItemViewChildren.Count; ++i)
            {
                m_ItemViewChildren[i].ItemRectTransform.gameObject.SetActive(false);
                m_RecycleBin.AddScrapView(m_ItemViewChildren[i]);
            }
            m_ItemViewChildren.Clear();
            m_FirstPosition = 0;
            m_TopStartPosition = 0;
            m_BottomEndPosition = 0;
            m_FillItemUp = false;

            if (m_Adapter != null)
            {
                m_MotionEvent = MotionEvent.ActionNone;
                if (m_Adapter.GetCount() > 0)
                {
                    m_ItemViewNeedLayout = true;
                }
            }
        }

        public virtual void RefreshCurrentItem()
        {
            if (m_Adapter != null)
            {
                for (int i = m_FirstPosition + m_ItemViewChildren.Count - 1; i > m_Adapter.GetCount() - 1 && (i - m_FirstPosition > -1); --i)
                {
                    var index = i - m_FirstPosition;
                    m_ItemViewChildren[index].ItemRectTransform.gameObject.SetActive(false);
                    CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(LayoutRebuilderUtility.GetLayoutRebuilder(m_ItemViewChildren[index].ItemRectTransform));
                    m_RecycleBin.AddScrapView(m_ItemViewChildren[index]);
                    m_ItemViewChildren.RemoveAt(index);
                }

                if (m_ItemViewChildren.Count <= 0)
                {
                    m_FirstPosition = m_Adapter.GetCount() - 1;
                    m_ItemViewNeedLayout = true;
                }

                for (int i = 0; i < m_ItemViewChildren.Count; ++i)
                {
                    m_Adapter.ProcessView(m_FirstPosition + i, m_ItemViewChildren[i], this);
                }
                m_FillItemUp = false;
                CheckFillItemView(0, 0, true);
                CheckFillItemView(0, 0, false);
            }
            else
            {
                for (int i = 0; i < m_ItemViewChildren.Count; ++i)
                {
                    m_ItemViewChildren[i].ItemRectTransform.gameObject.SetActive(false);
                    m_RecycleBin.AddScrapView(m_ItemViewChildren[i]);
                }
                m_ItemViewChildren.Clear();
                m_FillItemUp = false;
            }
        }

        public abstract void ChildrenLayoutGroupComplete();

        protected virtual void DetachViewsFromParent(int start, int count,bool dragDown)
        {
            CheckFillItemView(start, count, dragDown);
            if (count > 0)
            {
                for (int i = start; i < start + count; ++i)
                {
                    var child = itemViewChildren[i].ItemRectTransform;
                    child.gameObject.SetActive(false);
                    CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(LayoutRebuilderUtility.GetLayoutRebuilder(child));
                    m_RecycleBin.AddScrapView(itemViewChildren[i]);
                }
                itemViewChildren.RemoveRange(start,count);
            }
        }

        protected virtual void OffsetChildrenTopAndBottom(float delta)
        {
            int axis = isVerticalLayout ? 1 : 0;
            float deltaDir = isVerticalLayout ? 1 : -1;
            for (int i = 0; i < itemViewChildren.Count; ++i)
            {
                var child = itemViewChildren[i].ItemRectTransform;
                var positionX = axis == 1 ? child.anchoredPosition.x : child.anchoredPosition.x + delta;
                var positionY = axis == 1 ? child.anchoredPosition.y + delta : child.anchoredPosition.y;

                SetContentAnchoredPosition(child, new Vector2(positionX, positionY));
            }

            //用于没有移除和添加物体时候计算m_TopStartPosition，用于当屏幕大小改变导致重算布局等
            m_TopStartPosition = GetChildItemViewTop(0, axis) * deltaDir;
            m_BottomEndPosition = GetChildItemViewBottom(GetShowItemCount() - 1, axis) * deltaDir;
            CheckArrowAndLock();
        }

        protected virtual void CheckFillItemViewInLayoutComplete()
        {
            int childCount = GetShowItemCount();
            int axis = isVerticalLayout ? 1 : 0;
            float deltaDir = isVerticalLayout ? 1 : -1;
            m_TopStartPosition = GetChildItemViewTop(0, axis) * deltaDir;
            float viewRectBottom = GetViewRectBottom(axis);
            var lastItemIndex = childCount - 1;
            var lastItemBottom = m_BottomEndPosition = GetChildItemViewBottom(lastItemIndex, axis) * deltaDir;
            float viewRectTop = GetViewRectTop(axis);

            if (m_TopStartPosition < viewRectTop && m_FirstPosition != 0)
            {
                m_ItemViewNeedLayout = true;
                m_FillItemUp = true;            
            }
            else if (lastItemBottom - m_Divider > viewRectBottom && lastItemIndex + m_FirstPosition < m_Adapter.GetCount() - 1)
            {
                m_ItemViewNeedLayout = true;
                m_FillItemUp = false;
            }

            //修正边界布局情况，突然有很大的拉动到最上面，由于布局先由下往上填充，再由上往下填充，导致边界情况，由上往下填充不成立
            if (m_FirstPosition == 0 && m_TopStartPosition < viewRectTop)
            {
                if (lastItemBottom - m_Divider - m_TopStartPosition > viewRectBottom && lastItemIndex + m_FirstPosition < m_Adapter.GetCount() - 1)
                {
                    m_ItemViewNeedLayout = true;
                    m_FillItemUp = false;
                }
            }
            else if (lastItemIndex + m_FirstPosition >= m_Adapter.GetCount() - 1 && lastItemBottom  > viewRectBottom) //修正边界布局情况，突然有很大的拉动到最下面，由于m_ItemViewNeedLayout = true不成立，导致边界情况，由下往上填充不成立
            {
                if (m_TopStartPosition  + viewRectBottom - lastItemBottom < viewRectTop && m_FirstPosition != 0)
                {
                    m_ItemViewNeedLayout = true;
                    m_FillItemUp = true;
                }
            }

            AttachItemView();
        }

        protected virtual void CheckFillItemView(int start, int count, bool dragDown)
        {
            int childCount = GetShowItemCount();
            if (childCount == 0)
                return;

            int axis = isVerticalLayout ? 1 : 0;
            float deltaDir = isVerticalLayout ? 1 : -1;
            float viewRectTop = GetViewRectTop(axis);
            var lastItemIndex = childCount - 1;
            var lastItemBottom = m_BottomEndPosition = GetChildItemViewBottom(lastItemIndex, axis) * deltaDir;

            if (dragDown)
            {          
                m_TopStartPosition = GetChildItemViewTop(0, axis) * deltaDir;
                if (m_TopStartPosition  < viewRectTop && m_FirstPosition != 0)
                {
                    m_ItemViewNeedLayout = true;
                    m_FillItemUp = true;
                }
            }
            else
            {
                float viewRectBottom = GetViewRectBottom(axis);
                
                if (count == childCount)
                {
                    m_TopStartPosition = lastItemBottom;
                }
                else
                {
                    m_TopStartPosition = GetChildItemViewTop(start + count, axis) * deltaDir;
                }

                if (lastItemBottom - m_Divider > viewRectBottom && lastItemIndex + m_FirstPosition < m_Adapter.GetCount() - 1)
                {
                    m_ItemViewNeedLayout = true;
                    m_FillItemUp = false;
                }
            }
        }

        protected virtual void AttachItemView()
        {
            if (m_ItemViewNeedLayout)
            {
                m_ItemViewNeedLayout = false;
                var position = 0;
                if (m_FillItemUp)
                {
                    position = m_FirstPosition - 1;
                    if (position < 0)
                    {
                        return;
                    }
                    float deltaDir = isVerticalLayout ? 1 : -1;
                    int axis = isVerticalLayout ? 1 : 0;
                    int childCount = GetShowItemCount();
                    float viewRectBottom = GetViewRectBottom(axis);
                    if (childCount > 0)
                    {
                        var index = childCount - 1;
                        var lastItemTop = GetChildItemViewTop(index, axis)*deltaDir;
                        if (lastItemTop < viewRectBottom)
                        {
                            var isRebuilding = LayoutRebuilderUtility.IsRectTransformLayoutRebuild(itemViewChildren[index].ItemRectTransform);
                            if (!isRebuilding)
                            {
                                m_RecycleBin.AddScrapView(itemViewChildren[index]);
                                itemViewChildren.RemoveAt(index);
                            }
                        }       
                    }
                }
                else
                {
                    float deltaDir = isVerticalLayout ? 1 : -1;
                    int axis = isVerticalLayout ? 1 : 0;
                    int childCount = GetShowItemCount();
                    float viewRectTop = GetViewRectTop(axis);
                    if (childCount > 0)
                    {
                        var firstItemBottom = GetChildItemViewBottom(0, axis) * deltaDir;
                        if (firstItemBottom > viewRectTop)
                        {
                            var isRebuilding = LayoutRebuilderUtility.IsRectTransformLayoutRebuild(itemViewChildren[0].ItemRectTransform);
                            if (!isRebuilding)
                            {
                                ++m_FirstPosition;
                                m_TopStartPosition -= (LayoutUtility.GetPreferredSize(itemViewChildren[0].ItemRectTransform, axis) + m_Divider);
                                m_RecycleBin.AddScrapView(itemViewChildren[0]);
                                itemViewChildren.RemoveAt(0);
                            }
                        }
                    }

                    position = m_FirstPosition + m_ItemViewChildren.Count;

                    if (position >= m_Adapter.GetCount())
                    {
                        return;
                    }
                }

                var item = m_RecycleBin.GetScrapView(position);

                if (item == null)
                {
                    item = m_Adapter.GetView(position, this);
                    item.ItemRectTransform.SetParent(rectTransform);
                }

                item.ItemRectTransform.localRotation = Quaternion.identity;
                item.ItemRectTransform.localScale = Vector3.one;
                item.ItemRectTransform.gameObject.SetActive(true);

                //如果是竖直ListView，设置item的宽度为ListView的宽度，如果水平ListView，设置Item的高度为ListView的高度
                var anotherAxis = isVerticalLayout ? 0 : 1;
                float size = rectTransform.rect.size[anotherAxis];
                item.ItemRectTransform.SetInsetAndSizeFromParentEdge(anotherAxis == 0 ? RectTransform.Edge.Left : RectTransform.Edge.Top, 0, size);

                m_Adapter.ProcessView(position, item, this);

                if (m_FillItemUp)
                {
                    --m_FirstPosition;
                    m_ItemViewChildren.Insert(0,item);
                }
                else
                {
                    m_ItemViewChildren.Add(item);
                }

                LayoutRebuilder.MarkLayoutForRebuild(item.ItemRectTransform);
            }
            else
            {
                if (m_AutoDragOrScrollView)
                {
                    int childCount = GetShowItemCount();
                    if (childCount > 0)
                    {
                        int axis = isVerticalLayout ? 1 : 0;
                        var firstItemTop = GetChildItemViewTop(0, axis);
                        var lastItemBottom = GetChildItemViewBottom(childCount - 1, axis);
                        m_EnableDragView = CheckAllItemOverViewSize(firstItemTop, lastItemBottom, axis);
                        m_EnableScrollView = m_EnableDragView;       
                    }
                    else
                    {
                        m_EnableDragView = false;
                        m_EnableScrollView = false;
                    }
                }
            }
        }

        protected virtual void CheckArrowAndLock()
        {

        }
        #endregion

        #region 辅助函数
        protected virtual void SetContentAnchoredPosition(RectTransform child, Vector2 position)
        {
            if (isVerticalLayout)
                position.x = child.anchoredPosition.x;
            else
                position.y = child.anchoredPosition.y;

            if (position != child.anchoredPosition)
            {
                child.anchoredPosition = position;
            }
        }

        /// <summary>
        ///  ///检查所有Item的显示区域是否超过显示区域
        /// </summary>
        protected virtual bool CheckAllItemOverViewSize(float firstItemTop, float lastItemBottom,int axis)
        {
            int deltaDir = isVerticalLayout ? 1 : -1;
            if (m_FirstPosition == 0 && (firstItemTop - lastItemBottom) * deltaDir < rectTransform.rect.size[axis])  
            {
                return false;
            }
            return true;
        }

        protected virtual int GetShowItemCount()
        {
            return itemViewChildren.Count;
        }

        protected virtual RectTransform GetChildItemAt(int i)
        {
            return itemViewChildren[i].ItemRectTransform;
        }

        protected virtual float GetViewRectTop(int axis)
        {
            return 0;
        }

        protected virtual float GetViewRectBottom(int axis)
        {
            return - rectTransform.rect.size[axis];
        }

        protected virtual float GetChildItemViewTop(int index, int axis)
        {
            return itemViewChildren[index].ItemRectTransform.anchoredPosition[axis];
        }

        protected virtual float GetChildItemViewTop(RectTransform rect, int axis)
        {
            return rect.anchoredPosition[axis];
        }

        protected virtual float GetChildItemViewBottom(int index, int axis)
        {
            int deltaDir = isVerticalLayout ? 1 : -1;
            return itemViewChildren[index].ItemRectTransform.anchoredPosition[axis] -
                   LayoutUtility.GetPreferredSize(itemViewChildren[index].ItemRectTransform, axis) * deltaDir;
        }

        protected virtual float GetChildItemViewBottom(RectTransform rect, int axis)
        {
            return rect.anchoredPosition[axis] - LayoutUtility.GetPreferredSize(rect, axis);
        }

        public void SetTopStartPositonOffset(float offset)
        {
            m_TopStartPosition -= offset;
        }

        #endregion

        #region Item缓存管理
        protected class RecycleBin
        {
            private List<IItemView>[] m_ScrapViews;
            private int m_ViewTypeCount;
            private List<IItemView> m_CurrentScrap;
            private AbstractListView m_Owner;
            public float lastAddScrapViewTime { get; set; }  //上一次添加废弃的ItemView到缓存的时间，可用于内存控制，比如缓存有好几分钟没访问了，可以外部调用AbstractListView的ClearScrapItemView

            public RecycleBin(AbstractListView absListView)
            {
                m_Owner = absListView;
            }

            public void SetViewTypeCount(int viewTypeCount)
            {
                if (viewTypeCount < 1)
                {
                    throw new Exception("Can't have a viewTypeCount < 1");
                }

                List<IItemView>[] scrapViews = new List<IItemView>[viewTypeCount];
                for (int i = 0; i < viewTypeCount; i++)
                {
                    scrapViews[i] = new List<IItemView>();
                }

                m_ViewTypeCount = viewTypeCount;
                m_CurrentScrap = scrapViews[0];
                m_ScrapViews = scrapViews;
            }

            public void Clear()
            {
                if (m_ViewTypeCount == 1)
                {
                    List<IItemView> scrap = m_CurrentScrap;
                    int scrapCount = scrap.Count;
                    for (int i = 0; i < scrapCount; i++)
                    {
                        Destroy(scrap[scrapCount - 1 - i].ItemRectTransform.gameObject);
                        scrap.RemoveAt(scrapCount - 1 - i);
                    }
                }
                else
                {
                    int typeCount = m_ViewTypeCount;
                    for (int i = 0; i < typeCount; i++)
                    {
                        List<IItemView> scrap = m_ScrapViews[i];
                        int scrapCount = scrap.Count;
                        for (int j = 0; j < scrapCount; j++)
                        {
                            Destroy(scrap[scrapCount - 1 - j].ItemRectTransform.gameObject);
                            scrap.RemoveAt(scrapCount - 1 - j);
                        }
                    }
                }
            }

            public IItemView GetScrapView(int position)
            {
                List<IItemView> scrapViews;
                if (m_ViewTypeCount == 1)
                {
                    scrapViews = m_CurrentScrap;
                    int size = scrapViews.Count;
                    if (size > 0)
                    {
                        var item = scrapViews[size - 1];
                        scrapViews.RemoveAt(size - 1);
                        return item;
                    }
                    return null;
                }

                int whichScrap = m_Owner.m_Adapter.GetItemViewType(position);
                if (whichScrap >= 0 && whichScrap < m_ScrapViews.Length)
                {
                    scrapViews = m_ScrapViews[whichScrap];
                    int size = scrapViews.Count;
                    if (size > 0)
                    {
                        var item = scrapViews[size - 1];
                        scrapViews.RemoveAt(size - 1);
                        return item;
                    }
                }
                
                return null;
            }

            public void AddScrapView(IItemView scrap)
            {
                lastAddScrapViewTime = Time.unscaledTime;
                int viewType = scrap.ViewType;
                if (m_ViewTypeCount == 1)
                {
                    m_CurrentScrap.Add(scrap);
                }
                else
                {
                    m_ScrapViews[viewType].Add(scrap);
                }  
            }
        }
        #endregion
    }
}