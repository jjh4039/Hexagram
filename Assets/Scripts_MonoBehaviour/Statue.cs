using UnityEngine;
using UnityEngine.InputSystem; // 키보드 입력을 위해 필요

public class Statue : MonoBehaviour
{
    [Header("--- Settings ---")]
    public GameObject interactEffect; // 상호작용 가능할 때 띄울 표시 (예: 'F' 아이콘)

    private bool isPlayerNearby = false; // 플레이어가 근처에 있나?
    private bool isActivated = false;    // 스테이지가 클리어되어 활성화되었나?

    private void Start()
    {
        // 처음엔 상호작용 불가능하게 꺼두기
        if (interactEffect != null) interactEffect.SetActive(false);
    }

    // StageController가 호출할 함수: "전투 끝났다! 석상 켜!"
    public void ActivateStatue()
    {
        isActivated = true;
        // 여기에 석상이 빛나는 효과 등을 넣으면 좋습니다.
    }

    private void Update()
    {
        // 활성화 안 됐거나, 플레이어가 멀리 있으면 무시
        if (!isActivated || !isPlayerNearby) return;

        // F키를 누르면 지도 열기
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (GameManager.instance != null && GameManager.instance.mapManager != null)
            {
                GameManager.instance.mapManager.ToggleMap();
            }
        }
    }

    // 플레이어가 근처에 왔을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isActivated)
        {
            isPlayerNearby = true;
            if (interactEffect != null) interactEffect.SetActive(true); // 'F' 표시 켜기
        }
    }

    // 플레이어가 멀어졌을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (interactEffect != null) interactEffect.SetActive(false); // 'F' 표시 끄기
        }
    }
}