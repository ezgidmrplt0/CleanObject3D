using UnityEngine;
using UnityEngine.EventSystems;

// Bu script eklendiği objeyi Clean Mode (kamera kilitliyken) sırasında XZ düzleminde sürüklenebilir yapar.
// CleanModeCamera: controlsEnabled == false iken hareket, true iken hareket yok.
public class MovableInCleanMode : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Yüksekliği sabitle (ilk Y değerinde kalsın)")] public bool lockY = true;
    [Tooltip("Raycast maksimum mesafe")] public float pickRayDistance = 1000f;
    [Tooltip("UI üzerindeyken sürükleme alma")] public bool ignoreWhenPointerOverUI = true;

    public enum LimitShape { Circle, Rectangle }

    [Header("Hareket Sınırı")]
    [Tooltip("Hareket mesafesini sınırla")] public bool limitMovement = false;
    [Tooltip("Merkez transformu (boşsa başlangıç pozisyonu kullanılır)")] public Transform movementCenter;
    [Tooltip("Merkeze opsiyonel offset")]
    public Vector3 centerOffset;
    [Tooltip("Daire sınırı kullan")] public LimitShape limitShape = LimitShape.Circle;
    [Tooltip("Daire için maksimum yarıçap")] [Min(0f)] public float maxRadius = 1f;
    [Tooltip("Dikdörtgen için ±X/±Z yarıçapları")] public Vector2 maxRectangleHalfSize = new Vector2(1f, 1f);

    [Header("Görsel")]
    [Tooltip("Seçiliyken opsiyonel highlight materyali (boşsa yok)")] public Material highlightMaterial;

    Camera cam;
    CleanModeCamera cleanController;
    Transform dragTarget; // genellikle kendi transform
    bool dragging = false;
    float dragPlaneY;
    Vector3 dragOffsetXZ;
    Material[] originalMats;
    Renderer rend;
    Vector3 initialCenter;

    void Awake()
    {
        dragTarget = transform;
        if (!cam) cam = Camera.main;
        if (!cleanController) cleanController = FindObjectOfType<CleanModeCamera>();
        rend = GetComponentInChildren<Renderer>();
        if (rend && highlightMaterial)
        {
            originalMats = rend.sharedMaterials;
        }

        initialCenter = transform.position;
    }

    void Update()
    {
        if (cleanController && cleanController.controlsEnabled) // Kamera aktifse hareket yok
        {
            if (dragging) EndDrag();
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
        if (ignoreWhenPointerOverUI && EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0)) TryBeginDrag(Input.mousePosition);
        if (dragging && Input.GetMouseButton(0)) UpdateDrag(Input.mousePosition);
        if (dragging && Input.GetMouseButtonUp(0)) EndDrag();
    }

    void HandleTouch()
    {
        if (Input.touchCount == 0) return;
        Touch t = Input.touches[0];
        if (ignoreWhenPointerOverUI && EventSystem.current && EventSystem.current.IsPointerOverGameObject(t.fingerId)) return;

        switch (t.phase)
        {
            case TouchPhase.Began:
                TryBeginDrag(t.position);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (dragging) UpdateDrag(t.position);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (dragging) EndDrag();
                break;
        }
    }

    void TryBeginDrag(Vector2 screenPos)
    {
        if (!cam) cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, pickRayDistance))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                dragging = true;
                dragPlaneY = lockY ? transform.position.y : hit.point.y;
                // offset XZ (objeyi pointer'ın altından tutma hissi)
                Vector3 objPos = transform.position;
                dragOffsetXZ = new Vector3(objPos.x - hit.point.x, 0f, objPos.z - hit.point.z);
                ApplyHighlight(true);
            }
        }
    }

    void UpdateDrag(Vector2 screenPos)
    {
        if (!cam) return;
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (IntersectRayWithHorizontalPlane(ray, dragPlaneY, out Vector3 hitPoint))
        {
            Vector3 target = hitPoint + dragOffsetXZ;
            Vector3 finalPos = new Vector3(target.x, dragPlaneY, target.z);
            if (limitMovement)
                finalPos = ClampToLimits(finalPos);
            dragTarget.position = finalPos;
        }
    }

    void EndDrag()
    {
        dragging = false;
        ApplyHighlight(false);
    }

    bool IntersectRayWithHorizontalPlane(Ray ray, float y, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        if (Mathf.Abs(ray.direction.y) < 1e-6f) return false;
        float t = (y - ray.origin.y) / ray.direction.y;
        if (t < 0f) return false;
        hitPoint = ray.origin + ray.direction * t;
        return true;
    }

    void ApplyHighlight(bool on)
    {
        if (!rend || !highlightMaterial) return;
        if (on)
        {
            // Basitçe tüm materyalleri geçici olarak highlight ile değiştir
            Material[] arr = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = highlightMaterial;
            rend.sharedMaterials = arr;
        }
        else if (originalMats != null)
        {
            rend.sharedMaterials = originalMats;
        }
    }

    Vector3 ResolveCenterPosition()
    {
        if (movementCenter)
            return movementCenter.position + centerOffset;
        return transform.position + centerOffset;
    }

    Vector3 ClampToLimits(Vector3 worldPos)
{
    Vector3 center = initialCenter;  // ← sadece bu!

    Vector3 flatDelta = new Vector3(worldPos.x - center.x, 0f, worldPos.z - center.z);

    if (limitShape == LimitShape.Circle)
    {
        float max = Mathf.Max(0.0001f, maxRadius);
        if (flatDelta.sqrMagnitude > max * max)
            flatDelta = flatDelta.normalized * max;
    }
    else
    {
        Vector2 halfSize = maxRectangleHalfSize;
        flatDelta.x = Mathf.Clamp(flatDelta.x, -halfSize.x, halfSize.x);
        flatDelta.z = Mathf.Clamp(flatDelta.z, -halfSize.y, halfSize.y);
    }

    return new Vector3(center.x + flatDelta.x, worldPos.y, center.z + flatDelta.z);
}

}
