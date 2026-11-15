using System.Collections;
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
                foreach (CameraController candidate in FindObjectsByType<CameraController>(FindObjectsInactive.Exclude))
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


    [Header("Player Turn Camera Return")]
    [SerializeField, Min(0.05f)] private float returnToStartDuration = 0.4f;

    [Header("Damage Camera Shake")]
    [SerializeField] private bool shakeOnDamage = true;
    [SerializeField, Min(0.01f)] private float damageShakeDuration = 0.28f;
    [SerializeField, Min(0f)] private float shakeAt20Damage = 0.10f;
    [SerializeField, Min(0f)] private float shakeAt40Damage = 0.24f;
    [SerializeField, Min(0f)] private float shakeAt80Damage = 0.55f;

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
    private TurnManager turnManager;
    private Coroutine playerTurnFocusRoutine;
    private Vector3 startPosition;
    private Vector3 returnFromPosition;
    private float returnElapsed;
    private bool returningToStart;
    private float shakeTimeRemaining;
    private float shakeStrength;





    private void Awake()
    {
        cameraControls = new CameraControls();
        moveAction = cameraControls.Camera.MoveCamera;
        rotateAction = cameraControls.Camera.RotateCamera;
        fastMoveAction = cameraControls.Camera.FastMove;

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

        startPosition = transform.position;
        zoomDirection = cameraTransform.localPosition.normalized;
        currentZoomDistance = cameraTransform.localPosition.magnitude;
        maxZoomDistance = Mathf.Max(minZoomDistance, maxZoomDistance);
        targetZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);

        cameraTransform.LookAt(transform.position);

    }

    private void OnEnable()
    {
        if (cameraControls == null)
        {
            return;

        }

        if (Instance == null)
        {
            Instance = this;

        }

        cameraControls.Camera.ZoomCamera.performed += ZoomCamera;
        cameraControls.Camera.Enable();
        CombatActionExecutor.DamageApplied += OnDamageApplied;
        SubscribeToTurns();

    }

    private void Start()
    {
        SubscribeToTurns();
        if (turnManager != null && turnManager.IsPlayerTurn && playerTurnFocusRoutine == null)
        {
            BeginPlayerTurnFocus();

        }

    }

    private void SubscribeToTurns()
    {
        TurnManager current = TurnManager.Instance;
        if (turnManager == current)
        {
            return;

        }

        if (turnManager != null)
        {
            turnManager.TurnStarted -= OnTurnStarted;

        }

        turnManager = current;
        if (turnManager != null)
        {
            turnManager.TurnStarted += OnTurnStarted;

        }

    }

    private void OnTurnStarted(FactionData faction)
    {
        if (turnManager != null && faction == turnManager.PlayerFaction)
        {
            BeginPlayerTurnFocus();

        }

    }

    private void BeginPlayerTurnFocus()
    {
        if (playerTurnFocusRoutine != null)
        {
            StopCoroutine(playerTurnFocusRoutine);

        }

        playerTurnFocusRoutine = StartCoroutine(PreparePlayerTurnFocus());

    }

    private IEnumerator PreparePlayerTurnFocus()
    {
        // Allow SelectionManager to pick a surviving player unit after turn changes.
        yield return null;

        SelectionManager selection = FindAnyObjectByType<SelectionManager>();
        if (turnManager == null || !turnManager.IsPlayerTurn || selection == null ||
            selection.selectedUnit == null || !selection.selectedUnit.isActiveAndEnabled)
        {
            playerTurnFocusRoutine = null;

            yield break;

        }

        CancelFocus();
        horizontalVelocity = Vector3.zero;
        returnFromPosition = transform.position;
        returnElapsed = 0f;
        focusTarget = selection.selectedUnit.transform;
        returningToStart = true;
        playerTurnFocusRoutine = null;

    }

    private void OnDamageApplied(CombatDamageEvent damage)
    {
        if (!shakeOnDamage || damage.ActualDamage <= 0)
        {
            return;

        }

        float amount = damage.ActualDamage <= 20
            ? Mathf.Lerp(0f, shakeAt20Damage, damage.ActualDamage / 20f)
            : damage.ActualDamage <= 40
                ? Mathf.Lerp(shakeAt20Damage, shakeAt40Damage, (damage.ActualDamage - 20f) / 20f)
                : Mathf.Lerp(shakeAt40Damage, shakeAt80Damage,
                    Mathf.Clamp01((damage.ActualDamage - 40f) / 40f));

        shakeStrength = Mathf.Max(amount, shakeStrength *
            (shakeTimeRemaining / Mathf.Max(0.01f, damageShakeDuration)));
        shakeTimeRemaining = damageShakeDuration;

    }

    private void OnDisable()
    {
        CombatActionExecutor.DamageApplied -= OnDamageApplied;
        if (turnManager != null)
        {
            turnManager.TurnStarted -= OnTurnStarted;
            turnManager = null;

        }

        if (playerTurnFocusRoutine != null)
        {
            StopCoroutine(playerTurnFocusRoutine);
            playerTurnFocusRoutine = null;

        }

        returningToStart = false;
        shakeTimeRemaining = 0f;
        if (cameraTransform != null && zoomDirection.sqrMagnitude > 0f)
        {
            cameraTransform.localPosition = zoomDirection * currentZoomDistance;
            cameraTransform.LookAt(transform.position);

        }

        if (activeInstance == this)
        {
            activeInstance = null;

        }

        if (cameraControls != null)
        {
            cameraControls.Camera.ZoomCamera.performed -= ZoomCamera;
            cameraControls.Camera.Disable();

        }

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
        if ((isFocusing || returningToStart) && cancelFocusOnManualMovement &&
            targetMovement.sqrMagnitude > 0.0001f)
        {
            returningToStart = false;
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

        returningToStart = false;
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
        SelectionManager selection = FindAnyObjectByType<SelectionManager>();
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
        if (returningToStart)
        {
            returnElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(returnElapsed / returnToStartDuration);
            transform.position = Vector3.Lerp(returnFromPosition, startPosition,
                Mathf.SmoothStep(0f, 1f, progress));

            if (progress >= 1f)
            {
                returningToStart = false;
                SelectionManager selection = FindAnyObjectByType<SelectionManager>();
                UnitController selected = selection != null ? selection.selectedUnit : null;
                if (selected != null && selected.isActiveAndEnabled && selected.data != null &&
                    selected.data.currentHealth > 0 && turnManager != null && turnManager.IsPlayerTurn)
                {
                    focusTarget = selected.transform;
                    isFocusing = true;
                    focusVelocity = Vector3.zero;

                }
                else
                {
                    CancelFocus();

                }

            }

            return;

        }

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

        Vector3 shakeOffset = Vector3.zero;
        if (shakeTimeRemaining > 0f)
        {
            shakeTimeRemaining = Mathf.Max(0f, shakeTimeRemaining - Time.unscaledDeltaTime);
            float strength = shakeStrength * (shakeTimeRemaining / damageShakeDuration);
            float phase = Time.unscaledTime * 32f;
            shakeOffset = new Vector3(
                Mathf.PerlinNoise(phase, 0.37f) * 2f - 1f,
                Mathf.PerlinNoise(0.73f, phase) * 2f - 1f,
                0f) * strength;

        }

        cameraTransform.localPosition = zoomDirection * currentZoomDistance + shakeOffset;
        cameraTransform.LookAt(transform.position);

    }

}


