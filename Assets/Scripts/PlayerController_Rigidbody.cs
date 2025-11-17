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

    // [MODIFIED] 아이템 들기 (범용성 있게 변경)
    [Header("Object Holding")]
    [SerializeField] private Transform holdPosition; // 아이템을 들고 있을 위치
    [SerializeField] private float throwForce = 10f; // 아이템을 던지는 힘

    // [NEW] 에너지 시스템
    [Header("Energy System")]
    [SerializeField] private PlayerEnergy playerEnergy; // 인스펙터에서 연결
    [SerializeField] private float sprintEnergyCost = 2f; // 초당 소모량
    [SerializeField] private float jumpEnergyCost = 5f;   // 1회 소모량
    [SerializeField] private float mineEnergyCost = 10f;  // 1회 소모량

    // [NEW] 채굴(Mining) 시스템
    [Header("Mining System")]
    [SerializeField] private float miningRange = 2f; // 채굴 가능 범위
    [SerializeField] private LayerMask oreLayer;     // "Ore" 레이어만 감지

    // Components
    private Rigidbody rb; 
    private InputSystem_Actions inputActions;

    // Movement
    private Vector3 moveDirection; 
    private float currentSpeed; 
    private bool isGrounded;
    private Vector2 moveInput;
    private bool isSprinting = false; // [NEW] 달리기 상태
    private HandcartController currentCartController = null;

    // [MODIFIED] 아이템 참조 (범용성 있게 변경)
    private HoldableObject nearbyHoldable = null; // 집을 수 있는 근처의 아이템
    private HoldableObject heldHoldable = null;   // 현재 들고 있는 아이템

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

        // [NEW] 에너지 컴포넌트 자동 찾기 (없으면 경고)
        if (playerEnergy == null)
        {
            playerEnergy = GetComponent<PlayerEnergy>();
        }
        if (playerEnergy == null)
        {
            Debug.LogWarning("PlayerEnergy 컴포넌트가 연결되지 않았습니다. 에너지 시스템이 작동하지 않습니다.");
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
        HandleCartInput();
        HandleInteraction(); // 줍기/내려놓기 (E키)
        HandleAttack();      // 아이템 사용 (마우스 좌클릭)
        HandleEnergyCosts(); // [NEW] 지속적인 에너지 소모 처리
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
        HandleHoldingItem(); // [MODIFIED]
    }

    void ReadInput()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        isSprinting = inputActions.Player.Sprint.IsPressed(); // [NEW]
    }

    void HandleGroundCheck()
    {
        // (이전 코드와 동일)
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
        // (이전 코드와 동일)
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

        // [MODIFIED] 달리기 시도 시 에너지 확인
        bool canSprint = playerEnergy == null || playerEnergy.GetCurrentEnergy() > 0;
        currentSpeed = (isSprinting && canSprint) ? sprintSpeed : moveSpeed;
    }

    // [NEW] 지속적인 에너지 소모 (달리기)
    void HandleEnergyCosts()
    {
        // 달리고 있고, 움직이고 있을 때
        if (isSprinting && moveDirection.magnitude > 0.1f)
        {
            playerEnergy?.UseEnergy(sprintEnergyCost * Time.deltaTime);
        }
    }

    void HandleMovement()
    {
        Vector3 targetVelocity = moveDirection * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y; 
        rb.linearVelocity = targetVelocity;
    }

    void HandleRotation()
    {
        // (이전 코드와 동일)
        float targetYRotation = cameraTransform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f);
        Quaternion newRotation = Quaternion.Slerp(
            rb.rotation, 
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime 
        );
        rb.MoveRotation(newRotation);
    }

    void HandleJump()
    {
        if (inputActions.Player.Jump.triggered && isGrounded)
        {
            // [NEW] 점프 시 에너지 소모
            if (playerEnergy != null)
            {
                // 에너지가 충분한지 확인 (에너지가 0이면 점프 안됨)
                if (playerEnergy.GetCurrentEnergy() <= 0) return;
                
                playerEnergy.UseEnergy(jumpEnergyCost);
            }

            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void HandleCursorToggle()
    {
        // (이전 코드와 동일)
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

    // ----- [MODIFIED] 리어카 및 아이템 상호작용 로직 -----

    private void OnTriggerEnter(Collider other)
    {
        // 리어카 로직 (이전과 동일)
        if (other.CompareTag("HandcartControlZone"))
        {
            currentCartController = other.GetComponentInParent<HandcartController>();
            if (currentCartController != null)
            {
                currentCartController.StartControl();
            }
        }
        // [MODIFIED] "Pickupable" 태그 대신 "Holdable" 태그 사용 권장
        else if (other.CompareTag("Pickupable") || other.CompareTag("Holdable")) 
        {
            // 아직 아무것도 안 들고 있을 때만
            if (heldHoldable == null)
            {
                // [MODIFIED] HoldableObject 컴포넌트를 찾음
                nearbyHoldable = other.GetComponent<HoldableObject>();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 리어카 로직 (이전과 동일)
        if (other.CompareTag("HandcartControlZone"))
        {
            HandcartController exitedCart = other.GetComponentInParent<HandcartController>();
            if (currentCartController == exitedCart && currentCartController != null)
            {
                currentCartController.StopControl();
                currentCartController = null;
            }
        }
        // [MODIFIED]
        else if (other.CompareTag("Pickupable") || other.CompareTag("Holdable"))
        {
            HoldableObject exitedItem = other.GetComponent<HoldableObject>();
            if (nearbyHoldable == exitedItem)
            {
                nearbyHoldable = null;
            }
        }
    }
    
    /// <summary>
    /// [MODIFIED] 'Interact' 버튼 (E키) - 줍기 / 내려놓기
    /// </summary>
    private void HandleInteraction()
    {
        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            // 1. 이미 아이템을 들고 있는 경우 -> 내려놓기
            if (heldHoldable != null)
            {
                heldHoldable.Drop();
                heldHoldable = null;
            }
            // 2. 아이템을 안 들고 있고, 근처에 아이템이 있는 경우 -> 줍기
            else if (nearbyHoldable != null)
            {
                heldHoldable = nearbyHoldable;
                heldHoldable.PickUp();
                
                // [NEW] holdPosition의 자식으로 만들어 위치 고정
                heldHoldable.transform.SetParent(holdPosition);
                heldHoldable.transform.localPosition = Vector3.zero;
                heldHoldable.transform.localRotation = Quaternion.identity;

                nearbyHoldable = null; 
            }
        }
    }

    /// <summary>
    /// [MODIFIED] 'Attack' 버튼 (마우스 좌클릭) - 아이템 사용 (채굴 / 던지기 / 먹기)
    /// </summary>
    private void HandleAttack()
    {
        if (!inputActions.Player.Attack.WasPressedThisFrame())
        {
            return; // 공격 키 안 눌렀으면 종료
        }

        if (heldHoldable == null)
        {
            return; // 아무것도 안 들고 있으면 종료
        }

        // --- 아이템 종류에 따라 다른 행동 ---

        // 1. 들고 있는 것이 'Food' 인가? (음식 먹기)
        // (가장 자주 할 행동이거나, 위험한 Pickaxe보다 먼저 체크하는 것이 좋습니다)
        if (heldHoldable is Food food)
        {
            Debug.Log("손에 든 아이템은 '음식'입니다.");

            // 이 음식이 '구매해야 하는' 아이템인지 확인
            BuyableItem buyable = food.GetComponent<BuyableItem>();

            bool canEat = false;
            
            if (buyable != null)
            {
                // 상점 아이템이라면, 구매(isPurchased)했을 때만 먹을 수 있음
                if (buyable.isPurchased)
                {
                    canEat = true;
                }
                else
                {
                    Debug.Log("아직 구매하지 않은 음식이라 먹을 수 없습니다.");
                    // TODO: "구매 전" 사운드 재생
                }
            }
            else
            {
                // BuyableItem 컴포넌트가 아예 없음 = 상점 아이템이 아님 (필드 드랍 등)
                // -> 즉시 먹을 수 있음
                canEat = true;
            }

            if (canEat)
            {
                // PlayerEnergy 컴포넌트가 있는지 확인 (Awake에서 이미 찾았음)
                if (playerEnergy != null)
                {
                    // Food.cs에 구현된 Consume 함수 호출
                    food.Consume(playerEnergy);

                    // 중요: 아이템을 먹어서 파괴했으므로, 손에서 비워야 함
                    heldHoldable = null; 
                }
                else
                {
                    Debug.LogWarning("PlayerEnergy 컴포넌트가 없어 음식을 먹을 수 없습니다!");
                }
            }
        }
        // 2. 들고 있는 것이 'Pickaxe' 인가? (채굴)
        else if (heldHoldable is Pickaxe pickaxe)
        {
            // (이전 채굴 로직... 그대로 둡니다)
            
            if (playerEnergy != null && playerEnergy.GetCurrentEnergy() <= 0)
            {
                Debug.Log("에너지가 부족하여 채굴할 수 없습니다.");
                return;
            }
            Debug.Log("곡괭이 휘두름!");
            playerEnergy?.UseEnergy(mineEnergyCost); 
            Collider[] hitOres = Physics.OverlapSphere(transform.position, miningRange, oreLayer);
            foreach (Collider oreCol in hitOres)
            {
                Ore ore = oreCol.GetComponent<Ore>();
                if (ore != null)
                {
                    Debug.Log($"광석 {ore.name} 발견! 데미지 {pickaxe.damage}!");
                    ore.TakeDamage(pickaxe.damage);
                    break; 
                }
            }
        }
        // 3. 들고 있는 것이 'PickupableBox' 인가? (던지기)
        else if (heldHoldable is PickupableBox box)
        {
            // (이전 던지기 로직... 그대로 둡니다)
            Debug.Log("상자 던지기!");
            ThrowHeldItem();
        }
        // 4. (추가) 다른 종류의 아이템이 있다면 else if ...
    }
    /// <summary>
    /// [NEW] 들고 있는 아이템을 던집니다.
    /// </summary>
    private void ThrowHeldItem()
    {
        if (heldHoldable == null) return;

        HoldableObject itemToThrow = heldHoldable;
        heldHoldable = null; // 참조 해제

        itemToThrow.Drop(); // 물리력 활성화, 부모 해제

        // 카메라 정면으로 힘 가하기
        Rigidbody itemRb = itemToThrow.GetComponent<Rigidbody>();
        if (itemRb != null)
        {
            itemRb.AddForce(cameraTransform.forward * throwForce, ForceMode.Impulse);
        }
    }


    /// <summary>
    /// [MODIFIED] (FixedUpdate에서 실행) 들고 있는 아이템의 위치를 'holdPosition'으로 고정
    /// (HandleInteraction에서 SetParent로 변경되어 이 함수는 이제 필요 없을 수 있으나,
    /// 물리적 충돌을 무시하고 강제로 위치를 고정하고 싶다면 이 로직을 사용합니다.)
    /// </summary>
    private void HandleHoldingItem()
    {
        if (heldHoldable != null)
        {
            // 이미 HandleInteraction에서 SetParent를 했다면, Rigidbody의 isKinematic = true
            // 속성 때문에 자동으로 따라옵니다.
            // 만약 SetParent를 사용하지 않는다면, 아래 코드로 위치를 강제 고정합니다.
            
            // rb.MovePosition(holdPosition.position);
            // rb.MoveRotation(holdPosition.rotation);
        }
    }

    /// <summary>
    /// 리어카 제어 '입력'을 '명령'으로 변환 (이전과 동일)
    /// </summary>
    private void HandleCartInput()
    {
        if (currentCartController == null)
        {
            return; 
        }

        if (inputActions.Player.RearCarUp.IsPressed())
        {
            currentCartController.MoveTargetY(1f); 
        }
        
        if (inputActions.Player.RearCarDown.IsPressed())
        {
            currentCartController.MoveTargetY(-1f);
        }
    }
}