using UnityEngine;

public class GuideArrow : MonoBehaviour
{
    public static GuideArrow Instance { get; private set; } 

    [Header("Settings")]
    public float radius = 0.55f;           
    public float angleOffset = -135f;      
    public float rewardHideDistance = 1.4f; 
    public float statueHideDistance = 2.0f; 

    [Header("Sprites")]
    public Sprite rewardSprite;            
    public Sprite statueSprite;            

    [Header("Runtime")]
    public bool isVisible = false;         

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
        // ★ 수정: 플레이어가 파괴(사망/씬전환)되었을 때 에러 방어 추가
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

        if (hasUncollectedFloorReward || hasPendingModuleReward)
        {
            if (_spriteRenderer)
            {
                _spriteRenderer.sprite = rewardSprite;
                _spriteRenderer.color = Color.white; 
            }
            // ★ 수정: 보상 오브젝트가 파괴되었을 수 있으므로 널 체크
            if (!_rewardTransform) return _statueTransform;
            
            return _rewardTransform;
        }
        else
        {
            if (_spriteRenderer)
            {
                _spriteRenderer.sprite = statueSprite;
                _spriteRenderer.color = Color.white; 
            }
            return _statueTransform; 
        }
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