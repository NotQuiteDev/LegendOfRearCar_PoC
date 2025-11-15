// HandcartController.cs
// 이 스크립트를 새로 생성하여 '리어카' 오브젝트에 붙여주세요.

using UnityEngine;

public class HandcartController : MonoBehaviour
{
    [Header("컴포넌트 연결")]
    [SerializeField] private Rigidbody rb; // 리어카의 Rigidbody
    [SerializeField] private Transform handleForcePoint; // 힘을 가할 손잡이 위치
    [SerializeField] private Transform targetIndicator; // 목표 높이를 보여줄 시각적 오브젝트

    [Header("PD 제어 설정")]
    [SerializeField] private float pGain = 1f; // 비례 게인 (목표에 도달하려는 힘)
    [SerializeField] private float dGain = 1f;  // 미분 게인 (목표 근처에서 감속하는 힘, 안정화)
    [SerializeField] private float maxForce = 10f; // 가할 수 있는 최대 힘 (캐릭터의 힘)

    [Header("목표값 설정")]
    [SerializeField] private float targetMoveSpeed = 0.1f; // Up/Down 버튼 누를 때 목표가 움직이는 속도 (m/s)

    private float currentTargetY; // 현재 목표 Y 높이
    private bool isBeingControlled = false; // 플레이어가 제어 중인지 여부

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        if (targetIndicator != null)
        {
            targetIndicator.gameObject.SetActive(false); // 평소에는 숨김
        }
    }

    private void Update()
    {
        if (isBeingControlled && targetIndicator != null)
        {
            // 목표 표시기(원)의 위치를 현재 목표 높이로 계속 업데이트
            Vector3 indicatorPos = handleForcePoint.position; // X, Z는 손잡이를 따라가고
            indicatorPos.y = currentTargetY; // Y는 목표값을 따름
            targetIndicator.position = indicatorPos;
        }
    }

    private void FixedUpdate()
    {
        // 플레이어가 제어하고 있을 때만 PD 제어 로직 실행
        if (!isBeingControlled)
        {
            return;
        }

        // --- PD 컨트롤러 로직 ---

        // 1. 현재 상태 (손잡이의 실제 Y 높이와 Y축 속도)
        float currentY = handleForcePoint.position.y;
        float currentVelocityY = rb.GetPointVelocity(handleForcePoint.position).y;

        // 2. 오차 계산 (목표 높이 - 현재 높이)
        float error = currentTargetY - currentY;

        // 3. 힘 계산 (P-force)
        float pForce = error * pGain;

        // 4. 감속 힘 계산 (D-force)
        // (목표에 가까워질 때 속도가 빠르면 반대 힘을 주어 출렁임을 막음)
        float dForce = -currentVelocityY * dGain;

        // 5. 최종 힘 계산 및 제한
        float finalForce = pForce + dForce;
        finalForce = Mathf.Clamp(finalForce, -maxForce, maxForce);

        // 6. 손잡이 위치에 Y축으로 힘 가하기
        rb.AddForceAtPosition(Vector3.up * finalForce, handleForcePoint.position, ForceMode.Force);
    }

    // --- PlayerController가 호출할 함수들 ---

    /// <summary>
    /// 플레이어가 제어 구역에 들어왔을 때 호출됩니다.
    /// </summary>
    public void StartControl()
    {
        isBeingControlled = true;
        currentTargetY = handleForcePoint.position.y; // 현재 손잡이 높이를 초기 목표로 설정
        
        if (targetIndicator != null)
        {
            targetIndicator.gameObject.SetActive(true); // 목표 표시기 활성화
        }
    }

    /// <summary>
    /// 플레이어가 제어 구역에서 나갔을 때 호출됩니다.
    /// </summary>
    public void StopControl()
    {
        isBeingControlled = false;
        if (targetIndicator != null)
        {
            targetIndicator.gameObject.SetActive(false); // 목표 표시기 비활성화
        }
    }

    /// <summary>
    /// 플레이어가 Up/Down을 눌렀을 때 목표 높이를 변경합니다.
    /// </summary>
    /// <param name="direction">1 (Up) 또는 -1 (Down)</param>
    public void MoveTargetY(float direction)
    {
        if (!isBeingControlled) return;
        
        // Time.deltaTime을 곱해서 버튼을 누르고 있는 동안 부드럽게 증가/감소
        currentTargetY += direction * targetMoveSpeed * Time.deltaTime;
    }
}