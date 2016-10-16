using UnityEngine;


public class CameraFollow : MonoBehaviour
{
    private Camera mainCamera;
    private Transform mainCameraTransform;
    private Vector3 initOffsetToPlayer;

    public Transform target;

    public float cameraSmoothing = 0.01f;
    public Vector3 cameraVelocity = Vector3.zero;

    public Vector3 initPos;
    public Vector3 initRotate;

    void Start ()
	{
	    mainCamera = GetComponent<Camera>();
	    mainCameraTransform = transform;

        mainCameraTransform.localPosition = initPos;
        mainCameraTransform.localRotation = Quaternion.Euler(initRotate);

        initOffsetToPlayer = mainCameraTransform.position - target.position;

    }
	
	void LateUpdate ()
	{

	    var cameraTargetPosition = target.position + initOffsetToPlayer;

        mainCameraTransform.position = Vector3.SmoothDamp(mainCameraTransform.position, cameraTargetPosition, ref cameraVelocity, cameraSmoothing);
    }
}
