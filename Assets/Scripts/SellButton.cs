using UnityEngine;

public class SellButton : MonoBehaviour
{
    [SerializeField] private SellingZone sellingZone;

    private void Awake()
    {
        // 초기에 한 번 찾아두는 건 괜찮지만, 이걸 "영원히 유효하다"고 믿으면 안 됨.
        if (sellingZone == null)
            sellingZone = FindObjectOfType<SellingZone>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[SellButton] 플레이어가 판매 버튼 존에 진입 → 판매 시도");

        // **여기서 매번 살아있는 SellingZone을 다시 확보한다**
        if (sellingZone == null)
        {
            sellingZone = FindObjectOfType<SellingZone>();
            if (sellingZone == null)
            {
                Debug.LogError("[SellButton] SellingZone을 씬에서 찾을 수 없습니다.");
                return;
            }
        }

        // 여기까지 왔으면 최소 하나는 살아있는 SellingZone
        sellingZone.SellCartInZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log("[SellButton] 플레이어가 판매 버튼에서 멀어짐");
    }
}
