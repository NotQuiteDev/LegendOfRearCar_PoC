// PickupableBox.cs
// 이 스크립트를 새로 생성하여 '상자' 오브젝트에 붙여주세요.

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupableBox : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    /// <summary>
    /// 플레이어가 이 상자를 집었을 때 호출됩니다.
    /// </summary>
    public void PickUp()
    {
        rb.useGravity = false; // 중력 끄기
        rb.isKinematic = true; // 물리 시뮬레이션 끄기 (우리가 직접 제어)
        col.isTrigger = true;  // 다른 물체에 부딪히지 않고 통과하게 (선택 사항)
    }

    /// <summary>
    /// 플레이어가 이 상자를 내려놓을 때 호출됩니다.
    /// </summary>
    public void Drop()
    {
        rb.useGravity = true;  // 중력 다시 켜기
        rb.isKinematic = false; // 물리 시뮬레이션 다시 켜기
        col.isTrigger = false; // 다시 물리적으로 부딪히도록
    }

    /// <summary>
    /// 상자를 특정 방향으로 던집니다.
    /// </summary>
    public void Throw(Vector3 forceDirection)
    {
        Drop(); // 우선 내려놓기(물리 활성화)
        rb.AddForce(forceDirection, ForceMode.Impulse); // 힘 가하기
    }
}