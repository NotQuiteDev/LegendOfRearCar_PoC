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
    }

    // --- [핵심] 캐릭터 앞 범위 타격 (OverlapSphere) ---
    void HandleMeleeMining()
    {
        if (Time.time < nextMineTime) return; 

        // 1. 타격 중심점 계산
        // (내 발 위치) + (위로 조금) + (내 앞쪽으로 조금)
        Vector3 attackPos = transform.position 
                          + (Vector3.up * attackHeightOffset) 
                          + (transform.forward * attackForwardOffset);

        // 2. 둥근 범위 감지 (물리 연산)
        Collider[] hitOres = Physics.OverlapSphere(attackPos, attackRadius, oreLayer);

        bool hitSomething = false;

        foreach (Collider oreCol in hitOres)
        {
            Ore ore = oreCol.GetComponent<Ore>();
            if (ore != null)
            {
                ore.TakeDamage(damage);
                hitSomething = true;
                // 광역 데미지(여러 개 동시 타격)를 원하면 break를 지우세요.
                // 단일 타겟을 원하면 break를 유지하세요.
                 break; 
            }
        }

        if (hitSomething)
        {
            // TODO: 타격음 재생 / 이펙트 생성
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

        // [중요] 이동 입력이 있을 때만 캐릭터 회전
        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
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

    // [시각화] 씬(Scene) 뷰에서 타격 범위를 노란색 공으로 보여줍니다.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        
        // 타격 중심점 예상 위치
        Vector3 attackPos = transform.position 
                          + (Vector3.up * attackHeightOffset) 
                          + (transform.forward * attackForwardOffset);

        Gizmos.DrawWireSphere(attackPos, attackRadius);
    }
}