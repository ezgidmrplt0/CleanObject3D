using UnityEngine;
using UnityEngine.InputSystem;

public class PinchDetection : MonoBehaviour
{
    // Inspector'da atayacaðýmýz Input Actions Asset'indeki TouchControls referansý
    public InputActionAsset inputActions;

    // Ayarlamalar
    [Header("Zoom Ayarlarý")]
    [Tooltip("Zoom hýzýný ayarlar. Daha yüksek deðer daha hýzlý zoom demektir.")]
    public float zoomSpeed = 0.01f;
    [Tooltip("Kameranýn minimum Field of View (FOV) deðeri.")]
    public float minFov = 15f;
    [Tooltip("Kameranýn maksimum Field of View (FOV) deðeri.")]
    public float maxFov = 60f;

    // Aksiyon Referanslarý
    private InputAction primaryFingerPositionAction;
    private InputAction secondaryFingerPositionAction;
    private InputAction secondaryTouchContactAction;

    // Durum Deðiþkenleri
    private Camera mainCamera;
    private float initialDistance;
    private bool isZooming = false;

    private void Awake()
    {
        // Ana Kamerayý al
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Sahne'de 'Main Camera' etiketine sahip bir Kamera bulunamadý!");
            enabled = false; // Script'i devre dýþý býrak
            return;
        }

        // Input Actions Asset'inden aksiyonlarý bulun
        var touchActionMap = inputActions.FindActionMap("Touch");
        if (touchActionMap == null)
        {
            Debug.LogError("Input Actions Asset'te 'Touch' adlý bir Action Map bulunamadý!");
            enabled = false;
            return;
        }

        primaryFingerPositionAction = touchActionMap.FindAction("PrimaryFingerPosition");
        secondaryFingerPositionAction = touchActionMap.FindAction("SecondaryFingerPosition");
        secondaryTouchContactAction = touchActionMap.FindAction("SecondaryTouchContact");

        if (primaryFingerPositionAction == null || secondaryFingerPositionAction == null || secondaryTouchContactAction == null)
        {
            Debug.LogError("Input Actions Asset'te gerekli aksiyonlar (PrimaryFingerPosition, SecondaryFingerPosition, SecondaryTouchContact) bulunamadý!");
            enabled = false;
            return;
        }

        // Aksiyonlara callback'leri baðlayýn
        secondaryTouchContactAction.started += OnSecondaryTouchStarted;
        secondaryTouchContactAction.canceled += OnSecondaryTouchCanceled;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
        // Abonelikleri kaldýr
        secondaryTouchContactAction.started -= OnSecondaryTouchStarted;
        secondaryTouchContactAction.canceled -= OnSecondaryTouchCanceled;
    }

    private void Update()
    {
        // Sadece iki parmak dokunuyorken çalýþýr
        if (isZooming)
        {
            // Ýki parmaðýn pozisyonlarýný al
            Vector2 primaryPosition = primaryFingerPositionAction.ReadValue<Vector2>();
            Vector2 secondaryPosition = secondaryFingerPositionAction.ReadValue<Vector2>();

            // Anlýk mesafeyi hesapla
            float currentDistance = Vector2.Distance(primaryPosition, secondaryPosition);

            // Mesafe farkýný hesapla (Yakýnlaþtýrma / Uzaklaþtýrma yönü)
            // Eðer currentDistance > initialDistance ise uzaklaþma (zoom out), < ise yakýnlaþma (zoom in)
            float deltaDistance = currentDistance - initialDistance;

            // Kameranýn Field of View (FOV) deðerini deðiþtir (Yakýnlaþtýrma)
            // deltaDistance'ý ters çeviriyoruz ki parmaklar ayrýlýnca (mesafe artýnca) zoom out olsun (FOV artsýn)
            float newFov = mainCamera.fieldOfView - deltaDistance * zoomSpeed;

            // FOV'u minimum ve maksimum deðerler arasýnda sýnýrla
            mainCamera.fieldOfView = Mathf.Clamp(newFov, minFov, maxFov);

            // initialDistance'ý güncelle
            initialDistance = currentDistance;
        }
    }

    // Ýkinci parmak dokunmaya baþladýðýnda
    private void OnSecondaryTouchStarted(InputAction.CallbackContext context)
    {
        // Emin olmak için: Eðer iki parmak dokunuyorsa
        if (Touchscreen.current.touches.Count >= 2)
        {
            isZooming = true;

            // Ýlk parmaklarýn pozisyonlarýný al
            Vector2 primaryPosition = primaryFingerPositionAction.ReadValue<Vector2>();
            Vector2 secondaryPosition = secondaryFingerPositionAction.ReadValue<Vector2>();

            // Baþlangýç mesafesini kaydet
            initialDistance = Vector2.Distance(primaryPosition, secondaryPosition);
        }
    }

    // Ýkinci parmak kalktýðýnda
    private void OnSecondaryTouchCanceled(InputAction.CallbackContext context)
    {
        isZooming = false;
        initialDistance = 0f; // Reset
    }
}