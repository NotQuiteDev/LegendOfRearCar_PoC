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

    [Header("Object Holding")] // [NEW]
    [SerializeField] private Transform holdPosition; // 상자를 들고 있을 위치 (플레이어 앞)
    [SerializeField] private float throwForce = 10f; // 상자를 던지는 힘

    

    // Components
    private Rigidbody rb; 
    private InputSystem_Actions inputActions;

    // Movement
    private Vector3 moveDirection; 
    private float currentSpeed;    
    private bool isGrounded;
    private Vector2 moveInput;
    private HandcartController currentCartController = null;
    private PickupableBox nearbyBox = null; // [NEW] 집을 수 있는 근처의 상자
    private PickupableBox heldBox = null;   // [NEW] 현재 들고 있는 상자

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
        HandleInteraction();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
        HandleHoldingBox();
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
        // [CHANGE] 플레이어가 항상 카메라의 Y축 방향을 바라보도록 변경
        
        // 1. 카메라의 Y축 회전값(수평)만 가져옵니다.
        float targetYRotation = cameraTransform.eulerAngles.y;

        // 2. Y축 회전값으로 목표 회전(Quaternion)을 생성합니다. (X, Z는 0으로 고정)
        Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f);

        // 3. Slerp를 사용해 부드럽게 그 방향으로 회전합니다.
        // (rotationSpeed를 매우 높게 설정하면(예: 100f) 즉시 반응하는 것처럼 보입니다.)
        Quaternion newRotation = Quaternion.Slerp(
            rb.rotation, 
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime 
        );

        // 4. Rigidbody의 회전을 물리적으로 안전하게 변경
        rb.MoveRotation(newRotation);
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
        else if (other.CompareTag("Pickupable"))
        {
            // 아직 아무것도 안 들고 있을 때만
            if (heldBox == null)
            {
                nearbyBox = other.GetComponent<PickupableBox>();
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
        else if (other.CompareTag("Pickupable"))
        {
            PickupableBox exitedBox = other.GetComponent<PickupableBox>();
            if (nearbyBox == exitedBox)
            {
                nearbyBox = null;
            }
        }
    }
    /// <summary>
    /// [NEW] 'Interact' 버튼 입력을 처리합니다. (줍기 / 놓기)
    /// '.triggered' 대신 '.WasPressedThisFrame()'으로 변경
    /// </summary>
    private void HandleInteraction()
    {
        // [IMPORTANT]
        // .triggered는 'Action Type'이 'Button'이 아닐 경우 
        // 제대로 작동하지 않을 수 있습니다.
        // .WasPressedThisFrame()은 '방금' 눌렸는지 확인하는 더 직접적인 방식입니다.
        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            Debug.Log("1. [Interact] 키 눌림! (WasPressedThisFrame)");

            // 1. 이미 상자를 들고 있는 경우 -> 내려놓기
            if (heldBox != null)
            {
                Debug.Log("2. 들고 있는 상자(heldBox)가 있음. 내려놓기 시도.");
                heldBox.Drop();
                heldBox = null;
            }
            // 2. 상자를 안 들고 있고, 근처에 상자가 있는 경우 -> 줍기
            else if (nearbyBox != null)
            {
                Debug.Log($"3. 근처에 상자 '{nearbyBox.name}' 감지! 줍기 시도.");
                heldBox = nearbyBox;
                heldBox.PickUp();
                nearbyBox = null; 
            }
            // 3. 둘 다 아닌 경우
            else
            {
                Debug.Log("2. 들고 있는 상자 없고, 3. 근처에도 상자 없음.");
            }
        }
    }
    /// <summary>
    /// [NEW] (FixedUpdate에서 실행) 들고 있는 상자의 위치를 'holdPosition'으로 고정
    /// </summary>
    private void HandleHoldingBox()
    {
        if (heldBox != null)
        {
            // Rigidbody의 위치를 물리적으로 안전하게 이동시킴
            heldBox.GetComponent<Rigidbody>().MovePosition(holdPosition.position);
            
            // (선택 사항) 상자가 플레이어를 바라보게 회전
            // heldBox.GetComponent<Rigidbody>().MoveRotation(holdPosition.rotation);
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