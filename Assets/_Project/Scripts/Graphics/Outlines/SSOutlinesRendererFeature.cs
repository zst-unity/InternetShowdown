using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Graphics.Outlines
{
    [Serializable]
    public struct SSOutlinesProperties
    {
        public Color color;
        public float thickness;

        [Space(9)]
        public float depthStrength;
        public float depthThickness;
        public float depthThreshold;

        [Space(9)]
        public float colorStrength;
        public float colorThickness;
        public float colorThreshold;

        [Space(9)]
        public float normalsStrength;
        public float normalsThickness;
        public float normalsThreshold;

        [Space(9)]
        public float acuteAngleStartDot;
        public float acuteDepthThreshold;

        [Space(9)]
        public float normalAdjustNearDepth;
        public float normalAdjustFarDepth;
        public float normalsFarThreshold;
    }

    public class SSOutlinesRendererFeature : ScriptableRendererFeature
    {
        public SSOutlinesProperties properties;
        public Shader shader;
        public RenderPassEvent injectionPoint;

        private Material _material;
        private SSOutlinesRenderPass _renderPass;

        public override void Create()
        {
            if (shader == null) return;

            _material = new(shader);
            _renderPass = new(_material, properties)
            {
                renderPassEvent = injectionPoint
            };

            _renderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            _renderPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            _renderPass.ConfigureInput(ScriptableRenderPassInput.Normal);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_renderPass == null) return;
            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(_renderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }
    }
}