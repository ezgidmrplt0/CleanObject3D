using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityEngine.Serialization;

public class DirtCleaner : MonoBehaviour
{
    [Header("Bağlantılar")]
    public Camera cam;
    [Tooltip("Kamera açıkken temizleme yapılsın mı? (false = her zaman, true = sadece kamera kilitliyken)")]
    public bool onlyWhenCameraLocked = true;
    public MonoBehaviour cameraController;   // MobileCameraController vb.

    [Header("Kir Listesi")]
    [Tooltip("Sahneden kir GameObject'lerini buraya sürükle-bırak.")]
    public List<Transform> dirtItems = new List<Transform>();

    [Header("Temizleme Modu")]
    [Tooltip("Brush (fırça) modu mu, yoksa eski Swipe modu mu?")]
    public bool useBrushMode = true;

    [Header("Brush Ayarları (Brush Mode İçin)")]
    [Tooltip("Fırça hareketi için minimum mesafe (piksel)")]
    public float brushMoveThreshold = 5f;

    [Header("Swipe Ayarları (Eski Mod İçin)")]
    [Tooltip("Bir temizleme sayılması için gereken yatay sürükleme (piksel).")]
    public float swipeThresholdPixels = 60f;

    [Header("Temizleme Animasyonu")]
    [FormerlySerializedAs("cleanDuration")]
    [Tooltip("Temel temizleme süresi (saniye). Varsayılan 1 sn.")]
    [Min(0.01f)] public float baseCleanDuration = 1f;

    [Tooltip("Seviye başına süre çarpanı.")]
    [Min(0.01f)] public float levelDurationMultiplier = 1f;

    [Tooltip("Geçerli seviye (1 taban).")]
    [Min(1)] public int currentLevel = 1;

    public Ease cleanEase = Ease.InBack;

    [Tooltip("Temizlendikten sonra obje silinsin mi?")]
    public bool destroyAfterClean = true;

    [Header("UI - Temizlik İlerleme Çubuğu")]
    [Tooltip("0..1 arasında değer alacak Slider.")]
    public Slider cleanProgressBar;
    [Tooltip("İsteğe bağlı yüzde metni.")]
    public Graphic progressTextGraphic;

    [Header("Olaylar")]
    [Tooltip("%100 temizlenince tetiklenir.")]
    public UnityEvent onAllCleaned;

    // Dahili durumlar
    Vector2 startPos;
    Vector2 lastBrushPos;
    bool dragging = false;
    Transform activeDirt = null;
    BrushErasableDirt activeBrushDirt = null;
    HashSet<Transform> cleaned = new HashSet<Transform>();

    int totalDirtPlanned = 0;
    int cleanedCount = 0;
    bool allCleanedFired = false;

    void Start()
    {
        if (!cam) cam = Camera.main;

        totalDirtPlanned = 0;
        var seen = new HashSet<Transform>();
        foreach (var t in dirtItems)
        {
            if (t != null && seen.Add(t)) totalDirtPlanned++;
        }

        cleanedCount = 0;
        foreach (var t in cleaned) if (t != null) cleanedCount++;

        UpdateProgressUI();
    }

    void Update()
    {
        // >>> BURASI ÖNEMLİ <<<
        // Kamera AÇIKSA (controlsEnabled == true) hiç temizleme yapma
        if (!CanCleanNow())
        {
            ResetDrag();
            return;
        }

        // Hem PC hem mobil girişleri aynı anda kontrol ediyoruz
        HandleMouse();
        HandleTouch();
    }

    // Kamera kapalı mı, temizleme izni var mı kontrolü
    bool CanCleanNow()
    {
        if (!onlyWhenCameraLocked) return true;          // kilitleme devre dışıysa her zaman izin ver
        if (cameraController == null) return true;       // controller yoksa engelleme

        var t = cameraController.GetType();

        // MobileCameraController: public bool controlsEnabled
        var fControls = t.GetField("controlsEnabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (fControls != null && fControls.FieldType == typeof(bool))
        {
            bool controlsEnabled = (bool)fControls.GetValue(cameraController);
            return !controlsEnabled; // kamera kilitliyse (false) temizleme VAR
        }

        // FreeCameraController: private bool inputEnabled
        var fInput = t.GetField("inputEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (fInput != null && fInput.FieldType == typeof(bool))
        {
            bool inputEnabled = (bool)fInput.GetValue(cameraController);
            return !inputEnabled; // input kapalıysa temizleme VAR
        }

        // Son çare: Camera.enabled durumu (FreeCameraController ToggleCamera ile senkron)
        var camComp = cameraController.GetComponent<Camera>();
        if (camComp != null)
        {
            return !camComp.enabled;
        }

        // Bilgi yoksa engelleme yapma
        return true;
    }

    // -------------------- INPUT --------------------
    void HandleMouse()
    {
        // Mouse yoksa ya da hiçbir buton kullanılmıyorsa boşuna devam etme
        if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0))
            return;

        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            lastBrushPos = startPos;

            if (useBrushMode)
            {
                activeDirt = PickDirtUnderPointer(startPos);
                if (activeDirt != null)
                {
                    activeBrushDirt = activeDirt.GetComponent<BrushErasableDirt>();
                    dragging = (activeBrushDirt != null);
                }
            }
            else
            {
                activeDirt = PickDirtUnderPointer(startPos);
                dragging = (activeDirt != null);
            }
        }

        if (Input.GetMouseButton(0) && dragging && useBrushMode)
        {
            Vector2 currentPos = Input.mousePosition;

            // Brush hareketi
            if (Vector2.Distance(currentPos, lastBrushPos) > brushMoveThreshold)
            {
                ApplyBrushStroke(currentPos);
                lastBrushPos = currentPos;
            }
        }

        if (Input.GetMouseButtonUp(0) && dragging)
        {
            if (!useBrushMode)
            {
                Vector2 endPos = (Vector2)Input.mousePosition;
                TryCleanBySwipe(endPos);
            }
            ResetDrag();
        }
    }

    void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId)) return;

        Touch t = Input.touches[0];

        switch (t.phase)
        {
            case TouchPhase.Began:
                startPos = t.position;
                lastBrushPos = startPos;

                if (useBrushMode)
                {
                    activeDirt = PickDirtUnderPointer(startPos);
                    if (activeDirt != null)
                    {
                        activeBrushDirt = activeDirt.GetComponent<BrushErasableDirt>();
                        dragging = (activeBrushDirt != null);
                    }
                }
                else
                {
                    activeDirt = PickDirtUnderPointer(startPos);
                    dragging = (activeDirt != null);
                }
                break;

            case TouchPhase.Moved:
                if (dragging && useBrushMode)
                {
                    Vector2 currentPos = t.position;
                    if (Vector2.Distance(currentPos, lastBrushPos) > brushMoveThreshold)
                    {
                        ApplyBrushStroke(currentPos);
                        lastBrushPos = currentPos;
                    }
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (dragging)
                {
                    if (!useBrushMode)
                    {
                        TryCleanBySwipe(t.position);
                    }
                    ResetDrag();
                }
                break;
        }
    }

    // -------------------- BRUSH MOD --------------------
    void ApplyBrushStroke(Vector2 screenPos)
    {
        if (activeBrushDirt == null || activeBrushDirt.IsFullyCleaned()) return;

        // Ekran pozisyonundan dünya pozisyonu bul
        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            if (hit.transform == activeDirt)
            {
                // Fırça vuruşu uygula
                activeBrushDirt.EraseBrushStroke(hit.point);
            }
        }
    }

    // BrushErasableDirt tarafından çağrılır
    public void OnDirtCleanedByBrush(Transform dirt)
    {
        if (!cleaned.Contains(dirt))
        {
            cleaned.Add(dirt);
            cleanedCount++;
            UpdateProgressUI();
        }
    }

    // -------------------- ESKİ SWIPE MOD --------------------
    void TryCleanBySwipe(Vector2 endPos)
    {
        if (activeDirt == null || cleaned.Contains(activeDirt)) return;

        float deltaX = Mathf.Abs(endPos.x - startPos.x);

        if (deltaX >= swipeThresholdPixels)
        {
            cleaned.Add(activeDirt);

            float finalDuration = GetCurrentCleanDuration();

            activeDirt.DOScale(Vector3.zero, finalDuration)
                      .SetEase(cleanEase)
                      .OnComplete(() =>
                      {
                          cleanedCount = Mathf.Max(cleanedCount, 0) + 1;
                          UpdateProgressUI();

                          if (destroyAfterClean && activeDirt != null)
                              Destroy(activeDirt.gameObject);
                      });
        }
    }

    float GetCurrentCleanDuration()
    {
        int lvl = Mathf.Max(1, currentLevel);
        float mult = Mathf.Max(0.01f, levelDurationMultiplier);
        float baseDur = Mathf.Max(0.01f, baseCleanDuration);
        return baseDur * Mathf.Pow(mult, lvl - 1);
    }

    void ResetDrag()
    {
        dragging = false;
        activeDirt = null;
        activeBrushDirt = null;
    }

    // -------------------- RAYCAST --------------------
    Transform PickDirtUnderPointer(Vector2 screenPos)
    {
        // 2D collider kontrolü
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z)));
        Collider2D[] hits2D = Physics2D.OverlapPointAll(wp);
        if (hits2D != null && hits2D.Length > 0)
        {
            for (int i = hits2D.Length - 1; i >= 0; i--)
            {
                Transform tr = hits2D[i].transform;
                if (dirtItems.Contains(tr) && !cleaned.Contains(tr))
                    return tr;
            }
        }

        // 3D collider raycast
        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit[] hits3D = Physics.RaycastAll(ray, 1000f);
        if (hits3D != null && hits3D.Length > 0)
        {
            System.Array.Sort(hits3D, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits3D)
            {
                Transform tr = hit.transform;
                if (dirtItems.Contains(tr) && !cleaned.Contains(tr))
                    return tr;
            }
        }
        return null;
    }

    // -------------------- İLERLEME / UI --------------------
    void UpdateProgressUI()
    {
        if (cleanProgressBar)
            cleanProgressBar.gameObject.SetActive(totalDirtPlanned > 0);

        float ratio = 0f;
        if (totalDirtPlanned > 0)
            ratio = Mathf.Clamp01((float)cleanedCount / totalDirtPlanned);

        if (cleanProgressBar)
            cleanProgressBar.value = ratio;

        if (progressTextGraphic)
        {
            string pct = Mathf.RoundToInt(ratio * 100f) + "%";
            var uiText = progressTextGraphic as Text;
            if (uiText) uiText.text = pct;

#if TMP_PRESENT
            var tmp = progressTextGraphic as TMPro.TMP_Text;
            if (tmp) tmp.text = pct;
#endif
        }

        if (!allCleanedFired && totalDirtPlanned > 0 && cleanedCount >= totalDirtPlanned)
        {
            allCleanedFired = true;
            onAllCleaned?.Invoke();
        }
    }

    // -------------------- YARDIMCI --------------------
    public void AddDirt(Transform dirt)
    {
        if (dirt == null) return;
        if (!dirtItems.Contains(dirt))
        {
            dirtItems.Add(dirt);
            if (!cleaned.Contains(dirt))
                totalDirtPlanned++;
            UpdateProgressUI();
        }
    }

    public void RemoveDirt(Transform dirt)
    {
        if (dirt == null) return;

        bool wasCountedAsPlanned = dirtItems.Contains(dirt) && !cleaned.Contains(dirt);
        dirtItems.Remove(dirt);

        if (wasCountedAsPlanned && totalDirtPlanned > cleanedCount)
            totalDirtPlanned--;

        cleaned.Remove(dirt);
        UpdateProgressUI();
    }

    public void RecalculateTotalsFromList()
    {
        var seen = new HashSet<Transform>();
        int newPlanned = 0;
        foreach (var t in dirtItems)
        {
            if (t != null && !cleaned.Contains(t) && seen.Add(t))
                newPlanned++;
        }
        totalDirtPlanned = cleanedCount + newPlanned;
        UpdateProgressUI();
    }
}
