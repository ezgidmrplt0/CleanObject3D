using UnityEngine;
using DG.Tweening;

public class BrushErasableDirt : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Fırça boyutu (texture pixel cinsinden, sabit kalır)")]
    public int brushPixelRadius = 30;

    [Tooltip("Fırça yumuşaklığı (1 = sert, 0.1 = yumuşak)")]
    [Range(0.1f, 1f)]
    public float brushHardness = 0.5f;

    [Tooltip("Temizlenme yüzdesi (0-1, ne kadar silinmesi gerek)")]
    [Range(0.5f, 0.99f)]
    public float cleanThreshold = 0.85f;

    [Tooltip("Her fırça vuruşunda ne kadar silinsin (0.05-1.0 arası tipik).")]
    [Range(0.01f, 1.0f)]
    public float eraseStrength = 0.6f;

    [Header("Otomatik Ayarlar")]
    [Tooltip("RenderTexture çözünürlüğü (mobil için 256 yeterli)")]
    public int textureResolution = 256;

    // Dahili değişkenler
    RenderTexture maskTexture;
    Material objectMaterial;
    Renderer objRenderer;
    Camera mainCam;

    float totalPixels;
    float erasedPixels = 0f;
    bool isFullyCleaned = false;

    Texture2D brushTexture;
    Texture2D cleanCheckTexture; // Performans için tekrar kullanılan texture
    Material brushDrawMaterial;  // GPU çizimi için materyal

    // DirtCleaner referansı (seçili fırça hız çarpanını almak için)
    DirtCleaner cleaner;

    void Start()
    {
        mainCam = Camera.main;
        objRenderer = GetComponent<Renderer>();

        if (objRenderer == null)
        {
            Debug.LogError("BrushErasableDirt: Renderer bulunamadı!");
            enabled = false;
            return;
        }

        // Malzemeyi kopyala (paylaşılan materyal değişmesin)
        objectMaterial = new Material(objRenderer.material);
        objRenderer.material = objectMaterial;

        // RenderTexture oluştur (başlangıçta tamamen beyaz = silinmemiş)
        maskTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
        maskTexture.Create();

        // Beyaz ile doldur
        RenderTexture.active = maskTexture;
        GL.Clear(true, true, Color.white);
        RenderTexture.active = null;

        objectMaterial.SetTexture("_MaskTex", maskTexture);

        totalPixels = textureResolution * textureResolution;

        // Fırça texture oluştur
        CreateBrushTexture();
        
        // GPU çizimi için basit materyal
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        brushDrawMaterial = new Material(shader);

        // Sahnedeki DirtCleaner'ı bul (hız çarpanını kullanmak için)
        cleaner = FindObjectOfType<DirtCleaner>();
    }

    void CreateBrushTexture()
    {
        int size = 64; // Fırça texture boyutu
        brushTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);

        Color[] pixels = new Color[size * size];
        float center = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float radius = size / 2f;

                // Yumuşak gradient (ortası siyah, kenarlara doğru beyaz)
                float alpha = Mathf.Clamp01(1f - (distance / radius));
                alpha = Mathf.Pow(alpha, 1f / brushHardness); // Sertlik ayarı

                // Siyah renk, alpha kanalı şekli belirler
                pixels[y * size + x] = new Color(0, 0, 0, alpha); 
            }
        }

        brushTexture.SetPixels(pixels);
        brushTexture.Apply();
        
        // GPU çizimi için materyale ata
        if (brushDrawMaterial != null)
            brushDrawMaterial.mainTexture = brushTexture;
    }

    // >>> BURASI GÜNCELLENDİ <<<
    public void EraseBrushStroke(Vector3 worldPosition)
    {
        if (isFullyCleaned) return;

        // Fırçanın dünya pozisyonundan UV hesapla
        Vector2 uv = WorldToUV(worldPosition);

        // Geçersiz UV ise işlem yapma
        if (uv == Vector2.zero || uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
        {
            // Debug.LogWarning("UV koordinatı geçersiz: " + uv);
            return;
        }

        // UV'den texture pixel koordinatına
        int pixelX = Mathf.RoundToInt(uv.x * textureResolution);
        int pixelY = Mathf.RoundToInt(uv.y * textureResolution);

        // Sınırları kontrol et
        pixelX = Mathf.Clamp(pixelX, 0, textureResolution - 1);
        pixelY = Mathf.Clamp(pixelY, 0, textureResolution - 1);

        // Debug.Log($"UV: {uv}, Pixel: ({pixelX}, {pixelY})");

        // Fırça çizimi
        DrawBrush(pixelX, pixelY);

        // Silinme yüzdesini kontrol et
        CheckCleanProgress();
    }

    // >>> BURASI DA FIRÇA POZİSYONUNU KULLANACAK ŞEKİLDE GÜNCELLENDİ <<<
    Vector2 WorldToUV(Vector3 worldPos)
    {
        // 1) Önce collider + UV üzerinden doğru hesaplamayı dene
        if (mainCam != null)
        {
            // Kameradan fırça pozisyonuna doğru ray at
            Ray ray = new Ray(mainCam.transform.position, (worldPos - mainCam.transform.position).normalized);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    if (hit.textureCoord != Vector2.zero)
                    {
                        return hit.textureCoord;
                    }
                }
            }
        }

        // 2) Raycast tutmazsa fallback: local space + mesh bounds ile yaklaşık UV
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.mesh != null)
        {
            Bounds bounds = mf.mesh.bounds;

            // En uzun 2 ekseni kullan (objenin yönüne göre)
            Vector3 size = bounds.size;
            float u, v;

            if (size.x >= size.y && size.x >= size.z)
            {
                // X-Z düzlemi (yatay zemin)
                u = Mathf.InverseLerp(bounds.min.x, bounds.max.x, localPos.x);
                v = Mathf.InverseLerp(bounds.min.z, bounds.max.z, localPos.z);
            }
            else if (size.y >= size.x && size.y >= size.z)
            {
                // Y-Z düzlemi (dikey duvar, sağ/sol)
                u = Mathf.InverseLerp(bounds.min.z, bounds.max.z, localPos.z);
                v = Mathf.InverseLerp(bounds.min.y, bounds.max.y, localPos.y);
            }
            else
            {
                // X-Y düzlemi (dikey duvar, ön/arka)
                u = Mathf.InverseLerp(bounds.min.x, bounds.max.x, localPos.x);
                v = Mathf.InverseLerp(bounds.min.y, bounds.max.y, localPos.y);
            }

            return new Vector2(u, v);
        }

        return Vector2.zero;
    }

    void DrawBrush(int centerX, int centerY)
    {
        // GPU tabanlı çizim (Mobil performans için optimize edildi)
        // ReadPixels/SetPixels yerine GL komutları kullanıyoruz.
        
        RenderTexture.active = maskTexture;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, textureResolution, textureResolution, 0);

        // Fırça boyutu
        int brushPixelSize = Mathf.Max(10, brushPixelRadius * 2);
        
        // Çizilecek alan
        float x = centerX - brushPixelSize / 2f;
        float y = centerY - brushPixelSize / 2f;
        float w = brushPixelSize;
        float h = brushPixelSize;

        // Seçili fırçaya göre efektif erase gücü
        float speedMultiplier = 1f;
        if (cleaner != null)
            speedMultiplier = cleaner.brushCleanSpeed;

        // Hız çarpanı ile güçlendirilmiş silme gücü
        float effectiveEraseStrength = Mathf.Clamp01(eraseStrength * speedMultiplier);

        // Fırça çizimi
        // Siyah renk (silme) ve Alpha (güç)
        // Sprites/Default shader'ı vertex color ile texture'ı çarpar.
        // Texture'ımız zaten siyah ve alpha içeriyor.
        // Vertex color alpha'sını eraseStrength ile çarparak gücü ayarlıyoruz.
        
        if (brushDrawMaterial.SetPass(0))
        {
            GL.Begin(GL.QUADS);
            GL.Color(new Color(0, 0, 0, effectiveEraseStrength)); // Siyah ve Alpha gücü
            
            GL.TexCoord2(0, 0); GL.Vertex3(x, y, 0);
            GL.TexCoord2(0, 1); GL.Vertex3(x, y + h, 0);
            GL.TexCoord2(1, 1); GL.Vertex3(x + w, y + h, 0);
            GL.TexCoord2(1, 0); GL.Vertex3(x + w, y, 0);
            
            GL.End();
        }

        GL.PopMatrix();
        RenderTexture.active = null;
    }

    void CheckCleanProgress()
    {
        // Her frame kontrol etme, performans için seyrek kontrol
        if (Time.frameCount % 15 != 0) return;

        // Texture buffer'ı tekrar kullan (GC alloc önlemek için)
        if (cleanCheckTexture == null)
        {
            cleanCheckTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGB24, false);
        }

        RenderTexture.active = maskTexture;
        cleanCheckTexture.ReadPixels(new Rect(0, 0, textureResolution, textureResolution), 0, 0);
        cleanCheckTexture.Apply();
        RenderTexture.active = null;

        // NativeArray veya ComputeShader olmadan en hızlı yöntem:
        // Ancak mobil için GetPixels() hala biraz ağır olabilir.
        // Yine de ReadPixels'i her frame yapmaktan çok daha iyidir.
        
        Color[] pixels = cleanCheckTexture.GetPixels();
        float blackPixels = 0;
        int step = 4; // Örnekleme sıklığı (daha yüksek = daha hızlı ama az hassas)

        for (int i = 0; i < pixels.Length; i += step)
        {
            if (pixels[i].r < 0.5f) // Siyaha yakın
                blackPixels++;
        }

        erasedPixels = (blackPixels * step) / totalPixels;

        if (erasedPixels >= cleanThreshold && !isFullyCleaned)
        {
            isFullyCleaned = true;
            OnFullyCleaned();
        }
    }

    void OnFullyCleaned()
    {
        Debug.Log("[BrushErasableDirt] " + gameObject.name + " temizlendi! DirtCleaner'a haber veriliyor...");

        // DirtCleaner'a haber ver
        DirtCleaner cleaner = FindObjectOfType<DirtCleaner>();
        if (cleaner != null)
        {
            cleaner.OnDirtCleanedByBrush(transform);
            Debug.Log("[BrushErasableDirt] DirtCleaner bilgilendirildi.");
        }
        else
        {
            Debug.LogError("[BrushErasableDirt] DirtCleaner bulunamadı!");
        }

        // Animasyon ile yok ol
        if (GetComponent<DG.Tweening.Core.DOTweenComponent>() == null)
        {
            transform.DOScale(Vector3.zero, 0.5f)
                .SetEase(DG.Tweening.Ease.InBack)
                .OnComplete(() => Destroy(gameObject));
        }
    }

    public float GetCleanProgress()
    {
        return erasedPixels;
    }

    public bool IsFullyCleaned()
    {
        return isFullyCleaned;
    }

    void OnDestroy()
    {
        if (maskTexture != null)
            maskTexture.Release();

        if (brushTexture != null)
            Destroy(brushTexture);
    }
}
