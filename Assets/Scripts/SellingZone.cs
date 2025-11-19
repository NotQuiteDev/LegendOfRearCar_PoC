using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SellingZone : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI totalPriceText;

    [Header("연결")]
    [SerializeField] private PlayerWallet playerWallet;

    // 현재 구역에 들어온 리어카
    private HandcartCollector currentCart = null;
    private int currentTotalPrice = 0;

    void Start()
    {
        if (playerWallet == null) playerWallet = FindObjectOfType<PlayerWallet>();
        UpdateTotalPriceUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 리어카 태그 확인 (Tag: Handcart)
        if (other.CompareTag("Handcart"))
        {
            currentCart = other.GetComponentInParent<HandcartCollector>();
            if (currentCart != null)
            {
                Debug.Log("상점: 리어카 진입 확인");
                CalculatePrice(); 
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Handcart"))
        {
            HandcartCollector exitingCart = other.GetComponentInParent<HandcartCollector>();
            
            // 지금 계산 중인 그 리어카가 나갔다면
            if (exitingCart == currentCart)
            {
                Debug.Log("상점: 리어카 나감");
                currentCart = null;
                currentTotalPrice = 0;
                UpdateTotalPriceUI();
            }
        }
    }

    void Update()
    {
        // 리어카가 안에 있는 동안 실시간으로 가격 갱신 (혹시 밖에서 더 주워 담았을까봐)
        if (currentCart != null)
        {
            CalculatePrice();
        }
    }

    void CalculatePrice()
    {
        if (currentCart == null) return;

        // [핵심] 리어카에게 직접 장부를 물어봄 (물리 감지 오류 없음)
        List<SellableItem> items = currentCart.GetAllItems();
        
        int tempPrice = 0;
        foreach (var item in items)
        {
            tempPrice += item.itemValue;
        }

        // UI 업데이트 (값이 변했을 때만)
        if (currentTotalPrice != tempPrice)
        {
            currentTotalPrice = tempPrice;
            UpdateTotalPriceUI();
        }
    }

    private void UpdateTotalPriceUI()
    {
        if (totalPriceText != null)
            totalPriceText.text = $"판매 금액: {currentTotalPrice} G";
    }

    // [버튼 연결용]
    public void SellItems()
    {
        if (currentCart == null || currentTotalPrice == 0)
        {
            Debug.Log("판매할 물건이 없습니다.");
            return;
        }

        // 1. 돈 지급
        playerWallet.AddMoney(currentTotalPrice);

        // 2. 리어카 비우기 명령 (장부 초기화 + 아이템 파괴)
        currentCart.SellAndClearAll();

        // 3. 가격 초기화
        currentTotalPrice = 0;
        UpdateTotalPriceUI();

        Debug.Log("판매 완료!");
    }
}