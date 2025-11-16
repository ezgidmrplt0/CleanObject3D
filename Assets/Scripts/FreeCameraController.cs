using UnityEngine;
using TMPro;

public class FreeCameraController : MonoBehaviour
{
    [Header("Genel Hareket Ayarlarý")]
    public float moveSpeed = 15f;
    public float fastMultiplier = 2f;

    [Header("Zoom Ayarlarý")]
    [Tooltip("Perspektif kamera için FOV, ortografik için Size sýnýrlarý")]
    public float minZoom = 20f;      // Daha yakýn
    public float maxZoom = 60f;      // Daha uzak
    public float mouseZoomSpeed = 40f;
    public float pinchZoomSpeed = 0.5f;

    [Header("Pozisyon Sýnýrlarý")]
    public float minX = -50f;
    public float maxX = 50f;
    public float minZ = -50f;
    public float maxZ = 50f;
    public float minY = 5f;
    public float maxY = 50f;

    [Header("Mobil Ayarlarý")]
    public float touchPanSpeed = 0.2f;   // ekranda sürükleme hýzý

    [Header("UI (TMP)")]
    public TextMeshProUGUI cameraButtonText;  // Button içindeki Text (TMP)

    private bool inputEnabled = true;
    private Camera cam;

    // Mobil pan için
    private Vector2 lastPanPosition;
    private bool isPanning;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        UpdateButtonText();
    }

    void Update()
    {
        if (!inputEnabled) return;
        if (cam == null) return;

        // PC giriþlerini de, mobil giriþlerini de her frame kontrol et
        DesktopControls();
        MobileControls();

        ApplyBounds();
    }

    // ================= DESKTOP (PC / EDITOR) =================

    void DesktopControls()
    {
        KeyboardMove();
        MouseZoom();
    }

    void KeyboardMove()
    {
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= fastMultiplier;

        float h = Input.GetAxisRaw("Horizontal"); // A-D
        float v = Input.GetAxisRaw("Vertical");   // W-S

        float y = 0f;
        if (Input.GetKey(KeyCode.Q)) y -= 1f;
        if (Input.GetKey(KeyCode.E)) y += 1f;

        Vector3 dir = new Vector3(h, y, v).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }

    void MouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            float amount = scroll * mouseZoomSpeed * Time.deltaTime * 100f;
            ZoomBy(amount);
        }
    }

    // ================= MOBÝL =================

    void MobileControls()
    {
        if (Input.touchCount == 1) // Pan
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                lastPanPosition = t.position;
                isPanning = true;
            }
            else if (t.phase == TouchPhase.Moved && isPanning)
            {
                Vector2 delta = t.position - lastPanPosition;
                lastPanPosition = t.position;

                // Ekranda parmaðý saða çekince sahne sola gitsin diye - ile çarpýyoruz
                float dx = -delta.x * touchPanSpeed * Time.deltaTime;
                float dz = -delta.y * touchPanSpeed * Time.deltaTime;

                Vector3 right = transform.right;
                right.y = 0;
                right.Normalize();

                Vector3 forward = transform.forward;
                forward.y = 0;
                forward.Normalize();

                transform.position += right * dx + forward * dz;
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                isPanning = false;
            }
        }
        else if (Input.touchCount == 2) // Pinch zoom
        {
            isPanning = false;

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 t0Prev = t0.position - t0.deltaPosition;
            Vector2 t1Prev = t1.position - t1.deltaPosition;

            float prevMag = (t0Prev - t1Prev).magnitude;
            float currentMag = (t0.position - t1.position).magnitude;

            float diff = currentMag - prevMag; // pozitif -> uzaklaþtýrma, negatif -> yakýnlaþtýrma

            float amount = diff * pinchZoomSpeed * Time.deltaTime;
            ZoomBy(amount);
        }
        else
        {
            isPanning = false;
        }
    }

    // ================= ORTAK =================

    void ZoomBy(float amount)
    {
        if (cam.orthographic)
        {
            float size = cam.orthographicSize;
            // orthoSize büyürse uzaklaþýr, o yüzden ters çeviriyoruz:
            size -= amount;
            size = Mathf.Clamp(size, minZoom, maxZoom);
            cam.orthographicSize = size;
        }
        else
        {
            float fov = cam.fieldOfView;
            fov -= amount;
            fov = Mathf.Clamp(fov, minZoom, maxZoom);
            cam.fieldOfView = fov;
        }
    }

    void ApplyBounds()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        p.z = Mathf.Clamp(p.z, minZ, maxZ);
        transform.position = p;
    }

    // ================= BUTTON =================

    public void ToggleCamera()
    {
        inputEnabled = !inputEnabled;

        if (cam != null)
            cam.enabled = inputEnabled;

        UpdateButtonText();
    }

    void UpdateButtonText()
    {
        if (cameraButtonText == null) return;

        cameraButtonText.text = inputEnabled ? "Kamera: Açýk" : "Kamera: Kapalý";
    }
}
