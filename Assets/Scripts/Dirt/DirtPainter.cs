using UnityEngine;

// Global painter: during inspection, hold-drag over a DirtPaintable to clean it by
// painting into its dirt mask. Requires MeshCollider with readable mesh to get UVs.
public class DirtPainter : MonoBehaviour
{
    [Header("General")]
    public Camera cam;                                 // if empty, uses Camera.main
    public bool onlyWhileInspecting = true;            // only active in inspection mode

    [Header("Brush")]
    [Tooltip("Brush radius in UV space (0..1) typical 0.02 - 0.08")] 
    [Range(0.001f, 0.25f)] public float brushRadiusUV = 0.04f;
    [Tooltip("How much to clean per dab (0..1)")] 
    [Range(0.01f, 1f)] public float brushStrength = 0.35f;
    [Tooltip("If true, paints continuously while dragging; else only on release")] 
    public bool continuous = true;

    [Header("Input")] 
    public bool ignoreUI = true; // optional: could check EventSystem if you want

    void Start()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        if (onlyWhileInspecting)
        {
            var sys = ObjectInspectionSystem.Instance;
            if (!sys || sys.CurrentState != ObjectInspectionSystem.GameState.Inspecting)
                return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    void HandleMouse()
    {
        bool pressed = Input.GetMouseButton(0);
        if (!pressed && !Input.GetMouseButtonUp(0)) return;
        TryPaint(Input.mousePosition, pressed && continuous);
    }

    void HandleTouch()
    {
        if (Input.touchCount == 0) return;
        var t = Input.touches[0];
        bool doPaint = (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary);
        if (!continuous && t.phase != TouchPhase.Ended) return;
        TryPaint(t.position, continuous ? doPaint : true);
    }

    void TryPaint(Vector2 screenPos, bool doIt)
    {
        if (!doIt) return;
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, 1000f))
        {
            var paintable = hit.collider.GetComponentInParent<DirtPaintable>();
            if (!paintable) return;

            // Need UVs from collider
            Vector2 uv = hit.textureCoord; // requires MeshCollider with readable mesh
            // optional smoothing from normal etc.
            paintable.PaintAtUV(uv, brushRadiusUV, brushStrength);
        }
    }
}
