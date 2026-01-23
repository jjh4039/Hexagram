using UnityEngine;
using TMPro;
using System.Collections;

public class StageMessageUI : MonoBehaviour
{
    public static StageMessageUI instance;

    [Header("--- Entry UI (Start) ---")]
    [SerializeField] private CanvasGroup entryGroup;
    [SerializeField] private TextMeshProUGUI entryTitle;
    [SerializeField] private TextMeshProUGUI entryDesc;

    [Header("--- Clear UI (End) ---")]
    [SerializeField] private CanvasGroup clearGroup;
    [SerializeField] private TextMeshProUGUI clearText;

    [Header("Settings")]
    [SerializeField] private float startDelay = 0.7f;
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float waitTime = 2.0f;
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip sfxClear; // ★ [추가] 클리어 텍스트 사운드

    private void Awake()
    {
        instance = this;

        if (entryGroup != null) entryGroup.alpha = 0f;
        if (clearGroup != null) clearGroup.alpha = 0f;
    }

    public void ShowEntryMessage(string title, string desc)
    {
        if (entryTitle != null) entryTitle.text = title;
        if (entryDesc != null) entryDesc.text = desc;

        StopAllCoroutines();
        if (clearGroup != null) clearGroup.alpha = 0f;

        StartCoroutine(FadeSequence(entryGroup, true));
    }

    public void ShowClearMessage()
    {
        StopAllCoroutines();

        // ★ [추가] 클리어 텍스트가 뜰 때 웅장하게 재생
        if (sfxClear != null)
        {
            SoundManager.instance.PlaySFX(sfxClear, 1.2f, 0.1f);
        }

        StartCoroutine(FadeSequence(clearGroup, false));
    }

    private IEnumerator FadeSequence(CanvasGroup targetGroup, bool isEntry)
    {
        if (targetGroup == null) yield break;

        if (isEntry) yield return new WaitForSeconds(startDelay);

        float timer = 0f;
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            targetGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInTime);
            yield return null;
        }
        targetGroup.alpha = 1f;

        yield return new WaitForSeconds(waitTime);

        timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            targetGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
            yield return null;
        }
        targetGroup.alpha = 0f;
    }
}