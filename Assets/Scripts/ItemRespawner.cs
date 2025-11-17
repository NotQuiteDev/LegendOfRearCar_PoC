using UnityEngine;
using System.Collections; // 코루틴 사용

public class ItemRespawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject itemPrefab; // 이 자리에 스폰시킬 아이템의 프리팹

    [Header("리스폰 딜레이")]
    [SerializeField] private float respawnDelay = 3.0f; // 아이템이 사라진 후 다시 스폰될 때까지 걸리는 시간 (초)

    // 현재 스폰되어 있는 아이템을 추적합니다.
    private GameObject currentItemInstance;
    // 현재 리스폰 코루틴이 실행 중인지 확인합니다.
    private bool isSpawning = false;

    void Start()
    {
        // 게임이 시작되면 즉시 첫 아이템을 스폰합니다.
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
        // 1. 현재 스폰된 아이템이 (파괴되어서) 사라졌고,
        // 2. 지금 새로 스폰하는 중이 아니라면
        if (currentItemInstance == null && !isSpawning)
        {
            // 리스폰 절차를 시작합니다.
            StartCoroutine(RespawnItem());
        }
    }

    /// <summary>
    /// 아이템을 즉시 스폰합니다.
    /// </summary>
    private void SpawnItem()
    {
        if (itemPrefab == null) return;

        // 이 스크립트(Respawner)의 위치에 프리팹을 생성합니다.
        currentItemInstance = Instantiate(itemPrefab, transform.position, transform.rotation);
        
        // (선택 사항) 스폰된 아이템을 Respawner의 자식으로 만들어
        // Hierarchy 뷰를 깔끔하게 정리합니다.
        currentItemInstance.transform.SetParent(this.transform);

        Debug.Log($"{itemPrefab.name}이(가) 리스폰되었습니다.");
    }

    /// <summary>
    /// 설정된 딜레이 시간 후에 아이템을 리스폰합니다.
    /// </summary>
    private IEnumerator RespawnItem()
    {
        isSpawning = true; // "지금 스폰하는 중" 플래그 켜기
        Debug.Log($"{itemPrefab.name}이(가) {respawnDelay}초 후 리스폰됩니다...");
        
        // 설정된 시간만큼 기다립니다.
        yield return new WaitForSeconds(respawnDelay);

        // 시간이 지난 후, 새 아이템 스폰
        SpawnItem();
        
        isSpawning = false; // "스폰 완료" 플래그 끄기
    }
}