using UnityEngine;
using UnityEngine.InputSystem; // Input System 사용

public class SellButton : MonoBehaviour
{
    [Header("연결 (필수)")]
    [SerializeField] private SellingZone sellingZone; // 이 버튼이 제어할 판매 구역

    private InputSystem_Actions inputActions; // 플레이어 입력을 받기 위함
    private bool isPlayerNearby = false;    // 플레이어가 근처에 있는지?

    void Awake()
    {
        // PlayerController와는 '별개로' 이 스크립트도 입력을 받아야 합니다.
        inputActions = new InputSystem_Actions();

        if (sellingZone == null)
        {
            Debug.LogError("SellButton에 SellingZone이 연결되지 않았습니다!", this.gameObject);
        }
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
    }

    void OnDisable()
    {
        inputActions.Player.Disable();
    }

    void Update()
    {
        // 1. 플레이어가 근처에 있고
        // 2. 상호작용(Interact) 키를 '방금' 눌렀다면
        if (isPlayerNearby && inputActions.Player.Interact.WasPressedThisFrame())
        {
            Debug.Log("판매 버튼 상호작용!");
            // 연결된 판매 구역에게 판매하라고 명령
            sellingZone?.SellItems();
        }
    }

    // 이 스크립트가 붙은 오브젝트의 트리거 설정
    private void OnTriggerEnter(Collider other)
    {
        // (중요) 플레이어에게 "Player" 태그가 있어야 합니다!
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            Debug.Log("플레이어가 판매 버튼 근처에 왔습니다.");
            // TODO: (선택 사항) "E키를 눌러 판매" 같은 UI 띄우기
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            Debug.Log("플레이어가 판매 버튼에서 멀어졌습니다.");
            // TODO: (선택 사항) "E키를 눌러 판매" UI 숨기기
        }
    }
}