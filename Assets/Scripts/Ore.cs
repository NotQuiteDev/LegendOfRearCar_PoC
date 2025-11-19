using UnityEngine;
using System.Collections.Generic;

// [유지] 드랍 아이템 정보 구조체
[System.Serializable]
public struct ItemDrop
{
    public GameObject itemPrefab;
    public float weight;
}

public class Ore : MonoBehaviour
{
    [Header("광석 스탯")]
    [SerializeField] private float maxHealth = 100f; // 최대 체력
    private float currentHealth;

    [Header("드랍 아이템 설정")]
    [SerializeField] private List<ItemDrop> possibleDrops; 
    [SerializeField] private float nothingWeight = 10f; 

    private bool isMined = false;

    private void Awake()
    {
        currentHealth = maxHealth; // 시작할 때 체력 채우기
    }

    /// <summary>
    /// [복구됨] 곡괭이가 이 함수를 호출해서 데미지를 줍니다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isMined) return;

        currentHealth -= amount;
        // Debug.Log($"{gameObject.name} 남은 체력: {currentHealth}");

        // 체력이 0 이하가 되면 채굴 성공
        if (currentHealth <= 0)
        {
            Mine();
        }
    }

    /// <summary>
    /// 채굴 완료 (아이템 드랍 및 파괴)
    /// </summary>
    public void Mine()
    {
        if (isMined) return;
        isMined = true;

        Debug.Log($"{gameObject.name} 채굴 성공!");

        HandleDrops();       
        Destroy(gameObject);
    }

    // [유지] 드랍 로직
    private void HandleDrops()
    {
        float totalWeight = nothingWeight;
        foreach (ItemDrop drop in possibleDrops) totalWeight += drop.weight;

        if (totalWeight <= 0) return;

        float randomPick = Random.Range(0f, totalWeight);

        if (randomPick < nothingWeight) return; // 꽝
        
        randomPick -= nothingWeight;

        foreach (ItemDrop drop in possibleDrops)
        {
            if (randomPick < drop.weight)
            {
                if (drop.itemPrefab != null)
                {
                    Instantiate(drop.itemPrefab, transform.position, Quaternion.identity);
                }
                return;
            }
            randomPick -= drop.weight;
        }
    }
}