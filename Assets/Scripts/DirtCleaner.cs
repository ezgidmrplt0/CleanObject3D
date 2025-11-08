using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

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

    [Header("Swipe / Temizleme Ayarlarý")]
    [Tooltip("Bir temizleme sayýlmasý için gereken yatay sürükleme (piksel).")]
    public float swipeThresholdPixels = 60f;
    [Tooltip("Temizleme animasyon süresi (saniye).")]
    public float cleanDuration = 0.45f;
    public Ease cleanEase = Ease.InBack;
    [Tooltip("Temizlendikten sonra obje silinsin mi?")]
    public bool destroyAfterClean = true;

    // dahili durumlar
    Vector2 startPos;
    bool dragging = false;
    Transform activeDirt = null;          // üstüne basýlmýþ olan kir
    HashSet<Transform> cleaned = new HashSet<Transform>();

    void Start()
    {
        if (!cam) cam = Camera.main;
        // DOTween setup'ý yaptýðýndan emin ol: Tools > Demigiant > DOTween Utility Panel > Setup DOTween
    }

    void Update()
    {
        // Kamera açýkken istemiyorsan engelle
        if (onlyWhenCameraLocked && cameraController != null)
        {
            // MobileCameraController'da public bool controlsEnabled vardý:
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

            // DOTween animasyonu: scale 0'a küçül
            activeDirt.DOScale(Vector3.zero, cleanDuration)
                      .SetEase(cleanEase)
                      .OnComplete(() =>
                      {
                          if (destroyAfterClean && activeDirt != null)
                              Destroy(activeDirt.gameObject);
                      });
        }
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

    // -------------------- YARDIMCI: Liste yönetimi --------------------
    public void AddDirt(Transform dirt)
    {
        if (!dirtItems.Contains(dirt)) dirtItems.Add(dirt);
    }

    public void RemoveDirt(Transform dirt)
    {
        dirtItems.Remove(dirt);
        cleaned.Remove(dirt);
    }
}
