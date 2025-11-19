using UnityEngine;

/// <summary>
/// WheelCollider 대체용 레이캐스트 바퀴.
/// - 서스펜션(스프링 + 댐핑)
/// - 전/측방 마찰
/// - 선택적으로 구동/브레이크/조향
/// 리어카, 카트, 간단한 차량용.
/// </summary>
public class SimpleRaycastWheel : MonoBehaviour
{
    [Header("필수 참조")]
    [Tooltip("이 바퀴가 힘을 전달할 본체(리어카/차량)의 Rigidbody")]
    public Rigidbody body;

    [Tooltip("시각적으로 보여줄 바퀴 메쉬 Transform (없으면 비워도 됨)")]
    public Transform wheelVisual;

    [Header("기하 설정")]
    [Tooltip("바퀴 반지름 (씬상의 크기와 맞춰 줄 것)")]
    public float radius = 0.35f;

    [Tooltip("서스펜션 기본 길이 (바퀴축 ~ 바닥 사이 평균 거리)")]
    public float restLength = 0.4f;

    [Tooltip("완전히 눌렸을 때 최소 서스펜션 길이")]
    public float minLength = 0.1f;

    [Tooltip("완전히 늘어났을 때 최대 서스펜션 길이")]
    public float maxLength = 0.5f;

    [Header("서스펜션 힘")]
    [Tooltip("스프링 강도. 값이 클수록 단단하고 튕김이 강해짐. (가벼운 리어카면 너무 크게 하지 말 것)")]
    public float springStrength = 8000f;

    [Tooltip("댐퍼 강도. 값이 클수록 출렁임이 빠르게 죽지만, 너무 크면 딱딱해 보임.")]
    public float damperStrength = 1500f;

    [Tooltip("true면 body.mass를 반영해서 스프링/댐퍼를 자동 스케일링")]
    public bool autoScaleByMass = false;

    [Header("마찰")]
    [Tooltip("앞/뒤 방향 속도를 줄이는 마찰 계수")]
    public float forwardFriction = 1.5f;

    [Tooltip("옆 방향(슬립)을 잡아주는 마찰 계수")]
    public float sideFriction = 2.0f;

    [Header("구동 / 조향 / 브레이크 (선택 사용)")]
    [Tooltip("true면 이 바퀴에 모터 힘(motorInput)이 적용됨")]
    public bool driven = false;

    [Tooltip("true면 이 바퀴가 조향(steerAngle)을 가짐")]
    public bool steerable = false;

    [Tooltip("모터 힘 크기. driven = true일 때만 의미 있음.")]
    public float motorForce = 2000f;

    [Tooltip("브레이크 힘 크기. brakeInput * brakeForce가 실제 힘 비율.")]
    public float brakeForce = 4000f;

    [Header("디버그 옵션")]
    [Tooltip("씬 뷰에서 레이캐스트 / 서스펜션 상태를 기즈모로 볼지 여부")]
    public bool showGizmos = true;

    [HideInInspector] public float motorInput;  // -1 ~ 1
    [HideInInspector] public float steerAngle;  // degrees
    [HideInInspector] public float brakeInput;  // 0 ~ 1

    float _currentLength;
    float _lastLength;
    float _wheelRotate;
    bool _isGrounded;
    Vector3 _hitPoint;
    Vector3 _hitNormal;

    void Start()
    {
        if (body == null)
        {
            body = GetComponentInParent<Rigidbody>();
        }

        _currentLength = restLength;
        _lastLength = restLength;
    }

    void FixedUpdate()
    {
        if (body == null) return;

        Vector3 origin = transform.position;
        Vector3 dir = -transform.up;
        float rayLength = maxLength + radius;

        _isGrounded = false;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayLength))
        {
            _isGrounded = true;
            _hitPoint = hit.point;
            _hitNormal = hit.normal;

            float dist = hit.distance - radius;
            _currentLength = Mathf.Clamp(dist, minLength, maxLength);

            float springCompression = restLength - _currentLength;

            // 스프링/댐퍼를 mass에 따라 스케일링 (선택)
            float k = springStrength;
            float c = damperStrength;
            if (autoScaleByMass && body != null)
            {
                float m = Mathf.Max(body.mass, 0.01f);
                k *= m;
                c *= m;
            }

            float springForce = springCompression * k;

            float suspensionVelocity = (_lastLength - _currentLength) / Time.fixedDeltaTime;
            float damperForce = suspensionVelocity * c;

            Vector3 suspensionForce = (springForce + damperForce) * transform.up;

            Vector3 wheelVelocity = body.GetPointVelocity(hit.point);
            wheelVelocity -= Vector3.Project(wheelVelocity, hit.normal);

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            float vForward = Vector3.Dot(wheelVelocity, forward);
            float vSide = Vector3.Dot(wheelVelocity, right);

            Vector3 forwardFricForce = -vForward * forwardFriction * forward;
            Vector3 sideFricForce = -vSide * sideFriction * right;

            Vector3 driveForce = Vector3.zero;
            if (driven)
            {
                driveForce += motorInput * motorForce * forward;
            }

            if (brakeInput > 0.001f)
            {
                float sign = Mathf.Sign(vForward);
                // 속도가 거의 0이면 그냥 반대방향으로 브레이크 힘을 줌
                if (Mathf.Abs(vForward) < 0.1f) sign = 1f;
                driveForce += -sign * brakeInput * brakeForce * forward;
            }

            Vector3 totalForce = suspensionForce + forwardFricForce + sideFricForce + driveForce;
            body.AddForceAtPosition(totalForce, hit.point, ForceMode.Force);

            _lastLength = _currentLength;

            // 바퀴 메쉬 업데이트
            if (wheelVisual != null)
            {
                wheelVisual.position = hit.point + hit.normal * radius;

                float travel = vForward * Time.fixedDeltaTime;
                float circumference = 2f * Mathf.PI * radius;
                float angleDelta = (circumference > 0.0001f)
                    ? (travel / circumference) * 360f
                    : 0f;
                _wheelRotate += angleDelta;

                Quaternion rotSpin = Quaternion.Euler(_wheelRotate, 0f, 0f);
                Quaternion rotSteer = steerable ? Quaternion.Euler(0f, steerAngle, 0f) : Quaternion.identity;
                wheelVisual.rotation = rotSteer * transform.rotation * rotSpin;
            }
        }
        else
        {
            _currentLength = maxLength;
            _lastLength = _currentLength;

            if (wheelVisual != null)
            {
                // 대충 축 위치 + restLength 기준으로 둠
                wheelVisual.position = origin - transform.up * (restLength - minLength);
            }
        }
    }

    /// <summary>
    /// 외부(차량 컨트롤러 등)에서 입력을 넣을 때 사용.
    /// motor: -1 ~ 1, steer: degrees, brake: 0 ~ 1
    public void SetInputs(float motor, float steer, float brake)
    {
        motorInput = Mathf.Clamp(motor, -1f, 1f);
        steerAngle = steer;
        brakeInput = Mathf.Clamp01(brake);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 서스펜션 레이 / 범위 표시
        Vector3 origin = transform.position;
        Vector3 dir = -transform.up;

        float rayLength = (maxLength > 0f ? maxLength : restLength) + radius;

        // 전체 레이
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(origin, origin + dir * rayLength);

        // restLength 위치
        Vector3 restPoint = origin + dir * restLength;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(restPoint, 0.03f);

        // min / max 길이 구간
        Vector3 minPoint = origin + dir * minLength;
        Vector3 maxPoint = origin + dir * maxLength;

        Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // 주황
        Gizmos.DrawLine(minPoint, maxPoint);

        // 바퀴 반지름 표현
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(restPoint, radius);

        // 접지 중이면 hit point, 노말 표시
        if (_isGrounded)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_hitPoint, 0.04f);
            Gizmos.DrawLine(_hitPoint, _hitPoint + _hitNormal * 0.3f);
        }
    }

    void OnDrawGizmosSelected()
    {
        // 선택했을 때는 좀 더 강조해서 보이도록
        if (!showGizmos) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
}
