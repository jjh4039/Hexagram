using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [Header("Sequence Delays")]
    [SerializeField] private float delayA = 1.0f;
    [SerializeField] private float delayB = 0.5f;
    [SerializeField] private float delayD = 0.3f;
    [SerializeField] private float delayC = 0.5f;
    [SerializeField] private float delayE = 0.8f;
    [SerializeField] private float countDelay = 0.05f;

    [Header("UI Objects (Left Texts)")]
    [SerializeField] private GameObject text0Obj;
    [SerializeField] private GameObject text1Obj;
    [SerializeField] private GameObject text2Obj;
    [SerializeField] private GameObject text3Obj;

    [Header("UI Objects (Right Texts)")]
    [SerializeField] private GameObject textAObj;
    [SerializeField] private GameObject textBObj;
    [SerializeField] private GameObject textCObj;
    [SerializeField] private GameObject spacePromptObj;

    [Header("Result Value Texts")]
    [SerializeField] private TextMeshProUGUI progressResultText; 
    [SerializeField] private TextMeshProUGUI timeResultText;
    [SerializeField] private TextMeshProUGUI damageResultText;
    [SerializeField] private TextMeshProUGUI currentOwnedGemText;

    [Header("Text Components For Counting")]
    [SerializeField] private TextMeshProUGUI timeRewardText;
    [SerializeField] private TextMeshProUGUI damageRewardText;
    [SerializeField] private TextMeshProUGUI totalGemText;
    [SerializeField] private TextMeshProUGUI spacePromptText;

    [Header("Settlement Variables")]
    public int rewardFromTime = 0;
    public int rewardFromDamage = 0;
    public int totalGainedReward = 0;
    public int currentOwnedGems = 0;

    [Header("Sound Clips")]
    [SerializeField] private AudioClip sfxStart;
    [SerializeField] private AudioClip sfxReveal;
    [SerializeField] private AudioClip sfxValue;
    [SerializeField] private AudioClip sfxCounting;

    [Header("Transition Settings")]
    [SerializeField] private Image fadeOutImage;
    [SerializeField] private float fadeOutDuration = 1.0f;

    private bool isCalculationDone = false;
    private bool isTransitioning = false;

    private void OnEnable()
    {
        InitializeUI();

        if (SoundManager.instance != null) SoundManager.instance.PlaySFX(sfxStart, 1.5f, 0f);

        StartCoroutine(Co_GameOverSequence());
    }

    private void Update()
    {
        if (!isCalculationDone || isTransitioning) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GoToTitle();
        }
    }

    private void InitializeUI()
    {
        isCalculationDone = false;
        isTransitioning = false;
        totalGainedReward = 0;

        GameObject[] allObjs = { text0Obj, text1Obj, text2Obj, text3Obj, textAObj, textBObj, textCObj, spacePromptObj };
        foreach (var obj in allObjs) if (obj != null) obj.SetActive(false);

        if (fadeOutImage != null)
        {
            Color c = fadeOutImage.color;
            c.a = 0f;
            fadeOutImage.color = c;
            fadeOutImage.gameObject.SetActive(false);
        }

        if (GameManager.instance != null)
        {
            float playTime = GameManager.instance.currentPlayTime;
            int damage = GameManager.instance.totalDamageDealt;

            Season currentSeason = GameManager.instance.currentSeason;
            int progress = GameManager.instance.currentProgress;
            
            rewardFromTime = Mathf.Min(Mathf.FloorToInt(playTime / 300f), 6);
            rewardFromDamage = damage / 1000;

            if (DataManager.instance != null)
            {
                currentOwnedGems = DataManager.instance.data.totalGems;
            }
            else
            {
                currentOwnedGems = 0;
            }

            if (progressResultText != null)
            {
                string seasonName = "";
                switch (currentSeason)
                {
                    case Season.Spring: seasonName = "봄"; break;
                    case Season.Summer: seasonName = "여름"; break;
                    case Season.Autumn: seasonName = "가을"; break;
                    case Season.Winter: seasonName = "겨울"; break;
                }
                progressResultText.text = $"{seasonName} - {progress}%";
            }

            if (timeResultText != null)
            {
                TimeSpan ts = TimeSpan.FromSeconds(playTime);
                timeResultText.text = string.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            }

            if (damageResultText != null)
            {
                damageResultText.text = damage.ToString("N0");
            }
        }

        UpdateRewardUI();
    }

    private IEnumerator Co_GameOverSequence()
    {
        yield return new WaitForSecondsRealtime(delayA);

        if (text0Obj != null) { text0Obj.SetActive(true); PlayRevealSFX(); }
        yield return new WaitForSecondsRealtime(delayB);

        if (text1Obj != null) { text1Obj.SetActive(true); PlayRevealSFX(); }
        yield return new WaitForSecondsRealtime(delayB);

        if (text2Obj != null) { text2Obj.SetActive(true); PlayRevealSFX(); }
        yield return new WaitForSecondsRealtime(delayB);

        if (text3Obj != null) { text3Obj.SetActive(true); PlayRevealSFX(); }

        yield return new WaitForSecondsRealtime(delayD);

        if (textAObj != null) { textAObj.SetActive(true); PlayValueSFX(); }
        yield return new WaitForSecondsRealtime(delayC);

        if (textBObj != null) { textBObj.SetActive(true); PlayValueSFX(); }
        yield return new WaitForSecondsRealtime(delayC);

        if (textCObj != null) { textCObj.SetActive(true); PlayValueSFX(); }

        yield return new WaitForSecondsRealtime(delayE);

        yield return StartCoroutine(Co_CalculateRewards());

        if (spacePromptObj != null) spacePromptObj.SetActive(true);
        if (spacePromptText != null) StartCoroutine(Co_BlinkPromptText());

        isCalculationDone = true;
    }

    private IEnumerator Co_CalculateRewards()
    {
        while (rewardFromTime > 0)
        {
            rewardFromTime--;
            totalGainedReward++;
            currentOwnedGems++;

            if (SoundManager.instance != null) SoundManager.instance.PlaySFX(sfxCounting, 0.7f, 0.1f);

            UpdateRewardUI();
            yield return new WaitForSecondsRealtime(countDelay);
        }

        yield return new WaitForSecondsRealtime(countDelay);

        while (rewardFromDamage > 0)
        {
            rewardFromDamage--;
            totalGainedReward++;
            currentOwnedGems++;

            if (SoundManager.instance != null) SoundManager.instance.PlaySFX(sfxCounting, 0.7f, 0.1f);

            UpdateRewardUI();
            yield return new WaitForSecondsRealtime(countDelay);
        }
    }

    private void UpdateRewardUI()
    {
        if (timeRewardText != null) timeRewardText.text = $"+{rewardFromTime}";
        if (damageRewardText != null) damageRewardText.text = $"+{rewardFromDamage}";
        if (currentOwnedGemText != null) currentOwnedGemText.text = currentOwnedGems.ToString();
        if (totalGemText != null) totalGemText.text = $"(+{totalGainedReward})";
    }

    private void PlayRevealSFX()
    {
        if (SoundManager.instance != null) SoundManager.instance.PlaySFX(sfxReveal, 0.8f, 0.05f);
    }

    private void PlayValueSFX()
    {
        if (SoundManager.instance != null) SoundManager.instance.PlaySFX(sfxValue, 0.9f, 0.05f);
    }

    private IEnumerator Co_BlinkPromptText()
    {
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = (Mathf.Cos(elapsed * 2.5f + Mathf.PI) + 1f) * 0.5f;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            if (spacePromptText != null) spacePromptText.alpha = Mathf.Lerp(0f, 1f, smoothT);
            yield return null;
        }
    }

    private void GoToTitle()
    {
        isTransitioning = true;

        StartCoroutine(Co_FadeAndLoadScene());
    }

    private IEnumerator Co_FadeAndLoadScene()
    {
        if (fadeOutImage != null)
        {
            fadeOutImage.gameObject.SetActive(true);
            Color startColor = fadeOutImage.color;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                startColor.a = Mathf.Clamp01(elapsed / fadeOutDuration);
                fadeOutImage.color = startColor;
                yield return null;
            }
        }

        if (DataManager.instance != null)
        {
            DataManager.instance.data.totalGems = currentOwnedGems;
            DataManager.instance.SaveGame();
        }

        Time.timeScale = 1f;
        
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.SetBlackScreen(true);
            TransitionManager.Instance.LoadScene("Title", 0f, 1.5f);
        }
        else
        {
            SceneManager.LoadScene("Title");
        }
    }
}