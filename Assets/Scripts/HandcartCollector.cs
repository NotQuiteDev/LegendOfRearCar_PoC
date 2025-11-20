using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HandcartCollector : MonoBehaviour
{
    [Header("수집 설정")]
    [SerializeField] private LayerMask collectibleLayer; // 예: Collectible
    [SerializeField] private float absorbSpeed = 10f;
    
    [Tooltip("수집된 아이템이 변경될 레이어 이름 (예: Default). 리어카가 다시 수집하지 않게 만듭니다.")]
    [SerializeField] private string collectedLayerName = "Default"; 

    [Header("3D 적재 설정 (피봇 = 구석)")]
    [SerializeField] private Transform cargoOrigin; 
    [SerializeField] private int gridWidthX = 3;   
    [SerializeField] private int gridLengthZ = 4;  
    [SerializeField] private int gridHeightY = 2; 
    [SerializeField] private Vector3 spacing = new Vector3(0.5f, 0.5f, 0.5f);

    // 내부 변수
    private List<Transform> slots = new List<Transform>();
    private bool[] isSlotOccupied;
    private HashSet<GameObject> collectingItems = new HashSet<GameObject>(); // 중복 방지용

    void Start()
    {
        GenerateSlots();
    }

    void GenerateSlots()
    {
        if (cargoOrigin == null) cargoOrigin = this.transform;

        // 기존 슬롯 정리
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in cargoOrigin)
        { 
            if (child.name.StartsWith("Slot_")) toDestroy.Add(child.gameObject);
        }
        foreach (var child in toDestroy) Destroy(child);
        
        slots.Clear();

        // 슬롯 생성 (Y -> Z -> X 순서)
        int totalSlots = gridWidthX * gridLengthZ * gridHeightY;
        isSlotOccupied = new bool[totalSlots];

        for (int y = 0; y < gridHeightY; y++)
        {
            for (int z = 0; z < gridLengthZ; z++)
            {
                for (int x = 0; x < gridWidthX; x++)
                {
                    GameObject slotObj = new GameObject($"Slot_Y{y}_Z{z}_X{x}");
                    slotObj.transform.SetParent(cargoOrigin);
                    slotObj.transform.localPosition = new Vector3(x * spacing.x, y * spacing.y, z * spacing.z);
                    slotObj.transform.localRotation = Quaternion.identity;
                    slots.Add(slotObj.transform);
                }
            }
        }

        // 생성 직후는 전부 비어 있음
        for (int i = 0; i < isSlotOccupied.Length; i++) isSlotOccupied[i] = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 레이어 확인
        if (((1 << other.gameObject.layer) & collectibleLayer) == 0)
            return;

        // 2. 중복 확인
        if (collectingItems.Contains(other.gameObject)) return;
        if (other.transform.parent != null && other.transform.IsChildOf(cargoOrigin)) return;

        CollectItem(other.gameObject);
    }

    void CollectItem(GameObject item)
    {
        // 혹시 내부 상태가 꼬여있을 수 있으니, 수집 전에 한 번 정리
        RebuildSlotsFromChildren();

        // 빈 슬롯 찾기
        int emptyIndex = -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!isSlotOccupied[i])
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex == -1)
        {
            Debug.LogWarning("[HandcartCollector] 더 이상 적재할 수 있는 슬롯이 없습니다.");
            return; // 꽉 참
        }

        // 수집 시작
        collectingItems.Add(item);
        isSlotOccupied[emptyIndex] = true;
        
        // 물리 끄기 및 트리거 전환
        Rigidbody rb = item.GetComponent<Rigidbody>();
        Collider col = item.GetComponent<Collider>();
        if (rb) rb.isKinematic = true;
        if (col) col.isTrigger = true;

        // [핵심] 레이어 변경 (더 이상 수집 대상이 아니게 됨)
        int newLayer = LayerMask.NameToLayer(collectedLayerName);
        if (newLayer != -1) item.layer = newLayer;

        StartCoroutine(FlyToSlot(item.transform, slots[emptyIndex], item));
    }

    IEnumerator FlyToSlot(Transform item, Transform dest, GameObject originalObj)
    {
        float t = 0;
        Vector3 startPos = item.position;
        Quaternion startRot = item.rotation;

        item.SetParent(dest);

        while (t < 1f)
        {
            if (item == null) yield break; // 파괴 방어
            t += Time.deltaTime * absorbSpeed;
            item.position = Vector3.Lerp(startPos, dest.position, t);
            item.rotation = Quaternion.Lerp(startRot, Quaternion.identity, t);
            yield return null;
        }

        if (item != null)
        {
            item.localPosition = Vector3.zero;
            item.localRotation = Quaternion.identity;
            
            // 수집 완료 후 목록에서 제거 (이후엔 레이어가 바뀌어서 괜찮음)
            collectingItems.Remove(originalObj);
        }
    }

    // ─────────────────────────────────────────
    // 슬롯 상태 자동 복구 (핵심)
    // ─────────────────────────────────────────
    private void RebuildSlotsFromChildren()
    {
        if (slots == null || slots.Count == 0 || isSlotOccupied == null)
            return;

        // 1) 플래그 초기화
        for (int i = 0; i < isSlotOccupied.Length; i++)
            isSlotOccupied[i] = false;

        // 2) 슬롯들에서 "첫 번째 SellableItem"만 남기고, 나머지는 extraItems로 모은다
        List<Transform> extraItems = new List<Transform>();

        for (int i = 0; i < slots.Count; i++)
        {
            Transform slot = slots[i];
            bool firstAssigned = false;

            // 자식이 0이면 그냥 비어있는 슬롯
            if (slot.childCount == 0)
                continue;

            // childCount가 1 이상일 수 있으니, 전부 검사
            for (int c = 0; c < slot.childCount; c++)
            {
                Transform child = slot.GetChild(c);
                var sellable = child.GetComponent<SellableItem>();
                if (sellable == null) continue; // 이상한 게 섞여 있으면 무시

                if (!firstAssigned)
                {
                    // 이 슬롯의 대표 아이템
                    firstAssigned = true;
                    isSlotOccupied[i] = true;
                    // 자리/회전 정리
                    child.localPosition = Vector3.zero;
                    child.localRotation = Quaternion.identity;
                }
                else
                {
                    // 이 슬롯에 중복으로 들어온 애들 → 나중에 다른 슬롯으로 옮김
                    extraItems.Add(child);
                }
            }
        }

        // 3) cargoOrigin 밑에 있지만 어떤 슬롯에도 들어있지 않은 SellableItem도 extraItems에 포함시키자
        foreach (Transform child in cargoOrigin.GetComponentsInChildren<Transform>())
        {
            if (child == cargoOrigin) continue;
            if (child.name.StartsWith("Slot_")) continue; // 슬롯 자체는 제외

            var sellable = child.GetComponent<SellableItem>();
            if (sellable == null) continue;

            // 이미 슬롯 아래에 있고, 위 루프에서 한 번 처리된 애는 건너뛸 수 있음
            bool isChildOfAnySlot = false;
            foreach (var slot in slots)
            {
                if (child.parent == slot)
                {
                    isChildOfAnySlot = true;
                    break;
                }
            }

            if (!isChildOfAnySlot)
            {
                extraItems.Add(child);
            }
        }

        // 4) extraItems를 남는 슬롯에 순서대로 재배치
        int extraIndex = 0;
        for (int i = 0; i < slots.Count && extraIndex < extraItems.Count; i++)
        {
            if (isSlotOccupied[i]) continue;

            Transform item = extraItems[extraIndex++];
            item.SetParent(slots[i], worldPositionStays: false);
            item.localPosition = Vector3.zero;
            item.localRotation = Quaternion.identity;
            isSlotOccupied[i] = true;
        }

        // 5) 아직도 extraItems가 남았다? → 슬롯 개수보다 아이템이 많다는 뜻 (경고)
        if (extraIndex < extraItems.Count)
        {
            Debug.LogWarning($"[HandcartCollector] 슬롯 수보다 아이템이 많습니다. 초과 아이템 수: {extraItems.Count - extraIndex}");
        }

        // 6) collectingItems도 다시 구성 (안 해도 치명적이진 않지만 깔끔하게)
        collectingItems.Clear();
    }

    // ─────────────────────────────────────────
    // [상점 연동용 함수들]
    // ─────────────────────────────────────────

    /// <summary>
    /// 현재 리어카에 있는 모든 판매 가능 아이템을 리스트로 반환 (상점이 호출)
    /// </summary>
    public List<SellableItem> GetAllItems()
    {
        // 먼저 슬롯/자식 상태를 실제 상황 기준으로 정리
        RebuildSlotsFromChildren();

        List<SellableItem> myItems = new List<SellableItem>();
        foreach (Transform slot in slots)
        {
            if (slot.childCount > 0)
            {
                // 여기도 혹시 2개 이상 있을 수 있으니 전부 검사
                for (int c = 0; c < slot.childCount; c++)
                {
                    var item = slot.GetChild(c).GetComponent<SellableItem>();
                    if (item != null) myItems.Add(item);
                }
            }
        }
        return myItems;
    }

    /// <summary>
    /// 판매 완료 후 리어카 비우기 (상점이 호출)
    /// </summary>
    public void SellAndClearAll()
    {
        // 1. 데이터 초기화
        for (int i = 0; i < isSlotOccupied.Length; i++) isSlotOccupied[i] = false;
        collectingItems.Clear();

        // 2. 실제 오브젝트 파괴
        foreach (Transform slot in slots)
        {
            // 슬롯 안의 모든 자식 제거
            for (int c = slot.childCount - 1; c >= 0; c--)
            {
                Destroy(slot.GetChild(c).gameObject);
            }
        }
    }

    // 에디터 미리보기
    void OnDrawGizmos()
    {
        if (cargoOrigin == null) return;
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        for (int y = 0; y < gridHeightY; y++)
        {
            for (int z = 0; z < gridLengthZ; z++)
            {
                for (int x = 0; x < gridWidthX; x++)
                {
                    Vector3 localPos = new Vector3(x * spacing.x, y * spacing.y, z * spacing.z);
                    Vector3 worldPos = cargoOrigin.TransformPoint(localPos);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.2f);
                }
            }
        }
    }
    public void SetGridHeight(int newHeight)
    {
        gridHeightY = newHeight;
        GenerateSlots(); // 높이가 바뀌었으니 슬롯 다시 만들기
        Debug.Log($"[업그레이드] 리어카 높이가 {gridHeightY}칸으로 확장되었습니다!");
    }
}
