using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 触摸遥感，兼容球球大作战的遥感处理方式
/// </summary>
public class TouchJoystick : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    public Action<Vector3> DirAction;                //遥感的方向回调，不能为null，方向向量的大小在0到1之间
    public bool            NeedResetPos = true;      //鼠标抬起的时候是否重置遥感到原始位置，如果是false,遥感将被隐藏

    protected Vector2       m_PointDownPos;          //鼠标按下的位置
    protected RectTransform m_SelfRectTransform;     
    protected Vector3       m_OuterStartPos;
    protected Vector3       m_InnerStartPos;
    protected float         m_MoveDistance;
    protected PointerEventData m_ProcessingEventData; //正在处理的事件数据

    public RectTransform OuterRectTransform;
    public RectTransform InnerRectTransform;

    protected void Awake()
    {
        m_SelfRectTransform  = transform as RectTransform;

        if (OuterRectTransform != null && InnerRectTransform != null)
        {
            m_OuterStartPos = OuterRectTransform.localPosition;
            m_InnerStartPos = InnerRectTransform.localPosition;
            m_MoveDistance = OuterRectTransform.rect.width * 0.5f;
        }

        if (DirAction == null)
        {
            DirAction = (dir) => { };
        }
    }

    /// <summary>
    /// 用于遥感不直接挂到GameObject，而是在代码中挂载的使用，游戏Awake的执行
    /// 时间不能保证，在赋值OuterRectTransform，InnerRectTransform后可以调用
    /// 此函数再次初始化
    /// </summary>
    public void InitilizeTouchJoystick()
    {
        Awake();
    }

    protected void OnDisable()
    {
        m_ProcessingEventData = null;
        if (DirAction != null)
            DirAction(Vector3.zero);
        Reset();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (m_ProcessingEventData != eventData)
        {
            return;
        }
        DirAction(Vector3.zero);
        Reset();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (m_ProcessingEventData == null || m_ProcessingEventData.pointerPress == null)
        {
            m_ProcessingEventData = eventData;
        }
        else
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_SelfRectTransform, eventData.position, eventData.pressEventCamera, out m_PointDownPos);

        if (!NeedResetPos)
        {
            OuterRectTransform.gameObject.SetActive(true);
            InnerRectTransform.gameObject.SetActive(true);
        }
        OuterRectTransform.localPosition = m_PointDownPos;
        InnerRectTransform.localPosition = m_PointDownPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_ProcessingEventData != eventData)
        {
            return;
        }

        var currentPos = m_PointDownPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_SelfRectTransform, eventData.position, eventData.pressEventCamera, out currentPos);

        var dir = currentPos - m_PointDownPos;
        if (dir.magnitude > m_MoveDistance)
        {
            dir = dir.normalized * m_MoveDistance;
            currentPos = m_PointDownPos + dir;
        }
        InnerRectTransform.localPosition = currentPos;
        DirAction(dir / m_MoveDistance);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_ProcessingEventData != eventData)
        {
            return;
        }
        Reset();
        m_ProcessingEventData = null;
    }

    protected void Reset()
    {
        if (OuterRectTransform != null && InnerRectTransform != null)
        {
            if (NeedResetPos)
            {
                OuterRectTransform.localPosition = m_OuterStartPos;
                InnerRectTransform.localPosition = m_InnerStartPos;
            }
            else
            {
                OuterRectTransform.gameObject.SetActive(false);
                InnerRectTransform.gameObject.SetActive(false);
            }
        }
    }
}

 