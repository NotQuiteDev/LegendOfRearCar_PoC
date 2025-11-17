using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // [NEW] List를 사용하기 위해 추가

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

    // [MODIFIED] 아이템 들기 (복수형으로 변경)
    [Header("Object Holding")]
    [Tooltip("아이템을 5개 들기 위한 5개의 위치 (플레이어 자식)")]
    [SerializeField] private List<Transform> holdPositions; // [NEW] 5개의 슬롯 Transform
    [SerializeField] private int maxHeldItems = 5;          // [NEW] 최대 아이템 개수
    [SerializeField] private float throwForce = 3f;      

    [Header("Energy System")]
    [SerializeField] private PlayerEnergy playerEnergy; 
    [SerializeField] private float sprintEnergyCost = 2f; 
    [SerializeField] private float jumpEnergyCost = 5f;   

    // Components
    private Rigidbody rb; 
    private InputSystem_Actions inputActions;

    // Movement
    private Vector3 moveDirection; 
    private float currentSpeed; 
    private bool isGrounded;
    private Vector2 moveInput;
    private bool isSprinting = false; 
    private HandcartController currentCartController = null;

    // [MODIFIED] 아이템 참조 (List로 변경)
    private HoldableObject nearbyHoldable = null; // 집을 수 있는 근처의 아이템
    private List<HoldableObject> heldItems = new List<HoldableObject>(); // [MODIFIED]

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

        if (playerEnergy == null)
        {
            playerEnergy = GetComponent<PlayerEnergy>();
        }
        if (playerEnergy == null)
        {
            Debug.LogWarning("PlayerEnergy 컴포넌트가 연결되지 않았습니다.");
        }

        // [NEW] Hold Positions 5개 할당되었는지 확인
        if (holdPositions == null || holdPositions.Count < maxHeldItems)
        {
            Debug.LogError($"[PlayerController] {maxHeldItems}개의 'Hold Positions'가 필요합니다! " +
                             $"플레이어 자식으로 빈 오브젝트 5개를 만들고, 리스트에 할당해주세요.", this.gameObject);
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
        
        // [MODIFIED] 키 입력 로직 분리
        HandleInteraction(); // 줍기 (E키)
        HandleDrop();        // [NEW] 버리기 (Crouch 키)
        HandleAttack();      // 아이템 사용 (마우스 좌클릭)
        
        HandleEnergyCosts(); 
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
        
        // [MODIFIED] 
        // 줍는 즉시 SetParent 방식으로 변경했기 때문에, 
        // FixedUpdate에서 무겁게 위치를 고정할 필요가 없어졌습니다.
        // HandleHoldingItems(); 
    }

    void ReadInput()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        isSprinting = inputActions.Player.Sprint.IsPressed(); 
    }

    void HandleGroundCheck()
    {
        // (이전과 동일)
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
        // (이전과 동일)
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();
        moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
        bool canSprint = playerEnergy == null || playerEnergy.GetCurrentEnergy() > 0;
        currentSpeed = (isSprinting && canSprint) ? sprintSpeed : moveSpeed;
    }

    void HandleEnergyCosts()
    {
        // (이전과 동일)
        if (isSprinting && moveDirection.magnitude > 0.1f)
        {
            playerEnergy?.UseEnergy(sprintEnergyCost * Time.deltaTime);
        }
    }

    void HandleMovement()
    {
        // (이전과 동일)
        Vector3 targetVelocity = moveDirection * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y; 
        rb.linearVelocity = targetVelocity;
    }

    void HandleRotation()
    {
        // (이전과 동일)
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
        // (이전과 동일)
        if (inputActions.Player.Jump.triggered && isGrounded)
        {
            if (playerEnergy != null)
            {
                if (playerEnergy.GetCurrentEnergy() <= 0) return;
                playerEnergy.UseEnergy(jumpEnergyCost);
            }
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void HandleCursorToggle()
    {
        // (이전과 동일)
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
        // (리어카 로직 - 이전과 동일)
        if (other.CompareTag("HandcartControlZone"))
        {
            currentCartController = other.GetComponentInParent<HandcartController>();
            if (currentCartController != null)
            {
                currentCartController.StartControl();
            }
        }
        // [MODIFIED] 아이템 감지 로직
        else if (other.CompareTag("Pickupable") || other.CompareTag("Holdable")) 
        {
            // 아직 줍기 가능한 아이템을 기억하지 못했다면
            if (nearbyHoldable == null)
            {
                HoldableObject item = other.GetComponent<HoldableObject>();
                
                // (중요) 이미 들고 있는 아이템이 다시 감지되는 것 방지
                if (item != null && !heldItems.Contains(item))
                {
                    nearbyHoldable = item;
                    Debug.Log($"[Player] 주울 수 있는 아이템 감지: {nearbyHoldable.name}");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // (리어카 로직 - 이전과 동일)
        if (other.CompareTag("HandcartControlZone"))
        {
            HandcartController exitedCart = other.GetComponentInParent<HandcartController>();
            if (currentCartController == exitedCart && currentCartController != null)
            {
                currentCartController.StopControl();
                currentCartController = null;
            }
        }
        // [MODIFIED] 아이템 이탈 로직
        else if (other.CompareTag("Pickupable") || other.CompareTag("Holdable"))
        {
            HoldableObject exitedItem = other.GetComponent<HoldableObject>();
            
            // 기억하고 있던 줍기 가능 아이템이 범위를 나갔다면
            if (nearbyHoldable == exitedItem)
            {
                nearbyHoldable = null;
                Debug.Log($"[Player] 주울 수 있는 아이템 범위 이탈: {exitedItem.name}");
            }
        }
    }
    
    /// <summary>
    /// [MODIFIED] 'Interact' 버튼 (E키) - 오직 '줍기'만 담당
    /// </summary>
    private void HandleInteraction()
    {
        // Interact(E) 키를 눌렀을 때
        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            // 1. 근처에 주울 아이템이 있고
            // 2. 인벤토리(리스트)에 자리가 있다면 (최대 5개)
            if (nearbyHoldable != null && heldItems.Count < maxHeldItems)
            {
                Debug.Log($"[Player] {nearbyHoldable.name} 줍기 시도...");
                
                HoldableObject itemToPickUp = nearbyHoldable;
                nearbyHoldable = null; // 근처 아이템 참조 해제
                
                itemToPickUp.PickUp(); // 아이템 상태 변경 (물리 끄기)
                heldItems.Add(itemToPickUp); // 리스트(스택)에 추가

                // [NEW] 줍는 즉시 올바른 슬롯(Hold Position)에 배치
                int newItemIndex = heldItems.Count - 1;
                if (newItemIndex < holdPositions.Count && holdPositions[newItemIndex] != null)
                {
                    Transform targetSlot = holdPositions[newItemIndex];
                    itemToPickUp.transform.SetParent(targetSlot);
                    itemToPickUp.transform.localPosition = Vector3.zero;
                    itemToPickUp.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    Debug.LogError($"Hold Position {newItemIndex}가 할당되지 않았습니다!");
                }
            }
            // (기존 '내려놓기' 로직은 HandleDrop으로 완전히 이동됨)
        }
    }

    /// <summary>
    /// [NEW] 'Crouch' 버튼 (예: L-Ctrl) - 아이템 버리기 (FILO)
    /// </summary>
    private void HandleDrop()
    {
        // Crouch 키를 눌렀고, 들고 있는 아이템이 1개 이상일 때
        // (Input Action의 'Crouch'가 'Button' 타입으로 설정되어야 .triggered가 작동)
        if (inputActions.Player.Crouch.triggered && heldItems.Count > 0)
        {
            Debug.Log("[Player] 마지막 아이템 버리기 (FILO) 시도...");
            
            // 1. FILO (First In, Last Out) -> 리스트의 맨 마지막 아이템 (마지막 인덱스)
            int lastItemIndex = heldItems.Count - 1;
            HoldableObject itemToDrop = heldItems[lastItemIndex];
            
            // 2. 리스트(인벤토리)에서 제거
            heldItems.RemoveAt(lastItemIndex);
            
            // 3. 아이템을 물리적으로 내려놓음 (Drop이 SetParent(null)을 처리함)
            itemToDrop.Drop(); 
            
            // (선택 사항) 살짝 앞으로 던지는 효과
            itemToDrop.GetComponent<Rigidbody>()?.AddForce(cameraTransform.forward * 2f, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// [MODIFIED] 'Attack' 버튼 (마우스 좌클릭) - 아이템 사용
    /// (조건: 아이템을 '딱 하나'만 들고 있을 때)
    /// </summary>
    private void HandleAttack()
    {
        // Attack(좌클릭) 키를 누르지 않았으면 종료
        if (!inputActions.Player.Attack.WasPressedThisFrame())
        {
            return; 
        }

        // [NEW] 요청 사항: 딱 1개 들고 있을 때만 사용 가능
        if (heldItems.Count != 1)
        {
            if (heldItems.Count > 1)
            {
                Debug.Log("[Player] 아이템을 여러 개 들고 있어 사용할 수 없습니다.");
            }
            else
            {
                // (손에 든 게 0개일 때)
            }
            return; // 0개 또는 2개 이상이면 사용 불가
        }
        
        // 유일하게 들고 있는 아이템 (인덱스 0)
        HoldableObject itemToUse = heldItems[0]; 

        // 1. 들고 있는 것이 'Food' 인가? (음식 먹기)
        if (itemToUse is Food food)
        {
            // (이전과 동일한 음식 먹기 로직)
            Debug.Log("손에 든 아이템은 '음식'입니다.");
            BuyableItem buyable = food.GetComponent<BuyableItem>();
            bool canEat = false;
            
            if (buyable != null)
            {
                if (buyable.isPurchased) canEat = true;
                else Debug.Log("아직 구매하지 않은 음식이라 먹을 수 없습니다.");
            }
            else
            {
                canEat = true; // 상점템이 아니면 바로 먹기 가능
            }

            if (canEat)
            {
                if (playerEnergy != null)
                {
                    food.Consume(playerEnergy); // 음식 먹고 스스로 파괴됨
                    heldItems.RemoveAt(0);      // [MODIFIED] 리스트(인벤토리)에서 제거
                }
                else
                {
                    Debug.LogWarning("PlayerEnergy 컴포넌트가 없어 음식을 먹을 수 없습니다!");
                }
            }
        }
        
        // 2. 들고 있는 것이 'Pickaxe' 인가? (채굴)
        else if (itemToUse is Pickaxe pickaxe)
        {
            // (이전과 동일한 곡괭이 사용 로직)
            pickaxe.Use(playerEnergy, transform.position); 
            
            // [NEW] 곡괭이가 사용 중 파괴되었는지 확인
            // (Pickaxe.cs가 Destroy()를 호출하면, 
            //  이 변수는 다음 프레임이나 즉시 null처럼 동작할 수 있음)
            if (pickaxe == null) // 유니티에서 파괴된 오브젝트는 null로 취급됨
            {
                Debug.Log("[Player] 곡괭이가 파괴되어 손에서 사라집니다.");
                heldItems.RemoveAt(0); // 리스트에서 제거
            }
        }
        
        // 3. 들고 있는 것이 'PickupableBox' 인가? (던지기)
        else if (itemToUse is PickupableBox box)
        {
            Debug.Log("상자 던지기!");
            
            heldItems.RemoveAt(0); // [MODIFIED] 리스트(인벤토리)에서 제거
            
            box.Drop(); // 물리 활성화, 부모 해제
            Rigidbody itemRb = box.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                itemRb.AddForce(cameraTransform.forward * throwForce, ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// [REMOVED] 이 기능은 HandleAttack의 'PickupableBox' 로직으로 흡수/대체되었습니다.
    /// </summary>
    // private void ThrowHeldItem() { ... } 


    /// <summary>
    /// [MODIFIED] SetParent 방식으로 변경되어 FixedUpdate에서 실행할 필요 X
    /// </summary>
    private void HandleHoldingItems()
    {
        // 줍는 즉시(HandleInteraction) 'holdPositions'의 자식으로 
        // SetParent(targetSlot) 처리를 하므로,
        // 이 함수는 더 이상 필요하지 않습니다.
        // 'holdPositions' Transform 들이 플레이어를 잘 따라다니기만 하면 됩니다.
        // (즉, 'holdPositions' 5개가 플레이어의 자식이어야 함)
    }

    /// <summary>
    /// (리어카 로직 - 이전과 동일)
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