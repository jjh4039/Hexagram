using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Queue를 사용하기 위해 추가

public class EnemyBoss : Enemy
{
    [Header("Debug / Testing")]
    [Tooltip("0: 랜덤(1~4), 1~4: 해당 패턴만 무한 반복")]
    [SerializeField][Range(0, 4)] private int forcePatternIndex = 1;
    [Tooltip("체력과 상관없이 강제로 폭주(Phase 2) 패턴을 켭니다.")]
    [SerializeField] private bool forceEnrage = false;

    [Header("Boss Specific Stats")]
    [SerializeField] private string bossName = "숲의 관리자";
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float spriteScale = 1f;

    [Header("Phase Settings")]
    [SerializeField] private float enragePauseTime = 2.0f;
    [SerializeField] private float enrageKnockbackForce = 30f;
    private bool isEnraged = false;
    private SpriteRenderer spriteRenderer;

    [Header("Aura Effect")]
    [SerializeField] private ParticleSystem auraParticle;
    [SerializeField] private Color normalAuraColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color enrageAuraColor = new Color(1f, 0.2f, 0.2f, 0.6f);
    [SerializeField] private float normalAuraRate = 10f;
    [SerializeField] private float enrageAuraRate = 30f;

    [Header("Global Attack Settings")]
    [SerializeField] private float dealTime = 1.0f;

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
    [SerializeField] private float baseContactDamage = 10f;
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

    [Header("Pattern 2: AoE Indicator (자녀 오브젝트 연결)")]
    [SerializeField] private GameObject attackMaxRangeObj;
    [SerializeField] private GameObject attackRangeObj;
    [SerializeField] private float aoeVisualScale = 4.5f;

    [Header("Pattern 2: Enrage Projectiles (Optional)")]
    [SerializeField] private GameObject aoeProjectilePrefab;
    [SerializeField] private float aoeProjectileSpeed = 10f;

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
    [SerializeField] private AudioClip bossBGM;
    [SerializeField] private AudioClip sfxDashFire;
    [SerializeField] private AudioClip sfxAoeExplode;
    [SerializeField] private AudioClip sfxSpikeWave;
    [SerializeField] private AudioClip sfxVineExplode;
    [SerializeField] private AudioClip sfxEnrageRoar;

    [Header("Death Settings")]
    [SerializeField] private Sprite deadStatueSprite; // 처치 시 변할 석상 이미지

    private GameObject maxRectInstance;
    private GameObject currentRectInstance;
    private GameObject sniperMaxInstance;
    private GameObject sniperCurrentInstance;

    private Transform target;
    private Rigidbody2D rigid;
    private bool isAttacking = false;
    private bool isStunned = false;
    private bool isDashing = false;
    private Queue<GameObject> trailPool = new Queue<GameObject>();
    private GameObject trailContainer;

    // ★ [핵심 추가] 모든 장판을 묶어둘 전용 폴더!
    private Transform telegraphContainer;

    public bool IsDashing => isDashing;
    public float BaseContactDamage => baseContactDamage;
    public float DashDamageMultiplier => dashDamageMultiplier;

    protected override void Awake()
    {
        base.Awake();
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        InitializeTrailPool();

        // ★ 시작할 때 장판 전용 컨테이너 생성
        telegraphContainer = new GameObject($"Guardian_Telegraphs").transform;
    }

    protected override void Start()
    {
        base.Start();
        initialSpawnPos = transform.position;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) target = playerObj.transform;

        ClearRectangles();

        if (bossBGM != null && SoundManager.instance != null)
        {
            SoundManager.instance.PlayBGM(bossBGM);
        }

        if (auraParticle != null)
        {
            auraParticle.Stop();
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.gray;

        if (CinematicManager.instance != null)
        {
            StartCoroutine(CinematicManager.instance.Co_PlayBossIntro(
                this.transform,
                onSunsetStart: () =>
                {
                    StartCoroutine(Co_WakeUpColorLerp(CinematicManager.instance.SunsetDuration));
                },
                onSunsetDone: () =>
                {
                    if (CameraFollow.instance != null) CameraFollow.instance.HitShake(1.7f, 0.1f, 1f);
                    if (sfxSpikeWave != null) SoundManager.instance.PlaySFX(sfxSpikeWave, 1.2f);
                    if (anim != null) anim.SetTrigger("Start");
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
            yield return null;
        }
        spriteRenderer.color = endColor;
    }

    private IEnumerator Co_PostCutsceneSetup()
    {
        if (BossHealthUI.instance != null)
        {
            BossHealthUI.instance.SetupBoss(bossName, maxHealth);
        }

        if (forceEnrage)
        {
            Debug.Log("테스트 모드: 컷신 종료 후 보스 체력 강제 50% 삭감!");
            currentHealth = maxHealth * 0.5f;

            yield return new WaitForSeconds(0.5f);
            if (BossHealthUI.instance != null) BossHealthUI.instance.UpdateBossHealth(currentHealth);
        }

        // 기동 완료 후 기본 파티클 켜기
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

        if (!isAttacking)
            LookAtTarget();

        if (!forceEnrage && isEnraged && currentHealth > maxHealth * 0.5f)
        {
            isEnraged = false;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            Debug.Log("보스 폭주 강제 해제!");
        }
    }

    public override void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead) return;
        base.TakeDamage(damage, isCritical);
        if (BossHealthUI.instance != null) BossHealthUI.instance.UpdateBossHealth(currentHealth);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.05f, 0.04f);
    }

    private IEnumerator Co_EnragePattern()
    {
        Debug.Log("보스 폭주 패턴 시작! 포효 및 넉백 발동!");
        isEnraged = true;
        isAttacking = true;
        rigid.linearVelocity = Vector2.zero;

        if (anim != null) anim.SetTrigger("Enrage");
        if (sfxEnrageRoar != null) SoundManager.instance.PlaySFX(sfxEnrageRoar, 1.2f);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(enragePauseTime, 0.2f, 1f);

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
        Debug.Log("폭주 포효 완료! 광폭화 전투 돌입.");
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

            int patternIndex = (forcePatternIndex == 0) ? Random.Range(1, 5) : forcePatternIndex;
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
        if (anim != null) anim.SetBool("isMoving", true);
        while (timer < moveDuration && !isDead && !isAttacking)
        {
            Vector2 dir = (target.position - transform.position).normalized;
            rigid.linearVelocity = dir * moveSpeed;
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rigid.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetBool("isMoving", false);
    }

    IEnumerator ExecutePattern(int patternIndex)
    {
        isAttacking = true;
        rigid.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetBool("isMoving", false);

        switch (patternIndex)
        {
            case 1: yield return StartCoroutine(Co_Pattern1_Dash()); break;
            case 2: yield return StartCoroutine(Co_Pattern2_AoE()); break;
            case 3: yield return StartCoroutine(Co_Pattern3_MultiLines()); break;
            case 4: yield return StartCoroutine(Co_Pattern4_CrossGrid()); break;
        }

        isAttacking = false;
    }

    IEnumerator Co_Pattern1_Dash()
    {
        int dashCount = (isEnraged || forceEnrage) ? 2 : 1;

        for (int i = 0; i < dashCount; i++)
        {
            if (anim != null) anim.SetTrigger("ReadyDash");

            float currentChargeTime = (i == 0) ? dashChargeTime : dashChargeTime * 0.5f;
            float currentDashDuration = (i == 0) ? dashDuration : dashDuration * 0.5f;
            float currentLimitLength = (i == 0) ? dashMaxLimitLength : dashMaxLimitLength * 0.5f;

            Vector2 currentDir = (target.position - transform.position).normalized;

            if (dashMaxRangeOrigin != null)
                maxRectInstance = Instantiate(dashMaxRangeOrigin, transform.position, Quaternion.identity);

            if (dashCurrentRangeOrigin != null)
                currentRectInstance = Instantiate(dashCurrentRangeOrigin, transform.position, Quaternion.identity);

            // ★ 부모를 telegraphContainer로 설정
            if (maxRectInstance != null) { maxRectInstance.transform.SetParent(telegraphContainer); maxRectInstance.SetActive(true); }
            if (currentRectInstance != null) { currentRectInstance.transform.SetParent(telegraphContainer); currentRectInstance.SetActive(true); }

            float timer = 0f;
            while (timer < currentChargeTime && !isDead)
            {
                timer += Time.deltaTime;
                Vector2 targetDir = (target.position - transform.position).normalized;
                currentDir = Vector2.Lerp(currentDir, targetDir, Time.deltaTime * dashHomingStrength);

                UpdateRectangle(maxRectInstance, currentDir, currentLimitLength, dashRectWidth);
                UpdateRectangle(currentRectInstance, currentDir, currentLimitLength * (timer / currentChargeTime), dashRectWidth);
                LookAtDirection(currentDir.x);

                yield return null;
            }

            ClearRectangles();

            if (anim != null) anim.SetTrigger("Dash");
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
            if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.1f, 0.08f);

            if (i == 0 && dashCount > 1)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }

        yield return new WaitForSeconds(dashRecoveryTime);
    }

    IEnumerator Co_Pattern2_AoE()
    {
        if (anim != null) anim.SetTrigger("ReadySlam");

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

        ClearRectangles();

        if (isDead) yield break;

        if (anim != null) anim.SetTrigger("Slam");
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.35f, 0.3f);

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
        if (anim != null) anim.SetTrigger("RaiseHand");

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

            // ★ 부모를 telegraphContainer로 설정
            if (dashMaxRangeOrigin != null)
            {
                GameObject maxObj = Instantiate(dashMaxRangeOrigin, transform.position, Quaternion.identity);
                maxObj.transform.SetParent(telegraphContainer); maxObj.SetActive(true); maxRects.Add(maxObj);
            }
            if (dashCurrentRangeOrigin != null)
            {
                GameObject curObj = Instantiate(dashCurrentRangeOrigin, transform.position, Quaternion.identity);
                curObj.transform.SetParent(telegraphContainer); curObj.SetActive(true); currentRects.Add(curObj);
            }
        }

        float timer = 0f;
        while (timer < linesChargeTime && !isDead)
        {
            timer += Time.deltaTime;
            for (int i = 0; i < lineDirections.Count; i++)
            {
                if (i < maxRects.Count) UpdateRectangle(maxRects[i], lineDirections[i], spikeMaxLimitLength, spikeRectWidth);
                if (i < currentRects.Count) UpdateRectangle(currentRects[i], lineDirections[i], spikeMaxLimitLength * (timer / linesChargeTime), spikeRectWidth);
            }
            yield return null;
        }

        ClearRectangles();

        if (isDead) yield break;
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.2f, 0.15f);

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

                    // ★ 부모를 telegraphContainer로 설정
                    if (dashMaxRangeOrigin != null)
                    {
                        GameObject maxObj = Instantiate(dashMaxRangeOrigin, startPos, Quaternion.identity);
                        maxObj.transform.SetParent(telegraphContainer); maxObj.SetActive(true); maxRects.Add(maxObj);
                    }
                    if (dashCurrentRangeOrigin != null)
                    {
                        GameObject curObj = Instantiate(dashCurrentRangeOrigin, startPos, Quaternion.identity);
                        curObj.transform.SetParent(telegraphContainer); curObj.SetActive(true); currentRects.Add(curObj);
                    }
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
                        if (i < maxRects.Count) UpdateRectangleFromPoint(maxRects[i], startPos, reverseDirections[i], spikeMaxLimitLength, 1f, spikeRectWidth);
                        if (i < currentRects.Count) UpdateRectangleFromPoint(currentRects[i], startPos, reverseDirections[i], spikeMaxLimitLength, progress, spikeRectWidth);
                    }
                    yield return null;
                }

                ClearRectangles();

                if (isDead) yield break;
                if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.25f, 0.2f);

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
        if (anim != null) anim.SetTrigger("GatherHands");
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
            // ★ 부모를 telegraphContainer로 설정
            if (dashMaxRangeOrigin != null)
            {
                sniperMaxInstance = Instantiate(dashMaxRangeOrigin, transform.position, Quaternion.identity);
                sniperMaxInstance.transform.SetParent(telegraphContainer);
                sniperMaxInstance.SetActive(true);
            }
            if (dashCurrentRangeOrigin != null)
            {
                sniperCurrentInstance = Instantiate(dashCurrentRangeOrigin, transform.position, Quaternion.identity);
                sniperCurrentInstance.transform.SetParent(telegraphContainer);
                sniperCurrentInstance.SetActive(true);
            }
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
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.2f, 0.2f);
        yield return new WaitForSeconds(gridFireDelay);

        if (sfxVineExplode != null) SoundManager.instance.PlaySFX(sfxVineExplode, 1f, 0.1f);
        FireVineSet(null, vSet1);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.2f, 0.2f);
        yield return new WaitForSeconds(gridFireDelay);

        if (isEnraged || forceEnrage)
        {
            isSniperTracking = false;
            if (sniperMaxInstance != null) sniperMaxInstance.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.5f);
            if (sniperCurrentInstance != null) sniperCurrentInstance.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.9f);
        }

        if (sfxVineExplode != null) SoundManager.instance.PlaySFX(sfxVineExplode, 1.2f, 0.1f);
        FireVineSet(hSet2, vSet2);
        if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.35f, 0.35f);

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
                if (CameraFollow.instance != null) CameraFollow.instance.HitShake(0.5f, 0.4f);
            }

            if (sniperMaxInstance != null) Destroy(sniperMaxInstance);
            if (sniperCurrentInstance != null) Destroy(sniperCurrentInstance);
        }

        if (anim != null) anim.SetTrigger("StopGatherHands");
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

                GameObject telegraph = Instantiate(dashCurrentRangeOrigin, data.startPos, Quaternion.identity);
                // ★ 부모를 telegraphContainer로 설정
                telegraph.transform.SetParent(telegraphContainer);
                telegraph.transform.localScale = Vector3.one;
                telegraph.SetActive(true);

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

                GameObject telegraph = Instantiate(dashCurrentRangeOrigin, data.startPos, Quaternion.identity);
                // ★ 부모를 telegraphContainer로 설정
                telegraph.transform.SetParent(telegraphContainer);
                telegraph.transform.localScale = Vector3.one;
                telegraph.SetActive(true);

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

        foreach (var m in markers) if (m != null) Destroy(m);
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

            GameObject proj = Instantiate(aoeProjectilePrefab, transform.position, Quaternion.identity);
            EnemyProjectile projectileScript = proj.GetComponent<EnemyProjectile>();

            if (projectileScript != null)
            {
                projectileScript.Initialize(dir, aoeProjectileSpeed);
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

    void UpdateRectangle(GameObject rect, Vector2 dir, float requestedLength, float width)
    {
        if (rect == null) return;
        float facingDirection = Mathf.Sign(transform.localScale.x);
        Vector2 visualOffset = new Vector2(dashIndicatorOffset.x * facingDirection, dashIndicatorOffset.y);
        Vector2 startPos = (Vector2)transform.position + visualOffset;

        rect.transform.position = startPos;
        rect.transform.right = dir.normalized;

        RaycastHit2D hit = Physics2D.Raycast(startPos, dir.normalized, requestedLength, wallLayer);
        float finalLength = hit.collider != null ? Mathf.Max(0, hit.distance - 0.5f) : requestedLength;

        SpriteRenderer sr = rect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float adjustedLength = finalLength + Mathf.Abs(dashIndicatorOffset.x);
            sr.size = new Vector2(adjustedLength, width);
        }
    }

    void UpdateRectangleFromPoint(GameObject rect, Vector2 startPos, Vector2 dir, float maxLimitLength, float progress, float width)
    {
        if (rect == null) return;

        rect.transform.position = startPos;
        rect.transform.right = dir.normalized;

        RaycastHit2D hit = Physics2D.Raycast(startPos, dir.normalized, maxLimitLength, wallLayer);
        float finalLength = hit.collider != null ? Mathf.Max(0, hit.distance - 0.5f) : maxLimitLength;

        SpriteRenderer sr = rect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float currentLength = finalLength * progress;
            sr.size = new Vector2(currentLength, width);
        }
    }

    // ★ [핵심 수정] 남아있는 모든 장판들을 폴더째로 싹 지웁니다!
    void ClearRectangles()
    {
        if (attackMaxRangeObj != null) attackMaxRangeObj.SetActive(false);
        if (attackRangeObj != null) attackRangeObj.SetActive(false);

        // 컨테이너 안에 찌꺼기처럼 남아있는 모든 클론 장판들을 완벽하게 파괴
        if (telegraphContainer != null)
        {
            foreach (Transform child in telegraphContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void LookAtTarget()
    {
        LookAtDirection(target.position.x - transform.position.x);
    }

    private void LookAtDirection(float dirX)
    {
        if (dirX > 0)
            transform.localScale = new Vector3(spriteScale, spriteScale, 1);
        else
            transform.localScale = new Vector3(-spriteScale, spriteScale, 1);
    }

    protected override void OnHit() { if (isDead) return; }

    protected override void Die()
    {
        isDead = true;

        StopAllCoroutines();

        // 죽는 즉시 맵에 깔린 모든 장판을 흔적도 없이 소멸시킵니다.
        ClearRectangles();

        if (auraParticle != null) auraParticle.Stop();

        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.simulated = false;
        }

        if (BossHealthUI.instance != null)
            BossHealthUI.instance.HideUI();

        if (trailContainer != null)
            Destroy(trailContainer, trailLifeTime);

        // ★ [추가] 장판을 담아둔 빈 폴더 객체도 메모리에서 날려버립니다.
        if (telegraphContainer != null)
            Destroy(telegraphContainer.gameObject);

        // 머티리얼 강제 복구 (하얗게 굳어버리는 현상 방지)
        if (spriteRenderer != null && originalMaterial != null)
        {
            spriteRenderer.material = originalMaterial;
            spriteRenderer.color = Color.white;
        }

        if (anim != null) anim.enabled = false;

        if (CinematicManager.instance != null)
        {
            CinematicManager.instance.PlayBossDeathCinematic(this);
        }

        Debug.Log("숲의 관리자가 처치되었습니다. 시네마틱 시작!");
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