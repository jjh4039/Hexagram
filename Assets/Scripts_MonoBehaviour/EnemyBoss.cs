using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyBoss : Enemy
{
    [Header("Debug / Testing")]
    [Tooltip("0: 랜덤(1~4), 1~4: 해당 패턴만 무한 반복")]
    [SerializeField][Range(0, 4)] private int forcePatternIndex = 0;
    [Tooltip("체력과 상관없이 강제로 폭주(Phase 2) 패턴을 켭니다.")]
    [SerializeField] private bool forceEnrage = false;

    [Header("Boss Specific Stats")]
    [SerializeField] private string bossName = "숲의 관리자";
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float spriteScale = 1f;

    [Header("Intro Settings")]
    [SerializeField] private float introTriggerRadius = 10f;
    private bool isAwake = false;

    [Header("Boss AI Logic")]
    [Tooltip("몇 번의 공격마다 4번(각성기) 패턴을 사용할지 결정합니다.")]
    [SerializeField] private int crossGridFrequency = 4;
    [Tooltip("플레이어와 겹쳤을 때 빠른 방향 전환을 막는 딜레이 시간")]
    [SerializeField] private float flipCooldown = 0.3f;

    private bool isFirstPattern = true;
    private int patternCounter = 0;
    private int lastPatternIndex = -1;
    private float lastFlipTime = 0f;

    [Header("Phase Settings")]
    [SerializeField] private float enragePauseTime = 2.0f;
    [SerializeField] private float enrageKnockbackForce = 15f;
    private bool isEnraged = false;
    private SpriteRenderer spriteRenderer;

    [Header("Aura Effect")]
    [SerializeField] private ParticleSystem auraParticle;
    [SerializeField] private Color normalAuraColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color enrageAuraColor = new Color(1f, 0.2f, 0.2f, 0.6f);
    [SerializeField] private float normalAuraRate = 25f;
    [SerializeField] private float enrageAuraRate = 40f;

    [Header("Global Attack Settings")]
    [SerializeField] private float dealTime = 0.7f;

    [Header("Pattern 1: Dash")]
    [SerializeField] private float dashChargeTime = 1.0f;
    [SerializeField] private float dashSpeed = 80f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashRecoveryTime = 0.5f;

    [Header("Pattern 1: Dash Indicator")]
    [SerializeField] private GameObject dashMaxRangeOrigin;
    [SerializeField] private GameObject dashCurrentRangeOrigin;
    [SerializeField] private float dashRectWidth = 2f;
    [SerializeField] private float dashMaxLimitLength = 24f;
    [SerializeField] private float dashHomingStrength = 2.0f;
    [SerializeField] private Vector2 dashIndicatorOffset = new Vector2(-1f, 0f);
    [SerializeField] private AnimationCurve dashSpeedCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Header("Pattern 1: Dash Damage & Visuals")]
    [SerializeField] private float dashDamageMultiplier = 1.5f;
    [SerializeField] private int trailPoolSize = 10;
    [SerializeField] private float trailSpawnDelay = 0.04f;
    [SerializeField] private float trailLifeTime = 0.4f;
    [SerializeField] private Color trailColor;
    [SerializeField] private GameObject dashDebrisPrefab;
    [SerializeField] private float debrisSpawnDelay = 0.05f;
    [SerializeField] private Vector2 debrisOffset = new Vector2(0f, 0f);

    [Header("Pattern 2: Point Blank AoE")]
    [SerializeField] private float aoeChargeTime = 2.0f;
    [SerializeField] private float hugeAoeRadius = 9f;
    [SerializeField] private float aoeDamage = 20f;
    [SerializeField] private float aoeRecoveryTime = 1.0f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private GameObject aoeEffectPrefab;

    [Header("Pattern 2: AoE Indicator")]
    [SerializeField] private GameObject attackMaxRangeObj;
    [SerializeField] private GameObject attackRangeObj;
    [SerializeField] private float aoeVisualScale = 4.5f;

    [Header("Pattern 2: Enrage Projectiles (Optional)")]
    [SerializeField] private GameObject aoeProjectilePrefab;
    [SerializeField] private float aoeProjectileSpeed = 10f;
    [SerializeField] private float aoeProjectileDamage = 15f;

    [Header("Pattern 3: Multi-Lines (Earth Spikes)")]
    [SerializeField] private float linesChargeTime = 1.5f;
    [SerializeField] private int minLines = 7;
    [SerializeField] private int maxLines = 10;
    [SerializeField] private float linesRecoveryTime = 0.5f;
    [SerializeField] private float spikeRectWidth = 1.5f;
    [SerializeField] private GameObject earthSpikePrefab;
    [SerializeField] private float spikeDamage = 15f;
    [SerializeField] private float spikeDistance = 1.5f;
    [SerializeField] private float spikeSpawnDelay = 0.05f;
    [SerializeField] private float spikeMaxLimitLength = 30f;

    [Header("Pattern 4: Cross Grid (Vine)")]
    [SerializeField] private float gridStartupDelay = 1.5f;
    [SerializeField] private float gridChargeTime = 0.5f;
    [SerializeField] private float gridTelegraphDuration = 0.8f;
    [SerializeField] private float gridTelegraphGap = 0.2f;
    [SerializeField] private float gridFireDelay = 1f;
    [SerializeField] private float gridRecoveryTime = 2.0f;
    [SerializeField] private GameObject giantVinePrefab;
    [SerializeField] private float vineDamage = 25f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float gridLineWidth = 3f;

    private Vector2 initialSpawnPos;
    [SerializeField] private float[] gridOffsetY = new float[] { 4.5f, 3f, 1.5f, 0f, -1.5f, -3f, -4.5f };
    [SerializeField] private float[] gridOffsetX = new float[] { -6f, -4.5f, -3f, -1.5f, 0f, 1.5f, 3f, 4.5f, 6f, 7.5f };

    [Header("Sound")]
    [SerializeField] private AudioClip bossBgmIntro;
    [SerializeField] private AudioClip bossBgmLoop;
    [SerializeField] private float bossBgmFadeInTime = 1.5f;
    [SerializeField] private AudioClip sfxDashFire;
    [SerializeField] private AudioClip sfxAoeExplode;
    [SerializeField] private AudioClip sfxSpikeWave;
    [SerializeField] private AudioClip sfxVineExplode;
    [SerializeField] private AudioClip sfxEnrageRoar;

    [Header("Death Settings")]
    [SerializeField] private Sprite deadStatueSprite;

    private GameObject sniperMaxInstance;
    private GameObject sniperCurrentInstance;

    private Transform target;
    private Rigidbody2D rigid;
    private bool isAttacking = false;
    private bool isStunned = false;
    private bool isDashing = false;
    private Queue<GameObject> trailPool = new Queue<GameObject>();
    private GameObject trailContainer;

    private Transform telegraphContainer;

    // ★ 수정 1: 이름표(String)를 Key로 사용하여 패턴별로 완전히 다른 방에서 장판을 관리합니다.
    private Dictionary<string, List<GameObject>> telegraphPools = new Dictionary<string, List<GameObject>>();

    private SpriteRenderer shadowSr;
    private Color shadowOriginalColor;

    public bool IsDashing => isDashing;
    public float BaseContactDamage => contactDamage;
    public float DashDamageMultiplier => dashDamageMultiplier;

    protected override void Awake()
    {
        base.Awake();
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        InitializeTrailPool();

        telegraphContainer = new GameObject($"Guardian_Telegraphs").transform;
    }

    protected override void Start()
    {
        base.Start();
        initialSpawnPos = transform.position;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) target = playerObj.transform;
        
        if (GameManager.instance != null && GameManager.instance.stats != null)
        {
            float diffMult = GameManager.instance.stats.enemyStatMultiplier;
            aoeDamage *= diffMult;
            spikeDamage *= diffMult;
            vineDamage *= diffMult;
            aoeProjectileDamage *= diffMult;
            dashDamageMultiplier *= diffMult; 
        }

        ClearAllTelegraphs();

        if (auraParticle != null)
        {
            auraParticle.Stop();
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.gray;

        Transform shadowT = transform.Find("Shadow"); 
        if (shadowT != null)
        {
            shadowSr = shadowT.GetComponent<SpriteRenderer>();
            if (shadowSr != null) shadowOriginalColor = shadowSr.color;
        }
    }

    // ★ 수정 2: 'poolKey'를 통해 같은 프리팹이라도 패턴별로 독립적인 풀링을 보장합니다.
    private GameObject GetTelegraph(string poolKey, GameObject prefabTemplate, Vector3 pos)
    {
        if (prefabTemplate == null) return null;

        if (!telegraphPools.ContainsKey(poolKey))
        {
            telegraphPools[poolKey] = new List<GameObject>();
        }

        foreach (var obj in telegraphPools[poolKey])
        {
            if (!obj.activeSelf)
            {
                obj.transform.position = pos;
                obj.transform.rotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;
                
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null) 
                {
                    // 이전 패턴에서 바뀐 색상, 투명도, 크기 찌꺼기를 완벽히 날려버립니다.
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
                }
                
                obj.SetActive(true);
                return obj;
            }
        }

        GameObject newObj = Instantiate(prefabTemplate, pos, Quaternion.identity, telegraphContainer);
        telegraphPools[poolKey].Add(newObj);
        return newObj;
    }

    // ★ 활성화된 모든 장판 끄기
    void ClearAllTelegraphs()
    {
        if (attackMaxRangeObj != null) attackMaxRangeObj.SetActive(false);
        if (attackRangeObj != null) attackRangeObj.SetActive(false);

        foreach (var pool in telegraphPools.Values)
        {
            foreach (var obj in pool)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }

    // ★ 핵심 수정 3: 물리 레이캐스트 원점과 시각적 장판 위치를 분리하여 벽 관통을 완벽 차단하는 통일 함수
    private void DrawTelegraph(GameObject rect, Vector3 rayOrigin, Vector3 drawPos, Vector2 dir, float maxLength, float progress, float width)
    {
        if (rect == null) return;

        rect.transform.position = drawPos;
        rect.transform.right = dir.normalized;

        float finalLength = maxLength;
        
        // 물리 레이캐스트는 무조건 장애물 검사 원점(rayOrigin)에서 발사합니다.
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, dir.normalized, maxLength, wallLayer);
        if (hit.collider != null)
        {
            // 벽에 부딪혔다면: 그림 그리기 시작점(drawPos)에서부터 벽(hit.point)까지의 거리를 수학적으로 도출
            float distFromDraw = Vector2.Dot(hit.point - (Vector2)drawPos, dir.normalized);
            finalLength = Mathf.Max(0, distFromDraw - 0.5f); // 약간의 패딩
        }
        else
        {
            // 허공이라면: 레이캐스트 최대 도달점까지의 거리를 계산
            Vector2 maxPoint = (Vector2)rayOrigin + dir.normalized * maxLength;
            float distFromDraw = Vector2.Dot(maxPoint - (Vector2)drawPos, dir.normalized);
            finalLength = Mathf.Max(0, distFromDraw);
        }

        SpriteRenderer sr = rect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // MaxRect(진행률 1f)이든 CurRect(차오르는 바)이든 벽을 절대 넘어갈 수 없게 됩니다.
            sr.size = new Vector2(finalLength * progress, width);
        }
    }

    private void StartIntroSequence()
    {
        isAwake = true;

        if (bossBgmLoop != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlayBGM(bossBgmLoop, bossBgmIntro, bossBgmFadeInTime);
        }

        if (CinematicManager.Instance != null)
        {
            StartCoroutine(CinematicManager.Instance.Co_PlayBossIntro(
                this.transform,
                onSunsetStart: () =>
                {
                    StartCoroutine(Co_WakeUpColorLerp(CinematicManager.Instance.SunsetDuration));
                },
                onSunsetDone: () =>
                {
                    if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(1.7f, 0.4f, 1f);
                    if (sfxSpikeWave != null) SoundManager.instance.PlaySFX(sfxSpikeWave, 1.2f);
                    if (Anim != null) Anim.SetTrigger("Start");
                },
                onFinish: () =>
                {
                    StartCoroutine(Co_PostCutsceneSetup());
                }
            ));
        }
        else
        {
            StartCoroutine(Co_PostCutsceneSetup());
        }
    }

    private IEnumerator Co_WakeUpColorLerp(float duration)
    {
        if (spriteRenderer == null) yield break;

        float elapsed = 0f;
        Color startColor = Color.gray;
        Color endColor = Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(startColor, endColor, elapsed / duration);

            if (shadowSr != null) shadowSr.color = shadowOriginalColor;

            yield return null;
        }
        spriteRenderer.color = endColor;
        if (shadowSr != null) shadowSr.color = shadowOriginalColor;
    }

    private IEnumerator Co_PostCutsceneSetup()
    {
        if (GameManager.instance != null)
        {
            float healthMultiplier = GameManager.instance.eventBossHealthMultiplier;
            if (healthMultiplier != 1.0f)
            {
                maxHealth = maxHealth * healthMultiplier;
                currentHealth = maxHealth;
            }
        }

        if (BossHealthUI.Instance != null)
        {
            BossHealthUI.Instance.SetupBoss(bossName, maxHealth);
        }

        if (forceEnrage)
        {
            currentHealth = maxHealth * 0.5f;

            yield return new WaitForSeconds(0.5f);
            if (BossHealthUI.Instance != null) BossHealthUI.Instance.UpdateBossHealth(currentHealth);
        }

        if (auraParticle != null)
        {
            var main = auraParticle.main;
            main.startColor = normalAuraColor;

            var emission = auraParticle.emission;
            emission.rateOverTime = normalAuraRate;

            auraParticle.Play();
        }

        StartCoroutine(Co_BossAI());
    }

    private void Update()
    {
        if (isDead || target == null) return;

        if (!isAwake)
        {
            if (Vector2.Distance(transform.position, target.position) <= introTriggerRadius)
            {
                StartIntroSequence();
            }
            return;
        }

        if (!isAttacking)
            LookAtTarget();

        if (!forceEnrage && isEnraged && currentHealth > maxHealth * 0.5f)
        {
            isEnraged = false;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }
    }

    public override void TakeDamage(float damage, bool isCritical = false)
    {
        if (!isAwake || isDead) return;

        base.TakeDamage(damage, isCritical);
        if (BossHealthUI.Instance != null) BossHealthUI.Instance.UpdateBossHealth(currentHealth);
        if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(0.05f, 0.04f);
    }

    private IEnumerator Co_EnragePattern()
    {
        isEnraged = true;
        isAttacking = true;
        rigid.linearVelocity = Vector2.zero;

        if (Anim != null) Anim.SetTrigger("Enrage");
        if (sfxEnrageRoar != null) SoundManager.instance.PlaySFX(sfxEnrageRoar, 1.2f);
        if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(enragePauseTime, 0.5f, 1f);

        KnockbackPlayer();

        float elapsed = 0f;
        Color startColor = Color.white;
        Color enrageColor = new Color(1f, 0.4f, 0.4f);

        while (elapsed < enragePauseTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / enragePauseTime;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(startColor, enrageColor, t);
            }

            if (auraParticle != null)
            {
                var main = auraParticle.main;
                main.startColor = Color.Lerp(normalAuraColor, enrageAuraColor, t);
                var emission = auraParticle.emission;
                emission.rateOverTime = Mathf.Lerp(normalAuraRate, enrageAuraRate, t);
            }

            yield return null;
        }

        if (spriteRenderer != null) spriteRenderer.color = enrageColor;
        if (auraParticle != null)
        {
            var main = auraParticle.main;
            main.startColor = enrageAuraColor;
            var emission = auraParticle.emission;
            emission.rateOverTime = enrageAuraRate;
        }

        isAttacking = false;
    }

    private void KnockbackPlayer()
    {
        if (GameManager.instance == null || GameManager.instance.player == null) return;

        Player playerScript = GameManager.instance.player;

        Vector2 knockbackDir = (playerScript.transform.position - transform.position);
        if (knockbackDir == Vector2.zero) knockbackDir = Vector2.down;

        playerScript.ApplyKnockback(knockbackDir.normalized, enrageKnockbackForce * 2f, 0.35f);
    }

    IEnumerator Co_BossAI()
    {
        yield return new WaitForSeconds(0.5f);

        while (!isDead)
        {
            if (isStunned) { yield return null; continue; }

            int patternIndex = 1;

            if (forcePatternIndex != 0)
            {
                patternIndex = forcePatternIndex;
            }
            else
            {
                patternCounter++;

                if (isFirstPattern)
                {
                    patternIndex = 1; 
                    isFirstPattern = false;
                }
                else if (patternCounter % crossGridFrequency == 0)
                {
                    patternIndex = 4; 
                }
                else
                {
                    do
                    {
                        patternIndex = Random.Range(1, 4);
                    }
                    while (patternIndex == lastPatternIndex);
                }
            }

            lastPatternIndex = patternIndex;

            yield return StartCoroutine(ExecutePattern(patternIndex));

            yield return new WaitForSeconds(dealTime);

            if (!isEnraged && currentHealth <= maxHealth * 0.5f)
            {
                yield return StartCoroutine(Co_EnragePattern());
                yield return new WaitForSeconds(dealTime);
                continue;
            }

            yield return StartCoroutine(Co_MoveTowardsPlayer(1.4f));
        }
    }

    IEnumerator Co_MoveTowardsPlayer(float moveDuration)
    {
        float timer = 0f;
        if (Anim != null) Anim.SetBool("isMoving", true);
        while (timer < moveDuration && !isDead && !isAttacking)
        {
            Vector2 dir = (target.position - transform.position).normalized;
            rigid.linearVelocity = dir * moveSpeed;
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rigid.linearVelocity = Vector2.zero;
        if (Anim != null) Anim.SetBool("isMoving", false);
    }

    IEnumerator ExecutePattern(int patternIndex)
    {
        isAttacking = true;
        rigid.linearVelocity = Vector2.zero;
        if (Anim != null) Anim.SetBool("isMoving", false);

        switch (patternIndex)
        {
            case 1: yield return StartCoroutine(Co_Pattern1_Dash()); break;
            case 2: yield return StartCoroutine(Co_Pattern2_AoE()); break;
            case 3: yield return StartCoroutine(Co_Pattern3_MultiLines()); break;
            case 4: yield return StartCoroutine(Co_Pattern4_CrossGrid()); break;
        }

        isAttacking = false;
    }

    private void LookAtTarget()
    {
        if (target == null) return;

        float dirX = target.position.x - transform.position.x;

        if (Mathf.Abs(dirX) < 0.1f) return;

        float targetSign = Mathf.Sign(dirX);
        float currentSign = Mathf.Sign(transform.localScale.x);

        if (targetSign != currentSign)
        {
            if (Time.time >= lastFlipTime + flipCooldown)
            {
                LookAtDirection(dirX);
                lastFlipTime = Time.time;
            }
        }
    }

    private void LookAtDirection(float dirX)
    {
        if (dirX > 0)
            transform.localScale = new Vector3(spriteScale, spriteScale, 1);
        else
            transform.localScale = new Vector3(-spriteScale, spriteScale, 1);
    }

    IEnumerator Co_Pattern1_Dash()
    {
        int dashCount = (isEnraged || forceEnrage) ? 2 : 1;

        for (int i = 0; i < dashCount; i++)
        {
            if (Anim != null) Anim.SetTrigger("ReadyDash");

            float currentChargeTime = (i == 0) ? dashChargeTime : dashChargeTime * 0.5f;
            float currentDashDuration = (i == 0) ? dashDuration : dashDuration * 0.5f;
            float currentLimitLength = (i == 0) ? dashMaxLimitLength : dashMaxLimitLength * 0.5f;

            Vector2 currentDir = (target.position - transform.position).normalized;

            // ★ 고유 Key 할당: "DashMax", "DashCur"
            GameObject maxRectInstance = GetTelegraph("DashMax", dashMaxRangeOrigin, transform.position);
            GameObject currentRectInstance = GetTelegraph("DashCur", dashCurrentRangeOrigin, transform.position);

            float facingDirection = Mathf.Sign(transform.localScale.x);
            Vector2 visualOffset = new Vector2(dashIndicatorOffset.x * facingDirection, dashIndicatorOffset.y);

            float timer = 0f;
            while (timer < currentChargeTime && !isDead)
            {
                timer += Time.deltaTime;
                Vector2 targetDir = (target.position - transform.position).normalized;
                currentDir = Vector2.Lerp(currentDir, targetDir, Time.deltaTime * dashHomingStrength);

                Vector3 drawPos = transform.position + (Vector3)visualOffset;
                float progress = timer / currentChargeTime;

                // ★ 통합된 DrawTelegraph 사용
                DrawTelegraph(maxRectInstance, transform.position, drawPos, currentDir, currentLimitLength, 1f, dashRectWidth);
                DrawTelegraph(currentRectInstance, transform.position, drawPos, currentDir, currentLimitLength, progress, dashRectWidth);
                
                LookAtDirection(currentDir.x);
                yield return null;
            }

            ClearAllTelegraphs();

            if (Anim != null) Anim.SetTrigger("Dash");
            if (sfxDashFire != null) SoundManager.instance.PlaySFX(sfxDashFire, 1f, 0.1f);

            isDashing = true;
            Coroutine trailCoroutine = StartCoroutine(Co_SpawnTrail());
            Coroutine debrisCoroutine = StartCoroutine(Co_SpawnDebris());

            Vector2 startDashPos = transform.position;
            RaycastHit2D hit = Physics2D.Raycast(startDashPos, currentDir, currentLimitLength, wallLayer);
            float safeDistance = hit.collider != null ? Mathf.Max(0, hit.distance - 1.5f) : currentLimitLength;

            timer = 0f;
            while (timer < currentDashDuration && !isDead)
            {
                if (Vector2.Distance(startDashPos, transform.position) >= safeDistance)
                {
                    break;
                }

                float progress = timer / currentDashDuration;
                float speedMultiplier = dashSpeedCurve.Evaluate(progress);

                rigid.linearVelocity = currentDir * (dashSpeed * speedMultiplier);

                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            isDashing = false;
            if (trailCoroutine != null) StopCoroutine(trailCoroutine);
            if (debrisCoroutine != null) StopCoroutine(debrisCoroutine);

            rigid.linearVelocity = Vector2.zero;
            if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(0.1f, 0.08f);

            if (i == 0 && dashCount > 1)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }

        yield return new WaitForSeconds(dashRecoveryTime);
    }

    IEnumerator Co_Pattern2_AoE()
    {
        if (Anim != null) Anim.SetTrigger("ReadySlam");

        if (attackMaxRangeObj != null)
        {
            attackMaxRangeObj.SetActive(true);
            attackMaxRangeObj.transform.localPosition = Vector3.zero;
            attackMaxRangeObj.transform.localScale = new Vector3(aoeVisualScale, aoeVisualScale, 1f);
        }

        if (attackRangeObj != null)
        {
            attackRangeObj.SetActive(true);
            attackRangeObj.transform.localPosition = Vector3.zero;
            attackRangeObj.transform.localScale = Vector3.zero;
        }

        float timer = 0f;
        while (timer < aoeChargeTime && !isDead)
        {
            timer += Time.deltaTime;
            float progress = timer / aoeChargeTime;

            if (attackRangeObj != null)
            {
                float currentScale = Mathf.Lerp(0f, aoeVisualScale, progress);
                attackRangeObj.transform.localScale = new Vector3(currentScale, currentScale, 1f);
            }
            yield return null;
        }

        ClearAllTelegraphs();

        if (isDead) yield break;

        if (Anim != null) Anim.SetTrigger("Slam");
        if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(0.35f, 0.45f);

        if (sfxAoeExplode != null) SoundManager.instance.PlaySFX(sfxAoeExplode, 0.9f);

        if (aoeEffectPrefab != null)
        {
            GameObject vfx = Instantiate(aoeEffectPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        if (isEnraged || forceEnrage)
        {
            FireProjectiles(32);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hugeAoeRadius, targetLayer);
        foreach (Collider2D hit in hits)
        {
            Player player = hit.GetComponent<Player>();
            if (player != null)
            {
                player.OnDamage(aoeDamage);
            }
        }

        yield return new WaitForSeconds(aoeRecoveryTime);
    }

    IEnumerator Co_Pattern3_MultiLines()
    {
        if (Anim != null) Anim.SetTrigger("RaiseHand");

        int lineCount = Random.Range(minLines, maxLines + 1);

        List<GameObject> maxRects = new List<GameObject>();
        List<GameObject> currentRects = new List<GameObject>();

        List<Vector2> lineDirections = new List<Vector2>();
        List<Vector2> wallHitPoints = new List<Vector2>();

        for (int i = 0; i < lineCount; i++)
        {
            Vector2 dir;
            Transform currentTarget = GameManager.instance?.player?.transform;

            if (i == 0 && currentTarget != null)
                dir = (currentTarget.position - transform.position).normalized;
            else
            {
                float randomAngle = Random.Range(0f, 360f);
                dir = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
            }
            lineDirections.Add(dir);

            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, spikeMaxLimitLength, wallLayer);
            float finalLength = hit.collider != null ? Mathf.Max(0, hit.distance - 1f) : spikeMaxLimitLength;
            wallHitPoints.Add((Vector2)transform.position + (dir * finalLength));

            // ★ 고유 Key 할당: "SpikeMax", "SpikeCur"
            if (dashMaxRangeOrigin != null)
                maxRects.Add(GetTelegraph("SpikeMax", dashMaxRangeOrigin, transform.position));
            if (dashCurrentRangeOrigin != null)
                currentRects.Add(GetTelegraph("SpikeCur", dashCurrentRangeOrigin, transform.position));
        }

        float timer = 0f;
        while (timer < linesChargeTime && !isDead)
        {
            timer += Time.deltaTime;
            float progress = timer / linesChargeTime;

            for (int i = 0; i < lineDirections.Count; i++)
            {
                if (i < maxRects.Count) 
                    DrawTelegraph(maxRects[i], transform.position, transform.position, lineDirections[i], spikeMaxLimitLength, 1f, spikeRectWidth);
                if (i < currentRects.Count) 
                    DrawTelegraph(currentRects[i], transform.position, transform.position, lineDirections[i], spikeMaxLimitLength, progress, spikeRectWidth);
            }
            yield return null;
        }

        ClearAllTelegraphs();

        if (isDead) yield break;
        if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(0.2f, 0.15f);

        if (sfxSpikeWave != null) SoundManager.instance.PlaySFX(sfxSpikeWave, 1.2f);
        foreach (Vector2 dir in lineDirections)
        {
            StartCoroutine(Co_SpawnSpikeWave(transform.position, dir, spikeMaxLimitLength));
        }

        if (isEnraged || forceEnrage)
        {
            float maxSpikeCount = spikeMaxLimitLength / spikeDistance;
            float waveDuration = maxSpikeCount * spikeSpawnDelay;
            yield return new WaitForSeconds(waveDuration + 0.1f);

            Transform targetPlayer = GameManager.instance?.player?.transform;
            if (targetPlayer != null)
            {
                maxRects.Clear();
                currentRects.Clear();
                List<Vector2> reverseDirections = new List<Vector2>();

                for (int i = 0; i < wallHitPoints.Count; i++)
                {
                    Vector2 startPos = wallHitPoints[i];
                    Vector2 toPlayerDir = ((Vector2)targetPlayer.position - startPos).normalized;
                    reverseDirections.Add(toPlayerDir);

                    // ★ 고유 Key 할당: 돌아오는 가시 장판 전용 풀
                    if (dashMaxRangeOrigin != null)
                        maxRects.Add(GetTelegraph("SpikeReturnMax", dashMaxRangeOrigin, startPos));
                    if (dashCurrentRangeOrigin != null)
                        currentRects.Add(GetTelegraph("SpikeReturnCur", dashCurrentRangeOrigin, startPos));
                }

                float returnChargeTime = linesChargeTime * 0.6f;
                timer = 0f;
                while (timer < returnChargeTime && !isDead)
                {
                    timer += Time.deltaTime;
                    float progress = timer / returnChargeTime;

                    for (int i = 0; i < reverseDirections.Count; i++)
                    {
                        Vector2 startPos = wallHitPoints[i];
                        if (i < maxRects.Count) 
                            DrawTelegraph(maxRects[i], startPos, startPos, reverseDirections[i], spikeMaxLimitLength, 1f, spikeRectWidth);
                        if (i < currentRects.Count) 
                            DrawTelegraph(currentRects[i], startPos, startPos, reverseDirections[i], spikeMaxLimitLength, progress, spikeRectWidth);
                    }
                    yield return null;
                }

                ClearAllTelegraphs();

                if (isDead) yield break;
                if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(0.25f, 0.2f);

                if (sfxSpikeWave != null) SoundManager.instance.PlaySFX(sfxSpikeWave, 1.2f);
                for (int i = 0; i < wallHitPoints.Count; i++)
                {
                    StartCoroutine(Co_SpawnSpikeWave(wallHitPoints[i], reverseDirections[i], spikeMaxLimitLength));
                }
            }
        }

        yield return new WaitForSeconds(linesRecoveryTime);
    }

    IEnumerator Co_Pattern4_CrossGrid()
    {
        if (Anim != null) Anim.SetTrigger("GatherHands");
        yield return new WaitForSeconds(gridStartupDelay);

        List<int> hSet1 = new List<int>(); List<int> hSet2 = new List<int>();
        for (int i = 0; i < gridOffsetY.Length; i++) { if (i % 2 == 0) hSet1.Add(i); else hSet2.Add(i); }

        List<int> vSet1 = new List<int>(); List<int> vSet2 = new List<int>();
        for (int i = 0; i < gridOffsetX.Length; i++) { if (i % 2 == 0) vSet1.Add(i); else vSet2.Add(i); }

        yield return StartCoroutine(Co_FlashTelegraph(hSet1, null));
        yield return StartCoroutine(Co_FlashTelegraph(null, vSet1));
        yield return StartCoroutine(Co_FlashTelegraph(hSet2, vSet2));

        Vector2 lockedSniperDir = Vector2.zero;
        Vector2 lockedSniperStartPos = Vector2.zero;
        float lockedSniperLength = 0f;
        bool isSniperTracking = false;

        if (isEnraged || forceEnrage)
        {
            // ★ 고유 Key 할당: 폭주 저격 전용 풀
            if (dashMaxRangeOrigin != null)
                sniperMaxInstance = GetTelegraph("SniperMax", dashMaxRangeOrigin, transform.position);
            
            if (dashCurrentRangeOrigin != null)
                sniperCurrentInstance = GetTelegraph("SniperCur", dashCurrentRangeOrigin, transform.position);
            
            isSniperTracking = true;

            float totalSniperChargeTime = gridChargeTime + (gridFireDelay * 2f);

            StartCoroutine(Co_SniperTrackingRoutine(
                (startPos, dir, len) => { lockedSniperStartPos = startPos; lockedSniperDir = dir; lockedSniperLength = len; },
                () => isSniperTracking,
                totalSniperChargeTime
            ));
        }

        yield return new WaitForSeconds(gridChargeTime);

        if (sfxVineExplode != null) SoundManager.instance.PlaySFX(sfxVineExplode, 1f, 0.1f);
        FireVineSet(hSet1, null);
        if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(0.2f, 0.2f);
        yield return new WaitForSeconds(gridFireDelay);

        if (sfxVineExplode != null) SoundManager.instance.PlaySFX(sfxVineExplode, 1f, 0.1f);
        FireVineSet(null, vSet1);
        if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(0.2f, 0.2f);
        yield return new WaitForSeconds(gridFireDelay);

        if (isEnraged || forceEnrage)
        {
            isSniperTracking = false;
            if (sniperMaxInstance != null) sniperMaxInstance.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.5f);
            if (sniperCurrentInstance != null) sniperCurrentInstance.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.9f);
        }

        if (sfxVineExplode != null) SoundManager.instance.PlaySFX(sfxVineExplode, 1.2f, 0.1f);
        FireVineSet(hSet2, vSet2);
        if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(0.35f, 0.35f);

        if (isEnraged || forceEnrage)
        {
            yield return new WaitForSeconds(1f);

            if (giantVinePrefab != null && lockedSniperDir != Vector2.zero)
            {
                if (sfxVineExplode != null) SoundManager.instance.PlaySFX(sfxVineExplode, 1.2f, 0.05f);

                float angle = Mathf.Atan2(lockedSniperDir.y, lockedSniperDir.x) * Mathf.Rad2Deg - 90f;

                GameObject sniperVine = Instantiate(giantVinePrefab, lockedSniperStartPos, Quaternion.Euler(0, 0, angle));
                sniperVine.transform.localScale = new Vector3(2, 2, 1);

                GiantVine vineScript = sniperVine.GetComponent<GiantVine>();
                if (vineScript != null)
                {
                    vineScript.Fire(vineDamage * 1.5f, lockedSniperLength);
                }
                if (CameraFollow.Instance != null) CameraFollow.Instance.HitShake(0.5f, 0.4f);
            }

            if (sniperMaxInstance != null) sniperMaxInstance.SetActive(false);
            if (sniperCurrentInstance != null) sniperCurrentInstance.SetActive(false);
        }

        if (Anim != null) Anim.SetTrigger("StopGatherHands");
        yield return new WaitForSeconds(gridRecoveryTime);
    }

    private (Vector2 startPos, float length) GetLineData(bool isHorizontal, int index)
    {
        Vector2 origin = isHorizontal ?
            new Vector2(initialSpawnPos.x, initialSpawnPos.y + gridOffsetY[index]) :
            new Vector2(initialSpawnPos.x + gridOffsetX[index], initialSpawnPos.y);

        Vector2 dir1 = isHorizontal ? Vector2.left : Vector2.down;
        Vector2 dir2 = isHorizontal ? Vector2.right : Vector2.up;

        RaycastHit2D hit1 = Physics2D.Raycast(origin, dir1, 100f, wallLayer);
        RaycastHit2D hit2 = Physics2D.Raycast(origin, dir2, 100f, wallLayer);

        float dist1 = hit1.collider != null ? hit1.distance : 50f;
        float dist2 = hit2.collider != null ? hit2.distance : 50f;

        Vector2 startPos = origin + (dir1 * dist1);
        float totalLength = dist1 + dist2;

        return (startPos, totalLength);
    }

    IEnumerator Co_FlashTelegraph(List<int> hIndices, List<int> vIndices)
    {
        List<SpriteRenderer> srs = new List<SpriteRenderer>();
        List<GameObject> markers = new List<GameObject>();

        if (hIndices != null)
        {
            foreach (int index in hIndices)
            {
                if (dashCurrentRangeOrigin == null) continue;
                var data = GetLineData(true, index);

                // ★ 고유 Key 할당: 가로세로 십자 장판 전용 풀
                GameObject telegraph = GetTelegraph("GridFlash", dashCurrentRangeOrigin, data.startPos);

                SpriteRenderer sr = telegraph.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    telegraph.transform.rotation = Quaternion.Euler(0, 0, 0);
                    sr.size = new Vector2(data.length, gridLineWidth);
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
                    srs.Add(sr);
                }
                markers.Add(telegraph);
            }
        }

        if (vIndices != null)
        {
            foreach (int index in vIndices)
            {
                if (dashCurrentRangeOrigin == null) continue;
                var data = GetLineData(false, index);

                GameObject telegraph = GetTelegraph("GridFlash", dashCurrentRangeOrigin, data.startPos);

                SpriteRenderer sr = telegraph.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    telegraph.transform.rotation = Quaternion.Euler(0, 0, 90);
                    sr.size = new Vector2(data.length, gridLineWidth);
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
                    srs.Add(sr);
                }
                markers.Add(telegraph);
            }
        }

        float halfTime = gridTelegraphDuration / 2f;
        float maxAlpha = 0.7f;

        for (float t = 0; t < halfTime; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(0f, maxAlpha, t / halfTime);
            foreach (var sr in srs) if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            yield return null;
        }

        for (float t = 0; t < halfTime; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(maxAlpha, 0f, t / halfTime);
            foreach (var sr in srs) if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            yield return null;
        }

        foreach (var m in markers) if (m != null) m.SetActive(false);
        yield return new WaitForSeconds(gridTelegraphGap);
    }

    private void FireVineSet(List<int> hIndices, List<int> vIndices)
    {
        if (giantVinePrefab == null) return;

        if (hIndices != null)
        {
            foreach (int index in hIndices)
            {
                var data = GetLineData(true, index);
                GameObject vine = Instantiate(giantVinePrefab, data.startPos, Quaternion.Euler(0, 0, -90));

                vine.transform.localScale = new Vector3(2, 2, 1);

                GiantVine vineScript = vine.GetComponent<GiantVine>();
                if (vineScript != null)
                    vineScript.Fire(vineDamage, data.length);
            }
        }

        if (vIndices != null)
        {
            foreach (int index in vIndices)
            {
                var data = GetLineData(false, index);
                GameObject vine = Instantiate(giantVinePrefab, data.startPos, Quaternion.Euler(0, 0, 0));

                vine.transform.localScale = new Vector3(2, 2, 1);

                GiantVine vineScript = vine.GetComponent<GiantVine>();
                if (vineScript != null)
                    vineScript.Fire(vineDamage, data.length);
            }
        }
    }

    private void InitializeTrailPool()
    {
        trailContainer = new GameObject($"Guardian_TrailPool");

        for (int i = 0; i < trailPoolSize; i++)
        {
            GameObject trailObj = new GameObject($"BossTrail_{i}");
            trailObj.transform.SetParent(trailContainer.transform);
            trailObj.AddComponent<SpriteRenderer>();
            trailObj.SetActive(false);
            trailPool.Enqueue(trailObj);
        }
    }

    IEnumerator Co_SpawnTrail()
    {
        while (isDashing && !isDead)
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null && trailPool.Count > 0)
            {
                GameObject trailObj = trailPool.Dequeue();

                trailObj.transform.position = transform.position;
                trailObj.transform.localScale = transform.localScale;
                trailObj.transform.rotation = transform.rotation;

                SpriteRenderer trailSr = trailObj.GetComponent<SpriteRenderer>();
                trailSr.sprite = spriteRenderer.sprite;
                trailSr.color = trailColor;

                trailSr.sortingOrder = spriteRenderer.sortingOrder - 1;

                trailObj.SetActive(true);
                StartCoroutine(Co_FadeOutTrail(trailObj, trailSr));
            }
            yield return new WaitForSeconds(trailSpawnDelay);
        }
    }

    IEnumerator Co_FadeOutTrail(GameObject trailObj, SpriteRenderer trailSr)
    {
        float timer = 0f;
        Color startColor = trailSr.color;

        while (timer < trailLifeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, timer / trailLifeTime);
            trailSr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        trailObj.SetActive(false);
        trailPool.Enqueue(trailObj);
    }

    IEnumerator Co_SpawnDebris()
    {
        while (isDashing && !isDead)
        {
            if (dashDebrisPrefab != null)
            {
                Vector2 spawnPos = (Vector2)transform.position + debrisOffset;
                GameObject debris = Instantiate(dashDebrisPrefab, spawnPos, Quaternion.identity);
                Destroy(debris, 1f);
            }
            yield return new WaitForSeconds(debrisSpawnDelay);
        }
    }

    private void FireProjectiles(int count)
    {
        if (aoeProjectilePrefab == null) return;

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            
            EnemyProjectile projectileScript = EnemyProjectile.Spawn(aoeProjectilePrefab, transform.position, Quaternion.identity);

            if (projectileScript != null)
            {
                projectileScript.Initialize(dir, aoeProjectileSpeed, aoeProjectileDamage); 
            }
        }
    }

    IEnumerator Co_SpawnSpikeWave(Vector2 origin, Vector2 dir, float maxLength)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxLength, wallLayer);
        float availableLength = hit.collider != null ? Mathf.Max(0, hit.distance - 1f) : maxLength;
        int spikeCount = Mathf.FloorToInt(availableLength / spikeDistance);

        for (int i = 1; i <= spikeCount; i++)
        {
            if (isDead) yield break;

            Vector2 spawnPos = origin + dir * (i * spikeDistance);

            if (earthSpikePrefab != null)
            {
                GameObject spikeObj = Instantiate(earthSpikePrefab, spawnPos, Quaternion.identity);
                EarthSpike spikeScript = spikeObj.GetComponent<EarthSpike>();
                if (spikeScript != null) spikeScript.Initialize(spikeDamage);
            }

            yield return new WaitForSeconds(spikeSpawnDelay);
        }
    }

    IEnumerator Co_SniperTrackingRoutine(System.Action<Vector2, Vector2, float> onUpdateData, System.Func<bool> isTracking, float chargeDuration)
    {
        float timer = 0f;

        Vector2 currentDir = Vector2.right;
        if (GameManager.instance?.player != null)
        {
            currentDir = (GameManager.instance.player.transform.position - transform.position).normalized;
        }

        while (isTracking() && !isDead)
        {
            if (GameManager.instance?.player != null)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / chargeDuration);

                Vector2 bossPos = transform.position;
                Vector2 targetPos = GameManager.instance.player.transform.position;

                Vector2 targetDir = (targetPos - bossPos).normalized;
                currentDir = Vector2.Lerp(currentDir, targetDir, Time.deltaTime * 5f).normalized;

                RaycastHit2D backHit = Physics2D.Raycast(bossPos, -currentDir, 100f, wallLayer);
                RaycastHit2D frontHit = Physics2D.Raycast(bossPos, currentDir, 100f, wallLayer);

                float backDist = backHit.collider != null ? backHit.distance : 50f;
                float frontDist = frontHit.collider != null ? frontHit.distance : 50f;

                Vector2 startPos = bossPos - (currentDir * (backDist - 0.1f));
                float totalLength = (backDist - 0.1f) + (frontDist - 0.5f);

                onUpdateData(startPos, currentDir, totalLength);

                if (sniperMaxInstance != null)
                {
                    sniperMaxInstance.transform.position = startPos;
                    sniperMaxInstance.transform.right = currentDir;
                    sniperMaxInstance.GetComponent<SpriteRenderer>().size = new Vector2(totalLength, gridLineWidth);
                }

                if (sniperCurrentInstance != null)
                {
                    sniperCurrentInstance.transform.position = startPos;
                    sniperCurrentInstance.transform.right = currentDir;
                    sniperCurrentInstance.GetComponent<SpriteRenderer>().size = new Vector2(totalLength * progress, gridLineWidth);
                }
            }
            yield return null;
        }
    }

    protected override void OnHit() { if (isDead) return; }

    protected override void Die()
    {
        isDead = true;

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.isInvincible = true;
        }

        StopAllCoroutines();
        ClearAllTelegraphs();

        if (auraParticle != null) auraParticle.Stop();

        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.simulated = false;
        }

        if (BossHealthUI.Instance != null)
            BossHealthUI.Instance.HideUI();

        if (spriteRenderer != null && OriginalMaterial != null)
        {
            spriteRenderer.material = OriginalMaterial;
            spriteRenderer.color = Color.white;
        }

        if (Anim != null) Anim.enabled = false;

        if (CinematicManager.Instance != null)
        {
            CinematicManager.Instance.PlayBossDeathCinematic(this);
        }
    }

    private void OnDestroy()
    {
        if (trailContainer != null) Destroy(trailContainer.gameObject);
        if (telegraphContainer != null) Destroy(telegraphContainer.gameObject);
    }

    public void TurnIntoStatue()
    {
        if (spriteRenderer != null)
        {
            if (deadStatueSprite != null)
            {
                spriteRenderer.sprite = deadStatueSprite;
            }
            spriteRenderer.color = Color.gray;
        }
    }
}