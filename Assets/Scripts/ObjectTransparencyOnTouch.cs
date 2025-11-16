using UnityEngine;
using System.Collections.Generic;

public class ObjectTransparencyOnTouch : MonoBehaviour
{
    [Header("Şeffaflık Ayarları")]
    [Range(0f, 1f)]
    public float transparentAlpha = 0.3f;

    [Header("Kamera Etkileşimi")]
    [Tooltip("Genelde MobileCameraController script'i olan obje.")]
    public MonoBehaviour cameraController;      // MobileCameraController
    [Tooltip("Sadece kamera KİLİTLİ (controlsEnabled = false) iken tıklamayı kabul et.")]
    public bool onlyWhenCameraLocked = true;

    private List<Collider> colliders = new List<Collider>();
    private List<Collider2D> colliders2D = new List<Collider2D>();

    private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();
    private bool isTransparent = false;

    void Awake()
    {
        // Collider’lar
        colliders.AddRange(GetComponentsInChildren<Collider>());
        colliders2D.AddRange(GetComponentsInChildren<Collider2D>());

        // Renkleri sakla
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        foreach (var r in rends)
        {
            Color[] cols = new Color[r.materials.Length];
            for (int i = 0; i < cols.Length; i++)
                cols[i] = r.materials[i].color;

            originalColors[r] = cols;
        }
    }

    void Update()
    {
        // Kamera şu an "gezinme" modundaysa (controlsEnabled = true) hiç input alma
        if (!CameraAllowsInteraction())
            return;

        // PC Mouse
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = Input.mousePosition;
            if (IsTouched(pos))
                ToggleTransparency();
        }

        // Mobil Touch
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                if (IsTouched(t.position))
                    ToggleTransparency();
            }
        }
    }

    // ------------------ Kamera durumunu kontrol et ------------------
    bool CameraAllowsInteraction()
    {
        if (!onlyWhenCameraLocked || cameraController == null)
            return true;

        // MobileCameraController içindeki "controlsEnabled" alanını reflection ile okuyalım
        var field = cameraController.GetType().GetField("controlsEnabled");
        if (field == null)
            return true; // alan yoksa engelleme

        bool controlsEnabled = (bool)field.GetValue(cameraController);

        // controlsEnabled = true  → kamera serbest, etkileşime izin verme
        // controlsEnabled = false → kamera kilitli, etkileşime izin ver
        return !controlsEnabled;
    }

    // ------------------ TOGGLE ------------------
    void ToggleTransparency()
    {
        if (isTransparent)
            Restore();
        else
            MakeTransparent();
    }

    // ------------------ TIKLANIP TIKLANMADIĞINI ANLAMA ------------------
    bool IsTouched(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(screenPos);

        // 3D
        if (Physics.Raycast(ray, out RaycastHit hit3D, 2000f))
        {
            if (hit3D.transform == transform || hit3D.transform.IsChildOf(transform))
                return true;
        }

        // 2D
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        Collider2D hit2D = Physics2D.OverlapPoint(wp);
        if (hit2D != null)
        {
            if (hit2D.transform == transform || hit2D.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    // ------------------ ŞEFFAF YAP ------------------
    public void MakeTransparent()
    {
        if (isTransparent) return;

        foreach (var r in originalColors.Keys)
        {
            for (int i = 0; i < r.materials.Length; i++)
            {
                Color c = r.materials[i].color;
                c.a = transparentAlpha;

                SetMaterialTransparent(r.materials[i]);
                r.materials[i].color = c;
            }
        }

        // Colliderları kapat
        foreach (var col in colliders) if (col) col.enabled = false;
        foreach (var col in colliders2D) if (col) col.enabled = false;

        isTransparent = true;
    }

    // ------------------ ORİJİNAL HALE DÖNDÜR ------------------
    public void Restore()
    {
        if (!isTransparent) return;

        foreach (var kv in originalColors)
        {
            Renderer r = kv.Key;
            Color[] cols = kv.Value;

            for (int i = 0; i < r.materials.Length; i++)
            {
                SetMaterialOpaque(r.materials[i]);
                r.materials[i].color = cols[i];
            }
        }

        foreach (var col in colliders) if (col) col.enabled = true;
        foreach (var col in colliders2D) if (col) col.enabled = true;

        isTransparent = false;
    }

    // ------------------ MATERYAL DEĞİŞİM YARDIMCILARI ------------------
    void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    void SetMaterialOpaque(Material mat)
    {
        mat.SetFloat("_Mode", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = -1;
    }
}
