Shader "FullScreen/PS1SignalisHDRP"
{
    Properties
    {
        _PixelHeight ("Pixel Height", Float) = 240
        _ColorLevels ("Color Levels", Range(4, 64)) = 16
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.6
        _Saturation ("Saturation", Range(0, 1.5)) = 0.75
        _Contrast ("Contrast", Range(0.5, 1.5)) = 1.1
        _Tint ("Tint", Color) = (0.85, 0.95, 1, 1)
        _Aberration ("Chromatic Aberration", Range(0, 0.02)) = 0.006
        _GrainStrength ("Grain Strength", Range(0, 0.3)) = 0.06
        _ScanlineStrength ("Scanline Strength", Range(0, 0.6)) = 0.18
        _Vignette ("Vignette", Range(0, 1)) = 0.5
    }

    HLSLINCLUDE

    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    float _PixelHeight;
    float _ColorLevels;
    float _DitherStrength;
    float _Saturation;
    float _Contrast;
    float4 _Tint;
    float _Aberration;
    float _GrainStrength;
    float _ScanlineStrength;
    float _Vignette;

    // ตาราง Bayer 4x4 สำหรับ ordered dithering
    static const float BAYER4[16] =
    {
         0,  8,  2, 10,
        12,  4, 14,  6,
         3, 11,  1,  9,
        15,  7, 13,  5
    };

    float Bayer4x4(float2 pixel)
    {
        int x = (int)fmod(pixel.x, 4.0);
        int y = (int)fmod(pixel.y, 4.0);
        return BAYER4[y * 4 + x] / 16.0 - 0.5;
    }

    float Hash(float2 p)
    {
        return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
    }

    // อ่านสีจาก camera color buffer แบบ point โดย "ปัด" ตำแหน่งเป็นช่องพิกเซลหยาบๆ
    float3 SamplePixelated(float2 uv, float2 res)
    {
        float2 snapped = (floor(uv * res) + 0.5) / res;
        float2 maxCoord = _ScreenSize.xy - 1.0;
        uint2 coord = (uint2)clamp(snapped * _ScreenSize.xy, 0.0, maxCoord);
        return CustomPassLoadCameraColor(coord, 0);
    }

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float2 uv = varyings.positionCS.xy * _ScreenSize.zw;

        // 1) ความละเอียดต่ำ
        float aspect = _ScreenSize.x / _ScreenSize.y;
        float2 res = float2(_PixelHeight * aspect, _PixelHeight);
        float2 pixel = floor(uv * res);

        // 2) Chromatic Aberration
        float2 dir = uv - 0.5;
        float2 off = dir * _Aberration;
        float3 col;
        col.r = SamplePixelated(uv + off, res).r;
        col.g = SamplePixelated(uv, res).g;
        col.b = SamplePixelated(uv - off, res).b;

        // 3) Color grading
        float lum = dot(col, float3(0.299, 0.587, 0.114));
        col = lerp(lum.xxx, col, _Saturation);
        col = (col - 0.5) * _Contrast + 0.5;
        col *= _Tint.rgb;

        // 4) Film grain (เปลี่ยนลายประมาณ 12 ครั้ง/วินาที)
        float t = floor(_TimeParameters.x * 12.0);
        col += (Hash(pixel + t * 17.0) - 0.5) * _GrainStrength;

        // 5) Dithering + ลดจำนวนระดับสี
        float steps = _ColorLevels - 1.0;
        col += Bayer4x4(pixel) * _DitherStrength / steps;
        col = round(saturate(col) * steps) / steps;

        // 6) Scanline
        float scan = 0.5 + 0.5 * cos(uv.y * res.y * 6.28318);
        col *= 1.0 - _ScanlineStrength * scan;

        // 7) Vignette
        float d = length(dir);
        col *= 1.0 - _Vignette * smoothstep(0.3, 0.85, d);

        return float4(col, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Custom Pass 0"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
