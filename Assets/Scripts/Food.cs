using UnityEngine;

// HoldableObject를 상속받아 줍고/놓을 수 있습니다.
public class Food : HoldableObject
{
    [Header("음식 스탯")]
    public float energyToRestore = 25f;

    // 만약 이 음식이 '상점 판매용'이라면,
    // 이 프리팹에 BuyableItem.cs도 같이 붙어있어야 합니다.
    // 만약 이 음식이 '판매도 가능'하다면,
    // SellableItem.cs도 같이 붙어있을 수 있습니다. (다재다능한 아이템)

    protected override void Awake()
    {
        base.Awake(); // 부모(HoldableObject)의 Awake 실행
    }

    /// <summary>
    /// [PUBLIC] 이 음식을 먹습니다. (PlayerController에서 호출)
    /// </summary>
    /// <param name="playerEnergy">에너지를 회복시킬 대상</param>
    public void Consume(PlayerEnergy playerEnergy)
    {
        if (playerEnergy != null)
        {
            playerEnergy.RestoreEnergy(energyToRestore);
            Debug.Log($"음식 섭취! 에너지 {energyToRestore} 회복!");

            // 먹었으니 오브젝트 파괴
            Destroy(this.gameObject);
        }
    }
}