using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class HandcartItemRow : MonoBehaviour
{
    [Header("프리팹 내부 연결")]
    [SerializeField] private TextMeshProUGUI infoText;     // 광석 이름 뜰 곳
    [SerializeField] private Button discardOneButton;      // 1개 버리기 버튼
    [SerializeField] private Button discardAllButton;      // 전부 버리기 버튼

    // 외부(매니저)에서 이 함수를 불러서 세팅합니다.
    public void Setup(string name, int count, int price, UnityAction onDiscardOne, UnityAction onDiscardAll)
    {
        // 1. 텍스트 설정
        if (infoText != null)
        {
            infoText.text = $"{name} x {count}\n<size=70%>({price} G)</size>";
        }

        // 2. 버튼 리셋 (재사용 시 중복 클릭 방지)
        discardOneButton.onClick.RemoveAllListeners();
        discardAllButton.onClick.RemoveAllListeners();

        // 3. 버튼 기능 연결
        discardOneButton.onClick.AddListener(onDiscardOne);
        discardAllButton.onClick.AddListener(onDiscardAll);
    }
}