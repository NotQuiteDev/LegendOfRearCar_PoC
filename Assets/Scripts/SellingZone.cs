using UnityEngine;
using System.Collections.Generic; // List 사용
using TMPro; // TextMeshPro 사용

public class SellingZone : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI totalPriceText; // 총 가격을 표시할 World Canvas 텍스트

    [Header("연결")]
    [SerializeField] private PlayerWallet playerWallet; // 플레이어의 지갑 스크립트

    // 현재 구역 내에 있는 판매 가능 아이템 목록
    private List<SellableItem> itemsInZone = new List<SellableItem>();
    
    private int currentTotalPrice = 0;

    void Start()
    {
        // PlayerWallet을 찾지 못했다면 씬에서 직접 찾기 (비상용)
        if (playerWallet == null)
        {
            playerWallet = FindObjectOfType<PlayerWallet>();
        }

        if (playerWallet == null)
        {
            Debug.LogError("SellingZone이 PlayerWallet을 찾을 수 없습니다!");
        }

        UpdateTotalPriceUI(); // 시작할 때 0 G로 초기화
    }

    private void OnTriggerEnter(Collider other)
    {
        // 트리거에 들어온 오브젝트에서 SellableItem 컴포넌트를 찾습니다.
        SellableItem item = other.GetComponent<SellableItem>();

        // (중요) 'other.CompareTag("Holdable")' 보다 이 방식이 더 안전합니다.
        // 리어카나 곡괭이(SellableItem이 아닌)는 item이 null이 됩니다.
        if (item != null)
        {
            // 리스트에 이미 있는지 확인 (필수!)
            if (!itemsInZone.Contains(item))
            {
                itemsInZone.Add(item);
                UpdatePrice();
                Debug.Log($"{item.name} 판매 구역 진입. (총 {itemsInZone.Count}개)");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        SellableItem item = other.GetComponent<SellableItem>();

        if (item != null)
        {
            // 리스트에 존재하면 제거합니다.
            if (itemsInZone.Contains(item))
            {
                itemsInZone.Remove(item);
                UpdatePrice();
                Debug.Log($"{item.name} 판매 구역 이탈. (남은 개수 {itemsInZone.Count}개)");
            }
        }
    }

    /// <summary>
    /// 리스트를 기반으로 총 가격을 다시 계산합니다.
    /// </summary>
    private void UpdatePrice()
    {
        currentTotalPrice = 0;
        foreach (SellableItem item in itemsInZone)
        {
            currentTotalPrice += item.itemValue;
        }

        // UI 업데이트
        UpdateTotalPriceUI();
    }

    /// <summary>
    /// 계산된 총 가격을 UI 텍스트에 표시합니다.
    /// </summary>
    private void UpdateTotalPriceUI()
    {
        if (totalPriceText != null)
        {
            totalPriceText.text = $"총 가격: {currentTotalPrice} G";
        }
    }

    /// <summary>
    /// [PUBLIC] 판매 버튼이 이 함수를 호출합니다.
    /// 구역 내의 모든 아이템을 판매(파괴)하고 돈을 추가합니다.
    /// </summary>
    public void SellItems()
    {
        if (playerWallet == null)
        {
            Debug.LogError("PlayerWallet이 연결되지 않아 판매할 수 없습니다!");
            return;
        }

        if (itemsInZone.Count == 0)
        {
            Debug.Log("판매할 아이템이 없습니다.");
            return;
        }

        // 1. 플레이어에게 돈 추가
        playerWallet.AddMoney(currentTotalPrice);

        // 2. 구역 내의 모든 'SellableItem' 오브젝트 파괴
        // (주의: 리스트를 순회하면서 파괴하면 에러가 나므로, 임시 리스트를 복사하거나
        //      뒤에서부터 순회해야 합니다. 여기서는 뒤에서부터 순회합니다.)
        for (int i = itemsInZone.Count - 1; i >= 0; i--)
        {
            if (itemsInZone[i] != null)
            {
                Destroy(itemsInZone[i].gameObject);
            }
        }

        // 3. 리스트 비우기 및 UI 초기화
        itemsInZone.Clear();
        UpdatePrice(); // currentTotalPrice가 0이 되고 UI도 업데이트됨

        Debug.Log("아이템 판매 완료!");
    }
}