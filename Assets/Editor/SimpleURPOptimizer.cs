using UnityEngine;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class SimpleURPOptimizer
{
    [MenuItem("Tools/Simple URP Optimizer")]
    public static void OptimizeURP()
    {
        // 프로젝트 내 모든 UniversalRendererData 찾기
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");

        if (guids.Length == 0)
        {
            Debug.LogWarning("UniversalRendererData를 찾을 수 없습니다.");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);

            if (renderer != null)
            {
                OptimizeRenderer(renderer, path);
            }
        }

        // URP Asset도 찾아서 설정 최적화
        OptimizeURPAsset();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("URP 최적화 완료!");
    }

    private static void OptimizeRenderer(UniversalRendererData renderer, string path)
    {
        Debug.Log($"렌더러 최적화: {path}");

        // SerializedObject를 사용하여 안전하게 수정
        SerializedObject so = new SerializedObject(renderer);
        SerializedProperty rendererFeatures = so.FindProperty("m_RendererFeatures");

        if (rendererFeatures != null && rendererFeatures.isArray)
        {
            // 뒤에서부터 제거 (인덱스 변경 방지)
            for (int i = rendererFeatures.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty feature = rendererFeatures.GetArrayElementAtIndex(i);
                if (feature.objectReferenceValue != null)
                {
                    string featureName = feature.objectReferenceValue.GetType().Name;

                    // 제거할 기능들 (디버깅, 고급 효과)
                    if (featureName.Contains("Debug") ||
                        featureName.Contains("Decal") ||
                        featureName.Contains("ScreenSpace") ||
                        featureName.Contains("MotionBlur") ||
                        featureName.Contains("DepthOfField") ||
                        featureName.Contains("Bloom") ||
                        featureName.Contains("VolumetricFog"))
                    {
                        Debug.Log($"제거된 기능: {featureName}");
                        rendererFeatures.DeleteArrayElementAtIndex(i);
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void OptimizeURPAsset()
    {
        // URP Asset 찾기
        string[] urpGuids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");

        foreach (string guid in urpGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);

            if (urpAsset != null)
            {
                SerializedObject so = new SerializedObject(urpAsset);

                // 주요 설정 최적화
                SerializedProperty supportsCameraDepthTexture = so.FindProperty("m_SupportsCameraDepthTexture");
                SerializedProperty supportsCameraOpaqueTexture = so.FindProperty("m_SupportsCameraOpaqueTexture");
                SerializedProperty supportsHDR = so.FindProperty("m_SupportsHDR");
                SerializedProperty msaaSampleCount = so.FindProperty("m_MsaaSampleCount");
                SerializedProperty additionalLightsRenderingMode = so.FindProperty("m_AdditionalLightsRenderingMode");
                SerializedProperty additionalLightsPerObjectLimit = so.FindProperty("m_AdditionalLightsPerObjectLimit");
                SerializedProperty mainLightShadowsSupported = so.FindProperty("m_MainLightShadowsSupported");
                SerializedProperty additionalLightShadowsSupported = so.FindProperty("m_AdditionalLightShadowsSupported");

                if (supportsCameraDepthTexture != null) supportsCameraDepthTexture.boolValue = false;
                if (supportsCameraOpaqueTexture != null) supportsCameraOpaqueTexture.boolValue = false;
                if (supportsHDR != null) supportsHDR.boolValue = false;
                if (msaaSampleCount != null) msaaSampleCount.intValue = 1; // No MSAA
                if (additionalLightsRenderingMode != null) additionalLightsRenderingMode.intValue = 0; // Disabled
                if (additionalLightsPerObjectLimit != null) additionalLightsPerObjectLimit.intValue = 0;
                if (mainLightShadowsSupported != null) mainLightShadowsSupported.boolValue = false;
                if (additionalLightShadowsSupported != null) additionalLightShadowsSupported.boolValue = false;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(urpAsset);

                Debug.Log($"URP Asset 최적화 완료: {path}");
            }
        }
    }
}

// 빌드 시 자동 최적화
public class URPBuildOptimizer : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.WebGL)
        {
            Debug.Log("WebGL 빌드를 위한 URP 최적화 시작...");
            SimpleURPOptimizer.OptimizeURP();
        }
    }
}
#endif