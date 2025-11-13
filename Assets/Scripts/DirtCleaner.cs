using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;              // Slider / Text
using UnityEngine.Events;          // UnityEvent
using DG.Tweening;
using UnityEngine.Serialization;   // FormerlySerializedAs

public class DirtCleaner : MonoBehaviour
{
    [Header("Baðlantýlar")]
    public Camera cam;                                   // Boþsa Start'ta bulunur
    [Tooltip("Kamera açýkken temizleme yapýlsýn mý? (MobileCameraController kullanýyorsan)")]
    public bool onlyWhenCameraLocked = true;
    public MonoBehaviour cameraController;               // MobileCameraController referansý (opsiyonel)

    [Header("Kir Listesi")]
    [Tooltip("Sahneden kir GameObject'lerini buraya sürükle-býrak.")]
    public List<Transform> dirtItems = new List<Transform>();

    [Header("Swipe / Temizleme Giriþi")]
    [Tooltip("Bir temizleme sayýlmasý için gereken yatay sürükleme (piksel).")]
    public float swipeThresholdPixels = 60f;

    [Header("Temizleme Animasyonu")]
    [FormerlySerializedAs("cleanDuration")]
    [Tooltip("Temel temizleme süresi (saniye). Varsayýlan 1 sn.")]
    [Min(0.01f)] public float baseCleanDuration = 1f;

    [Tooltip("Seviye baþýna süre çarpaný. 1 = sabit, 1.1 = her seviyede %10 uzar, 0.9 = kýsalýr.")]
    [Min(0.01f)] public float levelDurationMultiplier = 1f;

    [Tooltip("Geçerli seviye (1 taban). finalSüre = base * multiplier^(level-1)")]
    [Min(1)] public int currentLevel = 1;

    public Ease cleanEase = Ease.InBack;

    [Tooltip("Temizlendikten sonra obje silinsin mi?")]
    public bool destroyAfterClean = true;

    [Header("UI - Temizlik Ýlerleme Çubuðu")]
    [Tooltip("0..1 arasýnda deðer alacak Slider (opsiyonel ama önerilir).")]
    public Slider cleanProgressBar;
    [Tooltip("Ýsteðe baðlý yüzde metni (UI Text veya TextMeshPro-UGUI).")]
    public Graphic progressTextGraphic; // Text ya da TMP_Text referansý olabilir

    [Header("Olaylar")]
    [Tooltip("%100 temizlenince tetiklenir (bir kere).")]
    public UnityEvent onAllCleaned;

    // dahili durumlar
    Vector2 startPos;
    bool dragging = false;
    Transform activeDirt = null;          // üstüne basýlmýþ olan kir
    HashSet<Transform> cleaned = new HashSet<Transform>();

    int totalDirtPlanned = 0;             // toplam hedef (baþlangýç + sonradan eklenenler)
    int cleanedCount = 0;                 // tamamlananlar
    bool allCleanedFired = false;

    void Start()
    {
        if (!cam) cam = Camera.main;
        // DOTween setup'ý: Tools > Demigiant > DOTween Utility Panel > Setup DOTween

        // Baþlangýç toplamýný belirle (benzersiz say)
        totalDirtPlanned = 0;
        var seen = new HashSet<Transform>();
        foreach (var t in dirtItems)
        {
            if (t != null && seen.Add(t)) totalDirtPlanned++;
        }

        // Güvenlik: cleaned baþlangýçta doluysa say
        cleanedCount = 0;
        foreach (var t in cleaned) if (t != null) cleanedCount++;

        UpdateProgressUI();
    }

    void Update()
    {
        // Kamera açýkken istemiyorsan engelle
        if (onlyWhenCameraLocked && cameraController != null)
        {
            var field = cameraController.GetType().GetField("controlsEnabled");
            if (field != null)
            {
                bool controlsEnabled = (bool)field.GetValue(cameraController);
                if (controlsEnabled) { ResetDrag(); return; }
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    // -------------------- INPUT --------------------
    void HandleMouse()
    {
        // UI üstünde týklama ise görmezden gel
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            activeDirt = PickDirtUnderPointer(startPos);
            dragging = (activeDirt != null);
        }

        if (Input.GetMouseButtonUp(0) && dragging)
        {
            Vector2 endPos = (Vector2)Input.mousePosition;
            TryCleanBySwipe(endPos);
            ResetDrag();
        }
    }

    void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        // UI üstü dokunuþlarý atla
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId)) return;

        Touch t = Input.touches[0];

        switch (t.phase)
        {
            case TouchPhase.Began:
                startPos = t.position;
                activeDirt = PickDirtUnderPointer(startPos);
                dragging = (activeDirt != null);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (dragging)
                {
                    TryCleanBySwipe(t.position);
                    ResetDrag();
                }
                break;
        }
    }

    // -------------------- TEMÝZLEME --------------------
    void TryCleanBySwipe(Vector2 endPos)
    {
        if (activeDirt == null || cleaned.Contains(activeDirt)) return;

        float deltaX = Mathf.Abs(endPos.x - startPos.x);

        if (deltaX >= swipeThresholdPixels)
        {
            cleaned.Add(activeDirt);

            float finalDuration = GetCurrentCleanDuration();

            // DOTween animasyonu: scale 0'a küçül (süre inspector'dan ayarlý)
            activeDirt.DOScale(Vector3.zero, finalDuration)
                      .SetEase(cleanEase)
                      .OnComplete(() =>
                      {
                          // Temizleme tamamlandý say
                          cleanedCount = Mathf.Max(cleanedCount, 0) + 1;
                          UpdateProgressUI();

                          if (destroyAfterClean && activeDirt != null)
                              Destroy(activeDirt.gameObject);
                      });
        }
    }

    float GetCurrentCleanDuration()
    {
        // final = base * multiplier^(level-1)
        int lvl = Mathf.Max(1, currentLevel);
        float mult = Mathf.Max(0.01f, levelDurationMultiplier);
        float baseDur = Mathf.Max(0.01f, baseCleanDuration);
        return baseDur * Mathf.Pow(mult, lvl - 1);
    }

    void ResetDrag()
    {
        dragging = false;
        activeDirt = null;
    }

    // -------------------- RAYCAST (2D/3D destekli) --------------------
    Transform PickDirtUnderPointer(Vector2 screenPos)
    {
        // Önce 2D collider var mý diye dene
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z)));
        Collider2D[] hits2D = Physics2D.OverlapPointAll(wp);
        if (hits2D != null && hits2D.Length > 0)
        {
            for (int i = hits2D.Length - 1; i >= 0; i--) // üstteki önce
            {
                Transform tr = hits2D[i].transform;
                if (dirtItems.Contains(tr) && !cleaned.Contains(tr))
                    return tr;
            }
        }

        // 3D collider için raycast
        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit[] hits3D = Physics.RaycastAll(ray, 1000f);
        if (hits3D != null && hits3D.Length > 0)
        {
            // Ekrana en yakýn olaný seç
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

    // -------------------- ÝLERLEME / UI --------------------
    void UpdateProgressUI()
    {
        // total 0 ise bar’ý gizlemek istersen:
        if (cleanProgressBar)
            cleanProgressBar.gameObject.SetActive(totalDirtPlanned > 0);

        float ratio = 0f;
        if (totalDirtPlanned > 0)
            ratio = Mathf.Clamp01((float)cleanedCount / totalDirtPlanned);

        if (cleanProgressBar)
            cleanProgressBar.value = ratio;

        // yüzde metni: hem Text hem TMP_Text’i destekleyelim
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

    // -------------------- YARDIMCI: Liste yönetimi --------------------
    public void AddDirt(Transform dirt)
    {
        if (dirt == null) return;
        if (!dirtItems.Contains(dirt))
        {
            dirtItems.Add(dirt);
            // temizlenmemiþ yeni hedef ekleniyorsa toplamý artýr
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

    // Ýstersen harici yerden “tekrar hesapla” çaðýrabilmen için:
    public void RecalculateTotalsFromList()
    {
        var seen = new HashSet<Transform>();
        int newPlanned = 0;
        foreach (var t in dirtItems)
        {
            if (t != null && !cleaned.Contains(t) && seen.Add(t))
                newPlanned++;
        }
        // total = temizlenenler + geriye kalanlar
        totalDirtPlanned = cleanedCount + newPlanned;
        UpdateProgressUI();
    }
}
