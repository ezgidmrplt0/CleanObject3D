using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;              // Slider / Text
using UnityEngine.Events;          // UnityEvent
using DG.Tweening;
using UnityEngine.Serialization;   // FormerlySerializedAs

public class DirtCleaner : MonoBehaviour
{
    [Header("Bağlantılar")]
    public Camera cam;                                   // Boşsa Start'ta bulunur

    [Header("Kir Listesi")]
    [Tooltip("Sahneden kir GameObject'lerini buraya s�r�kle-b�rak.")]
    public List<Transform> dirtItems = new List<Transform>();

    [Header("Swipe / Temizleme Giri�i")]
    [Tooltip("Bir temizleme say�lmas� i�in gereken yatay s�r�kleme (piksel).")]
    public float swipeThresholdPixels = 60f;

    [Header("Temizleme Animasyonu")]
    [FormerlySerializedAs("cleanDuration")]
    [Tooltip("Temel temizleme s�resi (saniye). Varsay�lan 1 sn.")]
    [Min(0.01f)] public float baseCleanDuration = 1f;

    [Tooltip("Seviye ba��na s�re �arpan�. 1 = sabit, 1.1 = her seviyede %10 uzar, 0.9 = k�sal�r.")]
    [Min(0.01f)] public float levelDurationMultiplier = 1f;

    [Tooltip("Ge�erli seviye (1 taban). finalS�re = base * multiplier^(level-1)")]
    [Min(1)] public int currentLevel = 1;

    public Ease cleanEase = Ease.InBack;

    [Tooltip("Temizlendikten sonra obje silinsin mi?")]
    public bool destroyAfterClean = true;

    [Header("UI - Temizlik �lerleme �ubu�u")]
    [Tooltip("0..1 aras�nda de�er alacak Slider (opsiyonel ama �nerilir).")]
    public Slider cleanProgressBar;
    [Tooltip("�ste�e ba�l� y�zde metni (UI Text veya TextMeshPro-UGUI).")]
    public Graphic progressTextGraphic; // Text ya da TMP_Text referans� olabilir

    [Header("Olaylar")]
    [Tooltip("%100 temizlenince tetiklenir (bir kere).")]
    public UnityEvent onAllCleaned;

    // dahili durumlar
    Vector2 startPos;
    bool dragging = false;
    Transform activeDirt = null;          // �st�ne bas�lm�� olan kir
    HashSet<Transform> cleaned = new HashSet<Transform>();

    int totalDirtPlanned = 0;             // toplam hedef (ba�lang�� + sonradan eklenenler)
    int cleanedCount = 0;                 // tamamlananlar
    bool allCleanedFired = false;

    void Start()
    {
        if (!cam) cam = Camera.main;
        // DOTween setup'�: Tools > Demigiant > DOTween Utility Panel > Setup DOTween

        // Ba�lang�� toplam�n� belirle (benzersiz say)
        totalDirtPlanned = 0;
        var seen = new HashSet<Transform>();
        foreach (var t in dirtItems)
        {
            if (t != null && seen.Add(t)) totalDirtPlanned++;
        }

        // G�venlik: cleaned ba�lang��ta doluysa say
        cleanedCount = 0;
        foreach (var t in cleaned) if (t != null) cleanedCount++;

        UpdateProgressUI();
    }

    void Update()
    {
        // Kamera kontrol scriptlerine bağlı bir kilitleme mekanizması kaldırıldı

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    // -------------------- INPUT --------------------
    void HandleMouse()
    {
        // UI �st�nde t�klama ise g�rmezden gel
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

        // UI �st� dokunu�lar� atla
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

    // -------------------- TEM�ZLEME --------------------
    void TryCleanBySwipe(Vector2 endPos)
    {
        if (activeDirt == null || cleaned.Contains(activeDirt)) return;

        float deltaX = Mathf.Abs(endPos.x - startPos.x);

        if (deltaX >= swipeThresholdPixels)
        {
            cleaned.Add(activeDirt);

            float finalDuration = GetCurrentCleanDuration();

            // DOTween animasyonu: scale 0'a k���l (s�re inspector'dan ayarl�)
            activeDirt.DOScale(Vector3.zero, finalDuration)
                      .SetEase(cleanEase)
                      .OnComplete(() =>
                      {
                          // Temizleme tamamland� say
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
        // �nce 2D collider var m� diye dene
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z)));
        Collider2D[] hits2D = Physics2D.OverlapPointAll(wp);
        if (hits2D != null && hits2D.Length > 0)
        {
            for (int i = hits2D.Length - 1; i >= 0; i--) // �stteki �nce
            {
                Transform tr = hits2D[i].transform;
                if (dirtItems.Contains(tr) && !cleaned.Contains(tr))
                    return tr;
            }
        }

        // 3D collider i�in raycast
        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit[] hits3D = Physics.RaycastAll(ray, 1000f);
        if (hits3D != null && hits3D.Length > 0)
        {
            // Ekrana en yak�n olan� se�
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

    // -------------------- �LERLEME / UI --------------------
    void UpdateProgressUI()
    {
        // total 0 ise bar�� gizlemek istersen:
        if (cleanProgressBar)
            cleanProgressBar.gameObject.SetActive(totalDirtPlanned > 0);

        float ratio = 0f;
        if (totalDirtPlanned > 0)
            ratio = Mathf.Clamp01((float)cleanedCount / totalDirtPlanned);

        if (cleanProgressBar)
            cleanProgressBar.value = ratio;

        // y�zde metni: hem Text hem TMP_Text�i destekleyelim
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

    // -------------------- YARDIMCI: Liste y�netimi --------------------
    public void AddDirt(Transform dirt)
    {
        if (dirt == null) return;
        if (!dirtItems.Contains(dirt))
        {
            dirtItems.Add(dirt);
            // temizlenmemi� yeni hedef ekleniyorsa toplam� art�r
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

    // �stersen harici yerden �tekrar hesapla� �a��rabilmen i�in:
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

    // Tüm ilerlemeyi sıfırla ve yeni hedef listesiyle başlat
    public void ResetAll(List<Transform> newDirt)
    {
        cleaned.Clear();
        cleanedCount = 0;
        allCleanedFired = false;
        dirtItems = new List<Transform>();
        if (newDirt != null)
            dirtItems.AddRange(newDirt);

        // total sayısını baştan kur
        var seen = new HashSet<Transform>();
        totalDirtPlanned = 0;
        foreach (var t in dirtItems)
        {
            if (t != null && seen.Add(t)) totalDirtPlanned++;
        }
        UpdateProgressUI();
    }
}
