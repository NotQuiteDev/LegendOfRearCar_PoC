using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UpgradeZone : MonoBehaviour
{
    [Header("===== 필수 연결 =====")]
    [SerializeField] private GameObject upgradePanel; // UI 패널 (평소엔 꺼둠)
    [SerializeField] private HandcartCollector targetCart; // 업그레이드할 리어카 (직접 연결)
    
    [Header("===== UI 버튼 연결 =====")]
    [SerializeField] private Button pickaxeButton;       // 곡괭이 버튼
    [SerializeField] private TextMeshProUGUI pickaxeText; // 곡괭이 버튼 텍스트
    
    [SerializeField] private Button cartButton;          // 리어카 버튼
    [SerializeField] private TextMeshProUGUI cartText;    // 리어카 버튼 텍스트

    [System.Serializable]
    public struct UpgradeStep
    {
        public int cost;   // 가격
        public float value; // 적용될 수치 (곡괭이는 속도, 리어카는 높이)
    }

    [Header("===== 업그레이드 데이터 설정 =====")]
    [Tooltip("곡괭이 강화 단계 (공격 속도: 낮을수록 빠름)")]
    public List<UpgradeStep> pickaxeLevels; 

    [Tooltip("리어카 강화 단계 (높이: 높을수록 많이 실음)")]
    public List<UpgradeStep> cartLevels;

    // 현재 레벨 상태
    private int currentPickaxeIndex = 0;
    private int currentCartIndex = 0;

    // 플레이어 지갑 참조
    private PlayerWallet playerWallet;
    private PlayerController_TPS_Melee playerController;

    void Start()
    {
        // 시작할 때 패널 끄기
        upgradePanel.SetActive(false);

        // 버튼에 기능 연결
        pickaxeButton.onClick.AddListener(TryUpgradePickaxe);
        cartButton.onClick.AddListener(TryUpgradeCart);
        
        // 버튼 텍스트 초기화
        UpdateUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerWallet = other.GetComponent<PlayerWallet>();
            playerController = other.GetComponent<PlayerController_TPS_Melee>();
            
            if (playerWallet != null)
            {
                upgradePanel.SetActive(true);
                Cursor.visible = true; // 마우스 커서 보이기
                Cursor.lockState = CursorLockMode.None;
                UpdateUI();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            upgradePanel.SetActive(false);
            Cursor.visible = false; // 마우스 커서 숨기기 (FPS 모드 복귀)
            Cursor.lockState = CursorLockMode.Locked;
            
            playerWallet = null;
            playerController = null;
        }
    }

    // --- 곡괭이 업그레이드 로직 ---
    public void TryUpgradePickaxe()
    {
        if (currentPickaxeIndex >= pickaxeLevels.Count) return; // 만렙

        UpgradeStep nextStep = pickaxeLevels[currentPickaxeIndex];

        if (playerWallet != null && playerWallet.SpendMoney(nextStep.cost))
        {
            // 1. 실제 능력치 적용
            if(playerController != null)
            {
                playerController.SetMiningRate(nextStep.value);
            }
            
            // 2. 레벨 인덱스 증가
            currentPickaxeIndex++;
            
            // 3. UI 갱신
            UpdateUI();
        }
    }

    // --- 리어카 업그레이드 로직 ---
    public void TryUpgradeCart()
    {
        if (currentCartIndex >= cartLevels.Count) return; // 만렙

        UpgradeStep nextStep = cartLevels[currentCartIndex];

        if (playerWallet != null && playerWallet.SpendMoney(nextStep.cost))
        {
            // 1. 실제 능력치 적용
            if (targetCart != null)
            {
                targetCart.SetGridHeight((int)nextStep.value);
            }

            // 2. 레벨 인덱스 증가
            currentCartIndex++;

            // 3. UI 갱신
            UpdateUI();
        }
    }

    // 버튼 텍스트 갱신
    private void UpdateUI()
    {
        // 곡괭이 버튼 텍스트
        if (currentPickaxeIndex < pickaxeLevels.Count)
        {
            UpgradeStep step = pickaxeLevels[currentPickaxeIndex];
            pickaxeText.text = $"곡괭이 강화\n속도: {step.value}초\n비용: {step.cost} G";
            pickaxeButton.interactable = true;
        }
        else
        {
            pickaxeText.text = "곡괭이\n최대 레벨";
            pickaxeButton.interactable = false;
        }

        // 리어카 버튼 텍스트
        if (currentCartIndex < cartLevels.Count)
        {
            UpgradeStep step = cartLevels[currentCartIndex];
            cartText.text = $"리어카 확장\n높이: {step.value}칸\n비용: {step.cost} G";
            cartButton.interactable = true;
        }
        else
        {
            cartText.text = "리어카\n최대 레벨";
            cartButton.interactable = false;
        }
    }
}