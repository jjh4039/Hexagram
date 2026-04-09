using System.Collections.Generic;
using UnityEngine;

public class BuffUIManager : MonoBehaviour
{
    [SerializeField] private BuffManager playerBuffManager;
    [SerializeField] private GameObject buffSlotPrefab;
    [SerializeField] private Transform buffContainer; // 버프 아이콘들이 생성될 부모 Transform

    private List<BuffSlotUI> spawnedSlots = new List<BuffSlotUI>();

    private void Start()
    {
        if (playerBuffManager != null)
        {
            // 이벤트 구독 (버프 변경 시 자동 호출)
            playerBuffManager.OnBuffUpdated += RefreshUI;
        }
    }

    private void OnDestroy()
    {
        if (playerBuffManager != null)
        {
            // 메모리 누수 방지를 위한 구독 해제
            playerBuffManager.OnBuffUpdated -= RefreshUI;
        }
    }

    private void RefreshUI()
    {
        if (playerBuffManager == null) return;

        int activeCount = playerBuffManager.activeBuffs.Count;

        // 슬롯 갯수 부족 시 추가 생성
        while (spawnedSlots.Count < activeCount)
        {
            GameObject go = Instantiate(buffSlotPrefab, buffContainer);
            BuffSlotUI slot = go.GetComponent<BuffSlotUI>();
            if (slot != null) spawnedSlots.Add(slot);
        }

        // 데이터 매핑 및 안 쓰는 슬롯 비활성화
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (i < activeCount)
            {
                spawnedSlots[i].Setup(playerBuffManager.activeBuffs[i]);
            }
            else
            {
                spawnedSlots[i].gameObject.SetActive(false);
            }
        }
    }
}