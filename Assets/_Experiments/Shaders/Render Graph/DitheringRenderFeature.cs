using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class DitheringRenderFeature : ScriptableRendererFeature
{
    class DitherEffectPass : ScriptableRenderPass
    {
        private const string _passName = "DitherEffectPass";

        private Material _blitMaterial;

        public void Setup(Material mat)
        {
            _blitMaterial = mat;
            requiresIntermediateTexture = true;
        }

       

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
           
            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.Log("Can't use active back buffer as target");
                return;
            }

            // Create a new texture descriptor for the intermediate texture
            var source = resourceData.activeColorTexture;
            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = $"CameraColor-{_passName}";
            destinationDesc.clearBuffer = false;
            
            // Create a new texture handle for the intermediate texture
            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
            
            // Create blit pass params and then add the blit pass to the render graph
            RenderGraphUtils.BlitMaterialParameters blitParams = new(source, destination, _blitMaterial, 0);
            renderGraph.AddBlitPass(blitParams);

            resourceData.cameraColor = destination;
        }

    }

    
    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    public Material Mat;
    DitherEffectPass _scriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        _scriptablePass = new DitherEffectPass();

        // Configures where the render pass should be injected.
        _scriptablePass.renderPassEvent = injectionPoint;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (Mat == null)
        {
            Debug.Log("No mat found");
            return;
        }
        
        _scriptablePass.Setup(Mat);
        renderer.EnqueuePass(_scriptablePass);
    }
}
