using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VirtualPad : MonoBehaviour
{
    [Header("Pad Settings")]
    public RectTransform padTransform;     // Pad 이미지 (조작용)
    public float moveRange = 100f;         // 패드 이동 범위
    public float returnSpeed = 5f;         // 패드 복귀 속도

    [Header("Dynamic Pad Settings")]
    public bool enableDynamicPosition = true;  // 동적 위치 생성 활성화
    public float padFadeSpeed = 10f;           // 패드 나타나기/사라지기 속도

    [Header("Auto Setup")]
    private RectTransform bgTransform;         // BG 이미지 (흰 원 배경)
    private Vector2 initialPadPosition;        // 패드의 초기 위치
    private Vector2 initialBgPosition;         // BG의 초기 위치
    private Vector2 dynamicPadCenter;          // 동적 패드의 중심 위치
    private Vector2 inputDirection = Vector2.zero;
    private Canvas parentCanvas;
    private bool isDragging = false;
    private bool isActive = false;             // 패드 활성화 상태

    // 터치 감지용 투명 오버레이
    private GameObject touchOverlay;
    private RectTransform touchOverlayRect;
    private PadEventHandler padEventHandler;

    // UI 컴포넌트들 (VirtualPad 전체 제어용)
    private CanvasGroup virtualPadCanvasGroup;

    void Awake()
    {
        // 자동으로 자식 오브젝트들 찾기
        if (padTransform == null)
        {
            Transform padChild = transform.Find("Pad");
            if (padChild != null)
                padTransform = padChild.GetComponent<RectTransform>();
        }

        Transform bgChild = transform.Find("BG");
        if (bgChild != null)
            bgTransform = bgChild.GetComponent<RectTransform>();

        // VirtualPad 전체에 CanvasGroup 설정 (BG와 Pad 함께 제어)
        SetupCanvasGroup();
    }

    void SetupCanvasGroup()
    {
        // VirtualPad 전체에 CanvasGroup 추가
        virtualPadCanvasGroup = GetComponent<CanvasGroup>();
        if (virtualPadCanvasGroup == null)
        {
            virtualPadCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        // 초기 위치들 저장 (BG와 Pad 모두)
        if (padTransform != null)
            initialPadPosition = padTransform.anchoredPosition;

        if (bgTransform != null)
            initialBgPosition = bgTransform.anchoredPosition;

        // 처음에는 패드 전체를 숨김
        SetPadVisibility(false, true);

        // 전체 화면 터치 감지용 오버레이 생성
        CreateTouchOverlay();
    }

    /// <summary>
    /// 전체 화면 터치 감지용 투명 오버레이 생성
    /// </summary>
    void CreateTouchOverlay()
    {
        // 터치 감지용 투명 오버레이 생성
        touchOverlay = new GameObject("TouchOverlay");
        touchOverlay.transform.SetParent(parentCanvas.transform, false);

        // RectTransform 설정 (전체 화면)
        touchOverlayRect = touchOverlay.AddComponent<RectTransform>();
        touchOverlayRect.anchorMin = Vector2.zero;
        touchOverlayRect.anchorMax = Vector2.one;
        touchOverlayRect.offsetMin = Vector2.zero;
        touchOverlayRect.offsetMax = Vector2.zero;

        // 완전 투명한 이미지 추가 (터치 감지용)
        Image overlayImage = touchOverlay.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0); // 완전 투명
        overlayImage.raycastTarget = true; // 터치 감지 활성화

        // 이벤트 핸들러 추가
        padEventHandler = touchOverlay.AddComponent<PadEventHandler>();
        padEventHandler.Initialize(this);

        Debug.Log("Touch overlay created for full screen touch detection");
    }

    void Update()
    {
        // 드래그 중이 아닐 때 패드를 중앙으로 복귀 (시각적 효과만)
        if (!isDragging && isActive && padTransform != null)
        {
            Vector2 targetPadPosition = enableDynamicPosition ?
                (dynamicPadCenter + (initialPadPosition - initialBgPosition)) :
                initialPadPosition;

            padTransform.anchoredPosition = Vector2.Lerp(
                padTransform.anchoredPosition,
                targetPadPosition,
                returnSpeed * Time.deltaTime
            );

            // 거의 중앙에 도달하면 완전히 목표 위치로 설정
            if (Vector2.Distance(padTransform.anchoredPosition, targetPadPosition) < 1f)
            {
                padTransform.anchoredPosition = targetPadPosition;
            }
        }

        // 패드가 비활성 상태이고 충분히 사라졌으면 완전히 숨김
        if (!isActive && virtualPadCanvasGroup != null && virtualPadCanvasGroup.alpha <= 0.01f)
        {
            SetPadVisibility(false, true);
        }
    }

    /// <summary>
    /// VirtualPad 전체의 가시성을 부드럽게 또는 즉시 설정
    /// </summary>
    void SetPadVisibility(bool visible, bool immediate = false)
    {
        if (immediate)
        {
            if (virtualPadCanvasGroup != null)
                virtualPadCanvasGroup.alpha = visible ? 1f : 0f;
        }
        else
        {
            StartCoroutine(FadePad(visible));
        }

        // 상호작용 가능 여부 설정 (BG와 Pad 함께)
        if (virtualPadCanvasGroup != null)
        {
            virtualPadCanvasGroup.interactable = visible;
            virtualPadCanvasGroup.blocksRaycasts = visible;
        }
    }

    /// <summary>
    /// VirtualPad 페이드 인/아웃 애니메이션
    /// </summary>
    System.Collections.IEnumerator FadePad(bool fadeIn)
    {
        if (virtualPadCanvasGroup == null) yield break;

        float targetAlpha = fadeIn ? 1f : 0f;
        float startAlpha = virtualPadCanvasGroup.alpha;

        float elapsedTime = 0f;
        float fadeDuration = 1f / padFadeSpeed;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeDuration;

            virtualPadCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        virtualPadCanvasGroup.alpha = targetAlpha;
    }

    // Touch Overlay 이벤트 핸들러에서 호출되는 함수들
    public void OnPadPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        isActive = true;

        if (enableDynamicPosition)
        {
            // 터치한 위치를 새로운 VirtualPad 중심으로 설정
            SetDynamicPadCenter(eventData);
        }

        // VirtualPad 전체 표시
        SetPadVisibility(true);

        // 패드 위치 업데이트
        UpdatePadPosition(eventData);

        Debug.Log("VirtualPad: Touch started at " + eventData.position);
    }

    public void OnPadPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        isActive = false;

        // 🔥 핵심: 즉시 정지 (관성 제거)
        inputDirection = Vector2.zero;

        // VirtualPad 전체 숨김
        SetPadVisibility(false);

        Debug.Log("VirtualPad: Touch ended - Input direction reset to zero");
    }

    public void OnPadDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            UpdatePadPosition(eventData);
        }
    }

    /// <summary>
    /// 동적 패드의 중심 위치를 터치 위치로 설정
    /// </summary>
    void SetDynamicPadCenter(PointerEventData eventData)
    {
        if (touchOverlayRect == null || bgTransform == null) return;

        Vector2 localPoint;

        // 터치한 화면 위치를 Canvas 로컬 좌표로 변환
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            touchOverlayRect,
            eventData.position,
            parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ? parentCanvas.worldCamera : null,
            out localPoint))
        {
            // Canvas 좌표를 VirtualPad 로컬 좌표로 변환
            Vector2 virtualPadLocalPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                eventData.position,
                parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ? parentCanvas.worldCamera : null,
                out virtualPadLocalPoint))
            {
                // 동적 패드 중심을 터치 위치로 설정
                dynamicPadCenter = virtualPadLocalPoint;

                // BG를 해당 위치로 이동
                if (bgTransform != null)
                    bgTransform.anchoredPosition = dynamicPadCenter;

                // Pad를 BG 중심에 배치
                if (padTransform != null)
                    padTransform.anchoredPosition = dynamicPadCenter + (initialPadPosition - initialBgPosition);

                Debug.Log($"Dynamic pad center set to: {dynamicPadCenter}");
            }
        }
    }

    void UpdatePadPosition(PointerEventData eventData)
    {
        if (touchOverlayRect == null || padTransform == null || !isDragging) return;

        Vector2 localPoint;

        // 전체 화면 기준으로 터치 위치 계산
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            eventData.position,
            parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ? parentCanvas.worldCamera : null,
            out localPoint))
        {
            // 패드 중심으로부터의 오프셋 계산
            Vector2 padCenter = enableDynamicPosition ? dynamicPadCenter : initialBgPosition;
            Vector2 offset = localPoint - padCenter;

            // 이동 범위 제한
            Vector2 clampedOffset = Vector2.ClampMagnitude(offset, moveRange);

            // 패드 위치 업데이트
            padTransform.anchoredPosition = padCenter + clampedOffset + (initialPadPosition - initialBgPosition);

            // 입력 방향 계산 (-1 ~ 1 범위로 정규화)
            inputDirection = clampedOffset / moveRange;
        }
    }

    // 외부에서 호출할 함수들
    public Vector2 GetInputDirection()
    {
        return inputDirection;
    }

    public float GetInputMagnitude()
    {
        return inputDirection.magnitude;
    }

    public bool IsPressed()
    {
        return isDragging;
    }

    public bool IsActive()
    {
        return isActive;
    }

    /// <summary>
    /// 패드 모드 전환 (동적/고정)
    /// </summary>
    public void SetDynamicMode(bool dynamic)
    {
        enableDynamicPosition = dynamic;

        if (!dynamic && !isDragging)
        {
            // 고정 모드로 전환 시 초기 위치로 복귀
            if (bgTransform != null)
                bgTransform.anchoredPosition = initialBgPosition;

            if (padTransform != null)
                padTransform.anchoredPosition = initialPadPosition;
        }
    }

    /// <summary>
    /// 강제 초기화 (외부에서 호출 가능)
    /// </summary>
    public void ResetPad()
    {
        isDragging = false;
        isActive = false;
        inputDirection = Vector2.zero;
        SetPadVisibility(false, true);

        // 원래 위치로 복귀
        if (bgTransform != null)
            bgTransform.anchoredPosition = initialBgPosition;

        if (padTransform != null)
            padTransform.anchoredPosition = initialPadPosition;
    }

    /// <summary>
    /// 패드 설정 조정 메서드들
    /// </summary>
    public void SetMoveRange(float range)
    {
        moveRange = Mathf.Max(10f, range);
    }

    public void SetFadeSpeed(float speed)
    {
        padFadeSpeed = Mathf.Max(1f, speed);
    }

    void OnDestroy()
    {
        // TouchOverlay 정리
        if (touchOverlay != null)
        {
            DestroyImmediate(touchOverlay);
        }
    }

    // 기즈모로 이동 범위 표시
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && isActive)
        {
            // 동적 모드에서는 현재 패드 중심 표시
            Vector2 center = enableDynamicPosition ? dynamicPadCenter : initialBgPosition;

            Gizmos.color = Color.yellow;
            if (transform != null)
            {
                Vector3 worldPos = transform.TransformPoint(center);
                Gizmos.DrawWireSphere(worldPos, moveRange * (transform as RectTransform).lossyScale.x);
            }
        }
        else if (transform != null)
        {
            // 비활성 상태에서는 초기 위치 표시
            Gizmos.color = Color.gray;
            Vector3 worldPos = transform.TransformPoint(initialBgPosition);
            Gizmos.DrawWireSphere(worldPos, moveRange * (transform as RectTransform).lossyScale.x);
        }
    }

    #region Debug Methods
    [ContextMenu("Test Show Pad")]
    void TestShowPad()
    {
        SetPadVisibility(true);
        isActive = true;
    }

    [ContextMenu("Test Hide Pad")]
    void TestHidePad()
    {
        SetPadVisibility(false);
        isActive = false;
    }

    [ContextMenu("Reset Pad")]
    void TestResetPad()
    {
        ResetPad();
    }

    [ContextMenu("Test Dynamic Position")]
    void TestDynamicPosition()
    {
        if (Application.isPlaying)
        {
            // 화면 중앙에 동적 패드 생성
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            PointerEventData testEvent = new PointerEventData(EventSystem.current);
            testEvent.position = screenCenter;

            SetDynamicPadCenter(testEvent);
            SetPadVisibility(true);
            isActive = true;
        }
    }
    #endregion
}