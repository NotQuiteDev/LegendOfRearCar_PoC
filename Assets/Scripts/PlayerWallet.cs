using TMPro;
using UnityEngine;
using UnityEngine.UI; // UI.Text를 사용합니다. (TextMeshPro 추천)

public class PlayerWallet : MonoBehaviour
{
    [Header("UI 연결")]
    // 만약 TextMeshPro를 사용한다면
    // public TMPro.TextMeshProUGUI moneyText;
    public TextMeshProUGUI moneyTMPText; // TextMeshPro용 텍스트

    [Header("초기 자금")]
    [SerializeField] private int startingMoney = 0;

    private int currentMoney;

    void Start()
    {
        currentMoney = startingMoney;
        UpdateMoneyUI();
    }

    /// <summary>
    /// 돈을 추가합니다. (예: 아이템 판매)
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;

        currentMoney += amount;
        UpdateMoneyUI();
        Debug.Log($"{amount}원 획득. 현재 잔액: {currentMoney}원");
    }

    /// <summary>
    /// 돈을 사용합니다. (예: 아이템 구매)
    /// </summary>
    /// <returns>구매에 성공하면 true, 돈이 부족하면 false</returns>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return false;

        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateMoneyUI();
            Debug.Log($"{amount}원 사용. 현재 잔액: {currentMoney}원");
            return true;
        }
        else
        {
            Debug.Log("돈이 부족합니다!");
            return false;
        }
    }

    /// <summary>
    /// 현재 돈이 얼마인지 확인합니다.
    /// </summary>
    public int GetCurrentMoney()
    {
        return currentMoney;
    }

    /// <summary>
    /// UI 텍스트를 현재 돈에 맞게 업데이트합니다.
    /// </summary>
    private void UpdateMoneyUI()
    {
        if (moneyTMPText != null)
        {
            // "G"는 Gold의 약자입니다. 원화(₩) 등으로 바꾸셔도 됩니다.
            moneyTMPText.text = currentMoney.ToString() + " G";
        }
    }
}