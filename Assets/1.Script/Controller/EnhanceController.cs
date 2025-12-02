using System.Collections;
using UnityEngine;
using TMPro;
public enum GimmicKType
{
    None,
    Arrow,
    NPC,
    Pet,
}

public class EnhanceController : MonoBehaviour , IHealth
{

    [Header("Enhance Settings")]
    public int enhanceCost = 20;
    public int currPaidCost = 0;
    public float ditectDistance = 2f;
    public GimmicKType m_type = GimmicKType.None;

    [Header("Health Settings")]
    public int maxHealth = 80;
    private int currentHealth;

    [Header("Enhance Parts")]
    public GameObject enhanceOBJ; // Mine_OBJ
    public GameObject outLine; // OutLine
    public GameObject coin; // Coin
    public TextMeshPro TxtGold;

    [Header("Visual Effects")]
    [SerializeField] private float damageFlashDuration = 0.15f;
    [SerializeField] private float damageScaleDuration = 0.2f;
    [SerializeField] private float damageScaleMultiplier = 1.1f;
    [Header("Animation")]
    [SerializeField] private float AnimationDuration = 1f;
    private bool isBuilt = false;
    private bool isVisible = true; // 순차 건설의 가시성 관리

    private Renderer[] buildingRenderers;
    private Color[] originalColors;
    private Vector3 originalScale;
    private bool isFlashingDamage = false;

    private PlayerController _player;
    public void Init()
    {
        currentHealth = maxHealth;

        _player = GameManager.Instance.m_Player;

        InitializeDamageEffect();

        SetBuildState(false);

        // 가시성이 활성화된 경우에만 건설 체크 시작
        if (isVisible && this.gameObject.activeInHierarchy)
        {
            StartCoroutine(CheckAndBuildBehavior());
        }
    }

    void InitializeDamageEffect()
    {
        // mineOBJ 하위의 모든 렌더러 수집
        if (enhanceOBJ != null)
        {
            buildingRenderers = enhanceOBJ.GetComponentsInChildren<Renderer>();
            originalColors = new Color[buildingRenderers.Length];

            for (int i = 0; i < buildingRenderers.Length; i++)
            {
                if (buildingRenderers[i].material != null)
                {
                    originalColors[i] = buildingRenderers[i].material.color;
                }
            }

            // 원본 크기 저장
            originalScale = enhanceOBJ.transform.localScale;
        }
    }

    void SetBuildState(bool built)
    {
        isBuilt = built;

        if (enhanceOBJ != null)
            enhanceOBJ.SetActive(built);

        if (outLine != null)
            outLine.SetActive(!built && isVisible); // 가시성도 고려

        if (coin != null)
            coin.SetActive(!built && isVisible); // 가시성도 고려

        // 건설 완료 시 올라오는 애니메이션 및 채굴 시작
        if (built)
        {
            StartCoroutine(EnhanceRiseAnimation());
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

            // 아직 건설되지 않았고 건설 체크가 실행 중이 아니라면 시작
            if (!isBuilt)
            {
                StartCoroutine(CheckAndBuildBehavior());
            }

            Debug.Log($"Enhance {gameObject.name} is now visible and available for construction");
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

            Debug.Log($"Enhance {gameObject.name} is now hidden");
        }
    }

    IEnumerator EnhanceRiseAnimation()
    {
        if (enhanceOBJ == null) yield break;

        //TODO 2025-11-26
        //소현님 0.5f정도 대기
        Vector3 originalenhnaceScale = enhanceOBJ.transform.localScale;

        enhanceOBJ.transform.localScale = Vector3.zero;

        yield return new WaitForSeconds(0.5f);

        EffectController _effect = ObjectPool.Instance.SpawnFromPool("Effect", this.transform.position, Quaternion.identity, ObjectPool.Instance.transform).GetComponent<EffectController>();
        if (_effect)
            _effect.Init(EffectType.Building, this.transform.position.x, this.transform.position.z);

        if (float.IsNaN(originalenhnaceScale.x) || float.IsNaN(originalenhnaceScale.y) || float.IsNaN(originalenhnaceScale.z))
        {
            originalenhnaceScale = Vector3.one;
        }


        // 광산을 땅 아래로 이동하고 작은 크기로 설정
        enhanceOBJ.transform.localScale = originalenhnaceScale * 0.3f;

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
            Vector3 currentEnhanceScale = originalenhnaceScale * scaleMultiplier;


            if (!float.IsNaN(currentEnhanceScale.x) && !float.IsNaN(currentEnhanceScale.y) && !float.IsNaN(currentEnhanceScale.z))
            {
                enhanceOBJ.transform.localScale = currentEnhanceScale;
            }

            yield return null;
        }

        if (!float.IsNaN(originalenhnaceScale.x) && !float.IsNaN(originalenhnaceScale.y) && !float.IsNaN(originalenhnaceScale.z))
        {
            enhanceOBJ.transform.localScale = originalenhnaceScale;
        }

        // 건설 완료 알림을 GameManager에 전송
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnhanceBuilt(this);
        }
        //TODO 여기에 기능 추가
        //2025-11-27
        //NPC 플레이어처럼
    }
    IEnumerator CheckAndBuildBehavior()
    {
        while (!isBuilt && isVisible) // 가시성 체크 추가
        {
            if (_player != null)
            {
                float distance = Vector3.Distance(_player.transform.position, transform.position);
                if (distance <= ditectDistance) // 건설 범위
                {
                    // 플레이어 골드를 가지고 있고 아직 건설이 완료되지 않은 경우
                    if (GameManager.Instance.GetCurrentGold() > 0 && currPaidCost < enhanceCost)
                    {
                        SendGoldToEnhance();
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    void SendGoldToEnhance()
    {
        if (GameManager.Instance.SpendGold(1))
        {
            // 플레이어로부터 골드를 시각적으로 보내는 메서드 호출
            _player.OnSendGoldToTurret(transform); // 기존 메서드 재사용

            currPaidCost++;
            if (TxtGold != null)
            {
                int _remainGold = enhanceCost - currPaidCost;
                TxtGold.text = $"{_remainGold}";
            }
            Debug.Log($"enhanceCost received gold: {currPaidCost}/{enhanceCost}");

            // 목표 비용에 도달하면 건설 완료
            if (currPaidCost >= enhanceCost)
            {
                BuildEnhance();
            }
        }
    }
    void BuildEnhance()
    {
        Debug.Log($"Enhance {gameObject.name} construction completed!");
        SetBuildState(true);
    }

    public bool IsBuilt() => isBuilt;

    void StartDamageFlashEffect()
    {
        if (!isFlashingDamage)
        {
            StartCoroutine(DamageFlashEffect());
        }
    }
    IEnumerator DamageFlashEffect()
    {
        if (buildingRenderers == null || enhanceOBJ == null) yield break;

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
        enhanceOBJ.transform.localScale = scaledSize;

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

            enhanceOBJ.transform.localScale = Vector3.Lerp(scaledSize, originalScale, progress);
            yield return null;
        }

        // 6. 정확한 원본 크기로 복귀
        enhanceOBJ.transform.localScale = originalScale;
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
        // 광산 파괴
        DestroyEnhance();
    }

    void DestroyEnhance()
    {
        // 파괴 이펙트 재생 (옵션)
        gameObject.SetActive(false);

        // 또는 ObjectPool로 반환 (풀링 사용 시)
        // ObjectPool.Instance.ReturnToPool(gameObject);
    }
    #endregion
}
