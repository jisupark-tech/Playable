// Made with Amplify Shader Editor v1.9.2.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "FX_APF_Master"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[Enum(Additive,1,AlphaBlend,10)]_BlendMode("BlendMode", Float) = 10
		[Enum(UnityEngine.Rendering.CullMode)]_CullMode("CullMode", Float) = 0
		[Enum(LEqual,4,Always,8)]_ZTest("ZTest", Float) = 4
		[Enum(Repeat,0,Clamp,1)]_MainTex_Repeat("MainTex_Repeat", Float) = 0
		_MainTex("MainTex", 2D) = "white" {}
		_MainColor("MainColor", Color) = (1,1,1,1)
		_Main_Intensity("Main_Intensity", Float) = 1
		_SecondTex("SecondTex", 2D) = "white" {}
		_ThirdTex("ThirdTex", 2D) = "white" {}
		_MainSecond_Panner("Main/Second_Panner", Vector) = (0,0,0,0)
		_Third_Panner("Third_Panner", Vector) = (0,0,0,0)
		_Dissolve_Mix("Dissolve_Mix", Range( 0 , 1)) = 0
		_Dissolve_Amount("Dissolve_Amount", Float) = 0
		[Toggle]_Use_ThirdEdgeColor("Use_ThirdEdgeColor", Float) = 0
		_Dissolve_Softness("Dissolve_Softness", Float) = 0.1
		_Dissolve_Edge("Dissolve_Edge", Float) = 0.1
		_Distortion_Amount("Distortion_Amount", Range( -0.1 , 0.1)) = 0
		[Toggle]_Use_Third_Color("Use_Third_Color", Float) = 0
		_EdgeColor("EdgeColor", Color) = (1,0,0,1)
		_Edge_Intensity("Edge_Intensity", Float) = 1
		[Toggle]_Use_Second_Mask("Use_Second_Mask", Float) = 0
		_Alpha_Clip_Value("Alpha_Clip_Value", Float) = 0
		[Toggle]_Use_Custom("Use_Custom", Float) = 0

		[HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25
	}

	SubShader
	{
		LOD 0

		
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
		
		Cull [_CullMode]
		AlphaToMask Off
		
		HLSLINCLUDE
		#pragma target 3.0

		#pragma prefer_hlslcc gles
		

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}
		
		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS

		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }
			
			Blend SrcAlpha [_BlendMode], One OneMinusSrcAlpha
			ZWrite Off
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM
			
			#define _RECEIVE_SHADOWS_OFF 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 140011

			
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma shader_feature _ _SAMPLE_GI
			#pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile _ DEBUG_DISPLAY
			#define SHADERPASS SHADERPASS_UNLIT


			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"


			#define ASE_NEEDS_FRAG_COLOR


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				#ifdef ASE_FOG
				float fogFactor : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _MainSecond_Panner;
			float4 _SecondTex_ST;
			float4 _EdgeColor;
			float4 _MainColor;
			float4 _ThirdTex_ST;
			float4 _Third_Panner;
			float _ZTest;
			float _Use_Second_Mask;
			float _Dissolve_Mix;
			float _Dissolve_Edge;
			float _Dissolve_Softness;
			float _Dissolve_Amount;
			float _Main_Intensity;
			float _Edge_Intensity;
			float _Distortion_Amount;
			float _MainTex_Repeat;
			float _Use_Custom;
			float _Use_ThirdEdgeColor;
			float _CullMode;
			float _BlendMode;
			float _Use_Third_Color;
			float _Alpha_Clip_Value;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _MainTex;
			sampler2D _SecondTex;
			sampler2D _ThirdTex;


						
			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord3.xy = v.ase_texcoord.xy;
				o.ase_texcoord4 = v.ase_texcoord2;
				o.ase_texcoord5 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				#ifdef ASE_FOG
				o.fogFactor = ComputeFogFactor( positionCS.z );
				#endif
				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord2 = v.ase_texcoord2;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif
				float2 uv_MainTex = IN.ase_texcoord3.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 appendResult63 = (float2(_MainSecond_Panner.x , _MainSecond_Panner.y));
				float2 Main_Panner66 = appendResult63;
				float4 _Vector1 = float4(1,1,0,0);
				float2 appendResult18 = (float2(_Vector1.z , _Vector1.w));
				float2 appendResult153 = (float2(IN.ase_texcoord4.x , IN.ase_texcoord4.y));
				float2 Main_UV157 = appendResult153;
				float2 temp_output_28_0 = ( ( ( uv_MainTex * float2( 1,1 ) ) + frac( ( Main_Panner66 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( Main_Panner66 + Main_UV157 ) ):( appendResult18 )) );
				float2 lerpResult35 = lerp( temp_output_28_0 , saturate( temp_output_28_0 ) , _MainTex_Repeat);
				float2 uv_SecondTex = IN.ase_texcoord3.xy * _SecondTex_ST.xy + _SecondTex_ST.zw;
				float2 appendResult64 = (float2(_MainSecond_Panner.z , _MainSecond_Panner.w));
				float2 Second_Panner67 = appendResult64;
				float4 _Vector0 = float4(1,1,0,0);
				float2 appendResult41 = (float2(_Vector0.z , _Vector0.w));
				float Second_R81 = tex2D( _SecondTex, ( ( ( uv_SecondTex * float2( 1,1 ) ) + frac( ( Second_Panner67 * _TimeParameters.x ) ) ) + appendResult41 ) ).r;
				float2 temp_cast_0 = ((-1.0 + (Second_R81 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0))).xx;
				float Distortion161 = IN.ase_texcoord5.w;
				float2 lerpResult140 = lerp( lerpResult35 , temp_cast_0 , (( _Use_Custom )?( ( _Distortion_Amount + Distortion161 ) ):( _Distortion_Amount )));
				float4 tex2DNode11 = tex2D( _MainTex, lerpResult140 );
				float4 Main_RGB114 = tex2DNode11;
				float2 uv_ThirdTex = IN.ase_texcoord3.xy * _ThirdTex_ST.xy + _ThirdTex_ST.zw;
				float2 appendResult69 = (float2(_Third_Panner.z , _Third_Panner.w));
				float2 Third_Panner70 = appendResult69;
				float4 _Vector2 = float4(1,1,0,0);
				float2 appendResult57 = (float2(_Vector2.z , _Vector2.w));
				float2 appendResult152 = (float2(IN.ase_texcoord4.z , IN.ase_texcoord4.w));
				float2 Third_UV159 = appendResult152;
				float4 tex2DNode55 = tex2D( _ThirdTex, ( ( ( uv_ThirdTex * float2( 1,1 ) ) + frac( ( Third_Panner70 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( appendResult57 + Third_UV159 ) ):( appendResult57 )) ) );
				float4 Third_RGB105 = tex2DNode55;
				float4 EdgeColor109 = (( _Use_ThirdEdgeColor )?( ( Third_RGB105 * _Edge_Intensity * 15.0 ) ):( ( _EdgeColor * _Edge_Intensity ) ));
				float Main_Intensity160 = IN.ase_texcoord5.z;
				float Dissolve_Amount158 = IN.ase_texcoord5.x;
				float temp_output_93_0 = ( (-5.0 + ((( _Use_Custom )?( ( _Dissolve_Amount + Dissolve_Amount158 ) ):( _Dissolve_Amount )) - 0.0) * (5.0 - -5.0) / (1.0 - 0.0)) + _Dissolve_Softness );
				float Third_R82 = tex2DNode55.r;
				float Dissolve_Mixing156 = IN.ase_texcoord5.y;
				float lerpResult86 = lerp( Second_R81 , Third_R82 , (( _Use_Custom )?( ( _Dissolve_Mix + Dissolve_Mixing156 ) ):( _Dissolve_Mix )));
				float Dissolve_Mix88 = lerpResult86;
				float smoothstepResult95 = smoothstep( ( temp_output_93_0 * _Dissolve_Softness ) , ( ( temp_output_93_0 * _Dissolve_Softness ) + _Dissolve_Edge ) , Dissolve_Mix88);
				float Dissolve_Alpha99 = smoothstepResult95;
				float4 lerpResult101 = lerp( EdgeColor109 , ( _MainColor * ( _Main_Intensity + Main_Intensity160 ) ) , Dissolve_Alpha99);
				float4 Dissolve_Color104 = lerpResult101;
				float4 temp_output_123_0 = abs( ( Main_RGB114 * Dissolve_Color104 ) );
				float Main_A113 = tex2DNode11.a;
				float temp_output_118_0 = saturate( ( IN.ase_color.a * Dissolve_Alpha99 * (( _Use_Second_Mask )?( Second_R81 ):( 1.0 )) * Main_A113 ) );
				float Alpha125 = temp_output_118_0;
				float4 temp_cast_1 = (1.0).xxxx;
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = ( (( _Use_ThirdEdgeColor )?( ( temp_output_123_0 * Alpha125 ) ):( temp_output_123_0 )) * IN.ase_color * (( _Use_Third_Color )?( Third_RGB105 ):( temp_cast_1 )) ).rgb;
				float Alpha = temp_output_118_0;
				float AlphaClipThreshold = _Alpha_Clip_Value;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#if defined(_DBUFFER)
					ApplyDecalToBaseColor(IN.clipPos, Color);
				#endif

				#if defined(_ALPHAPREMULTIPLY_ON)
				Color *= Alpha;
				#endif


				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif

				return half4( Color, Alpha );
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM
			
			#define _RECEIVE_SHADOWS_OFF 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 140011

			
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _MainSecond_Panner;
			float4 _SecondTex_ST;
			float4 _EdgeColor;
			float4 _MainColor;
			float4 _ThirdTex_ST;
			float4 _Third_Panner;
			float _ZTest;
			float _Use_Second_Mask;
			float _Dissolve_Mix;
			float _Dissolve_Edge;
			float _Dissolve_Softness;
			float _Dissolve_Amount;
			float _Main_Intensity;
			float _Edge_Intensity;
			float _Distortion_Amount;
			float _MainTex_Repeat;
			float _Use_Custom;
			float _Use_ThirdEdgeColor;
			float _CullMode;
			float _BlendMode;
			float _Use_Third_Color;
			float _Alpha_Clip_Value;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _SecondTex;
			sampler2D _ThirdTex;
			sampler2D _MainTex;


			
			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_color = v.ase_color;
				o.ase_texcoord2 = v.ase_texcoord1;
				o.ase_texcoord3.xy = v.ase_texcoord.xy;
				o.ase_texcoord4 = v.ase_texcoord2;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				o.clipPos = TransformWorldToHClip( positionWS );
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = o.clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_color = v.ase_color;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord2 = v.ase_texcoord2;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float Dissolve_Amount158 = IN.ase_texcoord2.x;
				float temp_output_93_0 = ( (-5.0 + ((( _Use_Custom )?( ( _Dissolve_Amount + Dissolve_Amount158 ) ):( _Dissolve_Amount )) - 0.0) * (5.0 - -5.0) / (1.0 - 0.0)) + _Dissolve_Softness );
				float2 uv_SecondTex = IN.ase_texcoord3.xy * _SecondTex_ST.xy + _SecondTex_ST.zw;
				float2 appendResult64 = (float2(_MainSecond_Panner.z , _MainSecond_Panner.w));
				float2 Second_Panner67 = appendResult64;
				float4 _Vector0 = float4(1,1,0,0);
				float2 appendResult41 = (float2(_Vector0.z , _Vector0.w));
				float Second_R81 = tex2D( _SecondTex, ( ( ( uv_SecondTex * float2( 1,1 ) ) + frac( ( Second_Panner67 * _TimeParameters.x ) ) ) + appendResult41 ) ).r;
				float2 uv_ThirdTex = IN.ase_texcoord3.xy * _ThirdTex_ST.xy + _ThirdTex_ST.zw;
				float2 appendResult69 = (float2(_Third_Panner.z , _Third_Panner.w));
				float2 Third_Panner70 = appendResult69;
				float4 _Vector2 = float4(1,1,0,0);
				float2 appendResult57 = (float2(_Vector2.z , _Vector2.w));
				float2 appendResult152 = (float2(IN.ase_texcoord4.z , IN.ase_texcoord4.w));
				float2 Third_UV159 = appendResult152;
				float4 tex2DNode55 = tex2D( _ThirdTex, ( ( ( uv_ThirdTex * float2( 1,1 ) ) + frac( ( Third_Panner70 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( appendResult57 + Third_UV159 ) ):( appendResult57 )) ) );
				float Third_R82 = tex2DNode55.r;
				float Dissolve_Mixing156 = IN.ase_texcoord2.y;
				float lerpResult86 = lerp( Second_R81 , Third_R82 , (( _Use_Custom )?( ( _Dissolve_Mix + Dissolve_Mixing156 ) ):( _Dissolve_Mix )));
				float Dissolve_Mix88 = lerpResult86;
				float smoothstepResult95 = smoothstep( ( temp_output_93_0 * _Dissolve_Softness ) , ( ( temp_output_93_0 * _Dissolve_Softness ) + _Dissolve_Edge ) , Dissolve_Mix88);
				float Dissolve_Alpha99 = smoothstepResult95;
				float2 uv_MainTex = IN.ase_texcoord3.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 appendResult63 = (float2(_MainSecond_Panner.x , _MainSecond_Panner.y));
				float2 Main_Panner66 = appendResult63;
				float4 _Vector1 = float4(1,1,0,0);
				float2 appendResult18 = (float2(_Vector1.z , _Vector1.w));
				float2 appendResult153 = (float2(IN.ase_texcoord4.x , IN.ase_texcoord4.y));
				float2 Main_UV157 = appendResult153;
				float2 temp_output_28_0 = ( ( ( uv_MainTex * float2( 1,1 ) ) + frac( ( Main_Panner66 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( Main_Panner66 + Main_UV157 ) ):( appendResult18 )) );
				float2 lerpResult35 = lerp( temp_output_28_0 , saturate( temp_output_28_0 ) , _MainTex_Repeat);
				float2 temp_cast_0 = ((-1.0 + (Second_R81 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0))).xx;
				float Distortion161 = IN.ase_texcoord2.w;
				float2 lerpResult140 = lerp( lerpResult35 , temp_cast_0 , (( _Use_Custom )?( ( _Distortion_Amount + Distortion161 ) ):( _Distortion_Amount )));
				float4 tex2DNode11 = tex2D( _MainTex, lerpResult140 );
				float Main_A113 = tex2DNode11.a;
				float temp_output_118_0 = saturate( ( IN.ase_color.a * Dissolve_Alpha99 * (( _Use_Second_Mask )?( Second_R81 ):( 1.0 )) * Main_A113 ) );
				
				float Alpha = temp_output_118_0;
				float AlphaClipThreshold = _Alpha_Clip_Value;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Universal2D"
			Tags { "LightMode"="Universal2D" }
			
			Blend SrcAlpha [_BlendMode], One OneMinusSrcAlpha
			ZWrite Off
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM
			
			#define _RECEIVE_SHADOWS_OFF 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 140011

			
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma shader_feature _ _SAMPLE_GI
			#pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile _ DEBUG_DISPLAY
			#define SHADERPASS SHADERPASS_UNLIT


			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"


			#define ASE_NEEDS_FRAG_COLOR


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				#ifdef ASE_FOG
				float fogFactor : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _MainSecond_Panner;
			float4 _SecondTex_ST;
			float4 _EdgeColor;
			float4 _MainColor;
			float4 _ThirdTex_ST;
			float4 _Third_Panner;
			float _ZTest;
			float _Use_Second_Mask;
			float _Dissolve_Mix;
			float _Dissolve_Edge;
			float _Dissolve_Softness;
			float _Dissolve_Amount;
			float _Main_Intensity;
			float _Edge_Intensity;
			float _Distortion_Amount;
			float _MainTex_Repeat;
			float _Use_Custom;
			float _Use_ThirdEdgeColor;
			float _CullMode;
			float _BlendMode;
			float _Use_Third_Color;
			float _Alpha_Clip_Value;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _MainTex;
			sampler2D _SecondTex;
			sampler2D _ThirdTex;


						
			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord3.xy = v.ase_texcoord.xy;
				o.ase_texcoord4 = v.ase_texcoord2;
				o.ase_texcoord5 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				#ifdef ASE_FOG
				o.fogFactor = ComputeFogFactor( positionCS.z );
				#endif
				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord2 = v.ase_texcoord2;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif
				float2 uv_MainTex = IN.ase_texcoord3.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 appendResult63 = (float2(_MainSecond_Panner.x , _MainSecond_Panner.y));
				float2 Main_Panner66 = appendResult63;
				float4 _Vector1 = float4(1,1,0,0);
				float2 appendResult18 = (float2(_Vector1.z , _Vector1.w));
				float2 appendResult153 = (float2(IN.ase_texcoord4.x , IN.ase_texcoord4.y));
				float2 Main_UV157 = appendResult153;
				float2 temp_output_28_0 = ( ( ( uv_MainTex * float2( 1,1 ) ) + frac( ( Main_Panner66 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( Main_Panner66 + Main_UV157 ) ):( appendResult18 )) );
				float2 lerpResult35 = lerp( temp_output_28_0 , saturate( temp_output_28_0 ) , _MainTex_Repeat);
				float2 uv_SecondTex = IN.ase_texcoord3.xy * _SecondTex_ST.xy + _SecondTex_ST.zw;
				float2 appendResult64 = (float2(_MainSecond_Panner.z , _MainSecond_Panner.w));
				float2 Second_Panner67 = appendResult64;
				float4 _Vector0 = float4(1,1,0,0);
				float2 appendResult41 = (float2(_Vector0.z , _Vector0.w));
				float Second_R81 = tex2D( _SecondTex, ( ( ( uv_SecondTex * float2( 1,1 ) ) + frac( ( Second_Panner67 * _TimeParameters.x ) ) ) + appendResult41 ) ).r;
				float2 temp_cast_0 = ((-1.0 + (Second_R81 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0))).xx;
				float Distortion161 = IN.ase_texcoord5.w;
				float2 lerpResult140 = lerp( lerpResult35 , temp_cast_0 , (( _Use_Custom )?( ( _Distortion_Amount + Distortion161 ) ):( _Distortion_Amount )));
				float4 tex2DNode11 = tex2D( _MainTex, lerpResult140 );
				float4 Main_RGB114 = tex2DNode11;
				float2 uv_ThirdTex = IN.ase_texcoord3.xy * _ThirdTex_ST.xy + _ThirdTex_ST.zw;
				float2 appendResult69 = (float2(_Third_Panner.z , _Third_Panner.w));
				float2 Third_Panner70 = appendResult69;
				float4 _Vector2 = float4(1,1,0,0);
				float2 appendResult57 = (float2(_Vector2.z , _Vector2.w));
				float2 appendResult152 = (float2(IN.ase_texcoord4.z , IN.ase_texcoord4.w));
				float2 Third_UV159 = appendResult152;
				float4 tex2DNode55 = tex2D( _ThirdTex, ( ( ( uv_ThirdTex * float2( 1,1 ) ) + frac( ( Third_Panner70 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( appendResult57 + Third_UV159 ) ):( appendResult57 )) ) );
				float4 Third_RGB105 = tex2DNode55;
				float4 EdgeColor109 = (( _Use_ThirdEdgeColor )?( ( Third_RGB105 * _Edge_Intensity * 15.0 ) ):( ( _EdgeColor * _Edge_Intensity ) ));
				float Main_Intensity160 = IN.ase_texcoord5.z;
				float Dissolve_Amount158 = IN.ase_texcoord5.x;
				float temp_output_93_0 = ( (-5.0 + ((( _Use_Custom )?( ( _Dissolve_Amount + Dissolve_Amount158 ) ):( _Dissolve_Amount )) - 0.0) * (5.0 - -5.0) / (1.0 - 0.0)) + _Dissolve_Softness );
				float Third_R82 = tex2DNode55.r;
				float Dissolve_Mixing156 = IN.ase_texcoord5.y;
				float lerpResult86 = lerp( Second_R81 , Third_R82 , (( _Use_Custom )?( ( _Dissolve_Mix + Dissolve_Mixing156 ) ):( _Dissolve_Mix )));
				float Dissolve_Mix88 = lerpResult86;
				float smoothstepResult95 = smoothstep( ( temp_output_93_0 * _Dissolve_Softness ) , ( ( temp_output_93_0 * _Dissolve_Softness ) + _Dissolve_Edge ) , Dissolve_Mix88);
				float Dissolve_Alpha99 = smoothstepResult95;
				float4 lerpResult101 = lerp( EdgeColor109 , ( _MainColor * ( _Main_Intensity + Main_Intensity160 ) ) , Dissolve_Alpha99);
				float4 Dissolve_Color104 = lerpResult101;
				float4 temp_output_123_0 = abs( ( Main_RGB114 * Dissolve_Color104 ) );
				float Main_A113 = tex2DNode11.a;
				float temp_output_118_0 = saturate( ( IN.ase_color.a * Dissolve_Alpha99 * (( _Use_Second_Mask )?( Second_R81 ):( 1.0 )) * Main_A113 ) );
				float Alpha125 = temp_output_118_0;
				float4 temp_cast_1 = (1.0).xxxx;
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = ( (( _Use_ThirdEdgeColor )?( ( temp_output_123_0 * Alpha125 ) ):( temp_output_123_0 )) * IN.ase_color * (( _Use_Third_Color )?( Third_RGB105 ):( temp_cast_1 )) ).rgb;
				float Alpha = temp_output_118_0;
				float AlphaClipThreshold = _Alpha_Clip_Value;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#if defined(_DBUFFER)
					ApplyDecalToBaseColor(IN.clipPos, Color);
				#endif

				#if defined(_ALPHAPREMULTIPLY_ON)
				Color *= Alpha;
				#endif


				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif

				return half4( Color, Alpha );
			}

			ENDHLSL
		}


		
        Pass
        {
			
            Name "SceneSelectionPass"
            Tags { "LightMode"="SceneSelectionPass" }
        
			Cull Off

			HLSLPROGRAM
        
			#define _RECEIVE_SHADOWS_OFF 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 140011

        
			#pragma only_renderers d3d11 glcore gles gles3 
			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
        
			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _MainSecond_Panner;
			float4 _SecondTex_ST;
			float4 _EdgeColor;
			float4 _MainColor;
			float4 _ThirdTex_ST;
			float4 _Third_Panner;
			float _ZTest;
			float _Use_Second_Mask;
			float _Dissolve_Mix;
			float _Dissolve_Edge;
			float _Dissolve_Softness;
			float _Dissolve_Amount;
			float _Main_Intensity;
			float _Edge_Intensity;
			float _Distortion_Amount;
			float _MainTex_Repeat;
			float _Use_Custom;
			float _Use_ThirdEdgeColor;
			float _CullMode;
			float _BlendMode;
			float _Use_Third_Color;
			float _Alpha_Clip_Value;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _SecondTex;
			sampler2D _ThirdTex;
			sampler2D _MainTex;


			
			int _ObjectId;
			int _PassValue;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};
        
			VertexOutput VertexFunction(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


				o.ase_color = v.ase_color;
				o.ase_texcoord = v.ase_texcoord1;
				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				o.ase_texcoord2 = v.ase_texcoord2;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				o.clipPos = TransformWorldToHClip(positionWS);
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_color = v.ase_color;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord2 = v.ase_texcoord2;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif
			
			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				float Dissolve_Amount158 = IN.ase_texcoord.x;
				float temp_output_93_0 = ( (-5.0 + ((( _Use_Custom )?( ( _Dissolve_Amount + Dissolve_Amount158 ) ):( _Dissolve_Amount )) - 0.0) * (5.0 - -5.0) / (1.0 - 0.0)) + _Dissolve_Softness );
				float2 uv_SecondTex = IN.ase_texcoord1.xy * _SecondTex_ST.xy + _SecondTex_ST.zw;
				float2 appendResult64 = (float2(_MainSecond_Panner.z , _MainSecond_Panner.w));
				float2 Second_Panner67 = appendResult64;
				float4 _Vector0 = float4(1,1,0,0);
				float2 appendResult41 = (float2(_Vector0.z , _Vector0.w));
				float Second_R81 = tex2D( _SecondTex, ( ( ( uv_SecondTex * float2( 1,1 ) ) + frac( ( Second_Panner67 * _TimeParameters.x ) ) ) + appendResult41 ) ).r;
				float2 uv_ThirdTex = IN.ase_texcoord1.xy * _ThirdTex_ST.xy + _ThirdTex_ST.zw;
				float2 appendResult69 = (float2(_Third_Panner.z , _Third_Panner.w));
				float2 Third_Panner70 = appendResult69;
				float4 _Vector2 = float4(1,1,0,0);
				float2 appendResult57 = (float2(_Vector2.z , _Vector2.w));
				float2 appendResult152 = (float2(IN.ase_texcoord2.z , IN.ase_texcoord2.w));
				float2 Third_UV159 = appendResult152;
				float4 tex2DNode55 = tex2D( _ThirdTex, ( ( ( uv_ThirdTex * float2( 1,1 ) ) + frac( ( Third_Panner70 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( appendResult57 + Third_UV159 ) ):( appendResult57 )) ) );
				float Third_R82 = tex2DNode55.r;
				float Dissolve_Mixing156 = IN.ase_texcoord.y;
				float lerpResult86 = lerp( Second_R81 , Third_R82 , (( _Use_Custom )?( ( _Dissolve_Mix + Dissolve_Mixing156 ) ):( _Dissolve_Mix )));
				float Dissolve_Mix88 = lerpResult86;
				float smoothstepResult95 = smoothstep( ( temp_output_93_0 * _Dissolve_Softness ) , ( ( temp_output_93_0 * _Dissolve_Softness ) + _Dissolve_Edge ) , Dissolve_Mix88);
				float Dissolve_Alpha99 = smoothstepResult95;
				float2 uv_MainTex = IN.ase_texcoord1.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 appendResult63 = (float2(_MainSecond_Panner.x , _MainSecond_Panner.y));
				float2 Main_Panner66 = appendResult63;
				float4 _Vector1 = float4(1,1,0,0);
				float2 appendResult18 = (float2(_Vector1.z , _Vector1.w));
				float2 appendResult153 = (float2(IN.ase_texcoord2.x , IN.ase_texcoord2.y));
				float2 Main_UV157 = appendResult153;
				float2 temp_output_28_0 = ( ( ( uv_MainTex * float2( 1,1 ) ) + frac( ( Main_Panner66 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( Main_Panner66 + Main_UV157 ) ):( appendResult18 )) );
				float2 lerpResult35 = lerp( temp_output_28_0 , saturate( temp_output_28_0 ) , _MainTex_Repeat);
				float2 temp_cast_0 = ((-1.0 + (Second_R81 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0))).xx;
				float Distortion161 = IN.ase_texcoord.w;
				float2 lerpResult140 = lerp( lerpResult35 , temp_cast_0 , (( _Use_Custom )?( ( _Distortion_Amount + Distortion161 ) ):( _Distortion_Amount )));
				float4 tex2DNode11 = tex2D( _MainTex, lerpResult140 );
				float Main_A113 = tex2DNode11.a;
				float temp_output_118_0 = saturate( ( IN.ase_color.a * Dissolve_Alpha99 * (( _Use_Second_Mask )?( Second_R81 ):( 1.0 )) * Main_A113 ) );
				
				surfaceDescription.Alpha = temp_output_118_0;
				surfaceDescription.AlphaClipThreshold = _Alpha_Clip_Value;


				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}

			ENDHLSL
        }

		
        Pass
        {
			
            Name "ScenePickingPass"
            Tags { "LightMode"="Picking" }
        
			HLSLPROGRAM

			#define _RECEIVE_SHADOWS_OFF 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 140011


			#pragma only_renderers d3d11 glcore gles gles3 
			#pragma vertex vert
			#pragma fragment frag

        
			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY
			

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
        
			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _MainSecond_Panner;
			float4 _SecondTex_ST;
			float4 _EdgeColor;
			float4 _MainColor;
			float4 _ThirdTex_ST;
			float4 _Third_Panner;
			float _ZTest;
			float _Use_Second_Mask;
			float _Dissolve_Mix;
			float _Dissolve_Edge;
			float _Dissolve_Softness;
			float _Dissolve_Amount;
			float _Main_Intensity;
			float _Edge_Intensity;
			float _Distortion_Amount;
			float _MainTex_Repeat;
			float _Use_Custom;
			float _Use_ThirdEdgeColor;
			float _CullMode;
			float _BlendMode;
			float _Use_Third_Color;
			float _Alpha_Clip_Value;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _SecondTex;
			sampler2D _ThirdTex;
			sampler2D _MainTex;


			
        
			float4 _SelectionID;

        
			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};
        
			VertexOutput VertexFunction(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


				o.ase_color = v.ase_color;
				o.ase_texcoord = v.ase_texcoord1;
				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				o.ase_texcoord2 = v.ase_texcoord2;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				o.clipPos = TransformWorldToHClip(positionWS);
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_color = v.ase_color;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord2 = v.ase_texcoord2;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				float Dissolve_Amount158 = IN.ase_texcoord.x;
				float temp_output_93_0 = ( (-5.0 + ((( _Use_Custom )?( ( _Dissolve_Amount + Dissolve_Amount158 ) ):( _Dissolve_Amount )) - 0.0) * (5.0 - -5.0) / (1.0 - 0.0)) + _Dissolve_Softness );
				float2 uv_SecondTex = IN.ase_texcoord1.xy * _SecondTex_ST.xy + _SecondTex_ST.zw;
				float2 appendResult64 = (float2(_MainSecond_Panner.z , _MainSecond_Panner.w));
				float2 Second_Panner67 = appendResult64;
				float4 _Vector0 = float4(1,1,0,0);
				float2 appendResult41 = (float2(_Vector0.z , _Vector0.w));
				float Second_R81 = tex2D( _SecondTex, ( ( ( uv_SecondTex * float2( 1,1 ) ) + frac( ( Second_Panner67 * _TimeParameters.x ) ) ) + appendResult41 ) ).r;
				float2 uv_ThirdTex = IN.ase_texcoord1.xy * _ThirdTex_ST.xy + _ThirdTex_ST.zw;
				float2 appendResult69 = (float2(_Third_Panner.z , _Third_Panner.w));
				float2 Third_Panner70 = appendResult69;
				float4 _Vector2 = float4(1,1,0,0);
				float2 appendResult57 = (float2(_Vector2.z , _Vector2.w));
				float2 appendResult152 = (float2(IN.ase_texcoord2.z , IN.ase_texcoord2.w));
				float2 Third_UV159 = appendResult152;
				float4 tex2DNode55 = tex2D( _ThirdTex, ( ( ( uv_ThirdTex * float2( 1,1 ) ) + frac( ( Third_Panner70 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( appendResult57 + Third_UV159 ) ):( appendResult57 )) ) );
				float Third_R82 = tex2DNode55.r;
				float Dissolve_Mixing156 = IN.ase_texcoord.y;
				float lerpResult86 = lerp( Second_R81 , Third_R82 , (( _Use_Custom )?( ( _Dissolve_Mix + Dissolve_Mixing156 ) ):( _Dissolve_Mix )));
				float Dissolve_Mix88 = lerpResult86;
				float smoothstepResult95 = smoothstep( ( temp_output_93_0 * _Dissolve_Softness ) , ( ( temp_output_93_0 * _Dissolve_Softness ) + _Dissolve_Edge ) , Dissolve_Mix88);
				float Dissolve_Alpha99 = smoothstepResult95;
				float2 uv_MainTex = IN.ase_texcoord1.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 appendResult63 = (float2(_MainSecond_Panner.x , _MainSecond_Panner.y));
				float2 Main_Panner66 = appendResult63;
				float4 _Vector1 = float4(1,1,0,0);
				float2 appendResult18 = (float2(_Vector1.z , _Vector1.w));
				float2 appendResult153 = (float2(IN.ase_texcoord2.x , IN.ase_texcoord2.y));
				float2 Main_UV157 = appendResult153;
				float2 temp_output_28_0 = ( ( ( uv_MainTex * float2( 1,1 ) ) + frac( ( Main_Panner66 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( Main_Panner66 + Main_UV157 ) ):( appendResult18 )) );
				float2 lerpResult35 = lerp( temp_output_28_0 , saturate( temp_output_28_0 ) , _MainTex_Repeat);
				float2 temp_cast_0 = ((-1.0 + (Second_R81 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0))).xx;
				float Distortion161 = IN.ase_texcoord.w;
				float2 lerpResult140 = lerp( lerpResult35 , temp_cast_0 , (( _Use_Custom )?( ( _Distortion_Amount + Distortion161 ) ):( _Distortion_Amount )));
				float4 tex2DNode11 = tex2D( _MainTex, lerpResult140 );
				float Main_A113 = tex2DNode11.a;
				float temp_output_118_0 = saturate( ( IN.ase_color.a * Dissolve_Alpha99 * (( _Use_Second_Mask )?( Second_R81 ):( 1.0 )) * Main_A113 ) );
				
				surfaceDescription.Alpha = temp_output_118_0;
				surfaceDescription.AlphaClipThreshold = _Alpha_Clip_Value;


				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = 0;
				outColor = _SelectionID;
				
				return outColor;
			}
        
			ENDHLSL
        }
		
		
        Pass
        {
			
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }

			ZTest LEqual
			ZWrite On

        
			HLSLPROGRAM
			
			#define _RECEIVE_SHADOWS_OFF 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 140011

			
			#pragma only_renderers d3d11 glcore gles gles3 
			#pragma multi_compile_fog
			#pragma instancing_options renderinglayer
			#pragma vertex vert
			#pragma fragment frag

        
			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define VARYINGS_NEED_NORMAL_WS

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
        
			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _MainSecond_Panner;
			float4 _SecondTex_ST;
			float4 _EdgeColor;
			float4 _MainColor;
			float4 _ThirdTex_ST;
			float4 _Third_Panner;
			float _ZTest;
			float _Use_Second_Mask;
			float _Dissolve_Mix;
			float _Dissolve_Edge;
			float _Dissolve_Softness;
			float _Dissolve_Amount;
			float _Main_Intensity;
			float _Edge_Intensity;
			float _Distortion_Amount;
			float _MainTex_Repeat;
			float _Use_Custom;
			float _Use_ThirdEdgeColor;
			float _CullMode;
			float _BlendMode;
			float _Use_Third_Color;
			float _Alpha_Clip_Value;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _SecondTex;
			sampler2D _ThirdTex;
			sampler2D _MainTex;


			      
			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};
        
			VertexOutput VertexFunction(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_color = v.ase_color;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				o.ase_texcoord3 = v.ase_texcoord2;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 normalWS = TransformObjectToWorldNormal(v.ase_normal);

				o.clipPos = TransformWorldToHClip(positionWS);
				o.normalWS.xyz =  normalWS;

				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_color = v.ase_color;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord2 = v.ase_texcoord2;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				float Dissolve_Amount158 = IN.ase_texcoord1.x;
				float temp_output_93_0 = ( (-5.0 + ((( _Use_Custom )?( ( _Dissolve_Amount + Dissolve_Amount158 ) ):( _Dissolve_Amount )) - 0.0) * (5.0 - -5.0) / (1.0 - 0.0)) + _Dissolve_Softness );
				float2 uv_SecondTex = IN.ase_texcoord2.xy * _SecondTex_ST.xy + _SecondTex_ST.zw;
				float2 appendResult64 = (float2(_MainSecond_Panner.z , _MainSecond_Panner.w));
				float2 Second_Panner67 = appendResult64;
				float4 _Vector0 = float4(1,1,0,0);
				float2 appendResult41 = (float2(_Vector0.z , _Vector0.w));
				float Second_R81 = tex2D( _SecondTex, ( ( ( uv_SecondTex * float2( 1,1 ) ) + frac( ( Second_Panner67 * _TimeParameters.x ) ) ) + appendResult41 ) ).r;
				float2 uv_ThirdTex = IN.ase_texcoord2.xy * _ThirdTex_ST.xy + _ThirdTex_ST.zw;
				float2 appendResult69 = (float2(_Third_Panner.z , _Third_Panner.w));
				float2 Third_Panner70 = appendResult69;
				float4 _Vector2 = float4(1,1,0,0);
				float2 appendResult57 = (float2(_Vector2.z , _Vector2.w));
				float2 appendResult152 = (float2(IN.ase_texcoord3.z , IN.ase_texcoord3.w));
				float2 Third_UV159 = appendResult152;
				float4 tex2DNode55 = tex2D( _ThirdTex, ( ( ( uv_ThirdTex * float2( 1,1 ) ) + frac( ( Third_Panner70 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( appendResult57 + Third_UV159 ) ):( appendResult57 )) ) );
				float Third_R82 = tex2DNode55.r;
				float Dissolve_Mixing156 = IN.ase_texcoord1.y;
				float lerpResult86 = lerp( Second_R81 , Third_R82 , (( _Use_Custom )?( ( _Dissolve_Mix + Dissolve_Mixing156 ) ):( _Dissolve_Mix )));
				float Dissolve_Mix88 = lerpResult86;
				float smoothstepResult95 = smoothstep( ( temp_output_93_0 * _Dissolve_Softness ) , ( ( temp_output_93_0 * _Dissolve_Softness ) + _Dissolve_Edge ) , Dissolve_Mix88);
				float Dissolve_Alpha99 = smoothstepResult95;
				float2 uv_MainTex = IN.ase_texcoord2.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 appendResult63 = (float2(_MainSecond_Panner.x , _MainSecond_Panner.y));
				float2 Main_Panner66 = appendResult63;
				float4 _Vector1 = float4(1,1,0,0);
				float2 appendResult18 = (float2(_Vector1.z , _Vector1.w));
				float2 appendResult153 = (float2(IN.ase_texcoord3.x , IN.ase_texcoord3.y));
				float2 Main_UV157 = appendResult153;
				float2 temp_output_28_0 = ( ( ( uv_MainTex * float2( 1,1 ) ) + frac( ( Main_Panner66 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( Main_Panner66 + Main_UV157 ) ):( appendResult18 )) );
				float2 lerpResult35 = lerp( temp_output_28_0 , saturate( temp_output_28_0 ) , _MainTex_Repeat);
				float2 temp_cast_0 = ((-1.0 + (Second_R81 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0))).xx;
				float Distortion161 = IN.ase_texcoord1.w;
				float2 lerpResult140 = lerp( lerpResult35 , temp_cast_0 , (( _Use_Custom )?( ( _Distortion_Amount + Distortion161 ) ):( _Distortion_Amount )));
				float4 tex2DNode11 = tex2D( _MainTex, lerpResult140 );
				float Main_A113 = tex2DNode11.a;
				float temp_output_118_0 = saturate( ( IN.ase_color.a * Dissolve_Alpha99 * (( _Use_Second_Mask )?( Second_R81 ):( 1.0 )) * Main_A113 ) );
				
				surfaceDescription.Alpha = temp_output_118_0;
				surfaceDescription.AlphaClipThreshold = _Alpha_Clip_Value;

				#if _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				float3 normalWS = IN.normalWS;
				return half4(NormalizeNormalPerPixel(normalWS), 0.0);

			}
        
			ENDHLSL
        }

		
        Pass
        {
			
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }
        
			ZTest LEqual
			ZWrite On
        
        
			HLSLPROGRAM
        
			#define _RECEIVE_SHADOWS_OFF 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 140011

        
			#pragma exclude_renderers glcore gles gles3 
			#pragma vertex vert
			#pragma fragment frag
        
			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define ATTRIBUTES_NEED_TEXCOORD1
			#define VARYINGS_NEED_NORMAL_WS
			#define VARYINGS_NEED_TANGENT_WS
        
			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
        
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
        
			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _MainSecond_Panner;
			float4 _SecondTex_ST;
			float4 _EdgeColor;
			float4 _MainColor;
			float4 _ThirdTex_ST;
			float4 _Third_Panner;
			float _ZTest;
			float _Use_Second_Mask;
			float _Dissolve_Mix;
			float _Dissolve_Edge;
			float _Dissolve_Softness;
			float _Dissolve_Amount;
			float _Main_Intensity;
			float _Edge_Intensity;
			float _Distortion_Amount;
			float _MainTex_Repeat;
			float _Use_Custom;
			float _Use_ThirdEdgeColor;
			float _CullMode;
			float _BlendMode;
			float _Use_Third_Color;
			float _Alpha_Clip_Value;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _SecondTex;
			sampler2D _ThirdTex;
			sampler2D _MainTex;


			
			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};
      
			VertexOutput VertexFunction(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_color = v.ase_color;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				o.ase_texcoord3 = v.ase_texcoord2;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 normalWS = TransformObjectToWorldNormal(v.ase_normal);

				o.clipPos = TransformWorldToHClip(positionWS);
				o.normalWS.xyz =  normalWS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_color = v.ase_color;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord2 = v.ase_texcoord2;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				float Dissolve_Amount158 = IN.ase_texcoord1.x;
				float temp_output_93_0 = ( (-5.0 + ((( _Use_Custom )?( ( _Dissolve_Amount + Dissolve_Amount158 ) ):( _Dissolve_Amount )) - 0.0) * (5.0 - -5.0) / (1.0 - 0.0)) + _Dissolve_Softness );
				float2 uv_SecondTex = IN.ase_texcoord2.xy * _SecondTex_ST.xy + _SecondTex_ST.zw;
				float2 appendResult64 = (float2(_MainSecond_Panner.z , _MainSecond_Panner.w));
				float2 Second_Panner67 = appendResult64;
				float4 _Vector0 = float4(1,1,0,0);
				float2 appendResult41 = (float2(_Vector0.z , _Vector0.w));
				float Second_R81 = tex2D( _SecondTex, ( ( ( uv_SecondTex * float2( 1,1 ) ) + frac( ( Second_Panner67 * _TimeParameters.x ) ) ) + appendResult41 ) ).r;
				float2 uv_ThirdTex = IN.ase_texcoord2.xy * _ThirdTex_ST.xy + _ThirdTex_ST.zw;
				float2 appendResult69 = (float2(_Third_Panner.z , _Third_Panner.w));
				float2 Third_Panner70 = appendResult69;
				float4 _Vector2 = float4(1,1,0,0);
				float2 appendResult57 = (float2(_Vector2.z , _Vector2.w));
				float2 appendResult152 = (float2(IN.ase_texcoord3.z , IN.ase_texcoord3.w));
				float2 Third_UV159 = appendResult152;
				float4 tex2DNode55 = tex2D( _ThirdTex, ( ( ( uv_ThirdTex * float2( 1,1 ) ) + frac( ( Third_Panner70 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( appendResult57 + Third_UV159 ) ):( appendResult57 )) ) );
				float Third_R82 = tex2DNode55.r;
				float Dissolve_Mixing156 = IN.ase_texcoord1.y;
				float lerpResult86 = lerp( Second_R81 , Third_R82 , (( _Use_Custom )?( ( _Dissolve_Mix + Dissolve_Mixing156 ) ):( _Dissolve_Mix )));
				float Dissolve_Mix88 = lerpResult86;
				float smoothstepResult95 = smoothstep( ( temp_output_93_0 * _Dissolve_Softness ) , ( ( temp_output_93_0 * _Dissolve_Softness ) + _Dissolve_Edge ) , Dissolve_Mix88);
				float Dissolve_Alpha99 = smoothstepResult95;
				float2 uv_MainTex = IN.ase_texcoord2.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 appendResult63 = (float2(_MainSecond_Panner.x , _MainSecond_Panner.y));
				float2 Main_Panner66 = appendResult63;
				float4 _Vector1 = float4(1,1,0,0);
				float2 appendResult18 = (float2(_Vector1.z , _Vector1.w));
				float2 appendResult153 = (float2(IN.ase_texcoord3.x , IN.ase_texcoord3.y));
				float2 Main_UV157 = appendResult153;
				float2 temp_output_28_0 = ( ( ( uv_MainTex * float2( 1,1 ) ) + frac( ( Main_Panner66 * _TimeParameters.x ) ) ) + (( _Use_Custom )?( ( Main_Panner66 + Main_UV157 ) ):( appendResult18 )) );
				float2 lerpResult35 = lerp( temp_output_28_0 , saturate( temp_output_28_0 ) , _MainTex_Repeat);
				float2 temp_cast_0 = ((-1.0 + (Second_R81 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0))).xx;
				float Distortion161 = IN.ase_texcoord1.w;
				float2 lerpResult140 = lerp( lerpResult35 , temp_cast_0 , (( _Use_Custom )?( ( _Distortion_Amount + Distortion161 ) ):( _Distortion_Amount )));
				float4 tex2DNode11 = tex2D( _MainTex, lerpResult140 );
				float Main_A113 = tex2DNode11.a;
				float temp_output_118_0 = saturate( ( IN.ase_color.a * Dissolve_Alpha99 * (( _Use_Second_Mask )?( Second_R81 ):( 1.0 )) * Main_A113 ) );
				
				surfaceDescription.Alpha = temp_output_118_0;
				surfaceDescription.AlphaClipThreshold = _Alpha_Clip_Value;
				
				#if _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				float3 normalWS = IN.normalWS;
				return half4(NormalizeNormalPerPixel(normalWS), 0.0);

			}

			ENDHLSL
        }
		
	}
	
	CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=19202
Node;AmplifyShaderEditor.CommentaryNode;164;1360,-448;Inherit;False;1616.262;883.7131;Output;22;117;125;116;128;130;1;147;148;149;137;138;139;118;12;126;129;123;120;115;122;119;121;Output;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;163;976,448;Inherit;False;2004;706.6666;Dissolve;19;93;96;95;101;98;99;104;94;97;92;91;145;103;14;111;0;187;186;191;Dissolve;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;162;-2480,800;Inherit;False;676;618.3333;Custom;10;159;152;153;157;158;156;161;160;150;151;Custom;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;112;368,1200;Inherit;False;898;385.3331;EdgeColor;8;134;132;131;109;108;107;106;135;EdgeColor;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;89;-1660,672;Inherit;False;736;380.0001;Mix;8;179;175;178;87;88;86;84;85;Mix;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;78;-896,16;Inherit;False;1839.778;439.086;MainUV;14;11;140;142;141;35;74;16;18;37;28;36;20;114;113;MainUV;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;77;-896,1200;Inherit;False;1035.333;386.6666;ThirdUV;6;54;55;57;58;72;56;ThirdUV;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;76;-898,670;Inherit;False;1035.333;386.6667;SecondUV;6;38;53;41;73;45;52;SecondUV;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;75;-1664,1088;Inherit;False;708;497.3334;Panner;8;64;63;69;66;67;70;68;62;Panner;1,1,1,1;0;0
Node;AmplifyShaderEditor.DynamicAppendNode;64;-1360,1248;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;63;-1360,1136;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;69;-1360,1440;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;66;-1200,1136;Inherit;False;Main_Panner;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;67;-1200,1248;Inherit;False;Second_Panner;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;70;-1200,1440;Inherit;False;Third_Panner;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;68;-1616,1376;Inherit;False;Property;_Third_Panner;Third_Panner;10;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;38;-416,800;Inherit;False;UV_Basic;-1;;5;94306aaeb224d624b91ef6a4b5414bc0;0;4;2;FLOAT2;0,0;False;3;FLOAT2;1,1;False;11;FLOAT2;0,0;False;4;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;41;-608,848;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;73;-672,944;Inherit;False;67;Second_Panner;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;45;-848,848;Inherit;False;Constant;_Vector0;Vector 0;2;0;Create;True;0;0;0;False;0;False;1,1,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;52;-672,720;Inherit;False;0;53;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;62;-1616,1136;Inherit;False;Property;_MainSecond_Panner;Main/Second_Panner;9;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;81;144,800;Inherit;False;Second_R;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;85;-1520,720;Inherit;False;81;Second_R;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;84;-1520,800;Inherit;False;82;Third_R;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;86;-1312,768;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;53;-176,768;Inherit;True;Property;_SecondTex;SecondTex;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;106;800,1328;Inherit;False;Property;_Use_ThirdEdgeColor;Use_ThirdEdgeColor;17;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;107;432,1424;Inherit;False;105;Third_RGB;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;108;400,1248;Inherit;False;Property;_EdgeColor;EdgeColor;18;0;Create;True;0;0;0;False;0;False;1,0,0,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;109;1040,1328;Inherit;False;EdgeColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;131;640,1280;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;132;640,1408;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;134;432,1488;Inherit;False;Property;_Edge_Intensity;Edge_Intensity;19;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;135;800,1456;Inherit;False;Constant;_Float0;Float 0;17;0;Create;True;0;0;0;False;0;False;15;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;87;-1616,880;Inherit;False;Property;_Dissolve_Mix;Dissolve_Mix;11;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;20;-848,192;Inherit;False;Constant;_Vector1;Vector 1;2;0;Create;True;0;0;0;False;0;False;1,1,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;1056,592;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;12;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;5;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;12;all;0;False;True;2;5;False;;10;True;_BlendMode;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;True;;True;True;0;False;;0;False;;True;1;LightMode=Universal2D;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;6;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;SceneSelectionPass;0;6;SceneSelectionPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=SceneSelectionPass;False;True;4;d3d11;glcore;gles;gles3;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;7;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ScenePickingPass;0;7;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;True;4;d3d11;glcore;gles;gles3;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;8;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormals;0;8;DepthNormals;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;True;4;d3d11;glcore;gles;gles3;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;9;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormalsOnly;0;9;DepthNormalsOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;True;9;d3d11;metal;vulkan;xboxone;xboxseries;playstation;ps4;ps5;switch;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.SaturateNode;36;-176,192;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;28;-416,144;Inherit;False;UV_Basic;-1;;6;94306aaeb224d624b91ef6a4b5414bc0;0;4;2;FLOAT2;0,0;False;3;FLOAT2;1,1;False;11;FLOAT2;0,0;False;4;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-208,272;Inherit;False;Property;_MainTex_Repeat;MainTex_Repeat;3;1;[Enum];Create;True;0;2;Repeat;0;Clamp;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;18;-608,192;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;16;-672,64;Inherit;False;0;11;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;74;-672,288;Inherit;False;66;Main_Panner;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;54;-416,1328;Inherit;False;UV_Basic;-1;;7;94306aaeb224d624b91ef6a4b5414bc0;0;4;2;FLOAT2;0,0;False;3;FLOAT2;1,1;False;11;FLOAT2;0,0;False;4;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;55;-176,1296;Inherit;True;Property;_ThirdTex;ThirdTex;8;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;57;-608,1376;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;58;-848,1376;Inherit;False;Constant;_Vector2;Vector 2;2;0;Create;True;0;0;0;False;0;False;1,1,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;72;-672,1472;Inherit;False;70;Third_Panner;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;56;-672,1248;Inherit;False;0;55;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;82;144,1344;Inherit;False;Third_R;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;105;144,1280;Inherit;False;Third_RGB;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;35;16,144;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;141;-176,352;Inherit;False;81;Second_R;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;140;224,240;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;11;400,112;Inherit;True;Property;_MainTex;MainTex;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;96;1664,752;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;94;1664,912;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;97;1856,976;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;92;1632,1040;Inherit;False;Property;_Dissolve_Edge;Dissolve_Edge;14;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;121;1408,-96;Inherit;False;104;Dissolve_Color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;119;1408,-176;Inherit;False;114;Main_RGB;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;122;1616,-160;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;115;2352,96;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;120;2416,-176;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;123;1760,-160;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;129;1904,-16;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ToggleSwitchNode;126;2080,-160;Inherit;False;Property;_Use_ThirdEdgeColor;Use_ThirdEdgeColor;13;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;12;2144,-64;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;118;2496,96;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;139;1680,272;Inherit;False;81;Second_R;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;138;1712,208;Inherit;False;Constant;_Float1;Float 1;18;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;137;1888,224;Inherit;False;Property;_Use_Second_Mask;Use_Second_Mask;20;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;149;1888,-400;Inherit;False;Constant;_Float2;Float 2;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;147;2048,-384;Inherit;False;Property;_Use_Third_Color;Use_Third_Color;16;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;152;-2208,1280;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;153;-2208,1168;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;156;-2208,912;Inherit;False;Dissolve_Mixing;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;161;-2208,1040;Inherit;False;Distortion;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;160;-2208,976;Inherit;False;Main_Intensity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;175;-1232,880;Inherit;False;Property;_Use_Custom;Use_Custom;22;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;179;-1344,944;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;88;-1168,768;Inherit;False;Dissolve_Mix;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;178;-1616,960;Inherit;False;156;Dissolve_Mixing;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;180;-672,1616;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ToggleSwitchNode;174;-560,1616;Inherit;False;Property;_Use_Custom;Use_Custom;19;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;181;-875.502,1623.291;Inherit;False;159;Third_UV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ToggleSwitchNode;172;-512,464;Inherit;False;Property;_Use_Custom;Use_Custom;22;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;182;-640,448;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;183;-832,464;Inherit;False;157;Main_UV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;185;176,656;Inherit;False;161;Distortion;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;184;336,624;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;171;432,592;Inherit;False;Property;_Use_Custom;Use_Custom;19;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;143;55.33331,557.3334;Inherit;False;Property;_Distortion_Amount;Distortion_Amount;15;0;Create;True;0;0;0;False;0;False;0;0;-0.1;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;95;2048,960;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;98;1824,896;Inherit;False;88;Dissolve_Mix;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;99;2240,960;Inherit;False;Dissolve_Alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;111;2288,544;Inherit;False;109;EdgeColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;145;2304,624;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;101;2528,832;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;104;2768,832;Inherit;False;Dissolve_Color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;150;-2432,880;Inherit;False;1;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexCoordVertexDataNode;151;-2432,1168;Inherit;False;2;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;157;-2048,1168;Inherit;False;Main_UV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;159;-2048,1280;Inherit;False;Third_UV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;190;3072,-112;Inherit;False;Property;_ZTest;ZTest;2;1;[Enum];Create;True;0;2;LEqual;4;Always;8;0;True;0;False;4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;188;3068.879,-247.3959;Inherit;False;Property;_BlendMode;BlendMode;0;1;[Enum];Create;True;0;2;Additive;1;AlphaBlend;10;0;True;0;False;10;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;189;3072,-176.6667;Inherit;False;Property;_CullMode;CullMode;1;1;[Enum];Create;True;0;2;Additive;1;AlphaBlend;10;1;UnityEngine.Rendering.CullMode;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;2688,-192;Float;False;True;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;10;FX_APF_Master;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;True;True;2;True;_CullMode;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;True;12;all;0;True;True;2;5;False;;10;True;_BlendMode;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;True;True;2;False;;True;3;True;_ZTest;True;True;0;False;;0;False;;True;1;LightMode=UniversalForwardOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;22;Surface;1;638803651854541172;  Blend;0;638803659147654351;Two Sided;1;0;Cast Shadows;0;638803651874000999;  Use Shadow Threshold;0;0;Receive Shadows;0;638803651878318398;GPU Instancing;0;638803651882811665;LOD CrossFade;0;0;Built-in Fog;0;0;DOTS Instancing;0;0;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Vertex Position,InvertActionOnDeselection;1;0;0;10;False;True;False;True;False;True;True;True;True;True;False;;False;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;158;-2208,848;Inherit;False;Dissolve_Amount;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;130;2688,-32;Inherit;False;Property;_Alpha_Clip_Value;Alpha_Clip_Value;21;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;128;1696,0;Inherit;False;125;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;116;2112,128;Inherit;False;99;Dissolve_Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;125;2656,96;Inherit;False;Alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;114;704,64;Inherit;False;Main_RGB;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;113;704,256;Inherit;False;Main_A;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;117;2128,304;Inherit;False;113;Main_A;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;103;2048,495.3333;Inherit;False;Property;_MainColor;MainColor;5;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;148;1856,-320;Inherit;False;105;Third_RGB;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;93;1472.667,718.6669;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;91;1280,944;Inherit;False;Property;_Dissolve_Softness;Dissolve_Softness;13;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;176;440.2746,829.9158;Inherit;False;158;Dissolve_Amount;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;177;648.2747,861.9158;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;173;732.2747,749.9158;Inherit;False;Property;_Use_Custom;Use_Custom;22;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;90;440.2746,749.9158;Inherit;False;Property;_Dissolve_Amount;Dissolve_Amount;12;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;142;16,272;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-1;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;187;1865.333,676;Inherit;False;160;Main_Intensity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;1865.333,596;Inherit;False;Property;_Main_Intensity;Main_Intensity;6;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;186;2105.333,660;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;191;1003.751,720.8452;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-5;False;4;FLOAT;5;False;1;FLOAT;0
WireConnection;64;0;62;3
WireConnection;64;1;62;4
WireConnection;63;0;62;1
WireConnection;63;1;62;2
WireConnection;69;0;68;3
WireConnection;69;1;68;4
WireConnection;66;0;63;0
WireConnection;67;0;64;0
WireConnection;70;0;69;0
WireConnection;38;2;52;0
WireConnection;38;11;41;0
WireConnection;38;4;73;0
WireConnection;41;0;45;3
WireConnection;41;1;45;4
WireConnection;81;0;53;1
WireConnection;86;0;85;0
WireConnection;86;1;84;0
WireConnection;86;2;175;0
WireConnection;53;1;38;0
WireConnection;106;0;131;0
WireConnection;106;1;132;0
WireConnection;109;0;106;0
WireConnection;131;0;108;0
WireConnection;131;1;134;0
WireConnection;132;0;107;0
WireConnection;132;1;134;0
WireConnection;132;2;135;0
WireConnection;36;0;28;0
WireConnection;28;2;16;0
WireConnection;28;11;172;0
WireConnection;28;4;74;0
WireConnection;18;0;20;3
WireConnection;18;1;20;4
WireConnection;54;2;56;0
WireConnection;54;11;174;0
WireConnection;54;4;72;0
WireConnection;55;1;54;0
WireConnection;57;0;58;3
WireConnection;57;1;58;4
WireConnection;82;0;55;1
WireConnection;105;0;55;0
WireConnection;35;0;28;0
WireConnection;35;1;36;0
WireConnection;35;2;37;0
WireConnection;140;0;35;0
WireConnection;140;1;142;0
WireConnection;140;2;171;0
WireConnection;11;1;140;0
WireConnection;96;0;93;0
WireConnection;96;1;91;0
WireConnection;94;0;93;0
WireConnection;94;1;91;0
WireConnection;97;0;94;0
WireConnection;97;1;92;0
WireConnection;122;0;119;0
WireConnection;122;1;121;0
WireConnection;115;0;12;4
WireConnection;115;1;116;0
WireConnection;115;2;137;0
WireConnection;115;3;117;0
WireConnection;120;0;126;0
WireConnection;120;1;12;0
WireConnection;120;2;147;0
WireConnection;123;0;122;0
WireConnection;129;0;123;0
WireConnection;129;1;128;0
WireConnection;126;0;123;0
WireConnection;126;1;129;0
WireConnection;118;0;115;0
WireConnection;137;0;138;0
WireConnection;137;1;139;0
WireConnection;147;0;149;0
WireConnection;147;1;148;0
WireConnection;152;0;151;3
WireConnection;152;1;151;4
WireConnection;153;0;151;1
WireConnection;153;1;151;2
WireConnection;156;0;150;2
WireConnection;161;0;150;4
WireConnection;160;0;150;3
WireConnection;175;0;87;0
WireConnection;175;1;179;0
WireConnection;179;0;87;0
WireConnection;179;1;178;0
WireConnection;88;0;86;0
WireConnection;180;0;57;0
WireConnection;180;1;181;0
WireConnection;174;0;57;0
WireConnection;174;1;180;0
WireConnection;172;0;18;0
WireConnection;172;1;182;0
WireConnection;182;0;74;0
WireConnection;182;1;183;0
WireConnection;184;0;143;0
WireConnection;184;1;185;0
WireConnection;171;0;143;0
WireConnection;171;1;184;0
WireConnection;95;0;98;0
WireConnection;95;1;96;0
WireConnection;95;2;97;0
WireConnection;99;0;95;0
WireConnection;145;0;103;0
WireConnection;145;1;186;0
WireConnection;101;0;111;0
WireConnection;101;1;145;0
WireConnection;101;2;99;0
WireConnection;104;0;101;0
WireConnection;157;0;153;0
WireConnection;159;0;152;0
WireConnection;1;2;120;0
WireConnection;1;3;118;0
WireConnection;1;4;130;0
WireConnection;158;0;150;1
WireConnection;125;0;118;0
WireConnection;114;0;11;0
WireConnection;113;0;11;4
WireConnection;93;0;191;0
WireConnection;93;1;91;0
WireConnection;177;0;90;0
WireConnection;177;1;176;0
WireConnection;173;0;90;0
WireConnection;173;1;177;0
WireConnection;142;0;141;0
WireConnection;186;0;14;0
WireConnection;186;1;187;0
WireConnection;191;0;173;0
ASEEND*/
//CHKSM=F0EE1A85B30FA404737FBEFB71782ABD20BE0E93