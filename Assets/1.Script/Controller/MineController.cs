using System.Collections;
using UnityEngine;
using TMPro;

public class MineController : MonoBehaviour, IHealth
{
    [Header("Mine Settings")]
    public int mineCost = 15; // 광산 건설 비용
    public float goldGenerationInterval = 3f; // 골드 생성 주기 (초)
    public int goldPerGeneration = 1; // 한 번에 생성되는 골드 수
    public int currPaidCost = 0; // 현재 지불된 비용
    public float ditectDIsatnce = 2f;

    [Header("Health Settings")]
    public int maxHealth = 80; // 광산의 체력
    private int currentHealth;

    [Header("Mine Parts")]
    public GameObject mineOBJ; // Mine_OBJ
    public GameObject outLine; // OutLine
    public GameObject coin; // Coin
    public GameObject goldStoragePoint; // 골드 저장소 위치

    [Header("Visual Effects")]
    public ParticleSystem miningEffect; // 채굴 파티클 (옵션)
    [SerializeField] private float damageFlashDuration = 0.15f;
    [SerializeField] private float damageScaleDuration = 0.2f;
    [SerializeField] private float damageScaleMultiplier = 1.1f;
    [Header("Animation")]
    [SerializeField] private float AnimationDuration = 1f;
    private bool isBuilt = false;
    private bool isVisible = true; // 순차 건설의 가시성 관리
    private bool isGeneratingGold = false;

    // 피격 연출 관련 변수
    private Renderer[] buildingRenderers;
    private Color[] originalColors;
    private Vector3 originalScale;
    private bool isFlashingDamage = false;

    private GoldStorage goldStorage;
    private PlayerController _player;
    public TextMeshPro TxtGold;
    public void Init()
    {
        // HP 초기화
        currentHealth = maxHealth;

        _player = GameManager.Instance.m_Player;

        // 골드 저장소 초기화
        InitializeGoldStorage();
        InitializeDamageEffect();

        SetBuildState(false);

        // 가시성이 활성화된 경우에만 건설 체크 시작
        if (isVisible && this.gameObject.activeInHierarchy)
        {
            StartCoroutine(CheckAndBuildBehavior());
        }
    }

    /// <summary>
    /// 피격 연출을 위한 초기화 (렌더러와 원본 색상 저장)
    /// </summary>
    void InitializeDamageEffect()
    {
        // mineOBJ 하위의 모든 렌더러 수집
        if (mineOBJ != null)
        {
            buildingRenderers = mineOBJ.GetComponentsInChildren<Renderer>();
            originalColors = new Color[buildingRenderers.Length];

            for (int i = 0; i < buildingRenderers.Length; i++)
            {
                if (buildingRenderers[i].material != null)
                {
                    originalColors[i] = buildingRenderers[i].material.color;
                }
            }

            // 원본 크기 저장
            originalScale = mineOBJ.transform.localScale;
        }
    }

    void InitializeGoldStorage()
    {
        // 골드 저장소 포인트가 없으면 자동 생성
        if (goldStoragePoint == null)
        {
            GameObject storagePoint = new GameObject("GoldStoragePoint");
            storagePoint.transform.SetParent(transform);
            storagePoint.transform.localPosition = Vector3.right * 1.5f; // 광산 오른쪽에 위치
            goldStoragePoint = storagePoint;
        }

        // GoldStorage 컴포넌트 확인 및 추가
        goldStorage = goldStoragePoint.GetComponent<GoldStorage>();
        if (goldStorage == null)
        {
            goldStorage = goldStoragePoint.AddComponent<GoldStorage>();
        }

        if (TxtGold != null)
            TxtGold.text = mineCost.ToString();
        // 골드 저장소 초기화 및 중심점 설정
        goldStorage.storageCenter = goldStoragePoint.transform;
        goldStorage.Init();

        Debug.Log($"MineController: GoldStorage initialized on {goldStoragePoint.name}");
    }

    void SetBuildState(bool built)
    {
        isBuilt = built;

        if (mineOBJ != null)
            mineOBJ.SetActive(built);

        if (outLine != null)
            outLine.SetActive(!built && isVisible); // 가시성도 고려

        if (coin != null)
            coin.SetActive(!built && isVisible); // 가시성도 고려

        if (goldStoragePoint != null)
            goldStoragePoint.SetActive(built);
        // 건설 완료 시 올라오는 애니메이션 및 채굴 시작
        if (built)
        {
            StartCoroutine(MineRiseAnimation());
        }
    }

    public void SetVisibility(bool visible)
    {
        isVisible = visible;

        if (visible)
        {
            // 광산을 보이게 할 때
            gameObject.SetActive(true);
            SetBuildState(isBuilt); // 현재 건설 상태에 맞게 UI 업데이트

            // 다시 활성화: GoldStorage 재초기화
            InitializeGoldStorage();

            // 아직 건설되지 않았고 건설 체크가 실행 중이 아니라면 시작
            if (!isBuilt)
            {
                StartCoroutine(CheckAndBuildBehavior());
            }

            Debug.Log($"Mine {gameObject.name} is now visible and available for construction");
        }
        else
        {
            // 광산을 숨길 때
            if (outLine != null) outLine.SetActive(false);
            if (coin != null) coin.SetActive(false);

            // 건설되지 않은 광산은 완전히 비활성화
            if (!isBuilt)
            {
                gameObject.SetActive(false);
            }

            Debug.Log($"Mine {gameObject.name} is now hidden");
        }
    }

    IEnumerator MineRiseAnimation()
    {
        if (mineOBJ == null) yield break;

        //TODO 2025-11-26
        //소현님 0.5f정도 대기
        Vector3 originalMineScale = mineOBJ.transform.localScale;
        Vector3 originalGoldScale = goldStoragePoint.transform.localScale;

        mineOBJ.transform.localScale = Vector3.zero;
        goldStoragePoint.transform.localScale = Vector3.zero;

        //EffectController _effect = ObjectPool.Instance.SpawnFromPool("Effect", this.transform.position, Quaternion.identity, ObjectPool.Instance.transform).GetComponent<EffectController>();
        //if (_effect)
        //    _effect.Init(EffectType.Building, this.transform.position.x, this.transform.position.z);

        yield return new WaitForSeconds(0.5f);



        if (float.IsNaN(originalMineScale.x) || float.IsNaN(originalMineScale.y) || float.IsNaN(originalMineScale.z))
        {
            originalMineScale = Vector3.one;
        }
        if (float.IsNaN(originalGoldScale.x) || float.IsNaN(originalGoldScale.y) || float.IsNaN(originalGoldScale.z))
        {
            originalGoldScale = Vector3.one;
        }


        // 광산을 땅 아래로 이동하고 작은 크기로 설정
        mineOBJ.transform.localScale = originalMineScale * 0.3f;
        goldStoragePoint.transform.localScale = originalGoldScale * 0.3f;

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
            Vector3 currentMineScale = originalMineScale * scaleMultiplier;
            Vector3 currentGoldScale = originalGoldScale * scaleMultiplier;


            if (!float.IsNaN(currentMineScale.x) && !float.IsNaN(currentMineScale.y) && !float.IsNaN(currentMineScale.z))
            {
                mineOBJ.transform.localScale = currentMineScale;
            }

            if (!float.IsNaN(currentGoldScale.x) && !float.IsNaN(currentGoldScale.y) && !float.IsNaN(currentGoldScale.z))
            {
                goldStoragePoint.transform.localScale = currentGoldScale;
            }
            yield return null;
        }

        if (!float.IsNaN(originalMineScale.x) && !float.IsNaN(originalMineScale.y) && !float.IsNaN(originalMineScale.z))
        {
            mineOBJ.transform.localScale = originalMineScale;
        }

        if (!float.IsNaN(originalGoldScale.x) && !float.IsNaN(originalGoldScale.y) && !float.IsNaN(originalGoldScale.z))
        {
            goldStoragePoint.transform.localScale = originalGoldScale;
        }

        // 애니메이션 완료 후 채굴 시작
        StartCoroutine(StartGoldGeneration());
        StartCoroutine(CheckPlayerNear());

        // 건설 완료 알림을 GameManager에 전송
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMineBuilt(this);
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
                    if (GameManager.Instance.GetCurrentGold() > 0 && currPaidCost < mineCost)
                    {
                        SendGoldToMine();
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void SendGoldToMine()
    {
        if (GameManager.Instance.SpendGold(1))
        {
            // 플레이어로부터 골드를 시각적으로 보내는 메서드 호출
            _player.OnSendGoldToTurret(transform); // 기존 메서드 재사용

            currPaidCost++;
            if(TxtGold!=null)
            {
                int _remainGold = mineCost - currPaidCost;
                TxtGold.text = $"{_remainGold}";
            }
            Debug.Log($"Mine received gold: {currPaidCost}/{mineCost}");

            // 목표 비용에 도달하면 건설 완료
            if (currPaidCost >= mineCost)
            {
                BuildMine();
            }
        }
    }

    void BuildMine()
    {
        Debug.Log($"Mine {gameObject.name} construction completed!");
        SetBuildState(true);

        // GameManager에 광산 건설 완료 알림 추가
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMineBuilt(this);
        }
    }

    IEnumerator StartGoldGeneration()
    {
        isGeneratingGold = true;

        while (isBuilt && isGeneratingGold)
        {
            yield return new WaitForSeconds(goldGenerationInterval);

            // 골드 생성
            GenerateGold();

            // 채굴 파티클 재생 (있다면)
            PlayMiningEffect();
        }
    }

    IEnumerator CheckPlayerNear()
    {
        while (isBuilt && GetStoredGold() > 0)
        {
            if (Vector3.Distance(_player.transform.position, this.transform.position) <= 2.2f)
            {
                CollectGold(GetStoredGold());
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void GenerateGold()
    {
        if (goldStorage != null)
        {
            goldStorage.AddGold(goldPerGeneration);
            Debug.Log($"Mine generated {goldPerGeneration} gold. Total stored: {goldStorage.GetStoredGoldCount()}");
        }
    }

    void PlayMiningEffect()
    {
        if (miningEffect != null && !miningEffect.isPlaying)
        {
            miningEffect.Play();
        }
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
            Debug.Log($"Mine transferred {amount} gold to player");
            return true;
        }
        return false;
    }

    // 순차 건설 시스템용 확인 메서드들
    public bool IsBuilt() => isBuilt;

    // 채굴 중단/재개 메서드 (필요시)
    public void StopGoldGeneration()
    {
        isGeneratingGold = false;
    }

    // 광산 업그레이드 메서드 (확장 기능)
    public void UpgradeGoldGeneration(float newInterval, int newAmount)
    {
        goldGenerationInterval = newInterval;
        goldPerGeneration = newAmount;
    }

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
        if (buildingRenderers == null || mineOBJ == null) yield break;

        isFlashingDamage = true;

        // 1. 색상 화이트 플래시
        for (int i = 0; i < buildingRenderers.Length; i++)
        {
            if (buildingRenderers[i] != null && buildingRenderers[i].material != null)
            {
                buildingRenderers[i].material.color = Color.red;
            }
        }

        // 2. 크기 증가
        Vector3 scaledSize = originalScale * damageScaleMultiplier;
        mineOBJ.transform.localScale = scaledSize;

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

            mineOBJ.transform.localScale = Vector3.Lerp(scaledSize, originalScale, progress);
            yield return null;
        }

        // 6. 정확한 원본 크기로 복귀
        mineOBJ.transform.localScale = originalScale;
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
        // Debug.Log($"Mine Health: {currentHealth}/{maxHealth}");
    }

    public void OnDeath()
    {
        // 채굴 중단
        StopGoldGeneration();

        // 광산 파괴 처리
        if (goldStorage != null && goldStorage.GetStoredGoldCount() > 0)
        {
            // 저장된 골드의 절반만 드롭
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

        // 광산 파괴
        DestroyMine();
    }

    void DestroyMine()
    {
        // 파괴 이펙트 재생 (옵션)
        // 광산 오브젝트 비활성화
        gameObject.SetActive(false);

        // 또는 ObjectPool로 반환 (풀링 사용 시)
        // ObjectPool.Instance.ReturnToPool(gameObject);
    }
    #endregion
}