using UnityEngine;

public class Pickaxe : HoldableObject
{
    [Header("곡괭이 스탯")]
    public float damage = 25f; // 광석에 가할 데미지

    protected override void Awake()
    {
        base.Awake(); // 부모의 Awake() 실행
        Debug.Log("Pickaxe가 준비되었습니다.");
    }

    // PickUp이나 Drop 시 특별한 동작이 필요하면
    // public override void PickUp() { ... } 로 오버라이드 가능
}