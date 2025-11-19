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
        foreach(Transform child in cargoOrigin) 
        { 
            if(child.name.StartsWith("Slot_")) toDestroy.Add(child.gameObject);
        }
        foreach(var child in toDestroy) Destroy(child);
        
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
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 레이어 확인
        if (((1 << other.gameObject.layer) & collectibleLayer) != 0)
        {
            // 2. 중복 확인
            if (collectingItems.Contains(other.gameObject)) return;
            if (other.transform.parent != null && other.transform.IsChildOf(cargoOrigin)) return;

            CollectItem(other.gameObject);
        }
    }

    void CollectItem(GameObject item)
    {
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

        if (emptyIndex == -1) return; // 꽉 참

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

    // ==========================================
    // [상점 연동용 함수들]
    // ==========================================

    /// <summary>
    /// 현재 리어카에 있는 모든 판매 가능 아이템을 리스트로 반환 (상점이 호출)
    /// </summary>
    public List<SellableItem> GetAllItems()
    {
        List<SellableItem> myItems = new List<SellableItem>();
        foreach (Transform slot in slots)
        {
            if (slot.childCount > 0)
            {
                SellableItem item = slot.GetChild(0).GetComponent<SellableItem>();
                if (item != null) myItems.Add(item);
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
            if (slot.childCount > 0)
            {
                Destroy(slot.GetChild(0).gameObject);
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
}