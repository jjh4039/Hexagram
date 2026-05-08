using System.Collections.Generic;
using UnityEngine;

public class BuffUIManager : MonoBehaviour
{
    [SerializeField] private BuffManager playerBuffManager;
    [SerializeField] private GameObject buffSlotPrefab;
    [SerializeField] private Transform buffContainer;

    private List<BuffSlotUI> spawnedSlots = new List<BuffSlotUI>();

    private void Start()
    {
        if (playerBuffManager != null)
        {
            playerBuffManager.OnBuffUpdated += RefreshUI;
        }
    }

    private void OnDestroy()
    {
        if (playerBuffManager != null)
        {
            playerBuffManager.OnBuffUpdated -= RefreshUI;
        }
    }

    private void RefreshUI()
    {
        if (!playerBuffManager) return;

        int activeCount = playerBuffManager.activeBuffs.Count;

        while (spawnedSlots.Count < activeCount)
        {
            GameObject go = Instantiate(buffSlotPrefab, buffContainer);
            BuffSlotUI slot = go.GetComponent<BuffSlotUI>();
            if (slot) spawnedSlots.Add(slot);
        }

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