using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // InputSystem 사용 시
using System.Collections.Generic;
using System.Linq; // 리스트 그룹화(GroupBy)를 위해 필요

public class HandcartUIManager : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private HandcartCollector targetCart; // UI가 보여줄 리어카
    [SerializeField] private GameObject cartUiPanel;       // 전체 UI 패널 (켜고 끄기용)
    [SerializeField] private Transform listContent;        // Scroll View의 Content
    [SerializeField] private GameObject itemRowPrefab;     // 목록에 띄울 한 줄 프리팹

    [Header("설정")]
    [SerializeField] private Key keyToToggle = Key.Tab; // 탭 키로 켜고 끄기

    private bool isUiOpen = false;

    void Start()
    {
        if (cartUiPanel != null) cartUiPanel.SetActive(false);
        
        // 만약 타겟 리어카를 직접 연결 안 했다면 플레이어 근처나 씬에서 찾기
        if (targetCart == null) targetCart = FindObjectOfType<HandcartCollector>();
    }

    void Update()
    {
        // Input System의 키보드 입력 감지
        if (Keyboard.current[keyToToggle].wasPressedThisFrame)
        {
            ToggleUI();
        }
    }

    public void ToggleUI()
    {
        isUiOpen = !isUiOpen;
        cartUiPanel.SetActive(isUiOpen);

        if (isUiOpen)
        {
            // UI 열릴 때 마우스 커서 보이기
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // 목록 갱신
            RefreshList();
        }
        else
        {
            // UI 닫힐 때 마우스 커서 숨기기 (FPS 모드라면)
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // 리어카 내용을 읽어서 UI를 새로고침하는 함수
    public void RefreshList()
    {
        if (targetCart == null) return;

        // 1. 기존 목록 싹 지우기
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        // 2. 리어카의 아이템 가져오기
        List<SellableItem> allItems = targetCart.GetAllItems();

        // 3. 이름별로 그룹화 (LINQ 사용)
        // 결과: { "금광석": [item1, item2], "은광석": [item3] } 형태가 됨
        var groupedItems = allItems
            .Where(item => item != null)
            .GroupBy(item => item.itemName);

        // 4. 각 그룹(광석 종류)마다 UI 한 줄씩 생성
        foreach (var group in groupedItems)
        {
            string itemName = group.Key;
            int count = group.Count();
            int unitPrice = group.First().itemValue; // 가격은 다 같다고 가정

            CreateRow(itemName, count, unitPrice);
        }
    }

    // UI 한 줄(Row) 생성 및 버튼 연결
    private void CreateRow(string name, int count, int price)
    {
        if (itemRowPrefab == null) return;

        // 1. 생성
        GameObject newRowObj = Instantiate(itemRowPrefab, listContent);
        
        // 2. 스크립트 가져오기 (이제 100% 정확함)
        HandcartItemRow rowScript = newRowObj.GetComponent<HandcartItemRow>();

        if (rowScript != null)
        {
            // 3. 데이터와 기능을 주입 (Setup 함수 호출)
            rowScript.Setup(
                name, 
                count, 
                price,
                () => OnDiscardOneClicked(name), // 1개 버림 버튼 누르면 실행될 함수
                () => OnDiscardAllClicked(name)  // 전부 버림 버튼 누르면 실행될 함수
            );
        }
        else
        {
            Debug.LogError("프리팹에 HandcartItemRow 스크립트가 안 붙어있습니다! 붙여주세요.");
        }
    }

    // --- 버튼 콜백 함수들 ---

    private void OnDiscardOneClicked(string itemName)
    {
        // 리어카에게 삭제 요청 (1개만, 뒤에서부터)
        targetCart.DiscardItemByName(itemName, false);
        
        // 삭제 후 UI 즉시 갱신 (숫자 줄어드는 것 보여주기 위해)
        RefreshList();
    }

    private void OnDiscardAllClicked(string itemName)
    {
        // 리어카에게 삭제 요청 (전부)
        targetCart.DiscardItemByName(itemName, true);
        
        // 삭제 후 UI 즉시 갱신
        RefreshList();
    }
    
}