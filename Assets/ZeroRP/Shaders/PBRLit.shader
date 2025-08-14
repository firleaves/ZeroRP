Shader "ZeroRP/PBRLit"
{
    Properties
    {
        [Main(MainTex, _, off, off)] _group_MainTex ("Main Textures", float) = 1
        [Sub(MainTex)] _BaseMap("Albedo", 2D) = "white" {}
        [Sub(MainTex)] [HDR] _BaseColor("Color", Color) = (1,1,1,1)
        [Space(5)]


        [Main(Normal, _, off, off)] _group_Normal ("Normal", float) = 1
        [Sub(Normal)] [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        [Sub(Normal)] _NormalStrength("Normal Strength", Range(0.0, 2.0)) = 1.0
        [Space(5)]

        [Main(Metallic, _, off, off)] _group_Surface ("Metallic", float) = 1
        [Sub(Metallic)] _MetallicMap("Metallic", 2D) = "white" {}
        [Sub(Metallic)]_MetallicStrength("Metallic", Range(0.0, 1.0)) = 0.0
        [Sub(Metallic)] _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Space(5)]

        [Main(Occlusion, _, off, off)] _group_Occlusion ("Occlusion", float) = 1
        [Sub(Occlusion)] _OcclusionMap("Occlusion", 2D) = "white" {}
        [Sub(Occlusion)] _OcclusionStrength("OcclusionStrength", Range(0.0, 1.0)) = 0.5
//        [Space(5)]

//        [Main(RenderSettings, _, off, off)] _group_RenderSettings ("Render Settings", float) = 0
//        [SubEnum(RenderSettings,Opaque, 0, Transparent, 1)] _Surface("Surface Type", Float) = 0.0
//        [ShowIf(RenderSettings, _Surface, Equal, 1)]
//        [SubEnum(RenderSettings,Alpha, 0, Premultiply, 1, Additive, 2, Multiply, 3 ,Custom,4)] _BlendMode("Blend Mode", Float) = 0.0
//        [ShowIf(RenderSettings, _Surface, Equal, 1)]
//        [SubEnum(RenderSettings,UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1.0
//        [ShowIf(RenderSettings, _Surface, Equal, 1)]
//        [SubEnum(RenderSettings,UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0.0
//        [ShowIf(RenderSettings, _Surface, Equal, 0)]
//        [SubToggle(RenderSettings)] _ZWrite("Z Write", Float) = 1.0
//        [SubEnum(RenderSettings,UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2.0
//
//        [ShowIf(RenderSettings, _Surface, Equal, 0)]
//        [SubToggle(RenderSettings,_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0.0
//        [ShowIf(RenderSettings, _AlphaClip, Equal, 1)]
//        [Sub(RenderSettings)] _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "ZeroRP"
            "RenderType"="Opaque"
        }
        
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "GBuffer"
            }
           ZWrite On

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex LitGBufferPassVertex
            #pragma fragment LitGBufferPassFragment

            // -------------------------------------
            //  Keywords
            // #pragma shader_feature_local _NORMALMAP
            // #pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Assets/ZeroRP/Shaders/PBRLitInput.hlsl"
            #include "Assets/ZeroRP/Shaders/PBRLitGBufferPass.hlsl"
            
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}
