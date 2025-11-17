using UnityEngine;
using System.Collections.Generic; // List를 사용하기 위해 필요합니다!

// [NEW] 인스펙터에 노출시키기 위한 드랍 아이템 정보 구조체
[System.Serializable]
public struct ItemDrop
{
    public GameObject itemPrefab; // 드랍할 아이템 프리팹
    [Range(0f, 1f)]
    public float dropChance; // 이 아이템이 드랍될 확률 (0.0 ~ 1.0)
}

public class Ore : MonoBehaviour
{
    [Header("광석 스탯")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    // [MODIFIED] 드랍 아이템 리스트
    [Header("드랍 아이템")]
    [SerializeField] private List<ItemDrop> possibleDrops; // 드랍 가능한 아이템 목록

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

        // TODO: 피격 이펙트 (파티클, 사운드) 재생

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} 파괴됨!");

        // TODO: 파괴 이펙트 (파티클, 사운드) 재생

        // [MODIFIED] 드랍 로직 변경
        HandleDrops();
        
        // 광석 오브젝트 파괴
        Destroy(gameObject);
    }

    /// <summary>
    /// [NEW] 드랍 리스트를 순회하며 아이템 드랍을 처리합니다.
    /// </summary>
    private void HandleDrops()
    {
        if (possibleDrops == null || possibleDrops.Count == 0)
        {
            return; // 드랍할 아이템이 설정되지 않았으면 종료
        }

        // 리스트에 있는 모든 아이템을 '각각' 검사합니다.
        foreach (ItemDrop drop in possibleDrops)
        {
            // Random.value는 0.0에서 1.0 사이의 난수를 반환합니다.
            // 난수가 설정된 확률(dropChance)보다 낮으면 당첨(드랍)입니다.
            if (Random.value <= drop.dropChance)
            {
                // 아이템 프리팹이 설정되어 있다면
                if (drop.itemPrefab != null)
                {
                    // 광석의 위치에 아이템 생성
                    Instantiate(drop.itemPrefab, transform.position, Quaternion.identity);
                }
            }
        }
    }
}