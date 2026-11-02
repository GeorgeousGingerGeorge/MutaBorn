using UnityEngine;
using UnityEngine.InputSystem;



public class CameraController : MonoBehaviour
{
    private static CameraController activeInstance;

    public static CameraController Instance
    {
        get
        {
            if (activeInstance == null || !activeInstance.isActiveAndEnabled)
            {
                activeInstance = null;

                // A disabled or destroyed rig must not keep receiving focus requests.
                foreach (CameraController candidate in FindObjectsByType<CameraController>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (candidate != null && candidate.isActiveAndEnabled && candidate.cameraTransform != null)
                    {
                        activeInstance = candidate;

                        break;

                    }

                }

            }

            return activeInstance;

        }
        private set
        {
            activeInstance = value;

        }

    }


    [Header("References")]
    [SerializeField] private Transform cameraTransform;


    [Header("Horizontal Movement")]
    [SerializeField, Min(0f)] private float maxMoveSpeed = 12f;
    [SerializeField, Min(0f)] private float acceleration = 30f;
    [SerializeField, Min(0f)] private float deceleration = 35f;
    [SerializeField, Min(1f)] private float fastMoveMultiplier = 2f;


    [Header("Automatic Unit Focus")]
    [SerializeField, Min(0.1f)] private float focusTravelSpeed = 12f;
    [SerializeField, Min(0.01f)] private float focusSmoothTime = 0.22f;
    [SerializeField, Min(0f)] private float focusArrivalDistance = 0.05f;
    [SerializeField] private bool focusOnUnitSwitch = true;
    [SerializeField] private bool focusOnAction = true;
    [SerializeField] private bool cancelFocusOnManualMovement = true;
    [SerializeField] private bool logFocusDiagnostics = false;


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
    private Transform focusTarget;
    private Vector3 focusVelocity;
    private bool isFocusing;





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

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple CameraControllers found; unit focus will use the first active rig.", this);

        }
        else
        {
            Instance = this;

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
        if (Instance == null)
        {
            Instance = this;

        }

        cameraControls.Camera.ZoomCamera.performed += ZoomCamera;
        cameraControls.Camera.Enable();

    }

    private void OnDisable()
    {
        if (activeInstance == this)
        {
            activeInstance = null;

        }

        cameraControls.Camera.ZoomCamera.performed -= ZoomCamera;
        cameraControls.Camera.Disable();
        horizontalVelocity = Vector3.zero;
        CancelFocus();

    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;

        }

        cameraControls?.Dispose();

    }

    private void Update()
    {
        RotateCamera();
        GetKeyboardMovement();
        if (isFocusing && cancelFocusOnManualMovement && targetMovement.sqrMagnitude > 0.0001f)
        {
            CancelFocus();

        }

        UpdateBasePosition();
        UpdateCameraPosition();

    }

    // This only moves the CameraRig pivot. Rotation and zoom stay under player control.
    public void FocusOnUnit(UnitController unit, bool actionFocus = false)
    {
        if (!isActiveAndEnabled || unit == null || !unit.isActiveAndEnabled ||
            unit.data == null || unit.data.currentHealth <= 0 ||
            (actionFocus ? !focusOnAction : !focusOnUnitSwitch))
        {
            if (logFocusDiagnostics)
            {
                Debug.LogWarning($"Camera focus rejected: controller active={isActiveAndEnabled}, " +
                    $"unit={(unit != null ? unit.name : "missing")}, " +
                    $"unit ready={unit != null && unit.isActiveAndEnabled && unit.data != null}, " +
                    $"action focus={actionFocus}, allowed={(actionFocus ? focusOnAction : focusOnUnitSwitch)}.", this);

            }

            return;

        }

        Transform target = unit.transform;
        if (isFocusing && focusTarget == target)
        {
            return;

        }

        focusTarget = target;
        isFocusing = true;
        horizontalVelocity = Vector3.zero;
        focusVelocity = Vector3.zero;

        if (logFocusDiagnostics)
        {
            Debug.Log($"Camera focus started: {unit.name} (action focus={actionFocus}).", this);

        }

    }

    public void CancelFocus()
    {
        if (isFocusing && logFocusDiagnostics)
        {
            Debug.Log($"Camera focus ended/canceled: " +
                $"{(focusTarget != null ? focusTarget.name : "no target")}", this);

        }

        isFocusing = false;
        focusTarget = null;
        focusVelocity = Vector3.zero;

    }

    [ContextMenu("Test / Focus Selected Player Unit")]
    private void TestFocusSelectedPlayerUnit()
    {
        SelectionManager selection = FindFirstObjectByType<SelectionManager>();
        if (selection == null || selection.selectedUnit == null)
        {
            Debug.LogWarning("Camera focus test: no selected player unit.", this);

            return;

        }

        Debug.Log($"Camera focus test requested for {selection.selectedUnit.name}.", this);
        FocusOnUnit(selection.selectedUnit);

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
        if (isFocusing)
        {
            if (focusTarget == null)
            {
                CancelFocus();

            }
            else
            {
                Vector3 destination = focusTarget.position;
                if ((transform.position - destination).sqrMagnitude <=
                    focusArrivalDistance * focusArrivalDistance)
                {
                    transform.position = destination;
                    CancelFocus();

                }
                else
                {
                    transform.position = Vector3.SmoothDamp(transform.position, destination,
                        ref focusVelocity, focusSmoothTime, focusTravelSpeed, Time.deltaTime);

                }

                return;

            }

        }

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


