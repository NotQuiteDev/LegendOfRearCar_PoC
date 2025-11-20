using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ItemDrop
{
    public string name;
    public GameObject itemPrefab;
    public float weight;
    public Color oreColor; // 드랍 아이템 고유 색상
}

public class Ore : MonoBehaviour
{
    [Header("광석 스탯")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("드랍 아이템 설정")]
    [SerializeField] private List<ItemDrop> possibleDrops; 
    [SerializeField] private float nothingWeight = 10f;    
    [SerializeField] private Color nothingColor = Color.gray;

    private bool isMined = false;
    private ItemDrop? determinedDrop = null; 

    // [추가] 내부 변수들
    private Renderer oreRenderer; // 렌더러 캐싱 (최적화)
    private Color originalColor;  // 처음에 정해진 '원본 색상' 기억용

    private void Awake()
    {
        currentHealth = maxHealth;
        oreRenderer = GetComponent<Renderer>(); // 미리 가져오기
    }

    private void Start()
    {
        DetermineDropAndColor();
    }

    private void DetermineDropAndColor()
    {
        float totalWeight = nothingWeight;
        foreach (ItemDrop drop in possibleDrops) totalWeight += drop.weight;

        if (totalWeight <= 0) 
        {
            SetOreColor(nothingColor);
            return;
        }

        float randomPick = Random.Range(0f, totalWeight);

        if (randomPick < nothingWeight)
        {
            determinedDrop = null;
            SetOreColor(nothingColor);
            return;
        }
        
        randomPick -= nothingWeight;

        foreach (ItemDrop drop in possibleDrops)
        {
            if (randomPick < drop.weight)
            {
                determinedDrop = drop;
                SetOreColor(drop.oreColor);
                return;
            }
            randomPick -= drop.weight;
        }
    }

    // [핵심 수정] 색상을 설정하면서 '원본 색상'도 기억함
    private void SetOreColor(Color color)
    {
        if (oreRenderer != null)
        {
            originalColor = color; // 1. 원본 색상 저장 (매우 중요)
            oreRenderer.material.color = color; // 2. 현재 색상 적용
        }
    }

    public void TakeDamage(float amount)
    {
        if (isMined) return;

        currentHealth -= amount;

        // [추가] 체력이 깎였으므로 색상을 어둡게 갱신
        UpdateColorBrightness();

        if (currentHealth <= 0)
        {
            Mine();
        }
    }

    // [신규 기능] 체력 비율에 따라 색상 어둡게 만들기
    private void UpdateColorBrightness()
    {
        if (oreRenderer == null) return;

        // 0 ~ 1 사이 비율 계산 (음수 방지)
        float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);

        // Color.Lerp(A, B, t): t가 0이면 A(검은색), 1이면 B(원래색)
        // 즉, 체력이 적을수록 검은색에 가까워짐
        oreRenderer.material.color = Color.Lerp(Color.black, originalColor, healthRatio);
        
        // (팁) 만약 너무 새카맣게 변하는 게 싫고 약간만 어두워지길 원하면
        // Color.black 대신 'Color.gray'나 'originalColor * 0.3f' 등을 넣으시면 됩니다.
    }

    public void Mine()
    {
        if (isMined) return;
        isMined = true;
        
        Debug.Log($"{gameObject.name} 채굴 성공!");

        if (determinedDrop.HasValue && determinedDrop.Value.itemPrefab != null)
        {
            Instantiate(determinedDrop.Value.itemPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}