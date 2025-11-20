using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController_TPS_Melee : MonoBehaviour
{
    [Header("===== 이동 설정 =====")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;

    [Header("===== TPS 근접 채굴 설정 =====")]
    [SerializeField] private float damage = 25f;         
    [SerializeField] private float miningRate = 0.5f;    // 공격 속도
    [SerializeField] private LayerMask oreLayer;         // 광석 레이어
    [Header("===== [추가] 공격 범위 시각화 =====")] 
    [SerializeField] private GameObject rangeVisualPrefab;
    
    // [중요] 타격 범위 미세 조정
    [Tooltip("타격 위치가 캐릭터 중심에서 얼마나 앞에 있는가")]
    [SerializeField] private float attackForwardOffset = 1.0f; 
    [Tooltip("타격 위치가 바닥에서 얼마나 위에 있는가 (가슴 높이 추천)")]
    [SerializeField] private float attackHeightOffset = 1.0f; 
    [Tooltip("타격 범위의 크기 (반지름)")]
    [SerializeField] private float attackRadius = 1.2f; 

    [Header("===== 필수 연결 =====")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("===== [추가] 리셋 설정 =====")]
    [SerializeField] private float fallThreshold = -30f; // 낙하 감지 높이
    [SerializeField] private float resetHoldTime = 2.0f; // R키 누르고 있어야 하는 시간
    [Tooltip("카메라가 보는 방향보다 몇 도 더 위를 때릴 것인가")]
    [SerializeField] private float attackAngleOffset = 20f; // 20도 정도 위로 휨

    // 내부 변수
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float rKeyPressedTime = 0f; // R키 누른 시간 체크용

    // 내부 변수
    private Rigidbody rb;
    private InputSystem_Actions inputActions;
    private bool isGrounded;
    private float nextMineTime = 0f;
    private HandcartController currentCartController;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        inputActions = new InputSystem_Actions();

        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        
        if (groundCheck == null)
        {
            GameObject obj = new GameObject("Auto_GroundCheck");
            obj.transform.SetParent(transform);
            obj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = obj.transform;
        }
    }

    void OnEnable() => inputActions.Player.Enable();
    void OnDisable() => inputActions.Player.Disable();

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // [추가] 시작 위치 저장
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        
        // 공격 입력 처리
        if (inputActions.Player.Attack.IsPressed()) 
        {
            HandleMeleeMining();
        }

        HandleCartInput();
        HandleResetLogic();
    }
    // [추가] 리셋 로직 함수
    void HandleResetLogic()
    {
        // 1. 맵 밖으로 떨어졌을 때 자동 리셋
        if (transform.position.y < fallThreshold)
        {
            RespawnPlayer();
        }

        // 2. R키를 길게 눌렀을 때 수동 리셋 (Input System 대신 키보드 직접 체크가 간편함)
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            rKeyPressedTime = Time.time; // 누르기 시작한 시간
        }

        if (Keyboard.current.rKey.isPressed)
        {
            // 누르고 있는 시간 계산
            float duration = Time.time - rKeyPressedTime;
            
            // 2초 이상 눌렀다면 리셋 실행
            if (duration > resetHoldTime)
            {
                RespawnPlayer();
                
                // 리셋 후 시간을 초기화해서 연속 실행 방지
                rKeyPressedTime = Time.time + 100f; 
            }
        }
    }

    // [추가] 플레이어와 리어카 모두 원위치
    void RespawnPlayer()
    {
        // 1. 플레이어 물리 초기화
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 2. 플레이어 위치 복구
        transform.position = startPosition;
        transform.rotation = startRotation;
        Debug.Log("플레이어 긴급 구조 실행!");

        // 3. 씬에 있는 리어카도 찾아서 같이 리셋 (선택 사항)
        HandcartCollector cart = FindObjectOfType<HandcartCollector>();
        if (cart != null)
        {
            cart.ResetCart();
        }
    }


    // --- [수정] 각도 오프셋 적용된 공격 로직 ---
    void HandleMeleeMining()
    {
        if (Time.time < nextMineTime) return; 

        // 1. 공격 방향 계산 (카메라 방향에서 위로 살짝 꺾기)
        // Quaternion.AngleAxis(-각도, 축) : 축을 기준으로 반시계 방향 회전 (X축 기준 -가 위쪽)
        Quaternion rot = Quaternion.AngleAxis(-attackAngleOffset, cameraTransform.right);
        Vector3 attackDir = rot * cameraTransform.forward;

        // 2. 타격 중심점 계산 (꺾인 방향 적용)
        Vector3 attackPos = transform.position 
                            + (Vector3.up * attackHeightOffset) 
                            + (attackDir * attackForwardOffset);

        // 3. 범위 내 모든 광석 감지
        Collider[] hitOres = Physics.OverlapSphere(attackPos, attackRadius, oreLayer);

        Ore closestOre = null;            
        float minDistance = float.MaxValue; 

        // 4. 가장 가까운 광석 찾기
        foreach (Collider oreCol in hitOres)
        {
            Ore ore = oreCol.GetComponent<Ore>();
            if (ore != null)
            {
                float dist = Vector3.Distance(attackPos, oreCol.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestOre = ore;
                }
            }
        }

        // 5. 타격 실행
        if (closestOre != null)
        {
            closestOre.TakeDamage(damage);
            
            // [시각화]
            if (rangeVisualPrefab != null)
            {
                GameObject visual = Instantiate(rangeVisualPrefab, attackPos, Quaternion.identity);
                visual.transform.localScale = Vector3.one * (attackRadius * 2);
                Destroy(visual, 0.2f);
            }

            nextMineTime = Time.time + miningRate;
        }
    }
    // ------------------------------------------

    void HandleMovement()
    {
        // 카메라 방향 기준으로 입력 변환
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();

        Vector3 moveDir = (forward * input.y + right * input.x).normalized;
        float speed = inputActions.Player.Sprint.IsPressed() ? sprintSpeed : moveSpeed;

        Vector3 vel = moveDir * speed;
        vel.y = rb.linearVelocity.y;
        rb.linearVelocity = vel;

        // 캐릭터의 Y축 회전을 카메라의 Y축 회전과 일치시킵니다.
        Quaternion targetRot = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
    }

    void HandleJump()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (inputActions.Player.Jump.triggered && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    // 리어카 제어
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HandcartControlZone"))
        {
            currentCartController = other.GetComponentInParent<HandcartController>();
            currentCartController?.StartControl();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HandcartControlZone"))
        {
            currentCartController?.StopControl();
            currentCartController = null;
        }
    }
    void HandleCartInput()
    {
        if (currentCartController == null) return;
        if (inputActions.Player.RearCarUp.IsPressed()) currentCartController.MoveTargetY(1f);
        if (inputActions.Player.RearCarDown.IsPressed()) currentCartController.MoveTargetY(-1f);
    }


    // [수정] 시각화도 똑같이 꺾여 보이게 수정
    void OnDrawGizmos() 
    {
        Gizmos.color = Color.yellow;

        // 카메라가 없으면 에러 방지용 기본값
        if(cameraTransform == null) return;

        // 공격 방향 계산 (위로 꺾기)
        Quaternion rot = Quaternion.AngleAxis(-attackAngleOffset, cameraTransform.right);
        Vector3 attackDir = rot * cameraTransform.forward;
        
        Vector3 attackPos = transform.position 
                            + (Vector3.up * attackHeightOffset) 
                            + (attackDir * attackForwardOffset);

        Gizmos.DrawWireSphere(attackPos, attackRadius);
    }

    public void SetMiningRate(float newRate)
    {
        miningRate = newRate;
        Debug.Log($"[업그레이드] 채굴 속도가 {miningRate}초로 변경되었습니다!");
    }
}