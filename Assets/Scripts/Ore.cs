using UnityEngine;
using System.Collections.Generic;

// [유지] 드랍 아이템 정보 구조체
[System.Serializable]
public struct ItemDrop
{
    public string name;
    public GameObject itemPrefab;
    public float weight;
    public Color oreColor;
}

public class Ore : MonoBehaviour
{
    [Header("광석 스탯")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("드랍 아이템 설정")]
    [SerializeField] private List<ItemDrop> possibleDrops; 
    [SerializeField] private float nothingWeight = 10f;    // 꽝일 확률
    [SerializeField] private Color nothingColor = Color.gray; // 꽝일 때 보여질 색상

    private bool isMined = false;
    
    // [추가] 미리 결정된 드랍 정보를 저장할 변수 (null이면 꽝)
    private ItemDrop? determinedDrop = null; 

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // [변경] 생성되자마자 결과 정하고 색깔 입히기
    private void Start()
    {
        DetermineDropAndColor();
    }

    /// <summary>
    /// 생성 시 호출: 무엇을 떨굴지 미리 계산하고 색상 변경
    /// </summary>
    private void DetermineDropAndColor()
    {
        // 1. 전체 가중치 계산
        float totalWeight = nothingWeight;
        foreach (ItemDrop drop in possibleDrops) totalWeight += drop.weight;

        // 설정된 드랍이 없거나 가중치가 0이면 -> 꽝 처리
        if (totalWeight <= 0) 
        {
            SetOreColor(nothingColor);
            determinedDrop = null;
            return;
        }

        // 2. 미리 추첨 (RNG)
        float randomPick = Random.Range(0f, totalWeight);

        // 3. '꽝' 당첨 체크
        if (randomPick < nothingWeight)
        {
            determinedDrop = null; // 꽝
            SetOreColor(nothingColor);
            return;
        }
        
        randomPick -= nothingWeight;

        // 4. 아이템 당첨 체크
        foreach (ItemDrop drop in possibleDrops)
        {
            if (randomPick < drop.weight)
            {
                determinedDrop = drop; // 당첨된 정보 저장
                SetOreColor(drop.oreColor); // 해당 광물의 색으로 변경
                return;
            }
            randomPick -= drop.weight;
        }
    }

    // 광석의 색상을 실제로 바꾸는 함수
    private void SetOreColor(Color color)
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = color;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isMined) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Mine();
        }
    }

    public void Mine()
    {
        if (isMined) return;
        isMined = true;

        Debug.Log($"{gameObject.name} 채굴 성공!");

        // [변경] 이미 정해진(determinedDrop) 아이템을 드랍
        if (determinedDrop.HasValue)
        {
            // 당첨된 아이템이 있고 프리팹도 존재하면 생성
            if (determinedDrop.Value.itemPrefab != null)
            {
                Instantiate(determinedDrop.Value.itemPrefab, transform.position, Quaternion.identity);
            }
            // 필요하다면 여기에 당첨 메시지 추가 (예: "금광석 발견!")
        }
        else
        {
            // 꽝인 경우 (돌멩이 파티클 등을 넣을 수 있음)
            // Debug.Log("아무것도 나오지 않았습니다.");
        }

        Destroy(gameObject);
    }
}