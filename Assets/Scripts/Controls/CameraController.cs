using UnityEngine;
using UnityEngine.InputSystem;



public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;


    [Header("Horizontal Movement")]
    [SerializeField, Min(0f)] private float maxMoveSpeed = 12f;
    [SerializeField, Min(0f)] private float acceleration = 30f;
    [SerializeField, Min(0f)] private float deceleration = 35f;
    [SerializeField, Min(1f)] private float fastMoveMultiplier = 2f;


    [Header("Rotation")]
    [SerializeField, Min(0f)] private float rotationSpeed = 90f;


    [Header("Zoom")]
    [SerializeField, Min(0f)] private float zoomStep = 2f;
    [SerializeField, Min(0.1f)] private float minZoomDistance = 7f;
    [SerializeField, Min(0.1f)] private float maxZoomDistance = 40f;
    [SerializeField, Min(0f)] private float zoomDamping = 10f;


    private CameraControls cameraControls;
    private InputAction moveAction;
    private InputAction rotateAction;
    private InputAction fastMoveAction;

    private Vector3 targetMovement;
    private Vector3 horizontalVelocity;
    private Vector3 zoomDirection;
    private float currentZoomDistance;
    private float targetZoomDistance;



    private void Awake()
    {
        if (cameraTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
            {
                cameraTransform = childCamera.transform;


            }

        }

        if (cameraTransform == null || cameraTransform.localPosition.sqrMagnitude < 0.0001f)
        {
            Debug.LogError("Assign a child camera positioned away from CameraRig.", this);
            enabled = false;

            return;

        }

        cameraControls = new CameraControls();
        moveAction = cameraControls.Camera.MoveCamera;
        rotateAction = cameraControls.Camera.RotateCamera;
        fastMoveAction = cameraControls.Camera.FastMove;

        zoomDirection = cameraTransform.localPosition.normalized;
        currentZoomDistance = cameraTransform.localPosition.magnitude;
        maxZoomDistance = Mathf.Max(minZoomDistance, maxZoomDistance);
        targetZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);

        cameraTransform.LookAt(transform.position);

    }

    private void OnEnable()
    {
        cameraControls.Camera.ZoomCamera.performed += ZoomCamera;
        cameraControls.Camera.Enable();

    }

    private void OnDisable()
    {
        cameraControls.Camera.ZoomCamera.performed -= ZoomCamera;
        cameraControls.Camera.Disable();
        horizontalVelocity = Vector3.zero;

    }

    private void OnDestroy()
    {
        cameraControls?.Dispose();

    }

    private void Update()
    {
        RotateCamera();
        GetKeyboardMovement();
        UpdateBasePosition();
        UpdateCameraPosition();

    }

    private Vector3 GetCameraForward()
    {
        return Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;

    }

    private Vector3 GetCameraRight()
    {
        return Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

    }

    private void GetKeyboardMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        targetMovement = Vector3.ClampMagnitude( GetCameraRight() * input.x + GetCameraForward() * input.y, 1f);

    }

    private void UpdateBasePosition()
    {
        float speed = maxMoveSpeed;
        if (fastMoveAction.IsPressed())
        {
            speed *= fastMoveMultiplier;

        }

        Vector3 desiredVelocity = targetMovement * speed;
        float changeRate = targetMovement.sqrMagnitude > 0.0001f ? acceleration : deceleration;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVelocity, changeRate * Time.deltaTime);

        transform.position += horizontalVelocity * Time.deltaTime;

    }

    private void RotateCamera()
    {
        float input = rotateAction.ReadValue<float>();
        transform.Rotate(Vector3.up, input * rotationSpeed * Time.deltaTime, Space.World);

    }

    private void ZoomCamera(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<Vector2>().y;
        if (Mathf.Abs(scroll) < 0.001f)
        {
            return;

        }

        targetZoomDistance = Mathf.Clamp(targetZoomDistance - Mathf.Sign(scroll) * zoomStep, minZoomDistance, maxZoomDistance);

    }

    private void UpdateCameraPosition()
    {
        float blend = 1f - Mathf.Exp(-zoomDamping * Time.deltaTime);
        currentZoomDistance = Mathf.Lerp(currentZoomDistance, targetZoomDistance, blend);

        cameraTransform.localPosition = zoomDirection * currentZoomDistance;
        cameraTransform.LookAt(transform.position);

    }

}


