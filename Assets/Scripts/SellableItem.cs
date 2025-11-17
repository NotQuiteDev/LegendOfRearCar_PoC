using UnityEngine;

// 이 스크립트는 HoldableObject의 모든 기능을 물려받습니다.
// (PickUp, Drop 기능이 이미 구현되어 있음)
public class SellableItem : HoldableObject
{
    [Header("아이템 가격")]
    [Tooltip("이 아이템을 상점에 팔 때 받을 수 있는 가격입니다.")]
    public int itemValue = 10;

    // HoldableObject의 Awake, PickUp, Drop 기능을
    // 그대로 사용하므로 추가로 코드를 작성할 필요가 없습니다.
    // 만약 줍거나 놓을 때 특별한 사운드를 재생하고 싶다면
    // PickUp, Drop 함수를 override 해서 구현할 수 있습니다.
}