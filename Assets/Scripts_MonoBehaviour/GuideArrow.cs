using UnityEngine;

public class GuideArrow : MonoBehaviour
{
    public static GuideArrow Instance { get; private set; } // 전역 접근용 싱글톤

    [Header("Settings")]
    public float radius = 0.55f;           // 플레이어 주변을 맴돌 반경
    public float angleOffset = -135f;      // 이미지 영점 보정값
    public float rewardHideDistance = 1.4f; // 보상을 향할 때 화살표 숨김 거리
    public float statueHideDistance = 2.0f; // 석상을 향할 때 화살표 숨김 거리

    [Header("Sprites")]
    public Sprite rewardSprite;            // 보상을 향할 때 스프라이트
    public Sprite statueSprite;            // 석상을 향할 때 스프라이트

    [Header("Runtime")]
    public bool isVisible = false;         // 화살표 표시 여부 플래그

    private SpriteRenderer _spriteRenderer;
    
    private Transform _playerTransform;    // 기준 플레이어 좌표
    private Transform _rewardTransform;    // 보상 오브젝트 좌표
    private IRewardItem _rewardItem;       // 보상 획득 상태 확인용
    private Transform _statueTransform;    // 석상 오브젝트 좌표

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
    }

    public void ActivateArrow(Transform player, Transform reward, IRewardItem rewardItem, Transform statue)
    {
        _playerTransform = player;
        _rewardTransform = reward;
        _rewardItem = rewardItem;
        _statueTransform = statue; // 연결된 석상의 실제 위치가 캐싱됩니다
        
        isVisible = true;
    }

    public void HideArrow()
    {
        isVisible = false;
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
    }

    private void Update()
    {
        if (!isVisible || _playerTransform == null)
        {
            if (_spriteRenderer != null && _spriteRenderer.enabled) _spriteRenderer.enabled = false;
            return;
        }

        Transform currentTarget = GetCurrentTarget();

        if (currentTarget == null)
        {
            if (_spriteRenderer.enabled) _spriteRenderer.enabled = false;
            return;
        }

        float distanceToTarget = Vector2.Distance(_playerTransform.position, currentTarget.position);
        
        // 현재 타겟이 석상인지 확인하여 알맞은 숨김 거리를 적용합니다
        float targetHideDistance = (currentTarget == _statueTransform) ? statueHideDistance : rewardHideDistance;

        if (distanceToTarget <= targetHideDistance)
        {
            if (_spriteRenderer.enabled) _spriteRenderer.enabled = false;
            return;
        }

        if (!_spriteRenderer.enabled) _spriteRenderer.enabled = true;
        
        UpdateArrowTransform(currentTarget);
    }

    private Transform GetCurrentTarget()
    {
        bool hasUncollectedFloorReward = _rewardItem != null && !_rewardItem.IsCollected;
        bool hasPendingModuleReward = StageMessageUI.instance != null && !StageMessageUI.instance.IsRewardQueueEmpty;

        if (hasUncollectedFloorReward || hasPendingModuleReward)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = rewardSprite;
                _spriteRenderer.color = Color.white; 
            }
            return _rewardTransform;
        }
        else
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = statueSprite;
                _spriteRenderer.color = Color.white; 
            }
            return _statueTransform; // 정확한 석상의 좌표를 타겟으로 반환합니다
        }
    }

    private void UpdateArrowTransform(Transform target)
    {
        Vector3 direction = (target.position - _playerTransform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.position = _playerTransform.position + direction * radius;
        transform.rotation = Quaternion.Euler(0, 0, angle + angleOffset);
    }
}