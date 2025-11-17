using UnityEngine;
using UnityEngine.EventSystems;

// Tek scriptte: Basit kamera kontrolü + Temizlik modu kilidi
// - controlsEnabled == true  -> kamera hareket eder, temizlik çalışmaz (DirtCleaner bunu görür)
// - controlsEnabled == false -> kamera kilitli, temizlik çalışır (DirtCleaner bunu görür)
// DirtCleaner tarafında `onlyWhenCameraLocked = true` olmalı. Bu scriptte otomatikler.
public class CleanModeCamera : MonoBehaviour
{
    [Header("Bağlantılar")]
    public Camera cam;                   // Otomatik bulunur
    public DirtCleaner dirtCleaner;      // Sahnedeki DirtCleaner (opsiyonel)

    [Header("Kontrol Durumu")] 
    [Tooltip("true: Kamera hareket eder (temizlik kapalı) / false: Kamera kilitli (temizlik açık)")]
    public bool controlsEnabled = true;

    [Header("Pan (Sürükleme)")]
    [Range(0.05f, 5f)] public float panSpeed = 1.0f;
    public bool blockWhenPointerOverUI = true;

    [Header("Zoom")]
    public float minOrthoSize = 3f;
    public float maxOrthoSize = 12f;
    [Range(0.01f, 1f)] public float pinchZoomSpeed = 0.08f;   // mobil
    public float mouseScrollStep = 1.0f;                       // PC

    [Header("Sınırlar (Dünya)")]
    public bool clampToBounds = false;
    public Rect worldBounds = new Rect(-10, -10, 20, 20);

    // Dahili
    Vector2 lastTouchPos;
    bool hadSingleTouchLastFrame;
    float targetOrtho;

    // Toggle için durum
    bool toggleOn = false;
    bool wasControlsBefore = true;
    bool cachedUseBrush;
    bool cachedOnlyWhenLocked;

    void Awake()
    {
        if (!cam) cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;

        // Ortho varsayıyoruz (perspektif de kullanılabilir, zoom farklı ele alınır)
        if (cam != null && !cam.orthographic)
            cam.orthographic = true;

        if (dirtCleaner == null) dirtCleaner = FindObjectOfType<DirtCleaner>();
        if (dirtCleaner != null)
        {
            // DirtCleaner'ın kameraController alanına bu scripti bağlamak faydalı
            if (dirtCleaner.cameraController == null)
                dirtCleaner.cameraController = this;

            cachedUseBrush = dirtCleaner.useBrushMode;
            cachedOnlyWhenLocked = dirtCleaner.onlyWhenCameraLocked;
            dirtCleaner.onlyWhenCameraLocked = true; // temizlik sadece kilitliyken
        }

        if (cam) targetOrtho = cam.orthographicSize;
    }

    void Update()
    {
        if (!controlsEnabled) return; // Kamera kilitliyken hiçbir giriş işleme

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMousePC();
#endif
        HandleTouchMobile();

        if (cam && Mathf.Abs(targetOrtho - cam.orthographicSize) > 0.0001f)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrtho, 0.3f);

        if (clampToBounds) ClampCameraToBounds();
    }

    // ---------------- PC ----------------
    void HandleMousePC()
    {
        if (blockWhenPointerOverUI && EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            return;

        // Scroll zoom
        float scroll = 0f;
#if ENABLE_LEGACY_INPUT_MANAGER
        scroll = Input.GetAxis("Mouse ScrollWheel");
#endif
        if (Mathf.Approximately(scroll, 0f))
            scroll = Input.mouseScrollDelta.y * 0.1f;

        if (Mathf.Abs(scroll) > 0.00001f)
        {
            targetOrtho = Mathf.Clamp(
                (cam ? cam.orthographicSize : targetOrtho) - scroll * mouseScrollStep,
                minOrthoSize, maxOrthoSize
            );
        }

        // Orta tuş ile pan (istersen sol tuşa çevirebilirsin)
        if (Input.GetMouseButtonDown(2)) lastTouchPos = Input.mousePosition;
        if (Input.GetMouseButton(2))
        {
            Vector2 now = Input.mousePosition;
            Vector3 wpNow = cam.ScreenToWorldPoint(new Vector3(now.x, now.y, 0f));
            Vector3 wpPrev = cam.ScreenToWorldPoint(new Vector3(lastTouchPos.x, lastTouchPos.y, 0f));
            Vector3 delta = (wpPrev - wpNow) * panSpeed;
            transform.position += delta;
            lastTouchPos = now;
        }
    }

    // ---------------- Mobil ----------------
    void HandleTouchMobile()
    {
        if (Input.touchCount == 0) { hadSingleTouchLastFrame = false; return; }

        if (blockWhenPointerOverUI && EventSystem.current)
        {
            for (int i = 0; i < Input.touchCount; i++)
                if (EventSystem.current.IsPointerOverGameObject(Input.touches[i].fingerId))
                    return;
        }

        if (Input.touchCount >= 2) // Pinch zoom
        {
            var t0 = Input.touches[0];
            var t1 = Input.touches[1];
            var prevDist = (t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition);
            var currDist = t0.position - t1.position;
            float deltaMag = currDist.magnitude - prevDist.magnitude;

            targetOrtho = Mathf.Clamp(
                (cam ? cam.orthographicSize : targetOrtho) - deltaMag * pinchZoomSpeed * Time.deltaTime * 60f,
                minOrthoSize, maxOrthoSize
            );
            hadSingleTouchLastFrame = false;
        }
        else // Tek parmak: pan
        {
            var t = Input.touches[0];
            if (t.phase == TouchPhase.Began || !hadSingleTouchLastFrame)
            {
                lastTouchPos = t.position;
                hadSingleTouchLastFrame = true;
                return;
            }

            Vector3 wpNow = cam.ScreenToWorldPoint(new Vector3(t.position.x, t.position.y, 0f));
            Vector3 wpPrev = cam.ScreenToWorldPoint(new Vector3(lastTouchPos.x, lastTouchPos.y, 0f));
            Vector3 delta = (wpPrev - wpNow) * panSpeed;
            transform.position += delta;
            lastTouchPos = t.position;
        }
    }

    void ClampCameraToBounds()
    {
        if (!cam) return;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = worldBounds.xMin + halfW;
        float maxX = worldBounds.xMax - halfW;
        float minY = worldBounds.yMin + halfH;
        float maxY = worldBounds.yMax - halfH;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }

    // ------------- Temizlik Modu API (UI Button eventlerine bağla) -------------

    // Hold yaklaşımı: UI Button OnPointerDown -> OnCleanButtonDown, OnPointerUp -> OnCleanButtonUp
    public void OnCleanButtonDown()
    {
        wasControlsBefore = controlsEnabled;
        controlsEnabled = false; // Kamera kilitli
        if (dirtCleaner)
        {
            cachedUseBrush = dirtCleaner.useBrushMode;
            cachedOnlyWhenLocked = dirtCleaner.onlyWhenCameraLocked;
            dirtCleaner.onlyWhenCameraLocked = true; // kilitliyken temizlik açık
            dirtCleaner.useBrushMode = true;         // Brush tercih – istersen kapat
        }
    }

    public void OnCleanButtonUp()
    {
        controlsEnabled = wasControlsBefore;
        if (dirtCleaner)
        {
            dirtCleaner.useBrushMode = cachedUseBrush;
            dirtCleaner.onlyWhenCameraLocked = cachedOnlyWhenLocked;
        }
    }

    // Toggle yaklaşımı: UI Button OnClick -> OnCleanToggle
    public void OnCleanToggle()
    {
        if (!toggleOn)
        {
            wasControlsBefore = controlsEnabled;
            controlsEnabled = false;
            if (dirtCleaner)
            {
                cachedUseBrush = dirtCleaner.useBrushMode;
                cachedOnlyWhenLocked = dirtCleaner.onlyWhenCameraLocked;
                dirtCleaner.onlyWhenCameraLocked = true;
                dirtCleaner.useBrushMode = true;
            }
            toggleOn = true;
        }
        else
        {
            controlsEnabled = wasControlsBefore;
            if (dirtCleaner)
            {
                dirtCleaner.useBrushMode = cachedUseBrush;
                dirtCleaner.onlyWhenCameraLocked = cachedOnlyWhenLocked;
            }
            toggleOn = false;
        }
    }
}
