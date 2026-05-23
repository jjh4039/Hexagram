using UnityEngine;

public class GuideArrow : MonoBehaviour
{
    public static GuideArrow Instance { get; private set; } 

    [Header("Settings")]
    public float radius = 0.55f;           // 화살표 회전 반경
    public float angleOffset = -135f;      // 화살표 이미지 기본 각도 보정
    public float rewardHideDistance = 1.4f; // 보상 숨김 거리
    public float statueHideDistance = 2.0f; // 동상 숨김 거리

    [Header("Sprites")]
    public Sprite rewardSprite;            // 보상 방향 이미지
    public Sprite statueSprite;            // 동상 방향 이미지

    [Header("Runtime")]
    public bool isVisible = false;         // 화살표 활성화 상태

    private SpriteRenderer _spriteRenderer;
    
    private Transform _playerTransform;    
    private Transform _rewardTransform;    
    private IRewardItem _rewardItem;       
    private Transform _statueTransform;    

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
        _statueTransform = statue; 
        
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
        bool hasPendingModuleReward = StageMessageUI.instance && !StageMessageUI.instance.IsRewardQueueEmpty;

        // 1. 맵에 안 먹은 보상 오브젝트가 있다면 주황색 화살표 표시
        if (hasUncollectedFloorReward)
        {
            if (_spriteRenderer)
            {
                _spriteRenderer.sprite = rewardSprite;
                _spriteRenderer.color = Color.white; 
            }
            
            if (!_rewardTransform) return _statueTransform;
            
            return _rewardTransform;
        }
        
        // 2. 오브젝트는 사용했지만 아직 UI 강화를 완료하지 않은 경우 화살표 숨김
        if (hasPendingModuleReward)
        {
            return null; 
        }

        // 3. 강화를 포함해 모든 보상을 다 획득/선택한 경우 파란색 화살표 표시
        if (_spriteRenderer)
        {
            _spriteRenderer.sprite = statueSprite;
            _spriteRenderer.color = Color.white; 
        }
        return _statueTransform; 
    }

    private void UpdateArrowTransform(Transform target)
    {
        if (target == null) return;
        Vector3 direction = (target.position - _playerTransform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.position = _playerTransform.position + direction * radius;
        transform.rotation = Quaternion.Euler(0, 0, angle + angleOffset);
    }
}