using UnityEngine;
using UnityEngine.Rendering;

namespace HollywoodFX.Lighting;

public class ShadowMapCopy : MonoBehaviour
{
    private CommandBuffer _cmd;
    
    public void OnEnable()
    {
        var camera = GetComponent<Camera>();
        if (camera == null) return;
        
        var allRTs = Resources.FindObjectsOfTypeAll<RenderTexture>();
        
        foreach (var rt in allRTs)
        {
            Plugin.Log.LogInfo($"RT: {rt.name} {rt.width}x{rt.height} format:{rt.format}");
            
            if (rt.name != "DSShadowMaskFull") continue;
            
            Shader.SetGlobalTexture("_HFXShadowMap", rt);
            Plugin.Log.LogInfo($"Found BSG shadow mask: {rt.width}x{rt.height}");
            break;
        }
        
        var cmd = new CommandBuffer { name = "HFX Shadow Copy" };
        var shadowCopyId = Shader.PropertyToID("_HFXShadowMapHack");

        // Copy the built-in screen-space shadow texture into a global texture your shaders can read
        cmd.GetTemporaryRT(shadowCopyId, Screen.width/2, Screen.height/2, 0, FilterMode.Bilinear, RenderTextureFormat.RGB565);
        cmd.Blit(BuiltinRenderTextureType.CurrentActive, shadowCopyId);
        cmd.SetGlobalTexture("_HFXShadowMapHack", shadowCopyId);

        camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, cmd);
        
        Plugin.Log.LogInfo("Shadow map copy command buffer attached");
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
    //         _cmd.SetGlobalTexture("_HFXShadowMap", _shadowRT);
    //         
    //         camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _cmd);
    //     }
    // }
    
    
    // public void OnPostRender()
    // {
    //     if (_shadowRT != null && _shadowRT.IsCreated())
    //     {
    //         Shader.SetGlobalTexture("_HFXShadowMap", _shadowRT);
    //     }
    // }
    
    public void OnGUI()
    {
        var hfxShadowMap = Shader.GetGlobalTexture("_HFXShadowMap");
        var hfxShadowMapHack = Shader.GetGlobalTexture("_HFXShadowMapHack");
    
        if (hfxShadowMap != null)
        {
            GUI.DrawTexture(new Rect(32, 32, 512, 512), hfxShadowMap);
        }
        
        if (hfxShadowMapHack != null)
        {
            GUI.DrawTexture(new Rect(32, 512+64, 512, 512), hfxShadowMapHack);
        }
    }

    public void OnDisable()
    {
        var camera = GetComponent<Camera>();
        if (camera == null || _cmd == null) return;

        camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, _cmd);
        _cmd.Release();
        _cmd = null;
    }
}