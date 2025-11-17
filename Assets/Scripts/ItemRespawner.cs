using UnityEngine;
using System.Collections; // 코루틴 사용

public class ItemRespawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject itemPrefab; // 이 자리에 스폰시킬 아이템의 프리팹

    [Header("리스폰 딜레이")]
    [SerializeField] private float respawnDelay = 3.0f; // 아이템이 사라진 후 다시 스폰될 때까지 걸리는 시간 (초)

    [Header("구매 시 리스폰")]
    [Tooltip("체크하면, 아이템이 파괴되지 않아도 '구매'되는 즉시 새 아이템을 리스폰합니다.")]
    [SerializeField] private bool respawnOnPurchase = false; // [NEW]

    // 현재 스폰되어 있는 아이템을 추적합니다.
    private GameObject currentItemInstance;
    private BuyableItem spawnedBuyableItem; // [NEW] 스폰된 아이템의 Buyable 컴포넌트
    private bool isSpawning = false;

    void Start()
    {
        if (itemPrefab != null)
        {
            SpawnItem();
        }
        else
        {
            Debug.LogError("리스폰할 아이템 프리팹이 등록되지 않았습니다!", this.gameObject);
        }
    }

    void Update()
    {
        // 이미 리스폰 절차가 진행 중이면, 아무것도 하지 않음
        if (isSpawning)
        {
            return;
        }

        bool triggerRespawn = false;

        // 1. 아이템이 '파괴'되었는지 확인 (예: 구매 실패)
        if (currentItemInstance == null)
        {
            triggerRespawn = true;
        }
        // 2. 파괴되진 않았지만, '구매 시 리스폰' 옵션이 켜져 있는지 확인
        else if (respawnOnPurchase)
        {
            // 3. 스폰된 아이템이 '구매'되었는지 확인
            if (spawnedBuyableItem != null && spawnedBuyableItem.isPurchased)
            {
                // 구매됨! 이 아이템은 이제 플레이어 소유입니다.
                // 스포너는 이 아이템을 "잊어버리고" (null 처리) 리스폰을 준비합니다.
                currentItemInstance = null; 
                spawnedBuyableItem = null;
                triggerRespawn = true;
                Debug.Log("아이템이 구매되어 새 아이템을 리스폰합니다...");
            }
            // (참고) spawnedBuyableItem이 null인 경우 = 프리팹에 BuyableItem이 없는 경우
            // 이 경우, StartCoroutine(RespawnItem) 안에서 자동으로 경고가 뜹니다.
        }

        // 1번(파괴) 또는 2,3번(구매)에 의해 리스폰이 결정되었다면
        if (triggerRespawn)
        {
            StartCoroutine(RespawnItem());
        }
    }

    /// <summary>
    /// 아이템을 즉시 스폰합니다.
    /// </summary>
    private void SpawnItem()
    {
        if (itemPrefab == null) return;

        currentItemInstance = Instantiate(itemPrefab, transform.position, transform.rotation);
        
        // [NEW] 스폰된 인스턴스에서 BuyableItem 컴포넌트를 찾아 저장합니다.
        if (currentItemInstance != null)
        {
            spawnedBuyableItem = currentItemInstance.GetComponent<BuyableItem>();
        }

        // (선택 사항) Hierarchy 정리
        currentItemInstance.transform.SetParent(this.transform);

        Debug.Log($"{itemPrefab.name}이(가) 리스폰되었습니다.");
    }

    /// <summary>
    /// 설정된 딜레이 시간 후에 아이템을 리스폰합니다.
    /// </summary>
    private IEnumerator RespawnItem()
    {
        isSpawning = true; 
        
        // [NEW] 리스폰 대기열에 들어가기 전에,
        // 혹시 모를 참조를 깨끗이 비웁니다.
        spawnedBuyableItem = null; 

        Debug.Log($"{itemPrefab.name}이(가) {respawnDelay}초 후 리스폰됩니다...");
        
        yield return new WaitForSeconds(respawnDelay);

        // [NEW] 프리팹 자체에 BuyableItem이 있는지 마지막으로 확인 (경고용)
        if (respawnOnPurchase && itemPrefab.GetComponent<BuyableItem>() == null)
        {
            Debug.LogWarning("'Respawn On Purchase'가 체크되어 있지만, " +
                             $"할당된 프리팹({itemPrefab.name})에 BuyableItem 스크립트가 없습니다!", this.gameObject);
        }

        SpawnItem();
        
        isSpawning = false; 
    }
}