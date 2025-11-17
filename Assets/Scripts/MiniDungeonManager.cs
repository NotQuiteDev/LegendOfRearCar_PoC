using UnityEngine;

public class MineDungeonManager : MonoBehaviour
{
    [Header("광산 설정")]
    [SerializeField] private GameObject minePrefab; // 소환할 광산 프리팹
    [SerializeField] private Transform orePoint;    // 광산이 소환될 위치 (말씀하신 orePoint)
    [SerializeField] private int entryCost = 20;

    // 씬에서 플레이어 지갑을 찾아 저장합니다.
    private PlayerWallet playerWallet;

    // 현재 소환된 광산 프리팹의 인스턴스를 저장합니다.
    private GameObject spawnedMineInstance;

    // 플레이어가 현재 '안에' 있는지 확인 (중복 입장 방지)
    private bool isPlayerInside = false;

    void Start()
    {
        playerWallet = FindObjectOfType<PlayerWallet>();
        if (playerWallet == null)
        {
            Debug.LogError("MineDungeonManager가 PlayerWallet을 찾을 수 없습니다!");
        }
        if (minePrefab == null || orePoint == null)
        {
            Debug.LogError("Mine Prefab 또는 Ore Point가 할당되지 않았습니다!", this.gameObject);
        }
    }

    /// <summary>
    /// [PUBLIC] Enter 트리거가 이 함수를 호출합니다.
    /// </summary>
    public void OnPlayerEnter(Collider playerCollider)
    {
        // 1. 플레이어가 맞는지 확인
        if (!playerCollider.CompareTag("Player")) return;

        // 2. 이미 안에 있거나, 알 수 없는 이유로 광산이 이미 소환된 상태면 무시
        if (isPlayerInside || spawnedMineInstance != null)
        {
            Debug.Log("이미 광산에 입장한 상태입니다.");
            return;
        }

        // 3. 지갑이 연결됐는지 확인
        if (playerWallet == null) return;

        // 4. 돈 차감 시도
        if (playerWallet.SpendMoney(entryCost))
        {
            // 4A. 구매 성공
            Debug.Log($"광산 입장! {entryCost}G가 차감되었습니다.");
            isPlayerInside = true;

            // 5. orePoint의 자식으로 광산 프리팹 소환
            spawnedMineInstance = Instantiate(minePrefab, orePoint.position, orePoint.rotation, orePoint);
        }
        else
        {
            // 4B. 구매 실패 (돈 부족)
            Debug.Log("돈이 부족하여 광산에 입장할 수 없습니다.");
            // TODO: (선택) "돈 부족" 사운드, UI 피드백 등
        }
    }

    /// <summary>
    /// [PUBLIC] Exit 트리거가 이 함수를 호출합니다.
    /// </summary>
    public void OnPlayerExit(Collider playerCollider)
    {
        if (!playerCollider.CompareTag("Player")) return;

        // 1. (중요) '안에' 있는 상태에서만 퇴장이 가능합니다.
        // (그냥 입구를 지나가는 행위와 구분)
        if (!isPlayerInside)
        {
            return;
        }

        Debug.Log("광산에서 퇴장합니다. 소환된 프리팹을 삭제합니다.");

        // 2. 소환되었던 광산 인스턴스가 존재하면 파괴
        if (spawnedMineInstance != null)
        {
            Destroy(spawnedMineInstance);
            spawnedMineInstance = null; // 참조를 깨끗하게 비움
        }

        // 3. '안에' 있지 않은 상태로 변경
        isPlayerInside = false;
    }
}