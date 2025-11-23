using UnityEngine;

namespace HollywoodFX.Render;

public class PostProcessing : MonoBehaviour
{
    public ConcussionController Concussion;
    private ScopeDofController _scopeDof;

    public void Init()
    {
        var dof = new DepthOfField();
        Concussion = new ConcussionController(dof);
        _scopeDof = new ScopeDofController(dof);
    }
    
    public void Update()
    {
        Concussion.Update();
        _scopeDof.Update();
    }
    
    public void OnDestroy()
    {
        Concussion.OnDestroy();       
    }
}