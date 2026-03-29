using UnityEngine;
using UnityEngine.Rendering;

namespace HollywoodFX.Lighting;

public class ShadowMapCopy : MonoBehaviour
{
    private RenderTexture _sceneTex1;
    private RenderTexture _sceneTex2;
    private RenderTexture _sceneCurrent;
    private RenderTexture _sceneHistory;

    private RenderTexture _depthTex1;
    private RenderTexture _depthTex2;
    private RenderTexture _depthCurrent;
    private RenderTexture _depthHistory;

    private Material _sceneCopyMat;
    private Material _sceneBlendMat;
    private Material _depthCopyMat;
    private Material _depthBlendMat;
    private Material _blurMat;

    public void OnEnable()
    {
        var camera = GetComponent<Camera>();
        if (camera == null) return;

        var qw = Screen.width / 32;
        var qh = Screen.height / 32;

        _sceneTex1 = new RenderTexture(qw, qh, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
        _sceneTex2 = new RenderTexture(qw, qh, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
        _sceneCurrent = new RenderTexture(qw, qh, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
        _sceneHistory = new RenderTexture(qw, qh, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };

        _depthTex1 = new RenderTexture(qw, qh, 0, RenderTextureFormat.RFloat) { filterMode = FilterMode.Bilinear };
        _depthTex2 = new RenderTexture(qw, qh, 0, RenderTextureFormat.RFloat) { filterMode = FilterMode.Bilinear };
        _depthCurrent = new RenderTexture(qw, qh, 0, RenderTextureFormat.RFloat) { filterMode = FilterMode.Bilinear };
        _depthHistory = new RenderTexture(qw, qh, 0, RenderTextureFormat.RFloat) { filterMode = FilterMode.Bilinear };

        _sceneCopyMat = AssetRegistry.AssetBundle.LoadAsset<Material>(
            "Assets/HollywoodFX/Particles/Material/SceneCopyMat.mat"
        );
        _sceneCopyMat.SetTexture("_MainTex", null);
        _sceneBlendMat = AssetRegistry.AssetBundle.LoadAsset<Material>(
            "Assets/HollywoodFX/Particles/Material/SceneBlendMat.mat"
        );
        _sceneBlendMat.SetTexture("_MainTex", null);

        _depthCopyMat = AssetRegistry.AssetBundle.LoadAsset<Material>(
            "Assets/HollywoodFX/Particles/Material/DepthCopyMat.mat"
        );
        _depthCopyMat.SetTexture("_MainTex", null);
        _depthBlendMat = AssetRegistry.AssetBundle.LoadAsset<Material>(
            "Assets/HollywoodFX/Particles/Material/DepthBlendMat.mat"
        );
        _depthBlendMat.SetTexture("_MainTex", null);

        _blurMat = AssetRegistry.AssetBundle.LoadAsset<Material>(
            "Assets/HollywoodFX/Particles/Material/BlurMat.mat"
        );
        _blurMat.SetTexture("_MainTex", null);

        var cmd = new CommandBuffer { name = "HFX Scene Copy" };

        var texelSize = new Vector4(1f / qw, 1f / qh, 0, 0);
        cmd.SetGlobalVector("_TexelSize", texelSize);
        cmd.SetGlobalFloat("_BlendFactor", 0.05f);

        cmd.Blit(BuiltinRenderTextureType.CurrentActive, _sceneTex1, _sceneCopyMat);
        cmd.Blit(_sceneTex1, _sceneTex2, _blurMat);
        cmd.Blit(_sceneTex2, _sceneTex1, _blurMat);
        cmd.Blit(_sceneTex1, _sceneTex2, _blurMat);
        cmd.Blit(_sceneTex2, _sceneTex1, _blurMat);
        cmd.Blit(_sceneTex1, _sceneTex2, _blurMat);
        cmd.Blit(_sceneTex2, _sceneTex1, _blurMat);
        cmd.Blit(_sceneTex1, _sceneTex2, _blurMat);

        cmd.SetGlobalTexture("_HistoryTex", _sceneHistory);
        cmd.Blit(_sceneTex2, _sceneCurrent, _sceneBlendMat);
        cmd.Blit(_sceneCurrent, _sceneHistory);
        cmd.SetGlobalTexture("_HFXSceneLightTex", _sceneCurrent);

        cmd.Blit(null, _depthTex1, _depthCopyMat);
        cmd.Blit(_depthTex1, _depthTex2, _blurMat);
        cmd.Blit(_depthTex2, _depthTex1, _blurMat);
        cmd.Blit(_depthTex1, _depthTex2, _blurMat);
        cmd.Blit(_depthTex2, _depthTex1, _blurMat);
        cmd.Blit(_depthTex1, _depthTex2, _blurMat);

        cmd.SetGlobalTexture("_HistoryTex", _depthHistory);
        cmd.Blit(_depthTex2, _depthCurrent, _depthBlendMat);
        cmd.Blit(_depthCurrent, _depthHistory);
        cmd.SetGlobalTexture("_HFXDepthTex", _depthCurrent);

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
        GUI.DrawTexture(
            new Rect(32, 32, _sceneCurrent.width * 8, _sceneCurrent.height * 8), _sceneCurrent
        );
        GUI.DrawTexture(
            new Rect(32, 64 + _sceneTex2.height * 8, _sceneTex2.width * 8, _sceneCurrent.height * 8), _sceneTex2
        );
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