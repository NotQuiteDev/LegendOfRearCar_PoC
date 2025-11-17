using UnityEngine;
using System.Collections.Generic; // Dictionary를 사용하기 위해 필요

public class CartCalmZone : MonoBehaviour
{
    [Header("안정화 설정")]
    [Tooltip("아이템이 구역 내에 있을 때 적용할 '저항 값'. 높을수록 둔해집니다.")]
    [SerializeField] private float calmDrag = 10f;
    [Tooltip("아이템이 구역 내에 있을 때 적용할 '회전 저항 값'.")]
    [SerializeField] private float calmAngularDrag = 10f;

    // 아이템의 원래 저항 값을 기억하기 위한 Dictionary
    // Key: 아이템의 Rigidbody
    // Value: 아이템의 원래 저항 값 (drag, angularDrag)
    private class OriginalDrags
    {
        public float drag;
        public float angularDrag;
    }
    private Dictionary<Rigidbody, OriginalDrags> originalDragsMap = new Dictionary<Rigidbody, OriginalDrags>();

    /// <summary>
    /// 이 스크립트가 붙은 오브젝트(수레)의 '자식' 콜라이더에서 
    /// OnTriggerEnter가 발생하면 이 함수가 호출됩니다.
    /// </summary>
    public void NotifyTriggerEnter(Collider other)
    {
        // 들어온 물체의 Rigidbody를 찾습니다.
        // attachedRigidbody는 콜라이더에 붙은 Rigidbody를 찾아줍니다.
        Rigidbody rb = other.attachedRigidbody;

        // Rigidbody가 없거나, 이미 맵에 등록된(구역 안에 있는) 녀석이면 무시
        if (rb == null || rb.isKinematic || originalDragsMap.ContainsKey(rb))
        {
            return;
        }

        // 1. 아이템의 '원래' 저항 값을 저장합니다.
        OriginalDrags drags = new OriginalDrags
        {
            drag = rb.linearDamping,
            angularDrag = rb.angularDamping
        };
        originalDragsMap.Add(rb, drags);

        // 2. '안정화(calm)' 저항 값을 '강제로' 적용합니다.
        rb.linearDamping = calmDrag;
        rb.angularDamping = calmAngularDrag;

        Debug.Log($"{other.name}이(가) 수레에 담겨 안정화됩니다. (drag: {calmDrag})");
    }

    /// <summary>
    /// 자식 콜라이더에서 OnTriggerExit이 발생하면 호출됩니다.
    /// </summary>
    public void NotifyTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        // 1. 맵에서 해당 아이템의 '원래' 저항 값을 찾아옵니다.
        if (originalDragsMap.TryGetValue(rb, out OriginalDrags drags))
        {
            // 2. '원래' 저항 값으로 되돌립니다.
            rb.linearDamping = drags.drag;
            rb.angularDamping = drags.angularDrag;

            // 3. 맵에서 아이템을 제거합니다. (이제 이 구역 소속이 아님)
            originalDragsMap.Remove(rb);

            Debug.Log($"{other.name}이(가) 수레에서 벗어나 원래 상태로 돌아갑니다.");
        }
    }
}