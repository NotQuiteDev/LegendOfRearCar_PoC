using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PowerUpgradeZone : MonoBehaviour
{
    [Header("===== UI 연결 =====")]
    [SerializeField] private GameObject powerPanel;       // 파워 업그레이드 전용 패널
    [SerializeField] private Button upgradeButton;        // 업그레이드 버튼
    [SerializeField] private TextMeshProUGUI infoText;    // 설명 텍스트 (현재 공격력 -> 다음 공격력)
    [SerializeField] private TextMeshProUGUI costText;    // 가격 텍스트

    [System.Serializable]
    public struct PowerLevel
    {
        public int cost;       // 비용
        public float damage;   // 적용될 공격력
    }

    [Header("===== 강화 단계 설정 =====")]
    public List<PowerLevel> powerLevels; // 인스펙터에서 설정

    // 내부 변수
    private int currentIndex = 0;
    private PlayerWallet playerWallet;
    private PlayerController_TPS_Melee playerController;

    void Start()
    {
        powerPanel.SetActive(false);
        upgradeButton.onClick.AddListener(TryUpgradePower);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerWallet = other.GetComponent<PlayerWallet>();
            playerController = other.GetComponent<PlayerController_TPS_Melee>();

            if (playerWallet != null && playerController != null)
            {
                // 플레이어의 현재 데미지가 어느 단계인지 대략 확인 (싱크 맞추기)
                SyncCurrentLevel();
                
                powerPanel.SetActive(true);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                UpdateUI();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            powerPanel.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            playerWallet = null;
            playerController = null;
        }
    }

    // 현재 플레이어 공격력을 보고 레벨 인덱스를 맞춤
    private void SyncCurrentLevel()
    {
        float currentDmg = playerController.GetDamage();
        currentIndex = 0;

        // 설정된 레벨들 중 현재 데미지보다 높거나 같은게 있으면 그 다음 단계부터 시작하도록
        for (int i = 0; i < powerLevels.Count; i++)
        {
            if (currentDmg >= powerLevels[i].damage)
            {
                currentIndex = i + 1;
            }
        }
    }

    public void TryUpgradePower()
    {
        // 만렙 체크
        if (currentIndex >= powerLevels.Count) return;

        PowerLevel nextLevel = powerLevels[currentIndex];

        // 돈 확인 및 사용
        if (playerWallet != null && playerWallet.SpendMoney(nextLevel.cost))
        {
            // 업그레이드 적용
            playerController.SetDamage(nextLevel.damage);
            
            // 레벨 증가
            currentIndex++;
            
            // UI 갱신
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (playerController == null) return;

        float currentDmg = playerController.GetDamage();

        if (currentIndex < powerLevels.Count)
        {
            PowerLevel nextStep = powerLevels[currentIndex];
            
            // 텍스트 표시
            infoText.text = $"현재 공격력 : <color=white>{currentDmg}</color>\n" +
                            $"다음 공격력 : <color=red>{nextStep.damage}</color>";
            
            costText.text = $"{nextStep.cost} G";
            
            upgradeButton.interactable = true;
            upgradeButton.GetComponentInChildren<TextMeshProUGUI>().text = "강화하기";
        }
        else
        {
            // 만렙
            infoText.text = $"현재 공격력 : <color=red>{currentDmg}</color>\n" +
                            "<color=yellow>최대 레벨 도달!</color>";
            costText.text = "-";
            
            upgradeButton.interactable = false;
            upgradeButton.GetComponentInChildren<TextMeshProUGUI>().text = "MAX";
        }
    }
}