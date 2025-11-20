using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text; // 텍스트 조합을 위해 필요

public class SellingZone : MonoBehaviour
{
    [Header("===== UI 연결 =====")]
    [SerializeField] private GameObject tradePanel;      // 전체 패널
    [SerializeField] private TextMeshProUGUI receiptText; // 상세 내역이 뜰 텍스트 (Scroll View 안의 Content)
    [SerializeField] private TextMeshProUGUI totalText;   // 총합 금액 텍스트
    [SerializeField] private Button sellButton;           // 판매 버튼

    [Header("===== 시스템 연결 =====")]
    [SerializeField] private PlayerWallet playerWallet;

    // 내부 변수
    private HandcartCollector currentCart = null; // 현재 존 안에 들어온 리어카
    private bool isPlayerInZone = false;          // 플레이어가 존 안에 있는지

    private void Start()
    {
        // 시작 시 UI 끄기
        tradePanel.SetActive(false);
        
        // 버튼에 판매 기능 연결
        sellButton.onClick.RemoveAllListeners();
        sellButton.onClick.AddListener(OnClickSellButton);

        if (playerWallet == null)
            playerWallet = FindObjectOfType<PlayerWallet>();
    }

    // ─────────────────────────────────
    //  트리거 감지 (플레이어 & 리어카)
    // ─────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        // 1. 리어카가 들어옴
        var cart = other.GetComponentInParent<HandcartCollector>();
        if (cart != null)
        {
            currentCart = cart;
            Debug.Log("상점: 리어카 감지됨");
            if (isPlayerInZone) UpdateTradeUI(); // 플레이어가 이미 보고 있다면 UI 갱신
            return;
        }

        // 2. 플레이어가 들어옴 (태그 확인)
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            OpenUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 1. 리어카가 나감
        var cart = other.GetComponentInParent<HandcartCollector>();
        if (cart != null && cart == currentCart)
        {
            currentCart = null;
            Debug.Log("상점: 리어카 나감");
            if (isPlayerInZone) UpdateTradeUI(); // 리어카 없어졌으니 UI 갱신 (빈 화면)
            return;
        }

        // 2. 플레이어가 나감
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            CloseUI();
        }
    }

    // ─────────────────────────────────
    //  UI 제어 (열기/닫기/갱신)
    // ─────────────────────────────────
    private void OpenUI()
    {
        tradePanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // UI 내용 채우기
        UpdateTradeUI();
    }

    private void CloseUI()
    {
        tradePanel.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // [핵심] 영수증 만들기 로직
    private void UpdateTradeUI()
    {
        // 1. 예외 처리 (리어카 없음)
        if (currentCart == null)
        {
            receiptText.text = "리어카가 없습니다.";
            if(totalText != null) totalText.text = "0 G";
            sellButton.interactable = false;
            return;
        }

        List<SellableItem> items = currentCart.GetAllItems();
        
        // 2. 예외 처리 (빈 리어카)
        if (items.Count == 0)
        {
            receiptText.text = "판매할 물건이 없습니다.";
            if(totalText != null) totalText.text = "0 G";
            sellButton.interactable = false;
            return;
        }

        // 3. 계산 로직 (이름별 정리)
        Dictionary<string, int> countMap = new Dictionary<string, int>();
        Dictionary<string, int> priceMap = new Dictionary<string, int>();

        int grandTotal = 0;

        foreach (var item in items)
        {
            if (item == null) continue;
            string itemName = item.itemName; 
            
            if (!countMap.ContainsKey(itemName))
            {
                countMap[itemName] = 0;
                priceMap[itemName] = item.itemValue;
            }
            countMap[itemName]++;
            grandTotal += item.itemValue;
        }

        // 4. 텍스트 생성 (영수증 스타일)
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("<b>[ 판매 목록 ]</b>"); 
        sb.AppendLine("--------------------"); // 구분선

        foreach (var pair in countMap)
        {
            string name = pair.Key;
            int count = pair.Value;
            int unitPrice = priceMap[name];
            int subTotal = count * unitPrice;

            // [요청하신 포맷]
            // 예시: - 금광석 : 100 * 5 = 500 G
            sb.AppendLine($"- {name} : {unitPrice} * {count} = <b>{subTotal} G</b>");
        }

        sb.AppendLine("--------------------"); // 구분선

        receiptText.text = sb.ToString();

        // 별도의 Total Text UI가 연결되어 있다면 거기에도 표시
        if (totalText != null) 
        {
            totalText.text = $"<color=yellow>{grandTotal} G</color>";
        }

        sellButton.interactable = true;
    }
    // ─────────────────────────────────
    //  판매 실행
    // ─────────────────────────────────
    private void OnClickSellButton()
    {
        if (currentCart == null) return;

        // 1. 최종 금액 계산
        List<SellableItem> items = currentCart.GetAllItems();
        int finalPrice = 0;
        foreach (var item in items)
        {
            if (item != null) finalPrice += item.itemValue;
        }

        // 2. 돈 지급
        if (playerWallet != null)
        {
            playerWallet.AddMoney(finalPrice);
        }

        // 3. 카트 비우기
        currentCart.SellAndClearAll();

        // 4. UI 갱신 (판매 완료 상태 보여주기)
        UpdateTradeUI(); 
        
        // (선택) 판매 후 바로 창을 닫고 싶으면 CloseUI() 호출
        // 여기서는 "판매됨!"을 보여주기 위해 갱신만 함.
        receiptText.text = "<color=green>판매가 완료되었습니다!</color>";
        sellButton.interactable = false;
    }
}