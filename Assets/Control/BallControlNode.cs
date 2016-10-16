using UnityEngine;

public class MsgBall
{
    public uint id;
    public int type;
    public ulong own;
    public uint score;
    public uint color;
    public int x;
    public int y;
    public int nx;
    public int ny;
}

public class BallControlNode : MonoBehaviour
{
    public BallRenderNode RenderNode;
    public MsgBall        BallInfo; 

    private Vector3   m_NetPosition;
    private Vector3   m_NetVelocity;

    private Transform m_SelfTransform;
    protected Transform SelfTransform
    {
        get
        {
            if (m_SelfTransform == null)
            {
                m_SelfTransform = transform;
            }
            return m_SelfTransform;
        }
    }

    protected virtual void Awake()
    {

    }

    void Update()
    {
        NetMoveing();
    }

    protected virtual void OnEnable()
    {
        RenderNode.enabled = true;

        m_SelfTransform.position = ToVec3(BallInfo.x, BallInfo.y);
        SetNetMovePos(ToVec3(BallInfo.x, BallInfo.y));

        RenderNode.RenderType = (BallRenderNode.BallRenderType) BallInfo.type;
        RenderNode.Set(BallInfo.score / 100f, BallInfo.color / 256f);

        if (BallInfo.nx == 0 && BallInfo.ny == 0)
            SetNetSpeed(Vector3.zero);
        else
            SetNetSpeed(ToVec3(BallInfo.nx, BallInfo.ny) - ToVec3(BallInfo.x, BallInfo.y));
    }

    /// <summary>
    /// 剥离代码
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static Vector3 ToVec3(int x, int y)
    {
        Vector3 zero = Vector3.zero;
        zero.x = x / 100f;
        zero.y = y / 100f;
        return zero;
    }

    protected virtual void Start()
    {

    }

    protected virtual void OnDisable()
    {
        RenderNode.enabled = false;
        BallInfo = null;
    }
 
    public virtual void NetMoveing()
    {
        var curpos = m_SelfTransform.position;
        m_NetPosition.z = curpos.z;
        m_NetPosition   += m_NetVelocity * Time.deltaTime;

        var sqrDistance = (curpos - m_NetPosition).sqrMagnitude;
        if (sqrDistance > 100.0)
            m_SelfTransform.position = Vector3.Lerp(curpos, m_NetPosition, 50f * Time.deltaTime);
        else if (sqrDistance > 50.0)
            m_SelfTransform.position = Vector3.Lerp(curpos, m_NetPosition, 25f * Time.deltaTime);
        else if (sqrDistance > 0.0001)
            m_SelfTransform.position = Vector3.Lerp(curpos, m_NetPosition, 10f * Time.deltaTime);
        else
            m_SelfTransform.position = m_NetPosition;
    }

    public virtual void SetNetMovePos(Vector3 pos)
    {
        m_NetPosition.x = pos.x;
        m_NetPosition.y = pos.y;
        m_NetPosition.z = SelfTransform.position.z;
    }
 
    public void SetNetSpeed(Vector3 speed)
    {
        m_NetVelocity   = speed;
        m_NetVelocity.z = 0.0f;
    }
}
