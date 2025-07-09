using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Game.Graphics.Outlines
{
    public class SSOutlinesRenderPass : ScriptableRenderPass
    {
        private static readonly int _colorID = Shader.PropertyToID("_Color");
        private static readonly int _thicknessID = Shader.PropertyToID("_Thickness");

        private static readonly int _depthStrengthID = Shader.PropertyToID("_DepthStrength");
        private static readonly int _depthThicknessID = Shader.PropertyToID("_DepthThickness");
        private static readonly int _depthThresholdID = Shader.PropertyToID("_DepthThreshold");

        private static readonly int _colorStrengthID = Shader.PropertyToID("_ColorStrength");
        private static readonly int _colorThicknessID = Shader.PropertyToID("_ColorThickness");
        private static readonly int _colorThresholdID = Shader.PropertyToID("_ColorThreshold");

        private static readonly int _normalsStrengthID = Shader.PropertyToID("_NormalsStrength");
        private static readonly int _normalsThicknessID = Shader.PropertyToID("_NormalsThickness");
        private static readonly int _normalsThresholdID = Shader.PropertyToID("_NormalsThreshold");

        private static readonly int _acuteAngleStartDotID = Shader.PropertyToID("_AcuteAngleStartDot");
        private static readonly int _acuteDepthThresholdID = Shader.PropertyToID("_AcuteDepthThreshold");

        private static readonly int _normalAdjustNearDepthID = Shader.PropertyToID("_NormalAdjustNearDepth");
        private static readonly int _normalAdjustFarDepthID = Shader.PropertyToID("_NormalAdjustFarDepth");
        private static readonly int _normalsFarThresholdID = Shader.PropertyToID("_NormalsFarThreshold");

        private SSOutlinesProperties _properties;
        private Material _material;

        public SSOutlinesRenderPass(Material material, SSOutlinesProperties properties)
        {
            _material = material;
            _properties = properties;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resData = frameData.Get<UniversalResourceData>();
            if (resData.isActiveTargetBackBuffer)
                return;

            var srcColor = resData.activeColorTexture;

            var dstDesc = srcColor.GetDescriptor(renderGraph);
            dstDesc.name = "_SSOTexture";
            var dstColor = renderGraph.CreateTexture(dstDesc);

            UpdateProperties();

            var param = new RenderGraphUtils.BlitMaterialParameters(srcColor, dstColor, _material, 0);
            renderGraph.AddBlitPass(param, "ScreenSpaceOutlinesPass");

            resData.cameraColor = dstColor;
        }

        private void UpdateProperties()
        {
            if (_material == null) return;

            _material.SetColor(_colorID, _properties.color);
            _material.SetFloat(_thicknessID, _properties.thickness);

            _material.SetFloat(_depthStrengthID, _properties.depthStrength);
            _material.SetFloat(_depthThicknessID, _properties.depthThickness);
            _material.SetFloat(_depthThresholdID, _properties.depthThreshold);

            _material.SetFloat(_colorStrengthID, _properties.colorStrength);
            _material.SetFloat(_colorThicknessID, _properties.colorThickness);
            _material.SetFloat(_colorThresholdID, _properties.colorThreshold);

            _material.SetFloat(_normalsStrengthID, _properties.normalsStrength);
            _material.SetFloat(_normalsThicknessID, _properties.normalsThickness);
            _material.SetFloat(_normalsThresholdID, _properties.normalsThreshold);

            _material.SetFloat(_acuteAngleStartDotID, _properties.acuteAngleStartDot);
            _material.SetFloat(_acuteDepthThresholdID, _properties.acuteDepthThreshold);

            _material.SetFloat(_normalAdjustNearDepthID, _properties.normalAdjustNearDepth);
            _material.SetFloat(_normalAdjustFarDepthID, _properties.normalAdjustFarDepth);
            _material.SetFloat(_normalsFarThresholdID, _properties.normalsFarThreshold);
        }
    }
}