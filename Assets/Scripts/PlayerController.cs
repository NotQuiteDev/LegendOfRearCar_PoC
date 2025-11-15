using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController_Rigidbody : MonoBehaviour 
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f; 
    [SerializeField] private float jumpForce = 5f; 

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform; 

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    // Components
    private Rigidbody rb; 
    private InputSystem_Actions inputActions;

    // Movement
    private Vector3 moveDirection; 
    private float currentSpeed;    
    private bool isGrounded;
    private Vector2 moveInput;
    private HandcartController currentCartController = null;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody 컴포넌트가 없습니다! 추가해주세요.");
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        inputActions = new InputSystem_Actions();

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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleGroundCheck();
        ReadInput();
        CalculateMovementDirection(); 
        HandleJump();
        HandleCursorToggle();

        // [NEW] 리어카 입력 처리
        HandleCartInput();
    }

    void FixedUpdate()
    {
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
            Debug.LogWarning("GroundCheck Transform이 설정되지 않았습니다.");
            isGrounded = false; 
        }
    }
 
    void CalculateMovementDirection()
    {
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

        currentSpeed = inputActions.Player.Sprint.IsPressed() ? sprintSpeed : moveSpeed;
    }

    void HandleMovement()
    {
        Vector3 targetVelocity = moveDirection * currentSpeed;

        // [FIXED] Y축 속도(중력, 점프)는 Rigidbody의 현재 값을 유지해야 합니다.
        // 'linearVelocity'가 아니라 'velocity'가 올바른 속성 이름입니다.
        targetVelocity.y = rb.linearVelocity.y; 

        // [FIXED] Rigidbody의 속도를 직접 설정합니다.
        rb.linearVelocity = targetVelocity;
    }

    void HandleRotation()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            Quaternion newRotation = Quaternion.Slerp(
                rb.rotation, 
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime 
            );

            rb.MoveRotation(newRotation);
        }
    }

    void HandleJump()
    {
        if (inputActions.Player.Jump.triggered && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void HandleCursorToggle()
    {
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

    // ----- [NEW] 리어카 로직 -----

    /// <summary>
    /// 플레이어가 리어카 제어 구역(트리거)에 들어갔을 때 호출됩니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 들어간 트리거의 태그가 "HandcartControlZone"인지 확인합니다.
        if (other.CompareTag("HandcartControlZone"))
        {
            // 그 트리거의 부모에서 Handcart 스크립트를 찾아서 저장합니다.
            currentCartController = other.GetComponentInParent<HandcartController>();
            if (currentCartController != null)
            {
                // [NEW] 리어카 컨트롤러에 제어 시작을 알립니다.
                currentCartController.StartControl();
            }
        }
    }

    /// <summary>
    /// 플레이어가 리어카 제어 구역(트리거)에서 나갔을 때 호출됩니다.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HandcartControlZone"))
        {
            HandcartController exitedCart = other.GetComponentInParent<HandcartController>();
            if (currentCartController == exitedCart && currentCartController != null)
            {
                // [NEW] 리어카 컨트롤러에 제어 종료를 알립니다.
                currentCartController.StopControl();
                currentCartController = null;
            }
        }
    }

    /// <summary>
    /// 리어카 제어 '입력'을 '명령'으로 변환하여 컨트롤러에 전달합니다.
    /// </summary>
    private void HandleCartInput() // (이전 HandleCartControl)
    {
        // 제어할 리어카가 없으면(= 영역 밖에 있으면) 아무것도 하지 않습니다.
        if (currentCartController == null)
        {
            return; 
        }

        // RearCarUp 입력 감지
        if (inputActions.Player.RearCarUp.IsPressed())
        {
            // [CHANGE] 목표 Y값을 직접 올리라고 '명령'합니다.
            currentCartController.MoveTargetY(1f); 
        }
        
        // RearCarDown 입력 감지
        if (inputActions.Player.RearCarDown.IsPressed())
        {
            // [CHANGE] 목표 Y값을 직접 내리라고 '명령'합니다.
            currentCartController.MoveTargetY(-1f);
        }
    }
}