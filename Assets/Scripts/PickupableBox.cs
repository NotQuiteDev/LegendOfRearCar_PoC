using UnityEngine;

public class PickupableBox : HoldableObject
{
    // 상자 고유의 로직이 있다면 여기에 추가합니다.
    // (예: 부서지는 기능 등)

    protected override void Awake()
    {
        base.Awake(); // 부모의 Awake() 실행
        Debug.Log("Box가 준비되었습니다.");
    }
}