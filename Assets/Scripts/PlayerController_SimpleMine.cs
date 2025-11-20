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


    void HandleMeleeMining()
    {
        if (Time.time < nextMineTime) return; 

        // 1. 타격 중심점 계산
        Vector3 attackPos = transform.position 
                            + (Vector3.up * attackHeightOffset) 
                            + (transform.forward * attackForwardOffset);

        // 2. 범위 내 모든 광석 감지
        Collider[] hitOres = Physics.OverlapSphere(attackPos, attackRadius, oreLayer);

        Ore closestOre = null;            // 가장 가까운 광석을 담을 변수
        float minDistance = float.MaxValue; // 최소 거리 비교용

        // 3. 감지된 것들 중 "가장 가까운 놈" 찾기
        foreach (Collider oreCol in hitOres)
        {
            Ore ore = oreCol.GetComponent<Ore>();
            if (ore != null)
            {
                // 타격 중심점(attackPos)과 광석 사이의 거리 계산
                float dist = Vector3.Distance(attackPos, oreCol.transform.position);

                // 지금 검사하는게 기존에 찾은것보다 더 가까우면 갱신
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestOre = ore;
                }
            }
        }

        // 4. 찾은 가장 가까운 광석이 있다면 데미지 주기
        if (closestOre != null)
        {
            closestOre.TakeDamage(damage);
            
            // [시각화] 공격 범위 이펙트 (아까 추가한 코드)
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
    void OnDrawGizmos() // OnDrawGizmosSelected -> OnDrawGizmos로 변경
    {
        Gizmos.color = Color.yellow;
        
        // 타격 중심점 예상 위치
        Vector3 attackPos = transform.position 
                            + (Vector3.up * attackHeightOffset) 
                            + (transform.forward * attackForwardOffset);

        Gizmos.DrawWireSphere(attackPos, attackRadius);
    }

    public void SetMiningRate(float newRate)
    {
        miningRate = newRate;
        Debug.Log($"[업그레이드] 채굴 속도가 {miningRate}초로 변경되었습니다!");
    }
}