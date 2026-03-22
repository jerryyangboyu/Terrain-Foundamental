using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonPlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] InputActionAsset InputActions;
    [SerializeField] string ActionMapName = "Player";
    [SerializeField] string MoveActionName = "Move";
    [SerializeField] string LookActionName = "Look";
    [SerializeField] string SprintActionName = "Sprint";

    [Header("Movement")]
    [SerializeField] float WalkSpeed = 8f;
    [SerializeField] float SprintMultiplier = 1.5f;
    [SerializeField] float Gravity = -20f;

    [Header("Look")]
    [SerializeField] float MouseLookSensitivity = 0.08f;
    [SerializeField] float GamepadLookSpeed = 120f;
    [SerializeField] float MaxLookAngle = 85f;

    [Header("Controller")]
    [SerializeField] float ControllerHeight = 1.8f;
    [SerializeField] float ControllerRadius = 0.35f;
    [SerializeField] float CameraHeight = 1.65f;

    [Header("Spawn")]
    [SerializeField] float SpawnPadding = 32f;
    [SerializeField] float SpawnHeightOffset = 1f;
    [SerializeField] float FallbackSeaLevelWorldY = 40f;
    [SerializeField] int SpawnSearchAttempts = 24;

    [Header("Development")]
    [SerializeField] bool SpawnRandomlyOnPlay = true;

    CharacterController Controller;
    Transform CameraTransform;
    Camera PlayerCamera;
    InputActionMap PlayerActionMap;
    InputAction MoveAction;
    InputAction LookAction;
    InputAction SprintAction;

    float Pitch;
    float VerticalSpeed;

    void Awake()
    {
        EnsureRigSetup();
    }

    void OnEnable()
    {
        BindInputActions();
        LockCursor();
    }

    void Start()
    {
        if (SpawnRandomlyOnPlay)
        {
            AdjustInitialPlayerLocation();
        }
    }

    void Update()
    {
        if (CameraTransform == null)
        {
            return;
        }

        HandleCursorState();
        HandleLook();
        HandleMovement();
    }

    void OnDisable()
    {
        PlayerActionMap?.Disable();
    }

    void ConfigureCharacterController()
    {
        Controller.height = ControllerHeight;
        Controller.radius = ControllerRadius;
        Controller.center = new Vector3(0f, ControllerHeight * 0.5f, 0f);
        Controller.minMoveDistance = 0f;
    }

    void EnsureRigSetup()
    {
        if (Controller == null)
        {
            Controller = GetComponent<CharacterController>();
        }

        ConfigureCharacterController();
        AttachSceneCamera();
    }

    void AttachSceneCamera()
    {
        PlayerCamera = Camera.main;

        if (PlayerCamera == null)
        {
            PlayerCamera = FindFirstObjectByType<Camera>();
        }

        if (PlayerCamera == null)
        {
            GameObject cameraObject = new("FirstPersonCamera");
            PlayerCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        CameraTransform = PlayerCamera.transform;
        CameraTransform.SetParent(transform, false);
        CameraTransform.localPosition = new Vector3(0f, CameraHeight, 0f);
        CameraTransform.localRotation = Quaternion.identity;
        PlayerCamera.tag = "MainCamera";
    }

    void BindInputActions()
    {
        var actionAsset = InputActions != null ? InputActions : InputSystem.actions;
        if (actionAsset == null)
        {
            Debug.LogError("FirstPersonPlayerController could not find an InputActionAsset.");
            enabled = false;
            return;
        }

        PlayerActionMap = actionAsset.FindActionMap(ActionMapName, false);
        if (PlayerActionMap == null)
        {
            Debug.LogError($"FirstPersonPlayerController could not find action map '{ActionMapName}'.");
            enabled = false;
            return;
        }

        MoveAction = PlayerActionMap.FindAction(MoveActionName, true);
        LookAction = PlayerActionMap.FindAction(LookActionName, true);
        SprintAction = PlayerActionMap.FindAction(SprintActionName, false);

        PlayerActionMap.Enable();
    }

    void SpawnAtRandomTerrainPoint()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            terrain = FindFirstObjectByType<Terrain>();
        }

        if (terrain == null)
        {
            Debug.LogWarning("FirstPersonPlayerController did not find a terrain. Using the current transform position.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        float maxPadding = Mathf.Max(0f, Mathf.Min(terrainSize.x, terrainSize.z) * 0.5f - 1f);
        float padding = Mathf.Min(SpawnPadding, maxPadding);
        Vector3 spawnPosition = FindSpawnPosition(terrain, terrainOrigin, terrainSize, padding);

        bool wasEnabled = Controller.enabled;
        Controller.enabled = false;
        transform.SetPositionAndRotation(
            spawnPosition,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        Controller.enabled = wasEnabled;

        Pitch = 0f;
        CameraTransform.localPosition = new Vector3(0f, CameraHeight, 0f);
        CameraTransform.localRotation = Quaternion.identity;
    }

    public void AdjustInitialPlayerLocation()
    {
        EnsureRigSetup();
        SpawnAtRandomTerrainPoint();
    }

    Vector3 FindSpawnPosition(Terrain terrain, Vector3 terrainOrigin, Vector3 terrainSize, float padding)
    {
        float preferredSeaLevelWorldY = ResolveSeaLevelWorldY();
        Vector3 fallbackPosition = SampleSpawnPosition(terrain, terrainOrigin, terrainSize, padding, out _);
        int attempts = Mathf.Max(1, SpawnSearchAttempts);

        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = SampleSpawnPosition(terrain, terrainOrigin, terrainSize, padding, out float groundHeight);
            if (groundHeight >= preferredSeaLevelWorldY)
            {
                return candidate;
            }
        }

        return fallbackPosition;
    }

    float ResolveSeaLevelWorldY()
    {
        ProcGenManager procGenManager = FindFirstObjectByType<ProcGenManager>();
        return LakeWaterLevelResolver.ResolveSeaLevelWorldY(procGenManager, FallbackSeaLevelWorldY);
    }

    Vector3 SampleSpawnPosition(Terrain terrain, Vector3 terrainOrigin, Vector3 terrainSize, float padding, out float groundHeight)
    {
        float spawnX = Random.Range(padding, terrainSize.x - padding);
        float spawnZ = Random.Range(padding, terrainSize.z - padding);
        groundHeight = terrain.SampleHeight(new Vector3(terrainOrigin.x + spawnX, 0f, terrainOrigin.z + spawnZ))
            + terrainOrigin.y;
        float spawnY = groundHeight + SpawnHeightOffset;

        return new Vector3(terrainOrigin.x + spawnX, spawnY, terrainOrigin.z + spawnZ);
    }

    void HandleCursorState()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked || LookAction == null)
        {
            return;
        }

        Vector2 lookInput = LookAction.ReadValue<Vector2>();
        if (lookInput.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        bool usingMouse = LookAction.activeControl?.device is Mouse;
        float lookScale = usingMouse
            ? MouseLookSensitivity
            : GamepadLookSpeed * Time.deltaTime;

        transform.Rotate(Vector3.up * (lookInput.x * lookScale));

        Pitch = Mathf.Clamp(Pitch - lookInput.y * lookScale, -MaxLookAngle, MaxLookAngle);
        CameraTransform.localRotation = Quaternion.Euler(Pitch, 0f, 0f);
    }

    void HandleMovement()
    {
        Vector2 moveInput = MoveAction != null ? MoveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 planarMove = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (planarMove.sqrMagnitude > 1f)
        {
            planarMove.Normalize();
        }

        float moveSpeed = WalkSpeed;
        if (SprintAction != null && SprintAction.IsPressed())
        {
            moveSpeed *= SprintMultiplier;
        }

        if (Controller.isGrounded && VerticalSpeed < 0f)
        {
            VerticalSpeed = -2f;
        }

        VerticalSpeed += Gravity * Time.deltaTime;

        Vector3 velocity = planarMove * moveSpeed;
        velocity.y = VerticalSpeed;
        Controller.Move(velocity * Time.deltaTime);
    }

    static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
