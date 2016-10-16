using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 触摸遥感，兼容火影忍者手游的遥感处理方式
/// </summary>
public class TouchJoystick2 : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    public Action<Vector3> DirAction;                //遥感的方向回调，不能为null，方向向量的大小在0到1之间
    public bool            FixPosition = true;       //遥感是否固定位置不动
    public float           NoramlScale = 1;          //遥感外圈圆在正常状态下的缩放
    public float           DownScale   = 1.1f;       //遥感外圈圆在按下状态下的缩放
    public bool            ReceiveDrag = true;       //是否接受拖拽事件
    public bool            ResetReceiveDrag = true;  //是否在拖拽结束后重置ReceiveDrag = true

    protected Vector2          m_PointDownPos;           
    protected RectTransform    m_SelfRectTransform;
    protected Vector3          m_OuterStartPos;
    protected Vector3          m_InnerStartPos;
    protected float            m_MoveDistance;
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
            m_MoveDistance = OuterRectTransform.rect.width * 0.5f * DownScale; ;
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
        if(DirAction != null)
            DirAction(Vector3.zero);
        Reset();
    }

    public void AdjustMoveDistance()
    {
        m_MoveDistance     = OuterRectTransform.rect.width * 0.5f * DownScale;
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

        OuterRectTransform.localScale = Vector3.one * DownScale;
        Vector3 dir = m_PointDownPos - new Vector2(m_OuterStartPos.x, m_OuterStartPos.y);

        if (FixPosition)
        {           
            if (dir.magnitude > m_MoveDistance)
            {
                dir = dir.normalized * m_MoveDistance;
            }
            InnerRectTransform.localPosition = m_OuterStartPos + dir;
        }
        else
        {
            InnerRectTransform.localPosition = m_PointDownPos;
            if (dir.magnitude > m_MoveDistance)
            {
                dir = dir.normalized * m_MoveDistance;
                OuterRectTransform.localPosition = InnerRectTransform.localPosition - dir;
            }
        }

        if (dir.magnitude > 0.05f)
        {
            DirAction(dir / m_MoveDistance);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_ProcessingEventData != eventData || !ReceiveDrag)
        {
            return;
        }

        var currentPos = m_PointDownPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_SelfRectTransform, eventData.position, eventData.pressEventCamera, out currentPos);

        if (FixPosition)
        {
            Vector3 dir = currentPos - new Vector2(m_OuterStartPos.x, m_OuterStartPos.y);
            if (dir.magnitude > m_MoveDistance)
            {
                dir = dir.normalized * m_MoveDistance;
            }
            InnerRectTransform.localPosition = m_OuterStartPos + dir;
            DirAction(dir / m_MoveDistance);
        }
        else
        {
            Vector3 dir = currentPos - new Vector2(OuterRectTransform.localPosition.x, OuterRectTransform.localPosition.y);
            InnerRectTransform.localPosition = currentPos;
            if (dir.magnitude > m_MoveDistance)
            {
                dir = dir.normalized * m_MoveDistance;
                OuterRectTransform.localPosition = InnerRectTransform.localPosition - dir;
            }
            DirAction(dir / m_MoveDistance);
        }      
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_ProcessingEventData != eventData)
        {
            return;
        }

        Reset();
        m_ProcessingEventData = null;
        if (ResetReceiveDrag)
        {
            ReceiveDrag = true;
        }
    }

    protected void Reset()
    {
        if (OuterRectTransform != null && InnerRectTransform != null)
        {
            OuterRectTransform.localScale = Vector3.one*NoramlScale;
            OuterRectTransform.localPosition = m_OuterStartPos;
            InnerRectTransform.localPosition = m_InnerStartPos;
        }
    }
}