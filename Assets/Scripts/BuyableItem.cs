using UnityEngine;

public class BuyableItem : MonoBehaviour
{
    [Header("상점 설정")]
    public int price = 20;

    [Header("상태 (디버그용)")]
    [Tooltip("구매가 완료되었는지 여부")]
    public bool isPurchased = false; 

    // 이 스크립트는 상태(가격, 구매여부)만 저장하므로
    // Pickaxe.cs, Food.cs, SellableItem.cs 등과
    // '한 오브젝트에 같이' 있을 수 있습니다.
}