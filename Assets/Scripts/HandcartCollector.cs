using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HandcartCollector: MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private LayerMask collectibleLayer; // 예: Collectible
    [SerializeField] private float absorbSpeed = 10f;
    
    // [NEW] 수집된 아이템이 변경될 레이어 (예: Default 혹은 CartItem)
    // 리어카의 collectibleLayer에 포함되지 않는 레이어여야 합니다!
    [SerializeField] private string collectedLayerName = "Default"; 

    [Header("3D Grid Settings")]
    [SerializeField] private Transform cargoOrigin; 
    [SerializeField] private int gridWidthX = 3;   
    [SerializeField] private int gridLengthZ = 4;  
    [SerializeField] private int gridHeightY = 2; 
    [SerializeField] private Vector3 spacing = new Vector3(0.5f, 0.5f, 0.5f);

    private List<Transform> slots = new List<Transform>();
    private bool[] isSlotOccupied;

    // 중복 방지용 HashSet (방금 수집 명령 내린 애는 또 검사 안 하게)
    private HashSet<GameObject> collectingItems = new HashSet<GameObject>();

    void Start()
    {
        Generate3DSlots();
    }

    void Generate3DSlots()
    {
        // (기존 슬롯 생성 코드와 동일)
        if (cargoOrigin == null) cargoOrigin = this.transform;

        foreach(Transform child in cargoOrigin) 
        { 
            if(child.name.StartsWith("Slot_")) Destroy(child.gameObject); 
        }
        slots.Clear();

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
        // 1. 레이어 확인 (수집 대상인지)
        if (((1 << other.gameObject.layer) & collectibleLayer) != 0)
        {
            // 2. 이미 수집 목록에 있는 놈이면 패스 (중복 방지 핵심)
            if (collectingItems.Contains(other.gameObject)) return;

            // 3. 이미 리어카 자식이어도 패스
            if (other.transform.parent != null && other.transform.IsChildOf(cargoOrigin)) return;

            CollectItem(other.gameObject);
        }
    }

    void CollectItem(GameObject item)
    {
        int emptyIndex = -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!isSlotOccupied[i])
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex == -1) return;

        // [중요] 중복 방지 목록에 등록
        collectingItems.Add(item);
        isSlotOccupied[emptyIndex] = true;
        
        // 컴포넌트 처리
        Rigidbody rb = item.GetComponent<Rigidbody>();
        Collider col = item.GetComponent<Collider>();

        if (rb) rb.isKinematic = true;
        if (col) col.isTrigger = true;

        // [핵심 해결책] 레이어를 바꿔버립니다.
        // 이제 OnTriggerEnter가 이 아이템을 '수집 대상'으로 보지 않습니다.
        int newLayer = LayerMask.NameToLayer(collectedLayerName);
        if (newLayer != -1)
        {
            item.layer = newLayer;
        }
        else
        {
            Debug.LogWarning($"Layer '{collectedLayerName}'가 존재하지 않습니다. Default로 설정합니다.");
            item.layer = 0; // Default
        }

        StartCoroutine(FlyToSlot(item.transform, slots[emptyIndex], item));
    }

    IEnumerator FlyToSlot(Transform item, Transform dest, GameObject originalItem)
    {
        float t = 0;
        Vector3 startPos = item.position;
        Quaternion startRot = item.rotation;

        item.SetParent(dest);

        while (t < 1f)
        {
            t += Time.deltaTime * absorbSpeed;
            if (item == null) yield break; // 중간에 파괴된 경우 방어

            item.position = Vector3.Lerp(startPos, dest.position, t);
            item.rotation = Quaternion.Lerp(startRot, Quaternion.identity, t);
            yield return null;
        }

        if (item != null)
        {
            item.localPosition = Vector3.zero;
            item.localRotation = Quaternion.identity;
            
            // 수집이 완전히 끝났으면 중복 방지 목록에서 제거 (혹시 나중에 버리거나 팔 때를 위해)
            if (collectingItems.Contains(originalItem))
            {
                collectingItems.Remove(originalItem);
            }
        }
    }
}