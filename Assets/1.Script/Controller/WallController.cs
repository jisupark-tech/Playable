using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallController : MonoBehaviour, IHealth
{
    [Header("Wall Requirements")]
    public TurretController[] requiredTurrets; // 건설에 필요한 터렛들
    public bool haveGate = false; // 문이 있는 벽인지

    [Header("Wall Objects - Wood (Pre-Upgrade)")]
    public GameObject gateWall_Wood; // 문이 있는 벽 (나무)
    public GameObject noneGateWall_Wood; // 일반 벽 (나무)

    [Header("Wall Objects - Upgraded")]
    public GameObject gateWall; // 문이 있는 벽 (업그레이드 후)
    public GameObject noneGateWall; // 일반 벽 (업그레이드 후)

    [Header("Gate Control")]
    public Transform Gate_L_Parent; // 왼쪽 문짝 부모 오브젝트
    public Transform Gate_R_Parent; // 오른쪽 문짝 부모 오브젝트
    public float gateDetectionRange = 3f; // 플레이어 감지 범위
    public float gateOpenSpeed = 2f; // 문 열리는 속도
    public LayerMask playerLayerMask = -1; // 플레이어 레이어 (기본: 모든 레이어)

    [Header("Health Settings")]
    public int maxHealth = 150; // 터렛보다 높은 체력
    private int currentHealth;

    [Header("HP Display (SpriteRenderer)")]
    public Transform hpBarParent; // HP 바 부모 오브젝트
    public SpriteRenderer hpBackgroundRenderer; // HP 배경 스프라이트 (HPBack)
    public SpriteRenderer hpFillRenderer; // HP 채우기 스프라이트 (HP)
    public Sprite hpBackgroundSprite; // HP 배경 스프라이트
    public Sprite hpFillSprite; // HP 채우기 스프라이트
    public Vector3 hpBarOffset = new Vector3(0, 3f, 0); // 벽 위 HP 바 위치
    public Vector2 hpBarSize = new Vector2(2f, 0.3f); // HP 바 크기 (World Space)
    public Color hpFullColor = Color.green; // 체력 100% 색상
    public Color hpLowColor = Color.red; // 체력 낮을 때 색상

    [Header("Animation Settings")]
    public float riseAnimationDuration = 2f; // 상승 애니메이션 시간
    public float upgradeAnimationDuration = 2f; // 업그레이드 애니메이션 시간
    public AnimationCurve riseAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve upgradeAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Debug")]
    public bool enableDebugLogs = true; // 디버그 로그 활성화

    // 내부 변수들
    private bool isBuilt = false;

    // 피격 연출 관련 변수
    [Header("Visual Effects")]
    [SerializeField] private float damageFlashDuration = 0.15f;
    [SerializeField] private float damageScaleDuration = 0.2f;
    [SerializeField] private float damageScaleMultiplier = 1.1f;
    private Renderer[] buildingRenderers;
    private Color[] originalColors;
    private Vector3 originalScale;
    private bool isFlashingDamage = false;
    private bool isUpgraded = false;
    private GameObject activeWall;
    private MainCenterController mainCenterController;

    [Header("Wall Boundary Gate")]
    public Vector2 boundaryLeftMin = new Vector2(-15f, -15f); // 최소 경계
    public Vector2 boundaryLeftMax = new Vector2(15f, 15f);   // 최대 경계
    public Vector2 boundaryRightMin = new Vector2(-15f, -15f); // 최소 경계
    public Vector2 boundaryRightMax = new Vector2(15f, 15f);   // 최대 경계
    [Header("Wall Boundary No Gate")]
    public Vector2 boundaryCenterMin = new Vector2(-15f, -15f); // 최소 경계
    public Vector2 boundaryCenterMax = new Vector2(15f, 15f);   // 최대 경계
    // 문 관련 변수들
    private bool isGateOpen = false;
    private bool isPlayerNearby = false;
    private Vector3 Gate_L_ClosedPos;
    private Vector3 Gate_R_ClosedPos;
    private Vector3 Gate_L_OpenPos;
    private Vector3 Gate_R_OpenPos;

    //=====================================================
    // 초기화
    //=====================================================
    public void Init()
    {
        // MainCenterController 찾기
        if (GameManager.Instance != null)
        {
            mainCenterController = GameManager.Instance.GetMainCenter();
        }
        if (mainCenterController == null)
        {
            mainCenterController = FindObjectOfType<MainCenterController>();
        }

        if (gateWall_Wood != null && gateWall_Wood.activeSelf) gateWall_Wood.SetActive(false);
        if (noneGateWall_Wood != null && noneGateWall_Wood.activeSelf) noneGateWall_Wood.SetActive(false);
        if (gateWall != null && gateWall.activeSelf) gateWall.SetActive(false);
        if (noneGateWall != null && noneGateWall.activeSelf) noneGateWall.SetActive(false);


        // 체력 초기화
        currentHealth = maxHealth;

        // HP 바 초기화
        InitializeHealthBar();

        // 피격
        InitializeDamageEffect();
        // 문 시스템 초기화
        if (haveGate)
        {
            InitializeGatePositions();
        }

        // 건설 조건 체크 시작
        StartCoroutine(CheckBuildCondition());

        DebugLog("Wall initialization completed");
    }

    void InitializeDamageEffect()
    {
        if(activeWall != null)
        {
            buildingRenderers = activeWall.GetComponentsInChildren<Renderer>();

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

    void InitializeHealthBar()
    {
        if (hpBarParent == null)
        {
            // HP 바 부모 오브젝트가 없으면 자동 생성
            GameObject hpBarObj = new GameObject("HPBar");
            hpBarObj.transform.SetParent(transform);
            hpBarObj.transform.localPosition = hpBarOffset;
            hpBarParent = hpBarObj.transform;
        }

        // HP 배경 설정
        if (hpBackgroundRenderer == null)
        {
            GameObject hpBg = new GameObject("HPBackground");
            hpBg.transform.SetParent(hpBarParent);
            hpBg.transform.localPosition = Vector3.zero;
            hpBackgroundRenderer = hpBg.AddComponent<SpriteRenderer>();
        }

        // HP 채우기 설정
        if (hpFillRenderer == null)
        {
            GameObject hpFill = new GameObject("HPFill");
            hpFill.transform.SetParent(hpBarParent);
            hpFill.transform.localPosition = Vector3.zero;
            hpFillRenderer = hpFill.AddComponent<SpriteRenderer>();
        }

        // 스프라이트 설정
        if (hpBackgroundSprite != null)
            hpBackgroundRenderer.sprite = hpBackgroundSprite;
        if (hpFillSprite != null)
            hpFillRenderer.sprite = hpFillSprite;

        // 크기 설정
        hpBackgroundRenderer.size = hpBarSize;
        hpFillRenderer.size = hpBarSize;

        // 정렬 순서
        hpBackgroundRenderer.sortingOrder = 1;
        hpFillRenderer.sortingOrder = 2;

        // 처음에는 HP 바 숨김
        if (hpBarParent != null)
            hpBarParent.gameObject.SetActive(false);

        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (hpFillRenderer == null) return;

        float healthRatio = (float)currentHealth / maxHealth;

        // HP 바 크기 조정 (왼쪽에서부터 채워지도록)
        Vector2 fillSize = hpBarSize;
        fillSize.x *= healthRatio;
        hpFillRenderer.size = fillSize;

        // 위치 조정 (중앙 정렬 유지)
        Vector3 fillPos = Vector3.zero;
        fillPos.x = -(hpBarSize.x - fillSize.x) * 0.5f;
        hpFillRenderer.transform.localPosition = fillPos;

        // 색상 변경
        Color targetColor = Color.Lerp(hpLowColor, hpFullColor, healthRatio);
        hpFillRenderer.color = targetColor;
    }

    void InitializeGatePositions()
    {
        if (Gate_L_Parent != null && Gate_R_Parent != null)
        {
            // 초기 위치 저장 (닫힌 상태)
            Gate_L_ClosedPos = Gate_L_Parent.localPosition;
            Gate_R_ClosedPos = Gate_R_Parent.localPosition;

            // 열린 위치 계산 (왼쪽은 더 왼쪽으로, 오른쪽은 더 오른쪽으로)
            Gate_L_OpenPos = Gate_L_ClosedPos + Vector3.left * 1f;
            Gate_R_OpenPos = Gate_R_ClosedPos + Vector3.right * 1f;

            DebugLog("Gate positions initialized");
        }
    }

    //=====================================================
    // 건설 조건 체크 및 건설 로직
    //=====================================================
    IEnumerator CheckBuildCondition()
    {
        while (!isBuilt)
        {
            bool canBuild = CanBuildWall();

            if (canBuild)
            {
                ShowWall(); // 건물 자체를 표시
            }
            else
            {
                HideWall(); // 건물 자체를 숨김
            }

            yield return new WaitForSeconds(0.5f); // 0.5초마다 체크
        }
    }

    bool CanBuildWall()
    {
        if (isBuilt) return false;

        // 1. Phase Building System이 활성화된 경우
        if (IsInPhaseBuilding())
        {
            // 1-1. 이 벽이 현재 Phase에 포함되지 않으면 건설 불가
            //if (!IsWallInCurrentPhase())
            //{
            //    DebugLog("Wall not in current phase - cannot build");
            //    return false;
            //}

            // 1-2. Phase에서 벽 조건을 무시하도록 설정된 경우
            if (ShouldIgnoreRequirements())
            {
                DebugLog("Phase Building mode: Ignoring wall requirements - can build");
                return true;
            }

            // 1-3. Phase에서 벽 조건을 유지하도록 설정된 경우 → requiredTurrets 확인
            DebugLog("Phase Building mode: Checking required turrets");
            return AreAllRequiredTurretsBuilt();
        }

        // 2. Phase Building이 비활성화된 경우 (기존 방식)
        DebugLog("Sequential Building mode: Checking required turrets");
        return AreAllRequiredTurretsBuilt();
    }
    bool IsInPhaseBuilding()
    {
        // GameManager의 Phase Building System이 활성화되어 있는지 확인
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.IsPhaseBuilding();
    }
    bool IsWallInCurrentPhase()
    {
        if (GameManager.Instance == null) return false;

        int wallIndex = GetWallIndex();
        if (wallIndex < 0) return false;

        // Building Rules에 의해 비활성화된 경우 체크
        if (IsWallDisabledByRules(wallIndex))
        {
            DebugLog($"Wall[{wallIndex}] disabled by Building Rules");
            return false;
        }

        int currentPhaseIndex = GetCurrentPhaseIndex();
        if (currentPhaseIndex < 0) return false;

        var phases = GameManager.Instance.buildingPhases;
        if (currentPhaseIndex >= phases.Count) return false;

        var currentPhase = phases[currentPhaseIndex];
        bool isInPhase = currentPhase.wallIndices.Contains(wallIndex);

        DebugLog($"Wall[{wallIndex}] in Phase[{currentPhaseIndex}]: {isInPhase}");
        return isInPhase;
    }

    bool IsWallDisabledByRules(int wallIndex)
    {
        // 간단한 방법: GameObject가 비활성화되어 있으면 Rules에 의해 비활성화된 것으로 간주
        // 더 정교한 방법이 필요하면 GameManager에 IsWallDisabled(int index) API 추가
        return !gameObject.activeInHierarchy;
    }

    bool ShouldIgnoreRequirements()
    {
        // 현재 Phase에서 이 벽이 포함되어 있고 ignoreWallRequirements가 true인지 확인
        if (GameManager.Instance == null) return false;

        int wallIndex = GetWallIndex();
        if (wallIndex >= 0)
        {
            return GameManager.Instance.ShouldIgnoreWallRequirements(wallIndex);
        }

        return false;
    }

    int GetCurrentPhaseIndex()
    {
        // GameManager의 public API 사용
        if (GameManager.Instance == null) return -1;
        return GameManager.Instance.GetCurrentPhaseIndex();
    }

    int GetWallIndex()
    {
        if (GameManager.Instance == null) return -1;
        return GameManager.Instance.GetWalls().IndexOf(this);
    }

    bool AreAllRequiredTurretsBuilt()
    {
        if (requiredTurrets == null || requiredTurrets.Length == 0)
        {
            DebugLog("No required turrets specified - wall can be built");
            return true;
        }

        bool allBuilt = true;

        foreach (TurretController turret in requiredTurrets)
        {
            if (turret == null)
            {
                DebugLog("Warning: Null turret reference in required turrets");
                continue;
            }

            if (!turret.IsBuilt())
            {
                DebugLog($"Required turret {turret.name} not built yet");
                allBuilt = false;
                break;
            }
        }

        if (allBuilt)
        {
            DebugLog("All required turrets are built!");
        }

        return allBuilt;
    }

    void ShowWall()
    {
        if (isBuilt) return;

        // 조건이 만족되면 즉시 건설 시작
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
            DebugLog("Wall shown and construction started");
            BuildWall();
        }
    }

    void HideWall()
    {
        if (!isBuilt && gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            DebugLog("Wall hidden - conditions not met");
        }
    }

    void BuildWall()
    {
        if (isBuilt)
        {
            DebugLog("이미 건설된 벽입니다");
            return;
        }

        DebugLog("벽 건설 시작!");

        // 메인 센터 업그레이드 상태 확인
        bool shouldUpgrade = mainCenterController != null && mainCenterController.IsUpgraded();

        // 적절한 벽 선택
        GameObject wallToActivate = GetWallToActivate(shouldUpgrade);

        if (wallToActivate != null)
        {
            activeWall = wallToActivate;
            isUpgraded = shouldUpgrade;

            DebugLog($"벽 건설: {activeWall.name} (업그레이드 상태: {isUpgraded})");

            StartCoroutine(BuildWallAnimation(activeWall));
        }
        else
        {
            DebugLog("활성화할 벽을 찾을 수 없습니다!");
        }
    }

    GameObject GetWallToActivate(bool upgraded)
    {
        if (upgraded)
        {
            // 업그레이드된 버전 사용
            return haveGate ? gateWall : noneGateWall;
        }
        else
        {
            // 기본 (나무) 버전 사용
            return haveGate ? gateWall_Wood : noneGateWall_Wood;
        }
    }

    IEnumerator BuildWallAnimation(GameObject wallObj)
    {
        //TODO 2025-11-26
        //소현님 0.5f정도 대기

        yield return new WaitForSeconds(0.5f);

        // 벽 오브젝트 활성화
        wallObj.SetActive(true);

        // 원본 위치와 크기 (안전한 값 확인)
        //Vector3 originalPosition = wallObj.transform.localPosition;
        Vector3 originalScale = wallObj.transform.localScale;


        if (float.IsNaN(originalScale.x) || float.IsNaN(originalScale.y) || float.IsNaN(originalScale.z))
        {
            originalScale = Vector3.one;
        }

        wallObj.transform.localScale = originalScale * 0.3f;

        float _animationDuration = riseAnimationDuration; // 1초로 설정 (기존 riseAnimationDuration 대신)
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
            Vector3 currentScale = originalScale * scaleMultiplier;


            if (!float.IsNaN(currentScale.x) && !float.IsNaN(currentScale.y) && !float.IsNaN(currentScale.z))
            {
                wallObj.transform.localScale = currentScale;
            }

            yield return null;
        }


        if (!float.IsNaN(originalScale.x) && !float.IsNaN(originalScale.y) && !float.IsNaN(originalScale.z))
        {
            wallObj.transform.localScale = originalScale;
        }

        // 건설 완료 처리
        isBuilt = true;

        // GameManager에 벽 건설 완료 알림
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWallBuilt(this);
            DebugLog("Notified GameManager of wall construction completion");
        }

        // HP 바 표시
        if (hpBarParent != null)
            hpBarParent.gameObject.SetActive(true);

        // 문 시스템 초기화 (문이 있는 벽이면)
        if (haveGate)
        {
            UpdateGateReferences();
            StartCoroutine(GateControlCoroutine());
        }

        DebugLog($"벽 건설 완료! {wallObj.name}");
    }

    //=====================================================
    // 업그레이드 시스템 (GameManager 호환성)
    //=====================================================

    // GameManager가 호출하는 메소드 - 기존 UpgradeWall을 StartUpgrade로 래핑
    public void StartUpgrade()
    {
        UpgradeWall();
    }

    // MainCenterController에서 호출되는 업그레이드 메소드
    public void UpgradeWall()
    {
        if (!isBuilt || isUpgraded)
        {
            DebugLog($"벽 업그레이드 불가 - 건설됨: {isBuilt}, 이미 업그레이드됨: {isUpgraded}");
            return;
        }

        DebugLog("벽 업그레이드 시작!");
        StartCoroutine(WallUpgradeAnimation());
    }

    IEnumerator WallUpgradeAnimation()
    {
        // 새로운 업그레이드된 벽 선택
        GameObject newWall = GetWallToActivate(true);

        if (newWall == null)
        {
            DebugLog("업그레이드할 벽을 찾을 수 없습니다!");
            yield break;
        }

        // 1. 기존 벽이 내려가는 애니메이션
        yield return StartCoroutine(WallDownAnimation());

        // 2. 벽 교체
        if (activeWall != null)
            activeWall.SetActive(false);

        activeWall = newWall;
        activeWall.SetActive(true);

        // 3. 새 벽이 올라오는 애니메이션
        yield return StartCoroutine(WallUpAnimation());

        // 4. 업그레이드 완료 처리
        isUpgraded = true;

        // GameManager에 벽 업그레이드 완료 알림 (업그레이드도 하나의 건설 완료로 간주)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWallBuilt(this);
            DebugLog("Notified GameManager of wall upgrade completion");
        }

        // 문 시스템 업데이트
        if (haveGate)
        {
            UpdateGateReferences();
        }

        DebugLog($"벽 업그레이드 완료! 새 벽: {activeWall.name}");
    }

    IEnumerator WallDownAnimation()
    {
        if (activeWall == null) yield break;

        Vector3 originalPosition = activeWall.transform.localPosition;
        Vector3 hiddenPosition = originalPosition - Vector3.up * 3f;

        float elapsedTime = 0f;
        while (elapsedTime < upgradeAnimationDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (upgradeAnimationDuration * 0.5f);
            float curveValue = upgradeAnimationCurve.Evaluate(t);

            activeWall.transform.localPosition = Vector3.Lerp(originalPosition, hiddenPosition, curveValue);
            yield return null;
        }

        activeWall.transform.localPosition = hiddenPosition;
    }

    IEnumerator WallUpAnimation()
    {
        if (activeWall == null) yield break;

        Vector3 originalPosition = Vector3.zero; // activeWall의 원래 위치
        Vector3 hiddenPosition = originalPosition - Vector3.up * 3f;

        activeWall.transform.localPosition = hiddenPosition;

        float elapsedTime = 0f;
        while (elapsedTime < upgradeAnimationDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (upgradeAnimationDuration * 0.5f);
            float curveValue = upgradeAnimationCurve.Evaluate(t);

            activeWall.transform.localPosition = Vector3.Lerp(hiddenPosition, originalPosition, curveValue);
            yield return null;
        }

        activeWall.transform.localPosition = originalPosition;
    }

    void UpdateGateReferences()
    {
        if (!haveGate || activeWall == null) return;

        // 활성 벽에서 문짝들을 다시 찾기
        //Transform[] childTransforms = activeWall.GetComponentsInChildren<Transform>();

        //foreach (Transform child in childTransforms)
        //{
        //    if (child.name.Contains("Gate_L") || child.name.Contains("Left"))
        //    {
        //        Gate_L_Parent = child;
        //    }
        //    else if (child.name.Contains("Gate_R") || child.name.Contains("Right"))
        //    {
        //        Gate_R_Parent = child;
        //    }
        //}

        // 문 위치 다시 초기화
        if (Gate_L_Parent != null && Gate_R_Parent != null)
        {
            InitializeGatePositions();
            DebugLog("Gate references updated after upgrade");
        }
    }

    //=====================================================
    // 문 제어 시스템
    //=====================================================
    IEnumerator GateControlCoroutine()
    {
        while (isBuilt && haveGate)
        {
            CheckPlayerNearby();
            UpdateGateState();
            yield return new WaitForSeconds(0.1f);
        }
    }

    void CheckPlayerNearby()
    {
        if (GameManager.Instance == null || GameManager.Instance.m_Player == null)
            return;

        float distance = Vector3.Distance(transform.position, GameManager.Instance.m_Player.transform.position);
        isPlayerNearby = distance <= gateDetectionRange;
    }

    void UpdateGateState()
    {
        bool shouldOpen = isPlayerNearby;

        if (shouldOpen && !isGateOpen)
        {
            StartCoroutine(OpenGate());
        }
        else if (!shouldOpen && isGateOpen)
        {
            StartCoroutine(CloseGate());
        }
    }

    //TODO X축으로 이동만 하면됨.
    //수정 요망
    //기능은 작동하지만, 수치가 잘못된듯
    IEnumerator OpenGate()
    {
        isGateOpen = true;
        DebugLog("Opening gate");

        float elapsedTime = 0f;
        Vector3 startL = Gate_L_Parent.localPosition;
        Vector3 startR = Gate_R_Parent.localPosition;

        while (elapsedTime < 1f / gateOpenSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime * gateOpenSpeed;

            Gate_L_Parent.localPosition = Vector3.Lerp(startL, Gate_L_OpenPos, t);
            Gate_R_Parent.localPosition = Vector3.Lerp(startR, Gate_R_OpenPos, t);

            yield return null;
        }

        Gate_L_Parent.localPosition = Gate_L_OpenPos;
        Gate_R_Parent.localPosition = Gate_R_OpenPos;
    }

    IEnumerator CloseGate()
    {
        isGateOpen = false;
        DebugLog("Closing gate");

        float elapsedTime = 0f;
        Vector3 startL = Gate_L_Parent.localPosition;
        Vector3 startR = Gate_R_Parent.localPosition;

        while (elapsedTime < 1f / gateOpenSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime * gateOpenSpeed;

            Gate_L_Parent.localPosition = Vector3.Lerp(startL, Gate_L_ClosedPos, t);
            Gate_R_Parent.localPosition = Vector3.Lerp(startR, Gate_R_ClosedPos, t);

            yield return null;
        }

        Gate_L_Parent.localPosition = Gate_L_ClosedPos;
        Gate_R_Parent.localPosition = Gate_R_ClosedPos;
    }

    //=====================================================
    // 조건 재체크 (GameManager에서 호출)
    //=====================================================
    public void RecheckCondition()
    {
        if (!isBuilt)
        {
            DebugLog("Rechecking wall build condition");

            bool canBuild = CanBuildWall();
            if (canBuild)
            {
                ShowWall();
            }
            else
            {
                HideWall();
            }
        }
    }

    //=====================================================
    // IHealth 인터페이스 구현
    //=====================================================
    public void TakeDamage(int damage)
    {
        if (!isBuilt) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();

        // OnHealthChanged 이벤트 호출
        OnHealthChanged(currentHealth, maxHealth);

        StartDamageFlashEffect();

        DebugLog($"Wall took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            OnDeath();
            DestroyWall();
        }
    }

    public void Heal(int amount)
    {
        if (!isBuilt) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthBar();

        // OnHealthChanged 이벤트 호출
        OnHealthChanged(currentHealth, maxHealth);

        StartDamageFlashEffect();

        DebugLog($"Wall healed {amount}. Health: {currentHealth}/{maxHealth}");
    }
    void StartDamageFlashEffect()
    {
        if (!isFlashingDamage)
        {
            StartCoroutine(DamageFlashEffect());
        }
    }

    IEnumerator DamageFlashEffect()
    {
        if (buildingRenderers == null || activeWall == null) yield break;

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
        activeWall.transform.localScale = scaledSize;

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

            activeWall.transform.localScale = Vector3.Lerp(scaledSize, originalScale, progress);
            yield return null;
        }

        // 6. 정확한 원본 크기로 복귀
        activeWall.transform.localScale = originalScale;
        isFlashingDamage = false;
    }
    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public void OnHealthChanged(int currentHealth, int maxHealth)
    {
        // 체력 변화 시 호출되는 이벤트
        // UI 업데이트나 다른 시스템에 알림을 보낼 수 있음
        DebugLog($"Health changed: {currentHealth}/{maxHealth} ({GetHealthRatio():F2})");
    }

    public void OnDeath()
    {
        // 죽을 때 호출되는 이벤트
        DebugLog("Wall has been destroyed!");

        // 추가 사망 처리 로직을 여기에 넣을 수 있음
        // 예: 파티클 이펙트, 사운드, 다른 오브젝트들에 알림 등
    }

    public float GetHealthRatio()
    {
        return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }

    void DestroyWall()
    {
        DebugLog("Wall destroyed!");

        // 활성 벽 비활성화
        if (activeWall != null)
            activeWall.SetActive(false);

        // HP 바 숨김
        if (hpBarParent != null)
            hpBarParent.gameObject.SetActive(false);

        // 상태 초기화
        isBuilt = false;
        isUpgraded = false;
        activeWall = null;
        currentHealth = maxHealth;

        // 건설 조건 다시 체크 시작
        StartCoroutine(CheckBuildCondition());
    }

    //=====================================================
    // Public API (GameManager 호환성)
    //=====================================================
    public bool IsBuilt()
    {
        return isBuilt;
    }
    public bool IsHasGate()
    {
        return haveGate;
    }
    public bool IsUpgraded()
    {
        return isUpgraded;
    }
    public bool ClampToBoundary(Vector3 _pos, bool _hasGate =false, bool _isplayer =true)
    {
        if (_hasGate)
        {
            bool _leftX = IsInRange(_pos.x, transform.position.x + boundaryLeftMin.x, transform.position.x + boundaryLeftMax.x);
            bool _leftY = IsInRange(_pos.z, transform.position.z + boundaryLeftMin.y, transform.position.z + boundaryLeftMax.y);
            bool _centerX = IsInRange(_pos.x, transform.position.x + boundaryCenterMin.x, transform.position.x + boundaryCenterMax.x);
            bool _centerY = IsInRange(_pos.z, transform.position.z + boundaryCenterMin.y, transform.position.z + boundaryCenterMax.y);
            bool _rightX = IsInRange(_pos.x, transform.position.x + boundaryRightMin.x, transform.position.x + boundaryRightMax.x);
            bool _rightY = IsInRange(_pos.z, transform.position.z + boundaryRightMin.y, transform.position.z + boundaryRightMax.y);

            if (_isplayer)
            {
                return (_rightX && _rightY) || (_leftX && _leftY) || (_centerX && _centerY);
            }
            else
            {
                return (_rightX && _rightY) || (_leftX && _leftY);
            }  
        }
        else
        {
            bool _noneGateX = IsInRange(_pos.x, transform.position.x + boundaryCenterMin.x, transform.position.x + boundaryCenterMax.x);
            bool _noneGateY = IsInRange(_pos.z, transform.position.z + boundaryCenterMin.y, transform.position.z + boundaryCenterMax.y);
            return (_noneGateX && _noneGateY);
        }
    }
    //=====================================================
    // Helper
    //=====================================================
    public bool IsInRange(float _target, float _min, float _max)
    {
        return _min <= _target && _target <= _max;
    }
    //=====================================================
    // 디버그 및 Gizmos
    //=====================================================
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] {message}");
        }
    }

    void OnDrawGizmosSelected()
    {
        // 문 감지 범위 표시
        if (haveGate)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, gateDetectionRange);
        }

        // HP 바 위치 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + hpBarOffset, new Vector3(hpBarSize.x, hpBarSize.y, 0.1f));

        Gizmos.color = Color.red;
        Vector3 LeftWallbottomLeft = new Vector3(boundaryLeftMin.x, transform.position.y, boundaryLeftMin.y) + transform.position;
        Vector3 LeftWallbottomRight = new Vector3(boundaryLeftMax.x, transform.position.y, boundaryLeftMin.y) + transform.position;
        Vector3 LeftWalltopLeft = new Vector3(boundaryLeftMin.x, transform.position.y, boundaryLeftMax.y) + transform.position;
        Vector3 LeftWalltopRight = new Vector3(boundaryLeftMax.x, transform.position.y, boundaryLeftMax.y) + transform.position;

        Gizmos.DrawLine(LeftWallbottomLeft, LeftWallbottomRight);
        Gizmos.DrawLine(LeftWallbottomRight, LeftWalltopRight);
        Gizmos.DrawLine(LeftWalltopRight, LeftWalltopLeft);
        Gizmos.DrawLine(LeftWalltopLeft, LeftWallbottomLeft);

        Gizmos.color = Color.red;
        Vector3 RightWallbottomLeft = new Vector3(boundaryRightMin.x, transform.position.y, boundaryRightMin.y) + transform.position;
        Vector3 RightWallbottomRight = new Vector3(boundaryRightMax.x, transform.position.y, boundaryRightMin.y) + transform.position;
        Vector3 RightWalltopLeft = new Vector3(boundaryRightMin.x, transform.position.y, boundaryRightMax.y) + transform.position;
        Vector3 RightWalltopRight = new Vector3(boundaryRightMax.x, transform.position.y, boundaryRightMax.y) + transform.position;

        Gizmos.DrawLine(RightWallbottomLeft, RightWallbottomRight);
        Gizmos.DrawLine(RightWallbottomRight, RightWalltopRight);
        Gizmos.DrawLine(RightWalltopRight, RightWalltopLeft);
        Gizmos.DrawLine(RightWalltopLeft, RightWallbottomLeft);

        Gizmos.color = Color.yellow;
        Vector3 CenterWallbottomLeft = new Vector3(boundaryCenterMin.x, transform.position.y, boundaryCenterMin.y) + transform.position;
        Vector3 CenterWallbottomRight = new Vector3(boundaryCenterMax.x, transform.position.y, boundaryCenterMin.y) + transform.position;
        Vector3 CenterWalltopLeft = new Vector3(boundaryCenterMin.x, transform.position.y, boundaryCenterMax.y) + transform.position;
        Vector3 CenterWalltopRight = new Vector3(boundaryCenterMax.x, transform.position.y, boundaryCenterMax.y) + transform.position;

        Gizmos.DrawLine(CenterWallbottomLeft, CenterWallbottomRight);
        Gizmos.DrawLine(CenterWallbottomRight, CenterWalltopRight);
        Gizmos.DrawLine(CenterWalltopRight, CenterWalltopLeft);
        Gizmos.DrawLine(CenterWalltopLeft, CenterWallbottomLeft);
    }
}