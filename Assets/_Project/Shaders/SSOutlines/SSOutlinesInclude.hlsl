#ifndef SOBELOUTLINES_INCLUDED
#define SOBELOUTLINES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

static float2 sobelSamplePoints[9] = {
    float2(-1, 1), float2(0, 1), float2(1, 1),
    float2(-1, 0), float2(0, 0), float2(1, 0),
    float2(-1, -1), float2(0, -1), float2(1, -1),
};

static float sobelXMatrix[9] = {
    1, 0, -1,
    2, 0, -2,
    1, 0, -1
};

static float sobelYMatrix[9] = {
    1, 2, 1,
    0, 0, 0,
    -1, -2, -1
};

static half2 sobelSamplePointsHalf[9] = {
    half2(-1, 1), half2(0, 1), half2(1, 1),
    half2(-1, 0), half2(0, 0), half2(1, 0),
    half2(-1, -1), half2(0, -1), half2(1, -1),
};

static half sobelXMatrixHalf[9] = {
    1, 0, -1,
    2, 0, -2,
    1, 0, -1
};

static half sobelYMatrixHalf[9] = {
    1, 2, 1,
    0, 0, 0,
    -1, -2, -1
};

void DepthSobel_float(float2 UV, float Thickness, out float Out) {
    float2 sobel = 0;

    [unroll] for (int i = 0; i < 9; i++) {
        float depth = SampleSceneDepth(UV + sobelSamplePoints[i] * Thickness);
        sobel += depth * float2(sobelXMatrix[i], sobelYMatrix[i]);
    }

    Out = length(sobel);
}

void ColorSobel_float(float2 UV, float Thickness, out float Out) {
    float2 sobelR = 0;
    float2 sobelG = 0;
    float2 sobelB = 0;

    [unroll] for (int i = 0; i < 9; i++) {
        float3 rgb = SampleSceneColor(UV + sobelSamplePoints[i] * Thickness);
        float2 kernel = float2(sobelXMatrix[i], sobelYMatrix[i]);

        sobelR += rgb.r * kernel;
        sobelG += rgb.g * kernel;
        sobelB += rgb.b * kernel;
    }

    Out = max(length(sobelR), max(length(sobelG), length(sobelB)));
}

void GetViewSpaceNormals_float(float2 UV, out float3 Out) {
    float3 worldNormal = SampleSceneNormals(UV);
    Out = mul((float3x3)UNITY_MATRIX_V, worldNormal);
}

void NormalsSobel_float(float2 UV, float Thickness, out float Out) {
    float2 sobelX = 0;
    float2 sobelY = 0;
    float2 sobelZ = 0;

    [unroll] for (int i = 0; i < 9; i++) {
        float3 viewNormal;
        GetViewSpaceNormals_float(UV + sobelSamplePoints[i] * Thickness, viewNormal);

        viewNormal = (viewNormal + 1) / 2;
        
        float2 kernel = float2(sobelXMatrix[i], sobelYMatrix[i]);

        sobelX += viewNormal.x * kernel;
        sobelY += viewNormal.y * kernel;
        sobelZ += viewNormal.z * kernel;
    }

    Out = max(length(sobelX), max(length(sobelY), length(sobelZ)));
}

void ViewDirectionFromScreenUV_float(float2 In, out float3 Out) {
    float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
    Out = -normalize(float3((In * 2 - 1) / p11_22, -1));
}

void DepthSobel_half(half2 UV, half Thickness, out half Out) {
    half2 sobel = 0;

    [unroll] for (int i = 0; i < 9; i++) {
        half depth = SampleSceneDepth(UV + sobelSamplePointsHalf[i] * Thickness);
        sobel += depth * half2(sobelXMatrixHalf[i], sobelYMatrixHalf[i]);
    }

    Out = length(sobel);
}

void ColorSobel_half(half2 UV, half Thickness, out half Out) {
    half2 sobelR = 0;
    half2 sobelG = 0;
    half2 sobelB = 0;

    [unroll] for (int i = 0; i < 9; i++) {
        half3 rgb = SampleSceneColor(UV + sobelSamplePointsHalf[i] * Thickness);
        half2 kernel = half2(sobelXMatrixHalf[i], sobelYMatrixHalf[i]);

        sobelR += rgb.r * kernel;
        sobelG += rgb.g * kernel;
        sobelB += rgb.b * kernel;
    }

    Out = max(length(sobelR), max(length(sobelG), length(sobelB)));
}

void GetViewSpaceNormals_half(half2 UV, out half3 Out) {
    half3 worldNormal = SampleSceneNormals(UV);
    Out = mul((half3x3)UNITY_MATRIX_V, worldNormal);
}

void NormalsSobel_half(half2 UV, half Thickness, out half Out) {
    half2 sobelX = 0;
    half2 sobelY = 0;
    half2 sobelZ = 0;

    [unroll] for (int i = 0; i < 9; i++) {
        half3 viewNormal;
        GetViewSpaceNormals_half(UV + sobelSamplePointsHalf[i] * Thickness, viewNormal);

        viewNormal = (viewNormal + 1) / 2;
        
        half2 kernel = half2(sobelXMatrixHalf[i], sobelYMatrixHalf[i]);

        sobelX += viewNormal.x * kernel;
        sobelY += viewNormal.y * kernel;
        sobelZ += viewNormal.z * kernel;
    }

    Out = max(length(sobelX), max(length(sobelY), length(sobelZ)));
}

void ViewDirectionFromScreenUV_half(half2 In, out half3 Out) {
    half2 p11_22 = half2(unity_CameraProjection._11, unity_CameraProjection._22);
    Out = -normalize(half3((In * 2 - 1) / p11_22, -1));
}

#endif