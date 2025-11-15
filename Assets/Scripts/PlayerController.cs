using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController_Rigidbody : MonoBehaviour // (클래스 이름 변경)
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f; // 플레이어 회전 속도

    // [CHANGE] jumpForce는 이제 '점프 높이'가 아니라 '순간적인 힘(Impulse)'의 크기입니다.
    // 인스펙터에서 5~10 정도의 값으로 새로 조절해야 합니다.
    [SerializeField] private float jumpForce = 5f; 

    // [CHANGE] Rigidbody가 중력을 자동으로 처리하므로 gravity 변수는 필요 없습니다.
    // [SerializeField] private float gravity = -9.81f; 

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform; // Main Camera

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    // Components
    private Rigidbody rb; // [CHANGE] CharacterController -> Rigidbody
    private InputSystem_Actions inputActions;

    // Movement
    private Vector3 moveDirection; // [NEW] FixedUpdate에서 사용할 이동 방향
    private float currentSpeed;    // [NEW] FixedUpdate에서 사용할 현재 속도
    private bool isGrounded;
    private Vector2 moveInput;

    void Awake()
    {
        // [CHANGE] Rigidbody 컴포넌트를 가져옵니다.
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody 컴포넌트가 없습니다! 추가해주세요.");
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // [NEW] Rigidbody가 물리적으로 넘어지지 않도록 X, Z축 회전을 고정합니다.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        inputActions = new InputSystem_Actions();

        // Main Camera 자동 찾기
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
    }

    void OnDisable()
    {
        inputActions.Player.Disable();
    }

    void Start()
    {
        // 마우스 커서 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update에서는 '입력'과 '상태 확인'을 주로 처리합니다.
    void Update()
    {
        HandleGroundCheck();
        ReadInput();
        CalculateMovementDirection(); // [NEW] 이동 방향과 속도를 '계산'만 합니다.
        HandleJump();
        HandleCursorToggle();
    }

    // FixedUpdate에서는 '물리' 관련 처리를 합니다.
    void FixedUpdate()
    {
        // 계산된 값을 바탕으로 '실제 물리적 이동'을 적용합니다.
        HandleMovement();
        HandleRotation();
    }

    void ReadInput()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
    }

    void HandleGroundCheck()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            // Rigidbody는 isGrounded 속성이 없으므로 groundCheck가 필수입니다.
            Debug.LogWarning("GroundCheck Transform이 설정되지 않았습니다.");
            isGrounded = false; // 기본값
        }
    }

    // [NEW] 실제 이동을 적용하기 전에 방향과 속도를 미리 계산합니다.
    void CalculateMovementDirection()
    {
        // 카메라 방향 기준으로 이동 방향 계산
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Y축 제거 (수평 이동만)
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 이동 방향 = 카메라 forward * W/S + 카메라 right * A/D
        moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

        // 스프린트
        currentSpeed = inputActions.Player.Sprint.IsPressed() ? sprintSpeed : moveSpeed;
    }

    // [CHANGE] FixedUpdate에서 실행되며, Rigidbody의 속도를 제어합니다.
    void HandleMovement()
    {
        // 계산된 이동 방향과 속도로 목표 속도를 설정합니다.
        Vector3 targetVelocity = moveDirection * currentSpeed;

        // [IMPORTANT] Y축 속도(중력, 점프)는 Rigidbody의 현재 값을 유지해야 합니다.
        targetVelocity.y = rb.linearVelocity.y;

        // Rigidbody의 속도를 직접 설정하여 즉각적인 반응을 만듭니다.
        rb.linearVelocity = targetVelocity;
    }

    // [NEW] 회전 로직을 분리하여 FixedUpdate에서 처리합니다.
    void HandleRotation()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            // Slerp를 사용하여 부드러운 회전 적용
            Quaternion newRotation = Quaternion.Slerp(
                rb.rotation, // transform.rotation 대신 rb.rotation 사용
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime // Time.deltaTime 대신 Time.fixedDeltaTime 사용
            );

            // Rigidbody의 회전을 물리적으로 안전하게 변경
            rb.MoveRotation(newRotation);
        }
    }

    // [CHANGE] 점프와 중력 로직이 대폭 변경됩니다.
    void HandleJump()
    {
        // 점프 (Update에서 입력을 감지)
        if (inputActions.Player.Jump.triggered && isGrounded)
        {
            // [IMPORTANT] 수동으로 Y 속도를 계산하는 대신,
            // Rigidbody에 '위쪽'으로 '순간적인 힘(Impulse)'을 가합니다.
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // [DELETED] 수동 중력 계산(velocity.y += gravity...)이 모두 삭제되었습니다.
        // Rigidbody의 'Use Gravity'가 이 역할을 대신합니다.
    }

    void HandleCursorToggle()
    {
        // (이 함수는 기존과 동일합니다)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}