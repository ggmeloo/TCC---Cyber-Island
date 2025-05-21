using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Water_Volume : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass, System.IDisposable
    {
        public RTHandle sourceColorHandle; // For the camera's color target

        private Material _material;
        private RTHandle m_TempColorTarget; // RTHandle for our intermediate texture
        private int m_TempColorTargetId;    // Cached SHADER ID for the temporary texture

        public CustomRenderPass(Material mat)
        {
            _material = mat;
            string tempTargetName = "_TemporaryColourTexture_WaterVolumePass";
            m_TempColorTarget = RTHandles.Alloc(tempTargetName, name: tempTargetName);
            m_TempColorTargetId = Shader.PropertyToID(tempTargetName);
        }

        public void Dispose()
        {
            m_TempColorTarget?.Release();
            m_TempColorTarget = null;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Corrigido: Remover .IsValid()
            if (sourceColorHandle != null)
            {
                ConfigureTarget(sourceColorHandle);
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Nenhuma mudança necessária aqui
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            // Corrigido: Remover .IsValid()
            if (_material == null || sourceColorHandle == null || m_TempColorTarget == null)
            {
                // Adicionar um log se algo estiver faltando para ajudar na depuração
                if (_material == null) Debug.LogError("Water_Volume Pass: Material is null.");
                if (sourceColorHandle == null) Debug.LogError("Water_Volume Pass: sourceColorHandle is null.");
                if (m_TempColorTarget == null) Debug.LogError("Water_Volume Pass: m_TempColorTarget is null.");
                return;
            }

            CommandBuffer commandBuffer = CommandBufferPool.Get("WaterVolumePass_Execute");

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            commandBuffer.GetTemporaryRT(m_TempColorTargetId, descriptor, FilterMode.Bilinear);

            // O Blit usa o RTHandle, que por sua vez usa seu nameID internamente
            // para referenciar a textura obtida por GetTemporaryRT.
            Blit(commandBuffer, sourceColorHandle, m_TempColorTarget, _material);
            Blit(commandBuffer, m_TempColorTarget, sourceColorHandle);

            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (m_TempColorTargetId != 0)
            {
                cmd.ReleaseTemporaryRT(m_TempColorTargetId);
            }
        }
    }

    [System.Serializable]
    public class _Settings
    {
        public Material material = null;
        public RenderPassEvent renderPass = RenderPassEvent.AfterRenderingSkybox;
    }

    public _Settings settings = new _Settings();
    CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        if (settings.material == null)
        {
            settings.material = Resources.Load<Material>("Water_Volume");
            if (settings.material == null)
            {
                Debug.LogError("Water_Volume material not found in Resources. Please assign it in the inspector.");
                return;
            }
        }

        if (m_ScriptablePass != null)
        {
            m_ScriptablePass.Dispose();
        }
        m_ScriptablePass = new CustomRenderPass(settings.material);
        m_ScriptablePass.renderPassEvent = settings.renderPass;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_ScriptablePass?.Dispose();
            m_ScriptablePass = null;
        }
        base.Dispose(disposing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_ScriptablePass == null || settings.material == null)
        {
            return;
        }
        m_ScriptablePass.sourceColorHandle = renderer.cameraColorTargetHandle;

        // Corrigido: Remover .IsValid()
        if (m_ScriptablePass.sourceColorHandle == null)
        {
            // Pode ser normal se o renderer ainda não tiver um cameraColorTargetHandle (ex: câmera inativa)
            // Debug.LogWarning("Water_Volume Pass: sourceColorHandle is null in AddRenderPasses. Pass will not be enqueued.");
            return;
        }

        renderer.EnqueuePass(m_ScriptablePass);
    }
}