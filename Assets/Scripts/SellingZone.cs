using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SellingZone : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI totalPriceText;

    [Header("연결")]
    [SerializeField] private PlayerWallet playerWallet;

    // 지금 존 안에 있는 리어카
    private HandcartCollector currentCart = null;
    private int currentTotalPrice = 0;

    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        if (zoneCollider == null)
            Debug.LogError("[SellingZone] Collider(Trigger)가 필요합니다.");

        if (zoneCollider != null && !zoneCollider.isTrigger)
            Debug.LogWarning("[SellingZone] 이 콜라이더는 isTrigger = true 여야 합니다.");
    }

    private void Start()
    {
        if (playerWallet == null)
            playerWallet = FindObjectOfType<PlayerWallet>();

        UpdateTotalPriceUI(0);
    }

    // ─────────────────────────────────
    //  리어카 입/출 인식 (이름, 태그 안 씀)
    // ─────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        // 이 콜라이더 또는 부모 중에 HandcartCollector가 있으면 리어카로 취급
        var cart = other.GetComponentInParent<HandcartCollector>();
        if (cart == null) return;

        currentCart = cart;
        Debug.Log($"[SellingZone] Handcart 진입: {cart.gameObject.name}");

        RecalculatePrice();
    }

    private void OnTriggerExit(Collider other)
    {
        var cart = other.GetComponentInParent<HandcartCollector>();
        if (cart == null) return;

        // 지금 트래킹 중인 그 리어카가 나갔을 때만 해제
        if (cart == currentCart)
        {
            Debug.Log("[SellingZone] Handcart 퇴장");
            currentCart = null;
            currentTotalPrice = 0;
            UpdateTotalPriceUI(0);
        }
    }

    // ─────────────────────────────────
    //  존 안에 있는 동안 가격 계속 갱신
    // ─────────────────────────────────

    private void Update()
    {
        if (currentCart == null) return;

        // 혹시 물리 버그/텔레포트 대비해서, 존 범위 밖으로 튀어나가면 해제
        if (zoneCollider != null)
        {
            var cartCollider = currentCart.GetComponentInChildren<Collider>();
            if (cartCollider != null && !zoneCollider.bounds.Intersects(cartCollider.bounds))
            {
                Debug.Log("[SellingZone] Handcart가 범위를 벗어난 것으로 감지 → 해제");
                currentCart = null;
                currentTotalPrice = 0;
                UpdateTotalPriceUI(0);
                return;
            }
        }

        RecalculatePrice();
    }

    private void RecalculatePrice()
    {
        if (currentCart == null) return;

        List<SellableItem> items = currentCart.GetAllItems();
        int tempPrice = 0;

        foreach (var item in items)
        {
            if (item == null) continue;
            tempPrice += item.itemValue;
        }

        if (tempPrice != currentTotalPrice)
        {
            currentTotalPrice = tempPrice;
            UpdateTotalPriceUI(currentTotalPrice);
            // Debug.Log($"[SellingZone] 프리뷰 가격: {currentTotalPrice}");
        }
    }

    private void UpdateTotalPriceUI(int price)
    {
        if (totalPriceText != null)
            totalPriceText.text = $"판매 금액: {price} G";
    }

    // ─────────────────────────────────
    //  실제 판매 (버튼에서 호출)
    // ─────────────────────────────────

    public void SellItems()
    {
        if (currentCart == null)
        {
            Debug.Log("[SellingZone] 존 안에 인식된 리어카가 없습니다. 판매 불가.");
            return;
        }

        // 판매 직전에 다시 한 번 정확히 계산
        List<SellableItem> items = currentCart.GetAllItems();
        int totalPrice = 0;

        foreach (var item in items)
        {
            if (item == null) continue;
            totalPrice += item.itemValue;
        }

        Debug.Log($"[SellingZone] 판매 시도: 아이템 수={items.Count}, 금액={totalPrice}");

        if (totalPrice <= 0)
        {
            Debug.Log("[SellingZone] 판매할 물건이 없습니다.");
            currentTotalPrice = 0;
            UpdateTotalPriceUI(0);
            return;
        }

        if (playerWallet == null)
            playerWallet = FindObjectOfType<PlayerWallet>();

        if (playerWallet != null)
            playerWallet.AddMoney(totalPrice);

        // 카트 비우기
        currentCart.SellAndClearAll();

        // 가격 리셋
        currentTotalPrice = 0;
        UpdateTotalPriceUI(0);

        Debug.Log($"[SellingZone] {totalPrice} G 판매 완료");
    }
}
