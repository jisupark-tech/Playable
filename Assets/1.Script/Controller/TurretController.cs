using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TurretController : MonoBehaviour, IHealth
{
    [Header("Turret Settings")]
    public int turretCost = 20; // 터렛 건설 비용
    public float detectionRange = 8f; // 적 감지 범위
    public int turretDamage = 25; // 터렛 공격력
    public float fireRate = 1f; // 발사 속도 (초당)
    public int currPaidCost = 0; // 현재 지불된 비용
    public float ditectDIsatnce = 2f; // 플레이어 감지 거리

    [Header("Health Settings")]
    public int maxHealth = 100; // 터렛의 체력
    private int currentHealth;

    [Header("Turret Parts")]
    public GameObject turretOBJ; // Turret_OBJ
    public GameObject outLine; // OutLine
    public GameObject coin; // Coin
    public GameObject goldStoragePoint; // 골드 저장소 위치
    public GameObject goldslider;
    private float origoldSliderSize = 1.9f;
    [Header("Visual Effects")]
    [SerializeField] private float damageFlashDuration = 0.15f;
    [SerializeField] private float damageScaleDuration = 0.2f;
    [SerializeField] private float damageScaleMultiplier = 1.1f;

    [Header("Animation")]
    [SerializeField] private float AnimationDuration = 1f;
    // 피격 연출용 원본 데이터 저장
    private Renderer[] buildingRenderers;
    private Color[] originalColors;
    private Vector3 originalScale;
    private bool isFlashingDamage = false;

    private bool isBuilt = false;
    private bool isVisible = true; // 순차 건설의 가시성 관리
    private Vector3 originalScale2;
    private bool originalScaleSaved = false;
    private GoldStorage goldStorage;

    private CombatComponent combatComponent;
    private PlayerController _player;
    public TextMeshPro textMeshPro; // 비용 표시용 텍스트

    public void Init()
    {
        currentHealth = maxHealth;
        _player = GameManager.Instance.m_Player;

        // Combat Component 초기화
        combatComponent = GetComponent<CombatComponent>();
        if (combatComponent == null)
        {
            combatComponent = gameObject.AddComponent<CombatComponent>();
        }

        // 원본 크기와 피격 연출 초기화
        SaveOriginalScale();
        InitializeDamageEffect();

        InitializeGoldSlider();
        InitializeGoldStorage();
        InitializeCostDisplay(); // 비용 표시 초기화
        SetBuildState(false);

        if (isVisible)
        {
            StartCoroutine(CheckAndBuildBehavior());
        }
    }
    void InitializeGoldSlider()
    {
        if (goldslider != null)
        {
            goldslider.transform.localScale = new Vector3(0, origoldSliderSize, 0);
        }
    }

    /// <summary>
    /// 피격 연출을 위한 초기화 (렌더러와 원본 색상 저장)
    /// </summary>
    void InitializeDamageEffect()
    {
        // turretOBJ 하위의 모든 렌더러 수집
        if (turretOBJ != null)
        {
            buildingRenderers = turretOBJ.GetComponentsInChildren<Renderer>();
            originalColors = new Color[buildingRenderers.Length];

            for (int i = 0; i < buildingRenderers.Length; i++)
            {
                if (buildingRenderers[i].material != null)
                {
                    originalColors[i] = buildingRenderers[i].material.color;
                }
            }
        }
    }

    /// <summary>
    /// turretOBJ의 원본 크기를 저장합니다
    /// </summary>
    void SaveOriginalScale()
    {
        if (turretOBJ != null && !originalScaleSaved)
        {
            originalScale = turretOBJ.transform.localScale;
            originalScale2 = originalScale; // 백업용
            originalScaleSaved = true;
        }
    }

    void InitializeGoldStorage()
    {
        // 골드 저장소 포인트가 없으면 자동 생성
        if (goldStoragePoint == null)
        {
            GameObject storagePoint = new GameObject("GoldStoragePoint");
            storagePoint.transform.SetParent(transform);
            storagePoint.transform.localPosition = Vector3.right * 1.5f; // 터렛 오른쪽에 위치
            goldStoragePoint = storagePoint;
        }

        // GoldStorage 컴포넌트 확인 및 추가
        goldStorage = goldStoragePoint.GetComponent<GoldStorage>();
        if (goldStorage == null)
        {
            goldStorage = goldStoragePoint.AddComponent<GoldStorage>();
        }

        // 골드 저장소 초기화 및 중심점 설정
        goldStorage.storageCenter = goldStoragePoint.transform;
        goldStorage.Init();

        Debug.Log($"TurretController: GoldStorage initialized on {goldStoragePoint.name}");
    }

    void InitializeCostDisplay()
    {
        // 비용 표시용 TextMeshPro 자동 생성
        if (textMeshPro == null)
        {
            GameObject textObj = new GameObject("CostText");

            // 부모를 outLine으로 설정 (outLine이 있을 때만)
            if (outLine != null)
                textObj.transform.SetParent(outLine.transform);
            else
                textObj.transform.SetParent(transform);

            textObj.transform.localPosition = new Vector3(0, 2f, 0); // 터렛 위쪽에 표시

            // TextMeshPro 컴포넌트 추가 및 설정
            textMeshPro = textObj.AddComponent<TextMeshPro>();
            textMeshPro.fontSize = 1.5f;
            textMeshPro.color = Color.yellow;
            textMeshPro.alignment = TextAlignmentOptions.Center;
            textMeshPro.sortingOrder = 10; // 다른 UI보다 위에 표시

            // 카메라를 향하도록 설정 (Billboard 효과)
            if (Camera.main != null)
            {
                textMeshPro.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
            }
        }

        UpdateCostDisplay();
    }

    void UpdateCostDisplay()
    {
        if (textMeshPro == null) return;

        // 터렛이 이미 건설되었거나 보이지 않으면 텍스트 숨김
        if (isBuilt || !isVisible)
        {
            textMeshPro.gameObject.SetActive(false);
            // 건설 완료되면 슬라이더도 숨김
            UpdateGoldSlider(1.0f); // 100% 채운 후
            if (goldslider != null)
            {
                StartCoroutine(HideSliderAfterCompletion());
            }
        }
        else
        {
            // 남은 비용 표시
            int remainingCost = turretCost - currPaidCost;
            if (remainingCost > 0)
            {
                textMeshPro.gameObject.SetActive(true);
                textMeshPro.text = $"{remainingCost}";

                // 건설 진행도에 따른 슬라이더 업데이트
                float progress = (float)currPaidCost / turretCost;
                UpdateGoldSlider(progress);
            }
            else
            {
                textMeshPro.gameObject.SetActive(false);
                UpdateGoldSlider(1.0f); // 100% 완료
            }
        }
    }

    /// <summary>
    /// 골드 슬라이더 업데이트 (건설 진행도 표시)
    /// </summary>
    /// <param name="progress">진행도 (0.0 ~ 1.0)</param>
    void UpdateGoldSlider(float progress)
    {
        if (goldslider == null) return;

        // 진행도에 따른 Y축 스케일 조정
        progress = Mathf.Clamp01(progress);
        float targetScaleX = origoldSliderSize * progress;

        Vector3 currentScale = goldslider.transform.localScale;
        Vector3 targetScale = new Vector3(targetScaleX, currentScale.y, currentScale.z);

        goldslider.transform.localScale = targetScale;

        // 슬라이더가 보이도록 활성화
        if (!goldslider.activeInHierarchy && progress > 0 && !isBuilt)
        {
            goldslider.SetActive(true);
        }
    }

    /// <summary>
    /// 건설 완료 후 슬라이더를 부드럽게 숨기는 코루틴
    /// </summary>
    IEnumerator HideSliderAfterCompletion()
    {
        // 1초 대기 후 슬라이더 숨김
        yield return new WaitForSeconds(1.0f);

        if (goldslider != null && isBuilt)
        {
            // 부드럽게 사라지는 애니메이션
            float duration = 0.5f;
            float elapsedTime = 0f;
            Vector3 startScale = goldslider.transform.localScale;
            Vector3 endScale = new Vector3(startScale.x, 0, startScale.z);

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                goldslider.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            goldslider.SetActive(false);
        }
    }

    void SetBuildState(bool built)
    {
        isBuilt = built;

        if (turretOBJ != null)
            turretOBJ.SetActive(built);

        if (outLine != null)
            outLine.SetActive(!built && isVisible); // 가시성도 고려

        if (coin != null)
            coin.SetActive(!built && isVisible); // 가시성도 고려

        if (goldStoragePoint != null)
            goldStoragePoint.SetActive(built && isVisible);
        // 비용 표시 업데이트
        UpdateCostDisplay();

        if (built)
        {
            StartCoroutine(TurretRiseAnimation());
        }
    }

    public void SetVisibility(bool visible)
    {
        isVisible = visible;

        if (visible)
        {
            // 터렛을 보이게 할 때
            gameObject.SetActive(true);
            SetBuildState(isBuilt); // 현재 건설 상태에 맞게 UI 업데이트

            // 다시 활성화: GoldStorage 및 텍스트 재초기화
            InitializeGoldStorage();
            InitializeCostDisplay();

            // 아직 건설되지 않았고 건설 체크가 실행 중이 아니라면 시작
            if (!isBuilt)
            {
                StartCoroutine(CheckAndBuildBehavior());
            }

            Debug.Log($"Turret {gameObject.name} is now visible and available for construction");
        }
        else
        {
            // 터렛을 숨길 때
            if (outLine != null) outLine.SetActive(false);
            if (coin != null) coin.SetActive(false);
            if (textMeshPro != null) textMeshPro.gameObject.SetActive(false);
            if (goldslider != null) goldslider.SetActive(false); // 슬라이더도 숨김

            // 건설되지 않은 터렛은 완전히 비활성화
            if (!isBuilt)
            {
                gameObject.SetActive(false);
            }

            Debug.Log($"Turret {gameObject.name} is now hidden");
        }
    }

    IEnumerator TurretRiseAnimation()
    {
        if (turretOBJ == null) yield break;

        //TODO 2025-11-26
        //소현님 0.5f정도 대기
        Vector3 originalTurretScale = turretOBJ.transform.localScale;
        turretOBJ.transform.localScale = Vector3.zero;

        Vector3 originalGoldScale = goldStoragePoint.transform.localScale;
        goldStoragePoint.transform.localScale = Vector3.zero;


        yield return new WaitForSeconds(0.5f);

        EffectController _effect = ObjectPool.Instance.SpawnFromPool("Effect", this.transform.position, Quaternion.identity, ObjectPool.Instance.transform).GetComponent<EffectController>();
        if (_effect)
            _effect.Init(EffectType.Building, this.transform.position.x, this.transform.position.z);

        if (float.IsNaN(originalTurretScale.x) || float.IsNaN(originalTurretScale.y) || float.IsNaN(originalTurretScale.z))
        {
            originalTurretScale = Vector3.one;
        }

        if (float.IsNaN(originalGoldScale.x) || float.IsNaN(originalGoldScale.y) || float.IsNaN(originalGoldScale.z))
        {
            originalGoldScale = Vector3.one;
        }

        turretOBJ.transform.localScale = originalTurretScale * 0.3f;
        goldStoragePoint.transform.localScale = originalGoldScale*0.3f;

        float _animationDuration = AnimationDuration; // 1초로 단축
        float elapsedTime = 0f;

        // 땅 아래서 위로 올라오면서 크기 변화하는 애니메이션
        while (elapsedTime < _animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / _animationDuration); // NaN 방지

            // 크기: 단순하고 안전한 탄성 애니메이션
            float scaleMultiplier = 1.0f;

            if (progress <= 0.5f)
            {
                // 0~50%: 0.3 → 1.15 (빠르게 커짐)
                float t = progress / 0.5f;
                t = Mathf.Clamp01(t);
                scaleMultiplier = Mathf.Lerp(0.3f, 1.15f, t);
            }
            else if (progress <= 0.8f)
            {
                // 50~80%: 1.15 → 0.9 (탄성 수축)
                float t = (progress - 0.5f) / 0.3f;
                t = Mathf.Clamp01(t);
                scaleMultiplier = Mathf.Lerp(1.15f, 0.9f, t);
            }
            else
            {
                // 80~100%: 0.9 → 1.0 (최종 크기)
                float t = (progress - 0.8f) / 0.2f;
                t = Mathf.Clamp01(t);
                scaleMultiplier = Mathf.Lerp(0.9f, 1.0f, t);
            }

            // 안전한 스케일 계산
            scaleMultiplier = Mathf.Clamp(scaleMultiplier, 0.1f, 2.0f); // 안전 범위 제한
            Vector3 currentTurretScale = originalTurretScale * scaleMultiplier;
            Vector3 currentGoldScal = originalGoldScale * scaleMultiplier;

            if (!float.IsNaN(currentTurretScale.x) && !float.IsNaN(currentTurretScale.y) && !float.IsNaN(currentTurretScale.z))
            {
                turretOBJ.transform.localScale = currentTurretScale;
            }

            if (!float.IsNaN(currentGoldScal.x) && !float.IsNaN(currentGoldScal.y) && !float.IsNaN(currentGoldScal.z))
            {
                goldStoragePoint.transform.localScale = currentGoldScal;
            }

            yield return null;
        }

        if (!float.IsNaN(originalTurretScale.x) && !float.IsNaN(originalTurretScale.y) && !float.IsNaN(originalTurretScale.z))
        {
            turretOBJ.transform.localScale = originalTurretScale;
        }

        if (!float.IsNaN(originalGoldScale.x) && !float.IsNaN(originalGoldScale.y) && !float.IsNaN(originalGoldScale.z))
        {
            goldStoragePoint.transform.localScale = originalGoldScale;
        }

        // 애니메이션 완료 후 전투 시작
        StartCombat();

        // 건설 완료 알림을 GameManager에 전송
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurretBuilt(this);
        }
    }


    void StartCombat()
    {
        if (combatComponent != null)
        {
            combatComponent.Initialize();
            Debug.Log($"Turret {gameObject.name} started combat");
        }
    }

    IEnumerator CheckAndBuildBehavior()
    {
        while (!isBuilt && isVisible) // 가시성 체크 추가
        {
            if (_player != null)
            {
                float distance = Vector3.Distance(_player.transform.position, transform.position);
                if (distance <= ditectDIsatnce) // 건설 범위
                {
                    // 플레이어 골드를 가지고 있고 아직 건설이 완료되지 않은 경우
                    if (GameManager.Instance.GetCurrentGold() > 0 && currPaidCost < turretCost)
                    {
                        SendGoldToTurret();
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void SendGoldToTurret()
    {
        if (GameManager.Instance.SpendGold(1))
        {
            // 플레이어로부터 골드를 시각적으로 보내는 메서드 호출
            _player.OnSendGoldToTurret(transform);

            currPaidCost++;

            // 비용 표시 업데이트 (슬라이더 포함)
            UpdateCostDisplay();

            Debug.Log($"Turret received gold: {currPaidCost}/{turretCost}");

            // 목표 비용에 도달하면 건설 완료
            if (currPaidCost >= turretCost)
            {
                BuildTurret();
            }
        }
    }

    void BuildTurret()
    {
        Debug.Log($"Turret {gameObject.name} construction completed!");
        SetBuildState(true);
    }

    // 골드 저장소 관련 API
    public int GetStoredGold()
    {
        return goldStorage != null ? goldStorage.GetStoredGoldCount() : 0;
    }

    public bool CollectGold(int amount)
    {
        if (goldStorage != null && goldStorage.GetStoredGoldCount() >= amount)
        {
            goldStorage.TransferGoldTo(_player.transform, amount);
            Debug.Log($"Turret transferred {amount} gold to player");
            return true;
        }
        return false;
    }

    public GoldStorage GetgoldStorage() => goldStorage == null ? null : goldStorage;
    // 순차 건설 시스템용 확인 메서드들
    public bool IsBuilt() => isBuilt;
    public bool IsVisible() => isVisible;
    public int GetPaidCost() => currPaidCost;
    public float GetBuildProgress() => (float)currPaidCost / turretCost;


    /// <summary>
    /// 피격 시 화이트 플래시 + 크기 변화 연출
    /// </summary>
    void StartDamageFlashEffect()
    {
        if (!isFlashingDamage)
        {
            StartCoroutine(DamageFlashEffect());
        }
    }

    IEnumerator DamageFlashEffect()
    {
        if (buildingRenderers == null || turretOBJ == null) yield break;

        isFlashingDamage = true;

        // 1. 색상 레드 플래시
        for (int i = 0; i < buildingRenderers.Length; i++)
        {
            if (buildingRenderers[i] != null && buildingRenderers[i].material != null)
            {
                buildingRenderers[i].material.color = Color.red;
            }
        }

        // 2. 크기 증가
        Vector3 scaledSize = originalScale * damageScaleMultiplier;
        turretOBJ.transform.localScale = scaledSize;

        // 3. 화이트 플래시 지속 시간
        yield return new WaitForSeconds(damageFlashDuration);

        // 4. 원본 색상으로 복귀
        for (int i = 0; i < buildingRenderers.Length; i++)
        {
            if (buildingRenderers[i] != null && buildingRenderers[i].material != null)
            {
                buildingRenderers[i].material.color = originalColors[i];
            }
        }

        // 5. 크기 복귀 애니메이션
        float elapsedTime = 0f;
        while (elapsedTime < damageScaleDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / damageScaleDuration;

            // Ease-out 효과
            progress = 1f - Mathf.Pow(1f - progress, 2f);

            turretOBJ.transform.localScale = Vector3.Lerp(scaledSize, originalScale, progress);
            yield return null;
        }

        // 6. 정확한 원본 크기로 복귀
        turretOBJ.transform.localScale = originalScale;
        isFlashingDamage = false;
    }

    #region IHealth Implementation
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead()) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        OnHealthChanged(currentHealth, maxHealth);

        // 피격 연출 효과
        StartDamageFlashEffect();

        if (currentHealth <= 0)
        {
            OnDeath();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead()) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        OnHealthChanged(currentHealth, maxHealth);
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public float GetHealthRatio()
    {
        return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }

    public void OnHealthChanged(int currentHealth, int maxHealth)
    {
        // HP바 UI 업데이트 등 (필요시 구현)
        // Debug.Log($"Turret Health: {currentHealth}/{maxHealth}");
    }

    public void OnDeath()
    {
        if (goldStorage != null && goldStorage.GetStoredGoldCount() > 0)
        {
            int goldToDrop = goldStorage.GetStoredGoldCount() / 2;
            for (int i = 0; i < goldToDrop; i++)
            {
                GameObject gold = ObjectPool.Instance.SpawnFromPool("Gold", transform.position + Random.insideUnitSphere * 1f, Quaternion.identity);
                if (gold != null)
                {
                    GoldPickup goldPickup = gold.GetComponent<GoldPickup>();
                    goldPickup.Initialize(1);
                }
            }
        }

        DestroyTurret();
    }

    void DestroyTurret()
    {
        gameObject.SetActive(false);
    }

    public bool IsBuild => isBuilt;
    #endregion

    #region Gizmos (기즈모)
    void OnDrawGizmosSelected()
    {
        // 터렛 감지 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 원본 스케일 영역 표시
        if (originalScaleSaved && turretOBJ != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = turretOBJ.transform.position;
            Gizmos.DrawWireCube(center, originalScale);
        }
    }
    #endregion
}