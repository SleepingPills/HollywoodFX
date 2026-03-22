using UnityEngine;
using UnityEngine.Rendering;

namespace HollywoodFX.Lighting;

public class ShadowMapCopy : MonoBehaviour
{
    private RenderTexture _sceneTex1;
    private RenderTexture _sceneTex2;
    
    private RenderTexture _depthTex1;
    private RenderTexture _depthTex2;

    private Material _sceneCopyMat;
    private Material _depthCopyMat;
    private Material _blurMat;
    
    public void OnEnable()
    {
        var camera = GetComponent<Camera>();
        if (camera == null) return;

        var qw = Screen.width / 16;
        var qh = Screen.height / 16;
        
        _sceneTex1 = new RenderTexture(qw, qh, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear
        };
        _sceneTex2 = new RenderTexture(qw, qh, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear
        };
        
        _depthTex1 = new RenderTexture(qw, qh, 0, RenderTextureFormat.RFloat)
        {
            filterMode = FilterMode.Bilinear
        };
        _depthTex2 = new RenderTexture(qw, qh, 0, RenderTextureFormat.RFloat)
        {
            filterMode = FilterMode.Bilinear
        };
        
        _sceneCopyMat = AssetRegistry.AssetBundle.LoadAsset<Material>("Assets/HollywoodFX/Particles/Material/SceneCopyMat.mat");
        _depthCopyMat = AssetRegistry.AssetBundle.LoadAsset<Material>("Assets/HollywoodFX/Particles/Material/DepthCopyMat.mat");
        _blurMat = AssetRegistry.AssetBundle.LoadAsset<Material>("Assets/HollywoodFX/Particles/Material/BlurMat.mat");
        
        Plugin.Log.LogInfo($"Depth copy material: {_depthCopyMat} Blur material: {_blurMat}");

        var cmd = new CommandBuffer { name = "HFX Scene Copy" };
        
        var texelSize = new Vector4(1f / qw, 1f / qh, 0, 0);
        cmd.SetGlobalVector("_TexelSize", texelSize);
        
        cmd.Blit(BuiltinRenderTextureType.CurrentActive, _sceneTex1, _sceneCopyMat);
        cmd.Blit(_sceneTex1, _sceneTex2, _blurMat);
        cmd.Blit(_sceneTex2, _sceneTex1, _blurMat);
        cmd.Blit(_sceneTex1, _sceneTex2, _blurMat);
        cmd.Blit(_sceneTex2, _sceneTex1, _blurMat);
        cmd.Blit(_sceneTex1, _sceneTex2, _blurMat);
        cmd.SetGlobalTexture("_HFXSceneTex", _sceneTex2);
        
        cmd.Blit(null, _depthTex1, _depthCopyMat);
        cmd.Blit(_depthTex1, _depthTex2, _blurMat);
        cmd.Blit(_depthTex2, _depthTex1, _blurMat);
        cmd.Blit(_depthTex1, _depthTex2, _blurMat);
        cmd.Blit(_sceneTex2, _sceneTex1, _blurMat);
        cmd.Blit(_sceneTex1, _sceneTex2, _blurMat);
        cmd.SetGlobalTexture("_HFXDepthTex", _depthTex2);
        
        camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, cmd);
        
        Plugin.Log.LogInfo("Scene map copy command buffer attached");
    }
    
    // public void Update()
    // {
    //     if (_shadowRT != null && (_shadowRT.width != Screen.width || _shadowRT.height != Screen.height))
    //     {
    //         var camera = GetComponent<Camera>();
    //         if (camera == null) return;
    //
    //         camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _cmd);
    //         _shadowRT.Release();
    //
    //         _shadowRT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8)
    //         {
    //             filterMode = FilterMode.Bilinear
    //         };
    //
    //         _copyMat = AssetRegistry.AssetBundle.LoadAsset<Material>("ShadowCopyMat");
    //         
    //         _cmd = new CommandBuffer { name = "HFX Shadow Copy" };
    //         _cmd.Blit(null, _shadowRT, _copyMat);
    //         _cmd.SetGlobalTexture("_HFXSceneTex", _shadowRT);
    //         
    //         camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _cmd);
    //     }
    // }
    
    
    // public void OnPostRender()
    // {
    //     if (_shadowRT != null && _shadowRT.IsCreated())
    //     {
    //         Shader.SetGlobalTexture("_HFXSceneTex", _shadowRT);
    //     }
    // }
    
    public void OnGUI()
    {
        GUI.DrawTexture(new Rect(32, 32, _sceneTex2.width * 2, _sceneTex2.height * 2), _sceneTex2);
        GUI.DrawTexture(new Rect(32, 64 + _sceneTex2.height * 2, _depthTex2.width * 2, _depthTex2.height * 2), _depthTex2);
        
        
    }

    // public void OnDisable()
    // {
    //     var camera = GetComponent<Camera>();
    //     if (camera == null || _cmd == null) return;
    //
    //     camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _cmd);
    //     _cmd.Release();
    //     _cmd = null;
    // }
}