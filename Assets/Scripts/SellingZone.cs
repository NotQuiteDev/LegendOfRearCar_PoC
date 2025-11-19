using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SellingZone : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI totalPriceText;

    [Header("연결")]
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private Collider zoneCollider;   // 존의 콜라이더 (BoxCollider 추천)

    private const string CartDetectorName = "ItemCollectCollider";

    private void Awake()
    {
        if (playerWallet == null)
            playerWallet = FindObjectOfType<PlayerWallet>();

        if (zoneCollider == null)
            zoneCollider = GetComponent<Collider>();

        if (zoneCollider == null)
            Debug.LogError("[SellingZone] zoneCollider가 설정되지 않았습니다.");
    }

    private void Start()
    {
        // 시작할 때 UI 초기화
        UpdateTotalPriceUI(0);
    }

    // ─────────────────────────────
    // 버튼에서 호출할 판매 함수
    // ─────────────────────────────
    public void SellCartInZone()
    {
        Debug.Log("[SellingZone] SellCartInZone() 호출됨");

        HandcartCollector cart = FindCartInZone();
        if (cart == null)
        {
            Debug.Log("[SellingZone] 존 안에 리어카가 없습니다. 판매 불가.");
            UpdateTotalPriceUI(0);
            return;
        }

        List<SellableItem> items = cart.GetAllItems();
        int totalPrice = 0;
        foreach (var item in items)
        {
            if (item == null) continue;
            totalPrice += item.itemValue;
        }

        Debug.Log($"[SellingZone] 존 안 리어카 아이템 수: {items.Count}, 금액: {totalPrice}");

        if (totalPrice <= 0)
        {
            Debug.Log("[SellingZone] 판매할 물건이 없습니다. 금액 0.");
            UpdateTotalPriceUI(0);
            return;
        }

        if (playerWallet == null)
            playerWallet = FindObjectOfType<PlayerWallet>();

        if (playerWallet != null)
        {
            playerWallet.AddMoney(totalPrice);
        }

        // 카트 비우기
        cart.SellAndClearAll();

        // UI 갱신
        UpdateTotalPriceUI(0);

        Debug.Log($"[SellingZone] {totalPrice} G 판매 완료");
    }

    // ─────────────────────────────
    // 존 안의 리어카 찾기
    // ─────────────────────────────
    private HandcartCollector FindCartInZone()
    {
        if (zoneCollider == null)
        {
            Debug.LogWarning("[SellingZone] zoneCollider 없음 → 리어카 탐색 불가");
            return null;
        }

        Bounds b = zoneCollider.bounds;

        // 박스 범위 안의 모든 콜라이더 조회
        Collider[] hits = Physics.OverlapBox(
            b.center,
            b.extents,
            zoneCollider.transform.rotation,
            ~0,                              // 모든 레이어
            QueryTriggerInteraction.Collide  // 트리거도 포함
        );

        HandcartCollector foundCart = null;

        foreach (var hit in hits)
        {
            // 우리가 리어카 검지용으로 쓰는 콜라이더만 본다
            if (hit.name != CartDetectorName) continue;

            var cart = hit.GetComponentInParent<HandcartCollector>();
            if (cart != null)
            {
                foundCart = cart;
                break;
            }
        }

        if (foundCart != null)
        {
            Debug.Log($"[SellingZone] 존 안에서 리어카 발견: {foundCart.gameObject.name}");
        }
        else
        {
            Debug.Log("[SellingZone] 존 안에서 리어카를 찾지 못함");
        }

        return foundCart;
    }

    // ─────────────────────────────
    // UI 업데이트 (옵션: 실시간 미리보기)
    // ─────────────────────────────

    // 필요하면 Update()에서 매 프레임 호출해도 되고,
    // 플레이어가 존에 들어올 때만 호출하도록 다른 트리거에서 불러도 됨.
    public void RefreshPreviewPrice()
    {
        HandcartCollector cart = FindCartInZone();
        if (cart == null)
        {
            UpdateTotalPriceUI(0);
            return;
        }

        List<SellableItem> items = cart.GetAllItems();
        int totalPrice = 0;
        foreach (var item in items)
        {
            if (item == null) continue;
            totalPrice += item.itemValue;
        }

        UpdateTotalPriceUI(totalPrice);
    }

    private void UpdateTotalPriceUI(int price)
    {
        if (totalPriceText != null)
            totalPriceText.text = $"판매 금액: {price} G";
    }
}
