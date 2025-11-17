using UnityEngine;

public class Pickaxe : HoldableObject
{
    [Header("곡괭이 스탯")]
    public float damage = 25f;
    public int maxDurability = 100; // 최대 내구도

    [Header("채굴 설정 (PlayerController에서 이동)")]
    [SerializeField] private float miningRange = 2f; // 채굴 가능 범위
    [SerializeField] private LayerMask oreLayer;     // "Ore" 레이어만 감지
    [SerializeField] private float mineEnergyCost = 1f;  // 1회 소모량

    // 현재 내구도
    private int currentDurability;

    protected override void Awake()
    {
        base.Awake(); // 부모의 Awake() 실행
        currentDurability = maxDurability;
    }

    /// <summary>
    /// [NEW] PlayerController가 '사용'을 요청하면 이 함수가 실행됩니다.
    /// </summary>
    /// <param name="playerEnergy">에너지를 소모할 플레이어의 에너지 컴포넌트</param>
    /// <param name="miningOrigin">채굴 탐색을 시작할 위치 (플레이어 위치)</param>
    public void Use(PlayerEnergy playerEnergy, Vector3 miningOrigin)
    {
        // 1. 에너지 체크
        if (playerEnergy != null)
        {
            // 에너지가 0이거나 소모량보다 적으면
            if (playerEnergy.GetCurrentEnergy() < mineEnergyCost)
            {
                Debug.Log("에너지가 부족하여 채굴할 수 없습니다.");
                return; // 에너지 없으면 중단 (내구도도 닳지 않음)
            }
            
            // 에너지 소모
            playerEnergy.UseEnergy(mineEnergyCost);
        }

        // TODO: 곡괭이 휘두르는 애니메이션/사운드 재생

        // 2. 광석 탐색 (원래 PlayerController가 하던 일)
        Collider[] hitOres = Physics.OverlapSphere(miningOrigin, miningRange, oreLayer);
        bool didHitOre = false; // 광석을 맞췄는지 여부

        foreach (Collider oreCol in hitOres)
        {
            Ore ore = oreCol.GetComponent<Ore>();
            if (ore != null)
            {
                Debug.Log($"광석 {ore.name} 발견! 데미지 {damage}!");
                ore.TakeDamage(damage);
                
                didHitOre = true; // 광석을 맞췄다고 표시
                
                // 한 번의 스윙에 하나의 광석만 타격
                break; 
            }
        }

        // 3. (중요) '광석을 맞췄을 때만' 내구도 감소
        // (만약 헛스윙에도 닳게 하려면 이 'if (didHitOre)' 블록을 지우고
        //  ReduceDurability()만 남기면 됩니다.)
        if (didHitOre)
        {
            ReduceDurability();
        }
    }

    /// <summary>
    /// [NEW] 내구도를 1 감소시키고 0이 되면 파괴됩니다.
    /// </summary>
    private void ReduceDurability()
    {
        currentDurability--;
        Debug.Log($"곡괭이 내구도: {currentDurability}/{maxDurability}");

        // TODO: 내구도 UI 업데이트 (필요하다면)

        if (currentDurability <= 0)
        {
            Debug.Log("곡괭이의 내구도가 다 닳아 파괴되었습니다.");
            
            // TODO: 파괴 사운드/이펙트

            // 스스로 파괴
            Destroy(this.gameObject); 
        }
    }
}