using UnityEngine;

public class Ela : MonoBehaviour
{
    [SerializeField] private int messageIndex = 0; // 출력할 메세지 인덱스
    [SerializeField] private bool triggerOnce = true; // 1회만 출력할지 여부

    private bool hasTriggered = false; // 출력 완료 여부

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnce) return;

        if (other.CompareTag("Player"))
        {
            if (PlayerFeedbackUI.Instance != null)
            {
                PlayerFeedbackUI.Instance.ShowWarning(messageIndex);
                hasTriggered = true;
            }
        }
    }
}