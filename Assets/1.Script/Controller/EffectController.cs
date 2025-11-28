using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectType
{
    Building,
    Hit,
}

public class EffectController : MonoBehaviour
{
    [Header("Effect Settings")]
    public float durationLength;

    [Header("Dust Line Effect Settings")]
    public LineRenderer lineRenderer;
    public int pointCount = 20;
    public float height = 10f;
    public float width = 2f;

    [Header("Animation Settings")]
    public float speed = 2f;
    public float noiseScale = 0.1f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float curDuration = 0;
    private Material lineMaterial;
    private float animTime;
    private Vector3[] originalPositions;
    private Vector3 startPos;
    private Color originalColor;
    private EffectType m_type;

    public void Init(EffectType _type, float _x = 0, float _z = 0)
    {
        curDuration = 0;
        animTime = 0;
        m_type = _type;

        switch (m_type)
        {
            case EffectType.Building:
                // GameObject의 위치를 설정
                transform.position = new Vector3(_x, 0f, _z);
                startPos = new Vector3(_x, 0f, _z);
                SetupDustEffect();
                StartCoroutine(ShowAnim());
                break;
            case EffectType.Hit:
                // Hit 타입의 경우에도 위치 설정이 필요하다면
                //transform.position = new Vector3(_x, 0f, _z);
                StartCoroutine(ShowAnim());
                break;
        }
    }

    void SetupDustEffect()
    {
        if (lineRenderer == null) return;

        // 간단한 쿼드 형태로 설정 (셰이더에서 실제 파티클 위치 계산)
        lineRenderer.positionCount = 4;
        lineRenderer.useWorldSpace = false;

        // 로컬 쿼드 설정
        Vector3[] positions = new Vector3[4]
        {
            new Vector3(-width * 0.5f, 0, -1),
            new Vector3(width * 0.5f, 0, -1),
            new Vector3(width * 0.5f, height, 1),
            new Vector3(-width * 0.5f, height, 1)
        };

        // originalPositions 배열 초기화 (쿼드의 4개 포인트)
        originalPositions = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            originalPositions[i] = positions[i];
            lineRenderer.SetPosition(i, positions[i]);
        }

        // 머티리얼 설정
        if (lineRenderer.material != null)
        {
            lineMaterial = lineRenderer.material;
            originalColor = lineMaterial.color;

            // 셰이더 파라미터 설정
            lineMaterial.SetFloat("_BuildingHeight", height);
            lineMaterial.SetFloat("_BuildingWidth", width);
            lineMaterial.SetFloat("_ParticleCount", pointCount);
            lineMaterial.SetFloat("_Speed", speed);
            lineMaterial.SetFloat("_WindStrength", noiseScale);
        }
    }

    IEnumerator ShowAnim()
    {
        while (curDuration <= durationLength)
        {
            curDuration += 0.05f;
            animTime += 0.05f;
            if (m_type == EffectType.Building)
                UpdateDustAnimation();

            yield return new WaitForSeconds(0.05f);
        }

        ObjectPool.Instance.ReturnToPool(this.gameObject);
    }

    void UpdateDustAnimation()
    {
        if (lineRenderer == null) return;

        float normalizedTime = curDuration / durationLength;

        // 페이드 애니메이션
        UpdateFade(normalizedTime);

        // 포인트 애니메이션 (쿼드의 경우 미세한 움직임만)
        UpdatePoints();

        // 머티리얼 애니메이션
        UpdateMaterialAnimation();
    }

    void UpdateFade(float normalizedTime)
    {
        if (lineMaterial == null) return;

        float fadeValue = fadeCurve.Evaluate(normalizedTime);
        Color currentColor = originalColor;
        currentColor.a = originalColor.a * fadeValue;

        lineMaterial.color = currentColor;

        // 셰이더의 _Alpha 파라미터도 업데이트
        lineMaterial.SetFloat("_Alpha", fadeValue);

        // 선택적: LineRenderer 전체 알파 조절
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.white, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),
                new GradientAlphaKey(fadeValue, 0.3f),
                new GradientAlphaKey(fadeValue, 0.7f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        lineRenderer.colorGradient = gradient;
    }

    void UpdatePoints()
    {
        // 쿼드의 경우 4개 포인트만 있으므로 안전하게 처리
        if (originalPositions == null || originalPositions.Length < 4) return;

        // 쿼드의 포인트들을 미세하게 움직여서 자연스러운 효과
        for (int i = 0; i < 4; i++)
        {
            Vector3 basePos = originalPositions[i];

            // 노이즈 추가 (매우 미세하게)
            float noiseX = Mathf.PerlinNoise(animTime * speed + i, 0) - 0.5f;
            float noiseZ = Mathf.PerlinNoise(0, animTime * speed + i) - 0.5f;

            Vector3 offset = new Vector3(
                noiseX * noiseScale * 0.1f, // 쿼드는 미세한 움직임만
                0,
                noiseZ * noiseScale * 0.1f
            );

            lineRenderer.SetPosition(i, basePos + offset);
        }
    }

    void UpdateMaterialAnimation()
    {
        if (lineMaterial == null) return;

        // 셰이더 프로퍼티 업데이트
        if (lineMaterial.HasProperty("_Speed"))
            lineMaterial.SetFloat("_Speed", speed);

        if (lineMaterial.HasProperty("_WindStrength"))
            lineMaterial.SetFloat("_WindStrength", noiseScale);

        // 시간은 유니티 내장 _Time을 사용하므로 별도 설정 불필요
    }

    // 오브젝트 풀로 반환될 때 초기화
    void OnDisable()
    {
        if (m_type == EffectType.Building && lineMaterial != null)
        {
            lineMaterial.color = originalColor;
            if (lineMaterial.HasProperty("_Alpha"))
                lineMaterial.SetFloat("_Alpha", originalColor.a);
        }
    }

    // 에디터에서 미리보기용
    void OnValidate()
    {
        if (Application.isPlaying && m_type == EffectType.Building && lineRenderer != null)
        {
            SetupDustEffect();
        }
    }
}