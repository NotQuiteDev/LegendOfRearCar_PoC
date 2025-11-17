using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MinePortalTrigger : MonoBehaviour
{
    // 이 트리거가 '입구'인지 '출구'인지 인스펙터에서 선택
    public enum TriggerType
    {
        Enter,
        Exit
    }
    public TriggerType type;

    // 이 트리거를 관리하는 부모의 Manager
    private MineDungeonManager manager;

    void Awake()
    {
        // 내 부모에게서 Manager를 찾아서 저장
        manager = GetComponentInParent<MineDungeonManager>();
        if (manager == null)
        {
            Debug.LogError("부모 오브젝트에 MineDungeonManager.cs가 없습니다!", this.gameObject);
        }

        // 콜라이더가 반드시 트리거여야 함
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("MinePortalTrigger의 콜라이더는 Is Trigger여야 합니다.", this.gameObject);
        }
    }

    // 플레이어가 닿았을 때
    private void OnTriggerEnter(Collider other)
    {
        if (manager == null) return;

        // 내 타입에 맞춰 Manager의 올바른 함수를 호출
        if (type == TriggerType.Enter)
        {
            manager.OnPlayerEnter(other);
        }
        else if (type == TriggerType.Exit)
        {
            manager.OnPlayerExit(other);
        }
    }
}