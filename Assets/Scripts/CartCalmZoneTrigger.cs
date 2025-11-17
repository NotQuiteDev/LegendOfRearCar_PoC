using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CartCalmZoneTrigger : MonoBehaviour
{
    // 이 트리거를 관리하는 부모의 CartCalmZone 스크립트
    private CartCalmZone parentZone;

    void Awake()
    {
        // 이 스크립트는 '자식'에 붙을 것이므로, '부모'에게서 CartCalmZone을 찾습니다.
        parentZone = GetComponentInParent<CartCalmZone>();

        if (parentZone == null)
        {
            Debug.LogError("부모 오브젝트에 CartCalmZone.cs 스크립트가 없습니다!", this.gameObject);
        }
        
        // 이 콜라이더가 반드시 트리거인지 확인
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("CartCalmZoneTrigger의 콜라이더는 Is Trigger여야 합니다.", this.gameObject);
        }
    }

    // 아이템이 '안정화 구역'에 들어왔을 때
    private void OnTriggerEnter(Collider other)
    {
        parentZone?.NotifyTriggerEnter(other);
    }

    // 아이템이 '안정화 구역'에서 나갔을 때
    private void OnTriggerExit(Collider other)
    {
        parentZone?.NotifyTriggerExit(other);
    }
}