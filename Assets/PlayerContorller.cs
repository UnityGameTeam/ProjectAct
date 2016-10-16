using UnityEngine;

public class PlayerContorller : MonoBehaviour
{
    public float walkingSpeed = 5f;
    public float walkingSnappyness = 50;
    public float turningSmoothing = 0.3f;

    private Vector3 lastMoveDir;
    private float   timeOffset;
    private Vector3 moveDir;

    [SerializeField]
    Rigidbody m_Rigidbody;
    [SerializeField]
    Animator m_Animator;

    private FSM m_Fsm;
      
    void Start()
    {
        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        m_Fsm = gameObject.AddComponent<FSM>();
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        moveDir = v * Vector3.forward + h * Vector3.right;
    }

    void FixedUpdate()
    {
        var targetVelocity  = moveDir * walkingSpeed;
        var deltaVelocity  = targetVelocity - m_Rigidbody.velocity;

        if (m_Rigidbody.useGravity)
            deltaVelocity.y = 0;

        m_Rigidbody.AddForce(deltaVelocity*walkingSnappyness, ForceMode.Acceleration);

        if (moveDir == Vector3.zero)
        {
            m_Rigidbody.angularVelocity = Vector3.zero;
        }
        else
        {
            var deltaAngle = AngleAroundAxis(m_Rigidbody.transform.forward, moveDir, Vector3.up);
            m_Rigidbody.angularVelocity = (Vector3.up*deltaAngle*turningSmoothing);
        }
    }

    float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
    {
        dirA = dirA - Vector3.Project(dirA, axis);
        dirB = dirB - Vector3.Project(dirB, axis);

        var angle = Vector3.Angle(dirA, dirB);
        return angle * (Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) < 0 ? -1 : 1);
    }
}
