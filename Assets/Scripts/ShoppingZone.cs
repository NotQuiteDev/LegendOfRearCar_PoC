using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ShoppingZone : MonoBehaviour
{
    private PlayerWallet playerWallet;
    private Collider zoneCollider; // 자기 자신의 콜라이더

    void Start()
    {
        playerWallet = FindObjectOfType<PlayerWallet>();
        if (playerWallet == null)
        {
            Debug.LogError("ShoppingZone이 PlayerWallet을 찾을 수 없습니다!");
        }

        // 자기 자신의 콜라이더 정보를 가져옴
        zoneCollider = GetComponent<Collider>();
        if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning("ShoppingZone의 콜라이더는 반드시 Is Trigger여야 합니다!", this.gameObject);
        }
    }

    /// <summary>
    /// [FINAL LOGIC]
    /// 아이템의 퇴장을 감지하되, '가짜' 퇴장과 '진짜' 퇴장을 구분합니다.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // 1. 나가는 것이 BuyableItem인지 확인
        BuyableItem item = other.GetComponent<BuyableItem>();
        if (item == null)
        {
            return; // 플레이어, 리어카 등 구매 불가 아이템은 무시
        }

        // 2. 이미 구매한 물건인지 확인
        if (item.isPurchased)
        {
            return; // 이미 샀으면 통과
        }

        // --- 3. [핵심] '가짜' 퇴장(줍기)인지 확인 ---
        
        // '가짜' 퇴장(줍기)은 아이템이 isTrigger=true로 바뀌는 순간 발생합니다.
        // 이때 아이템의 실제 위치(transform.position)는 '아직' 구역 안입니다.
        
        // '진짜' 퇴장(훔치기)은 아이템이 구역의 경계선을 '넘어갔을 때' 발생합니다.

        // ClosestPoint()는 어떤 지점에서 가장 가까운 '콜라이더 위의 점'을 반환합니다.
        // 만약 아이템이 '구역 안'에 있다면, 이 함수는 아이템의 위치 자체를 반환합니다.
        // (즉, 두 지점 간의 거리가 0이 됩니다)
        Vector3 closestPointOnBounds = zoneCollider.ClosestPoint(item.transform.position);
        float distanceToExitPoint = Vector3.Distance(item.transform.position, closestPointOnBounds);

        // 만약 아이템이 구역 안에 있다면 (distance가 0에 가깝다면)
        // 이것은 '줍기'로 인한 '가짜' 퇴장입니다.
        if (distanceToExitPoint < 0.1f) // (0.1f의 아주 작은 오차 범위)
        {
            Debug.Log($"'가짜' 퇴장 감지: {item.name} (줍는 중). 구매 로직 무시.");
            return;
        }

        // --- 4. [구매 시도] ---
        // 여기까지 왔다면, '정말로' 구역 밖으로 나간 것입니다.
        // (손에 들고 나가든, 리어카에 싣고 나가든 모두 잡힘)

        Debug.Log($"'진짜' 퇴장 감지: {item.name}. 구매 시도...");

        if (playerWallet.SpendMoney(item.price))
        {
            // 4-A. 구매 성공
            item.isPurchased = true;
            Debug.Log($"{item.name} 구매 성공! ({item.price} G)");
        }
        else
        {
            // 4-B. 구매 실패 (돈 부족)
            Debug.Log($"{item.name} 구매 실패! (돈 부족). 아이템을 파괴합니다.");
            
            // 플레이어가 들고 있든, 리어카에 있든 그냥 파괴
            Destroy(item.gameObject);
        }
    }
}