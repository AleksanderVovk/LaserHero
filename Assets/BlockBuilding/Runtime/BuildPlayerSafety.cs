using UnityEngine;

// Integrates with the existing controller through the session's reset event.
public class BuildPlayerSafety : MonoBehaviour
{
    [Header("Construction"), Tooltip("Existing runtime; only placed cells are considered solid.")]
    public BlockLevelRuntime runtime;
    [Header("Session"), Tooltip("Existing session and controller reset hook.")]
    public BuildLevelSession session;
    [Header("Player"), Tooltip("Existing player root. Its controller is never replaced.")]
    public Transform player;
    [Header("Capsule"), Tooltip("Existing CharacterController; optional for Transform-only controllers.")]
    public CharacterController character;
    [Header("Clearance"), Tooltip("Space above built surfaces and below a new ceiling."), Min(0.02f)]
    public float clearance=0.15f;
    [Header("Fallback height"), Tooltip("Player height when there is no CharacterController."), Min(0.2f)]
    public float fallbackHeight=1.8f;
    [Header("Fallback radius"), Tooltip("Player radius when there is no CharacterController."), Min(0.05f)]
    public float fallbackRadius=0.5f;
    [Header("Fallback feet"), Tooltip("Local feet height relative to the player pivot, without CharacterController.")]
    public float fallbackFeetOffset=0;
    [Header("Return button"), Tooltip("Normal gameplay button; unavailable until a block has been placed.")]
    public UnityEngine.UI.Button returnButton;
    [Header("Keyboard"), Tooltip("Old Input Manager shortcut for the same return action.")]
    public KeyCode returnKey=KeyCode.T;

    public int LiftCount { get; private set; }
    public int ReturnCount { get; private set; }
    public bool CanReturn { get { return Playable && runtime.LastPlacedSite!=null; } }
    private bool Playable { get { return runtime!=null && player!=null && (session==null || !session.MenuOpen && !session.AdminOverlayOpen && (!session.Completed || session.IsExploringCompleted)) && runtime.carrier!=null && !runtime.carrier.IsTraining; } }

    private void OnEnable()
    {
        if(runtime!=null) { runtime.BlockPlaced+=Placed; runtime.Changed+=Refresh; }
        if(session!=null) session.Changed+=Refresh;
        if(runtime!=null && runtime.carrier!=null) runtime.carrier.Changed+=Refresh;
        if(returnButton!=null) returnButton.onClick.AddListener(ReturnToLastBlock);
        Refresh();
    }
    private void OnDisable()
    {
        if(runtime!=null) { runtime.BlockPlaced-=Placed; runtime.Changed-=Refresh; }
        if(session!=null) session.Changed-=Refresh;
        if(runtime!=null && runtime.carrier!=null) runtime.carrier.Changed-=Refresh;
        if(returnButton!=null) returnButton.onClick.RemoveListener(ReturnToLastBlock);
    }
    private void Update()
    {
        if(CanReturn && Input.GetKeyDown(returnKey)) ReturnToLastBlock();
    }
    private void Refresh() { if(returnButton!=null) returnButton.interactable=CanReturn; }
    private Bounds PlayerBounds()
    {
        Vector3 scale=player.lossyScale;
        if(character!=null)
        {
            float radius=character.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
            float height=Mathf.Max(character.height*Mathf.Abs(scale.y),radius*2);
            return new Bounds(player.TransformPoint(character.center),new Vector3(radius*2,height,radius*2));
        }
        float h=fallbackHeight*Mathf.Abs(scale.y);
        return new Bounds(player.position+Vector3.up*(fallbackFeetOffset*scale.y+h*0.5f),new Vector3(fallbackRadius*2,h,fallbackRadius*2));
    }
    private void Placed(BlockType type,Vector3 position)
    {
        if(!Playable || runtime.LastPlacedSite==null) return;
        var site=runtime.LastPlacedSite;
        Bounds block=site.builder.CellWorldBounds(site.LastBuiltCell);
        Bounds body=PlayerBounds();
        // Include near-touching ceilings, not just an already embedded capsule.
        Bounds protectedBody=body; protectedBody.Expand(clearance*2);
        if(!protectedBody.Intersects(block)) return;
        float top=runtime.HighestBuiltTop(body.center,body.extents.x+clearance);
        if(float.IsNegativeInfinity(top)) return;
        float lift=top+clearance-body.min.y;
        if(lift<=0) return;
        MovePlayer(player.position+Vector3.up*lift); LiftCount++;
    }
    public void ReturnToLastBlock()
    {
        if(!CanReturn) return;
        Bounds body=PlayerBounds();
        Vector3 target=runtime.LastPlacedPosition;
        // Offset the pivot so the capsule center, including controller.center,
        // is above the last cell. Also account for higher completed structures.
        float top=runtime.HighestBuiltTop(target,body.extents.x+clearance);
        if(float.IsNegativeInfinity(top)) return;
        Vector3 centerOffset=body.center-player.position;
        target.y=top+clearance+body.extents.y;
        MovePlayer(target-centerOffset); ReturnCount++;
    }
    private void MovePlayer(Vector3 destination)
    {
        // The supporting cell must be solid now, not after the batching delay.
        runtime.FlushBuiltColliders();
        bool restore=character!=null && character.enabled;
        if(restore) character.enabled=false;
        player.position=destination;
        Physics.SyncTransforms();
        if(restore) character.enabled=true;
        if(session!=null) session.onPlayerReset.Invoke();
    }
}
