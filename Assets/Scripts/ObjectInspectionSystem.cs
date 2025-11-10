using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// Tüm obje inceleme sistemini yöneten tek script.
/// Tag ile çalışır - objelere script eklemeye gerek yok!
/// </summary>
public class ObjectInspectionSystem : MonoBehaviour
{
    public static ObjectInspectionSystem Instance { get; private set; }

    [Header("=== TAG AYARI ===")]
    [Tooltip("İncelenebilir objelerin tag'i (örn: 'Inspectable')")]
    public string inspectableTag = "Inspectable";

    [Header("=== KAMERA ===")]
    public Camera mainCamera;

    [Header("=== GEÇİŞ AYARLARI ===")]
    [Range(0.1f, 2f)] public float transitionDuration = 0.8f;
    public Ease transitionEase = Ease.OutCubic;

    [Header("=== İNCELEME POZİSYONU ===")]
    [Tooltip("Kameraya göre lokal pozisyon (kameranın önünde).")]
    public Vector3 inspectionOffset = new Vector3(0, 0, 1.5f);
    
    [Tooltip("İnceleme modundaki scale çarpanı")]
    [Range(0.5f, 5f)] public float inspectionScale = 1.5f;

    [Header("=== ARKA PLAN ===")]
    [Tooltip("İnceleme modunda diğer objeleri gizle?")]
    public bool hideOtherObjects = true;
    
    [Tooltip("İnceleme modunda UI'ı gizle?")]
    public bool hideUIElements = true;
    
    [Tooltip("UI Canvas (gizlemek için) - opsiyonel")]
    public Canvas mainCanvas;
    
    [Tooltip("Arka plan rengi (solid) - opsiyonel")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
    
    [Tooltip("Arka plan için solid color panel (opsiyonel)")]
    public GameObject backgroundPanel;

    [Header("=== DÖNDÜRME AYARLARI ===")]
    [Range(0.1f, 2f)] public float rotationSensitivity = 0.5f;
    [Range(0f, 0.5f)] public float rotationSmoothing = 0.15f;
    [Range(50f, 500f)] public float rotationSpeed = 150f;
    public bool canRotateX = true;
    public bool canRotateY = true;

    [Header("=== INPUT ===")]
    public bool ignoreUIClicks = true;
    public float raycastDistance = 100f;

    [Header("=== UI ===")]
    public GameObject backButton;

    [Header("=== OLAYLAR ===")]
    public UnityEvent onInspectionStarted;
    public UnityEvent onInspectionEnded;

    // Durum
    private GameObject currentObject;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Transform originalParent;
    
    private bool isTransitioning = false;
    private bool isDragging = false;
    private Vector2 lastInputPosition;
    private Vector3 targetRotation;

    // Arka plan - gizlenen objeler
    private List<GameObject> hiddenObjects = new List<GameObject>();
    private bool wasCanvasActive = false;

    // Oyun durumu
    public enum GameState { NormalView, Inspecting }
    private GameState currentState = GameState.NormalView;
    public GameState CurrentState => currentState;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;
        if (backButton) backButton.SetActive(false);
    }

    void Update()
    {
        // Normal modda obje seçimi
        if (currentState == GameState.NormalView && !isTransitioning)
        {
            HandleObjectSelection();
        }

        // İnceleme modunda döndürme
        if (currentState == GameState.Inspecting && !isTransitioning)
        {
            HandleRotation();
        }
    }

    // ========== OBJE SEÇİMİ ==========
    void HandleObjectSelection()
    {
        bool inputDown = false;
        Vector2 inputPosition = Vector2.zero;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            inputDown = true;
            inputPosition = Input.mousePosition;
        }
#else
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            inputDown = true;
            inputPosition = Input.touches[0].position;
        }
#endif

        if (!inputDown) return;

        if (ignoreUIClicks && EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = mainCamera.ScreenPointToRay(inputPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            // Tag kontrolü
            if (hit.collider.CompareTag(inspectableTag))
            {
                InspectObject(hit.collider.gameObject);
            }
        }
    }

    // ========== İNCELEME BAŞLAT ==========
    public void InspectObject(GameObject obj)
    {
        if (obj == null || isTransitioning) return;

        currentObject = obj;
        isTransitioning = true;
        currentState = GameState.Inspecting;

        // Orijinal transform kaydet
        originalPosition = obj.transform.position;
        originalRotation = obj.transform.rotation;
        originalScale = obj.transform.localScale;
        originalParent = obj.transform.parent;

        // Kameraya göre pozisyon hesapla (kamera önü)
        Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * inspectionOffset.z;
        targetPosition += mainCamera.transform.right * inspectionOffset.x;
        targetPosition += mainCamera.transform.up * inspectionOffset.y;

        // Diğer objeleri gizle
        if (hideOtherObjects)
        {
            HideSceneObjects(obj);
        }

        // UI'ı gizle
        if (hideUIElements && mainCanvas != null)
        {
            wasCanvasActive = mainCanvas.gameObject.activeSelf;
            mainCanvas.gameObject.SetActive(false);
        }

        // Arka plan panelini göster
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(true);
        }

        // Animasyon
        Sequence seq = DOTween.Sequence();
        
        seq.Append(obj.transform.DOMove(targetPosition, transitionDuration).SetEase(transitionEase));
        seq.Join(obj.transform.DORotate(Vector3.zero, transitionDuration).SetEase(transitionEase));
        seq.Join(obj.transform.DOScale(originalScale * inspectionScale, transitionDuration).SetEase(transitionEase));
        
        seq.OnComplete(() =>
        {
            isTransitioning = false;
            targetRotation = obj.transform.eulerAngles;
            if (backButton) backButton.SetActive(true);
            onInspectionStarted?.Invoke();
            Debug.Log($"[Inspection] {obj.name} inceleniyor.");
        });

        SetHighlight(obj, true);
    }

    // ========== İNCELEME ÇIKIŞ ==========
    public void ExitInspection()
    {
        if (currentObject == null || isTransitioning) return;

        isTransitioning = true;
        isDragging = false;

        SetHighlight(currentObject, false);

        Sequence seq = DOTween.Sequence();
        
        seq.Append(currentObject.transform.DOMove(originalPosition, transitionDuration).SetEase(transitionEase));
        seq.Join(currentObject.transform.DORotate(originalRotation.eulerAngles, transitionDuration).SetEase(transitionEase));
        seq.Join(currentObject.transform.DOScale(originalScale, transitionDuration).SetEase(transitionEase));
        
        seq.OnComplete(() =>
        {
            isTransitioning = false;
            currentState = GameState.NormalView;
            currentObject = null;
            
            // Gizli objeleri geri göster
            ShowSceneObjects();
            
            // UI'ı geri göster
            if (hideUIElements && mainCanvas != null && wasCanvasActive)
            {
                mainCanvas.gameObject.SetActive(true);
            }
            
            // Arka plan panelini gizle
            if (backgroundPanel != null)
            {
                backgroundPanel.SetActive(false);
            }
            
            if (backButton) backButton.SetActive(false);
            onInspectionEnded?.Invoke();
            Debug.Log("[Inspection] Normal moda dönüldü.");
        });
    }

    // ========== DÖNDÜRME ==========
    void HandleRotation()
    {
        if (currentObject == null) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseRotation();
#else
        HandleTouchRotation();
#endif

        ApplySmoothRotation();
    }

    void HandleMouseRotation()
    {
        if (ignoreUIClicks && EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            lastInputPosition = Input.mousePosition;
            isDragging = true;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastInputPosition;
            RotateByDelta(delta);
            lastInputPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    void HandleTouchRotation()
    {
        if (Input.touchCount > 0 && ignoreUIClicks && EventSystem.current)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId))
                return;
        }

        if (Input.touchCount == 1)
        {
            Touch touch = Input.touches[0];

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    lastInputPosition = touch.position;
                    isDragging = true;
                    break;

                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        Vector2 delta = touch.position - lastInputPosition;
                        RotateByDelta(delta);
                        lastInputPosition = touch.position;
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isDragging = false;
                    break;
            }
        }
        else
        {
            isDragging = false;
        }
    }

    void RotateByDelta(Vector2 delta)
    {
        if (currentObject == null) return;

        float speed = rotationSpeed * rotationSensitivity;

        if (canRotateY)
            targetRotation.y += -delta.x * speed * Time.deltaTime;

        if (canRotateX)
            targetRotation.x += delta.y * speed * Time.deltaTime;
    }

    void ApplySmoothRotation()
    {
        if (currentObject == null) return;

        if (rotationSmoothing > 0f)
        {
            currentObject.transform.rotation = Quaternion.Lerp(
                currentObject.transform.rotation,
                Quaternion.Euler(targetRotation),
                1f - Mathf.Exp(-rotationSmoothing * 60f * Time.deltaTime)
            );
        }
        else
        {
            currentObject.transform.rotation = Quaternion.Euler(targetRotation);
        }
    }

    // ========== HIGHLIGHT EFEKTİ ==========
    void SetHighlight(GameObject obj, bool enable)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;

        if (enable)
        {
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.yellow * 0.3f);
            }
        }
        else
        {
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    // ========== SAHNE OBJELERİNİ GİZLE/GÖSTER ==========
    void HideSceneObjects(GameObject exceptThis)
    {
        hiddenObjects.Clear();

        // Tüm renderer'ları bul ve gizle (except incelenen obje)
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        
        foreach (Renderer rend in allRenderers)
        {
            // İncelenen obje değilse ve aktifse
            if (rend.gameObject != exceptThis && rend.gameObject.activeSelf)
            {
                // Geri butonu ve arka plan paneli hariç
                if (backButton != null && rend.transform.IsChildOf(backButton.transform))
                    continue;
                if (backgroundPanel != null && rend.transform.IsChildOf(backgroundPanel.transform))
                    continue;

                rend.enabled = false;
                hiddenObjects.Add(rend.gameObject);
            }
        }

        // Kamera background rengini değiştir
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = backgroundColor;
        }
    }

    void ShowSceneObjects()
    {
        // Gizlenen renderer'ları geri aç
        foreach (GameObject obj in hiddenObjects)
        {
            if (obj != null)
            {
                Renderer rend = obj.GetComponent<Renderer>();
                if (rend != null) rend.enabled = true;
            }
        }
        hiddenObjects.Clear();

        // Kamera background'ı eski haline dön (skybox vs.)
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.Skybox; // veya SolidColor
        }
    }
}
