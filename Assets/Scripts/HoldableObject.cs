using UnityEngine;

// 이 스크립트를 상속받는 모든 아이템은 Rigidbody가 있어야 합니다.
[RequireComponent(typeof(Rigidbody))]
public class HoldableObject : MonoBehaviour
{
    protected Rigidbody rb;
    protected Collider col;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>(); // 물리적 충돌을 위해
    }

    /// <summary>
    /// 플레이어가 이 아이템을 집었을 때 호출됩니다.
    /// </summary>
    public virtual void PickUp()
    {
        rb.isKinematic = true; // 물리력을 끄고 플레이어를 따라다니게 함
        rb.useGravity = false;
        col.isTrigger = true;  // 다른 물체에 부딪히지 않게 트리거로 변경 (선택 사항)
    }

    /// <summary>
    /// 플레이어가 이 아이템을 내려놓았을 때 호출됩니다.
    /// </summary>
    public virtual void Drop()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        col.isTrigger = false;
        
        // 플레이어로부터 독립
        transform.SetParent(null); 
    }
}