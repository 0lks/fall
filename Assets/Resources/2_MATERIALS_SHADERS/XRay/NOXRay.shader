// Using parts from the template shader at https://gist.github.com/phi-lira/225cd7c5e8545be602dca4eb5ed111ba

Shader "Custom/NOXRay" {
	Properties{
		//_Color("Main Color", Color) = (1,1,1,1)
		_RimColor("Rim Color", Color) = (1,1,1,1)
		_RimPower("Rim Power", Range(0.5,8.0)) = 3.5
		//_MainTex("Albedo", 2D) = "white" {}

	// Specular vs Metallic workflow
		[HideInInspector] _WorkflowMode("WorkflowMode", Float) = 1.0

		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
		_SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

		_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		_SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
		_SpecGlossMap("Specular", 2D) = "white" {}

		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}

		_ReceiveShadows("Receive Shadows", Float) = 1.0

			// Blending state
			[HideInInspector] _Surface("__surface", Float) = 0.0
			[HideInInspector] _Blend("__blend", Float) = 0.0
			[HideInInspector] _AlphaClip("__clip", Float) = 0.0
			[HideInInspector] _SrcBlend("__src", Float) = 1.0
			[HideInInspector] _DstBlend("__dst", Float) = 0.0
			[HideInInspector] _ZWrite("__zw", Float) = 1.0
			[HideInInspector] _Cull("__cull", Float) = 2.0

	}
		SubShader{
			// This pass is used for creating the hologram rim. It is setup in way that it paints the fragments only if this object is occluded.
			/*
			Pass
			{
				Tags { "Queue" = "3000" "RenderType" = "Transparent" "RenderPipeline" = "LightweightPipeline"}

				Stencil {
				  Ref 3
				  Comp Greater
				  Fail keep
				  Pass replace
				}
				ZTest Greater // Do not hide this object behind others.
				ZWrite Off // Do not write to depth buffer because this is a semi-transparent pass
				Blend SrcAlpha OneMinusSrcAlpha // Blend to create a transparency effect.

				CGPROGRAM
			// Using vertex and fragment shaders
			#pragma vertex vert            
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float2 uv : TEXCOORD0;
			};

			struct v2f
			{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 worldVertex : TEXCOORD1;
					float3 viewDir : TEXCOORD2;
					float3 worldNormal : NORMAL;
			};

			sampler2D _MainTex;

			float4 _MainTex_ST;

			float4 _RimColor;
			float _RimPower;


			v2f vert(appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldVertex = mul(unity_ObjectToWorld, v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldVertex.xyz));

				//// float4 viewPos = UnityObjectToClipPos(pos);
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				// Calculate the color of this fragment based on the normal of the fragment and view direction.
				half rim = 1.0 - saturate(dot(normalize(i.viewDir), i.worldNormal));
				fixed4 rimColor = _RimColor * pow(rim, _RimPower) * 10;
				half a = pow(rim, _RimPower);

				rimColor.a = a;

				return rimColor;
			}
		ENDCG
		}
		*/
			Pass
			{
				// "Lightmode" tag must be "LightweightForward" or not be defined in order for
				// to render objects.
				Name "StandardLit"
				Tags
				{
				"LightMode" = "LightweightForward"
				}
				/*
				Stencil {
				  Ref 4
				  Comp always
				  Pass replace
				  ZFail keep
				}
				*/
				HLSLPROGRAM
				// Required to compile gles 2.0 with standard SRP library
				// All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x
				#pragma target 2.0

				// -------------------------------------
				// Unity defined keywords
				#pragma multi_compile _ DIRLIGHTMAP_COMBINED
				#pragma multi_compile _ LIGHTMAP_ON
				#pragma multi_compile_fog

				//--------------------------------------
				// GPU Instancing
				#pragma multi_compile_instancing

				#pragma vertex LitPassVertex
				#pragma fragment LitPassFragment

				// Including the following two function is enought for shading with Lightweight Pipeline. Everything is included in them.
				// Core.hlsl will include SRP shader library, all constant buffers not related to materials (perobject, percamera, perframe).
				// It also includes matrix/space conversion functions and fog.
				// Lighting.hlsl will include the light functions/data to abstract light constants. You should use GetMainLight and GetLight functions
				// that initialize Light struct. Lighting.hlsl also include GI, Light BDRF functions. It also includes Shadows.

				// Required by all Lightweight Render Pipeline shaders.
				// It will include Unity built-in shader variables (except the lighting variables)
				// (https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
				// It will also include many utilitary functions. 
				#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

				// Include this if you are doing a lit shader. This includes lighting shader variables,
				// lighting and shadow functions
				#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"

				// Material shader variables are not defined in SRP or LWRP shader library.
				// This means _Color, _MainTex, _MainTex_ST, and all variables in the Properties section of a shader
				// must be defined by the shader itself. If you define all those properties in CBUFFER named
				// UnityPerMaterial, SRP can cache the material properties between frames and reduce significantly the cost
				// of each drawcall.
				// In this case, for sinmplicity LitInput.hlsl is included. This contains the CBUFFER for the material
				// properties defined above. As one can see this is not part of the ShaderLibrary, it specific to the
				// LWRP Lit shader.
				#include "Packages/com.unity.render-pipelines.lightweight/Shaders/LitInput.hlsl"

				struct Attributes
				{
					float4 positionOS   : POSITION;
					float3 normalOS     : NORMAL;
					float4 tangentOS    : TANGENT;
					float2 uv           : TEXCOORD0;
					float2 uvLM         : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct Varyings
				{
					float2 uv                       : TEXCOORD0;
					float2 uvLM                     : TEXCOORD1;
					float4 positionWSAndFogFactor   : TEXCOORD2; // xyz: positionWS, w: vertex fog factor
					half3  normalWS                 : TEXCOORD3;

	#if _NORMALMAP
					half3 tangentWS                 : TEXCOORD4;
					half3 bitangentWS               : TEXCOORD5;
	#endif

	#ifdef _MAIN_LIGHT_SHADOWS
					float4 shadowCoord              : TEXCOORD6; // compute shadow coord per-vertex for the main light
	#endif
					float4 positionCS               : SV_POSITION;
				};

				Varyings LitPassVertex(Attributes input)
				{
					Varyings output;

					// VertexPositionInputs contains position in multiple spaces (world, view, homogeneous clip space)
					// Our compiler will strip all unused references (say you don't use view space).
					// Therefore there is more flexibility at no additional cost with this struct.
					VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

					// Similar to VertexPositionInputs, VertexNormalInputs will contain normal, tangent and bitangent
					// in world space. If not used it will be stripped.
					VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

					// Computes fog factor per-vertex.
					float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

					// TRANSFORM_TEX is the same as the old shader library.
					output.uv = TRANSFORM_TEX(input.uv, _MainTex);
					output.uvLM = input.uvLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;

					output.positionWSAndFogFactor = float4(vertexInput.positionWS, fogFactor);
					output.normalWS = vertexNormalInput.normalWS;

					// Here comes the flexibility of the input structs.
					// In the variants that don't have normal map defined
					// tangentWS and bitangentWS will not be referenced and
					// GetVertexNormalInputs is only converting normal
					// from object to world space
	#ifdef _NORMALMAP
					output.tangentWS = vertexNormalInput.tangentWS;
					output.bitangentWS = vertexNormalInput.bitangentWS;
	#endif

	#ifdef _MAIN_LIGHT_SHADOWS
					// shadow coord for the main light is computed in vertex.
					// If cascades are enabled, LWRP will resolve shadows in screen space
					// and this coord will be the uv coord of the screen space shadow texture.
					// Otherwise LWRP will resolve shadows in light space (no depth pre-pass and shadow collect pass)
					// In this case shadowCoord will be the position in light space.
					output.shadowCoord = GetShadowCoord(vertexInput);
	#endif
					// We just use the homogeneous clip position from the vertex input
					output.positionCS = vertexInput.positionCS;
					return output;
				}

				half4 LitPassFragment(Varyings input) : SV_Target
				{
					// Surface data contains albedo, metallic, specular, smoothness, occlusion, emission and alpha
					// InitializeStandarLitSurfaceData initializes based on the rules for standard shader.
					// You can write your own function to initialize the surface data of your shader.
					SurfaceData surfaceData;
					InitializeStandardLitSurfaceData(input.uv, surfaceData);

	#if _NORMALMAP
					half3 normalWS = TransformTangentToWorld(surfaceData.normalTS,
						half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
	#else
					half3 normalWS = input.normalWS;
	#endif
					normalWS = normalize(normalWS);

	#ifdef LIGHTMAP_ON
					// Normal is required in case Directional lightmaps are baked
					half3 bakedGI = SampleLightmap(input.uvLM, normalWS);
	#else
					// Samples SH fully per-pixel. SampleSHVertex and SampleSHPixel functions
					// are also defined in case you want to sample some terms per-vertex.
					half3 bakedGI = SampleSH(normalWS);
	#endif

					float3 positionWS = input.positionWSAndFogFactor.xyz;
					half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);

					// BRDFData holds energy conserving diffuse and specular material reflections and its roughness.
					// It's easy to plugin your own shading fuction. You just need replace LightingPhysicallyBased function
					// below with your own.
					BRDFData brdfData;
					InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

					// Light struct is provide by LWRP to abstract light shader variables.
					// It contains light direction, color, distanceAttenuation and shadowAttenuation.
					// LWRP take different shading approaches depending on light and platform.
					// You should never reference light shader variables in your shader, instead use the GetLight
					// funcitons to fill this Light struct.
	#ifdef _MAIN_LIGHT_SHADOWS
					// Main light is the brightest directional light.
					// It is shaded outside the light loop and it has a specific set of variables and shading path
					// so we can be as fast as possible in the case when there's only a single directional light
					// You can pass optionally a shadowCoord (computed per-vertex). If so, shadowAttenuation will be
					// computed.
					Light mainLight = GetMainLight(input.shadowCoord);
	#else
					Light mainLight = GetMainLight();
	#endif

					// Mix diffuse GI with environment reflections.
					half3 color = GlobalIllumination(brdfData, bakedGI, surfaceData.occlusion, normalWS, viewDirectionWS);

					// LightingPhysicallyBased computes direct light contribution.
					color += LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS);

					// Additional lights loop
	#ifdef _ADDITIONAL_LIGHTS

					// Returns the amount of lights affecting the object being renderer.
					// These lights are culled per-object in the forward renderer
					int additionalLightsCount = GetAdditionalLightsCount();
					for (int i = 0; i < additionalLightsCount; ++i)
					{
						// Similar to GetMainLight, but it takes a for-loop index. This figures out the
						// per-object light index and samples the light buffer accordingly to initialized the
						// Light struct. If _ADDITIONAL_LIGHT_SHADOWS is defined it will also compute shadows.
						Light light = GetAdditionalLight(i, positionWS);

						// Same functions used to shade the main light.
						color += LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS);
					}
	#endif
					// Emission
					color += surfaceData.emission;

					float fogFactor = input.positionWSAndFogFactor.w;

					// Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
					// with a custom one.
					color = MixFog(color, fogFactor);
					return half4(color, surfaceData.alpha);
				}
				ENDHLSL
			}
				// shadow casting support
				UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
					// Used for depth prepass
					// If shadows cascade are enabled we need to perform a depth prepass. 
					// We also need to use a depth prepass in some cases camera require depth texture
					// (e.g, MSAA is enabled and we can't resolve with Texture2DMS
					UsePass "Lightweight Render Pipeline/Lit/DepthOnly"

					// Used for Baking GI. This pass is stripped from build.
					//UsePass "Lightweight Render Pipeline/Lit/Meta"
		}
			FallBack "Diffuse"
}
