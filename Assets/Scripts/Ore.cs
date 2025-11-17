using UnityEngine;
using System.Collections.Generic; // List를 사용하기 위해 필요합니다.

// [MODIFIED] 드랍 아이템 정보 구조체
[System.Serializable]
public struct ItemDrop
{
    public GameObject itemPrefab; // 드랍할 아이템 프리팹

    [Tooltip("'확률(%)'이 아닌 '가중치'입니다. 상대적인 값입니다.")]
    public float weight; // 예: 돌 = 80, 보석 = 10
}

public class Ore : MonoBehaviour
{
    [Header("광석 스탯")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("드랍 아이템 (가중치)")]
    [Tooltip("드랍될 수 있는 아이템과 그 가중치 목록")]
    [SerializeField] private List<ItemDrop> possibleDrops; 

    [Header("드랍 없음 (가중치)")]
    [Tooltip("아무것도 드랍되지 않을 가중치 값입니다. (예: 10)")]
    [SerializeField] private float nothingWeight = 10f; 

    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 외부(예: 곡괭이)에서 호출하여 광석에 데미지를 줍니다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} 체력: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return; // 중복 실행 방지
        isDead = true;

        Debug.Log($"{gameObject.name} 파괴됨!");

        // 가중치에 따라 아이템 드랍 처리
        HandleDrops();
        
        // 광석 오브젝트 파괴
        Destroy(gameObject);
    }

    /// <summary>
    /// [REPLACED] 가중치 기반으로 "하나의" 아이템만 드랍합니다.
    /// </summary>
    private void HandleDrops()
    {
        // 1. '드랍 없음' 가중치부터 시작해서 총 가중치를 계산합니다.
        float totalWeight = nothingWeight;

        // 리스트에 있는 모든 아이템의 가중치를 더합니다.
        foreach (ItemDrop drop in possibleDrops)
        {
            totalWeight += drop.weight;
        }

        // 만약 총 가중치가 0이거나 (설정 오류) 
        // '드랍 없음' 가중치만 있다면 (totalWeight == nothingWeight)
        // 아무것도 드랍하지 않고 종료합니다.
        if (totalWeight <= 0 || (totalWeight == nothingWeight && possibleDrops.Count > 0))
        {
            Debug.LogWarning("아이템 드랍 가중치가 잘못 설정되었습니다.");
            return;
        }

        // 2. 0부터 총 가중치 사이의 랜덤 숫자를 하나 뽑습니다.
        float randomPick = Random.Range(0f, totalWeight);

        // 3. '드랍 없음' 가중치부터 깎아내립니다.
        // (뽑힌 숫자가 '드랍 없음' 가중치보다 작으면 '꽝'에 당첨된 것입니다)
        if (randomPick < nothingWeight)
        {
            Debug.Log("아무것도 드랍되지 않았습니다. (꽝)");
            return;
        }
        
        // '꽝'이 아니라면, '드랍 없음' 가중치를 뺀 값으로 다시 계산합니다.
        randomPick -= nothingWeight;

        // 4. 아이템 리스트를 순회하며 당첨 아이템을 찾습니다.
        foreach (ItemDrop drop in possibleDrops)
        {
            // 현재 아이템의 가중치 범위 안에 랜덤 숫자가 포함되면
            if (randomPick < drop.weight)
            {
                // 이 아이템이 당첨!
                if (drop.itemPrefab != null)
                {
                    Instantiate(drop.itemPrefab, transform.position, Quaternion.identity);
                    Debug.Log($"[아이템 드랍] {drop.itemPrefab.name} (가중치: {drop.weight})");
                }
                return; // ★★★ 중요: 하나만 드랍하고 즉시 함수 종료
            }
            
            // 이번 아이템이 아니라면, 가중치를 빼고 다음 아이템 검사
            randomPick -= drop.weight;
        }
    }
}