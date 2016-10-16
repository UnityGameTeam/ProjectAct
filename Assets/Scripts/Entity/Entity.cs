using UnityEngine;
using Xft;

public class Entity : MonoBehaviour
{
    private Rigidbody m_Rigidbody;
    private Animator  m_Animator;
    private FSM       m_Fsm;
    private Transform m_Transform;

    private IState m_IdleState;
    private IState m_RunState;
    private IState m_AttackState;
    private IState m_DeadState;
    private IState m_HitState;

    public XWeaponTrail longWeaponTrail;
    public XWeaponTrail shortWeaponTrail;

    public Animator EntityAnimator
    {
        get { return m_Animator; }
    }

    public FSM EntityFsm
    {
        get { return m_Fsm;}
    }

    public IState IdleState
    {
        get { return m_IdleState;}
    }

    public void Awake()
    {
        m_Transform = gameObject.transform.GetChild(0);
        m_Rigidbody = m_Transform.GetComponent<Rigidbody>();
        m_Animator = m_Transform.GetComponent<Animator>();

        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        m_Fsm = gameObject.AddComponent<FSM>();

        m_IdleState = new WalkState(this);
        m_RunState  = new RunState(this);
        m_AttackState = new AttackState(this);
        m_Fsm.ChangeState(m_IdleState);
    }

    public void Idle()
    {
        m_Animator.SetInteger("Action",0);
    }

    public void Run()
    {
        m_Animator.SetInteger("Action", 1);
    }

    public void Attack()
    {
        m_Animator.SetInteger("Action", 4);
    }

    public void Die()
    {
        m_Animator.SetInteger("Action", 3);
    }

    ////////////////////////////////////////////////
    protected virtual void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        moveDir = v * Vector3.forward + h * Vector3.right;

        if (Input.GetKeyDown(KeyCode.J))
        {
            if (m_Fsm.CurrentState != m_DeadState && m_Fsm.CurrentState != m_HitState && m_Fsm.CurrentState != m_AttackState)
            {
                m_Fsm.ChangeState(m_AttackState);
            }
        }
    }

    public float walkingSpeed = 5f;
    public float walkingSnappyness = 50;
    public float turningSmoothing = 0.3f;

    private Vector3 lastMoveDir;
    private float timeOffset;
    private Vector3 moveDir;

    protected virtual void FixedUpdate()
    {
        if (m_Fsm.CurrentState == m_IdleState && moveDir != Vector3.zero)
        {
            m_Fsm.ChangeState(m_RunState);
        }
        else if (m_Fsm.CurrentState == m_RunState && moveDir == Vector3.zero)
        {
            m_Fsm.ChangeState(m_IdleState);
        }
        else if(m_Fsm.CurrentState != m_RunState && m_Fsm.CurrentState != m_IdleState)
        {
            moveDir = Vector3.zero;
        }

        var targetVelocity = moveDir * walkingSpeed;
        var deltaVelocity = targetVelocity - m_Rigidbody.velocity;

        if (m_Rigidbody.useGravity)
            deltaVelocity.y = 0;

        m_Rigidbody.AddForce(deltaVelocity * walkingSnappyness, ForceMode.Acceleration);

        if (moveDir == Vector3.zero)
        {
            m_Rigidbody.angularVelocity = Vector3.zero;
        }
        else
        {
            var deltaAngle = AngleAroundAxis(m_Rigidbody.transform.forward, moveDir, Vector3.up);
            m_Rigidbody.angularVelocity = (Vector3.up * deltaAngle * turningSmoothing);
        }
    }

    float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
    {
        dirA = dirA - Vector3.Project(dirA, axis);
        dirB = dirB - Vector3.Project(dirB, axis);

        var angle = Vector3.Angle(dirA, dirB);
        return angle * (Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) < 0 ? -1 : 1);
    }

    //////////////////////////////////////
}
