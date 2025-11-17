using UnityEngine;
using UnityEngine.UI; // UI 요소를 사용하기 위해 꼭 필요합니다!

public class PlayerEnergy : MonoBehaviour
{
    [Header("에너지 스탯")]
    public float maxEnergy = 100f; // 에너지 최대치
    private float currentEnergy; // 현재 에너지

    [Header("UI 연결")]
    public Slider energySlider; // 인스펙터에서 연결할 에너지 슬라이더

    void Awake()
    {
        // 게임 시작 시 에너지를 최대로 설정
        currentEnergy = maxEnergy;
        UpdateEnergyUI();
    }

    /// <summary>
    /// 에너지를 소모시키는 함수 (다른 스크립트에서 이 함수를 호출)
    /// </summary>
    /// <param name="amount">소모할 에너지 양</param>
    public void UseEnergy(float amount)
    {
        if (currentEnergy <= 0) return; // 에너지가 없으면 아무것도 안 함

        currentEnergy -= amount;
        
        // 에너지가 0 밑으로 내려가지 않게 함
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        
        UpdateEnergyUI(); // UI 업데이트

        if (currentEnergy <= 0)
        {
            Debug.Log("에너지가 모두 소모되었습니다!");
            // TODO: 에너지가 0이 되었을 때의 로직 (예: 기절, 행동 불가)
        }
    }

    /// <summary>
    /// 에너지를 회복시키는 함수 (예: 음식 섭취, 잠)
    /// </summary>
    /// <param name="amount">회복할 에너지 양</param>
    public void RestoreEnergy(float amount)
    {
        currentEnergy += amount;

        // 에너지가 최대치를 넘지 않게 함
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        
        UpdateEnergyUI(); // UI 업데이트
    }

    /// <summary>
    /// UI 슬라이더 값을 현재 에너지 비율로 업데이트합니다.
    /// </summary>
    private void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            // 슬라이더의 value는 0과 1 사이의 비율로 작동합니다.
            energySlider.value = currentEnergy / maxEnergy;
        }
    }

    // (선택 사항) 현재 에너지를 다른 스크립트에서 읽을 수 있게 함
    public float GetCurrentEnergy()
    {
        return currentEnergy;
    }
}