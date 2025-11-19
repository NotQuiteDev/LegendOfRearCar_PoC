using UnityEngine;

public class SellButton : MonoBehaviour
{
    [SerializeField] private SellingZone sellingZone;

    private void Awake()
    {
        if (sellingZone == null)
            sellingZone = FindObjectOfType<SellingZone>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[SellButton] 플레이어가 판매 버튼 존에 진입 → 판매 시도");

        if (sellingZone == null)
        {
            sellingZone = FindObjectOfType<SellingZone>();
            if (sellingZone == null)
            {
                Debug.LogError("[SellButton] SellingZone을 씬에서 찾을 수 없습니다.");
                return;
            }
        }

        sellingZone.SellItems();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log("[SellButton] 플레이어가 판매 버튼에서 멀어짐");
    }
}
