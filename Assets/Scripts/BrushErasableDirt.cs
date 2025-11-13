using UnityEngine;
using DG.Tweening;

public class BrushErasableDirt : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Fırça boyutu (dünya birimi cinsinden)")]
    public float brushSize = 0.3f;
    
    [Tooltip("Fırça yumuşaklığı (1 = sert, 0.1 = yumuşak)")]
    [Range(0.1f, 1f)]
    public float brushHardness = 0.5f;
    
    [Tooltip("Temizlenme yüzdesi (0-1, ne kadar silinmesi gerek)")]
    [Range(0.5f, 0.99f)]
    public float cleanThreshold = 0.85f;
    
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
                
                pixels[y * size + x] = new Color(0, 0, 0, alpha); // Siyah = sil
            }
        }
        
        brushTexture.SetPixels(pixels);
        brushTexture.Apply();
    }
    
    public void EraseBrushStroke(Vector3 worldPosition)
    {
        if (isFullyCleaned) return;
        
        // Raycast ile doğru hit point ve UV al
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (!Physics.Raycast(ray, out hit, 1000f))
            return;
            
        if (hit.transform != transform)
            return;
        
        // RaycastHit'ten UV koordinatını al
        Vector2 uv = hit.textureCoord;
        
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
        {
            Debug.LogWarning("UV koordinatı geçersiz: " + uv);
            return;
        }
        
        // UV'den texture pixel koordinatına
        int pixelX = Mathf.RoundToInt(uv.x * textureResolution);
        int pixelY = Mathf.RoundToInt(uv.y * textureResolution);
        
        // Sınırları kontrol et
        pixelX = Mathf.Clamp(pixelX, 0, textureResolution - 1);
        pixelY = Mathf.Clamp(pixelY, 0, textureResolution - 1);
        
        Debug.Log($"UV: {uv}, Pixel: ({pixelX}, {pixelY})");
        
        // Fırça çizimi
        DrawBrush(pixelX, pixelY);
        
        // Silinme yüzdesini kontrol et
        CheckCleanProgress();
    }
    
    Vector2 WorldToUV(Vector3 worldPos)
    {
        // Raycast ile gerçek UV koordinatını al
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform == transform && hit.textureCoord != Vector2.zero)
            {
                // Raycast'ten gelen UV koordinatını kullan
                return hit.textureCoord;
            }
        }
        
        // Fallback: Local space üzerinden hesapla
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
        RenderTexture.active = maskTexture;
        
        // Fırça boyutunu texture space'e çevir
        int brushPixelSize = Mathf.RoundToInt(brushSize * textureResolution / 10f);
        brushPixelSize = Mathf.Max(10, brushPixelSize);
        
        // Manuel pixel yazma (daha stabil)
        Texture2D tempTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.ARGB32, false);
        RenderTexture.active = maskTexture;
        tempTexture.ReadPixels(new Rect(0, 0, textureResolution, textureResolution), 0, 0);
        tempTexture.Apply();
        
        Color[] pixels = tempTexture.GetPixels();
        
        // Fırça çiz (siyah = silinmiş)
        for (int y = -brushPixelSize/2; y < brushPixelSize/2; y++)
        {
            for (int x = -brushPixelSize/2; x < brushPixelSize/2; x++)
            {
                int px = centerX + x;
                int py = centerY + y;
                
                if (px >= 0 && px < textureResolution && py >= 0 && py < textureResolution)
                {
                    float dist = Mathf.Sqrt(x * x + y * y) / (brushPixelSize / 2f);
                    if (dist <= 1f)
                    {
                        float alpha = Mathf.Pow(1f - dist, 1f / brushHardness);
                        int index = py * textureResolution + px;
                        
                        // Mevcut değeri koyulaştır
                        float currentValue = pixels[index].r;
                        float newValue = currentValue * (1f - alpha * 0.3f); // Kademeli karartma
                        pixels[index] = new Color(newValue, newValue, newValue, 1f);
                    }
                }
            }
        }
        
        tempTexture.SetPixels(pixels);
        tempTexture.Apply();
        
        Graphics.Blit(tempTexture, maskTexture);
        RenderTexture.active = null;
        
        Destroy(tempTexture);
    }
    
    void CheckCleanProgress()
    {
        // Her frame kontrol etme, performans için seyrek kontrol
        if (Time.frameCount % 10 != 0) return;
        
        // Maskenin ne kadarının siyah olduğunu hesapla (CPU'da ağır, mobil için optimize)
        RenderTexture.active = maskTexture;
        Texture2D temp = new Texture2D(textureResolution, textureResolution, TextureFormat.RGB24, false);
        temp.ReadPixels(new Rect(0, 0, textureResolution, textureResolution), 0, 0);
        temp.Apply();
        
        Color[] pixels = temp.GetPixels();
        float blackPixels = 0;
        
        // Basit sampling (her 4. pixel, mobil performans)
        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i].r < 0.5f) // Siyaha yakın
                blackPixels++;
        }
        
        Destroy(temp);
        RenderTexture.active = null;
        
        erasedPixels = (blackPixels * 4f) / totalPixels; // *4 çünkü her 4. pixel kontrol ettik
        
        // Debug.Log($"Silinme yüzdesi: {erasedPixels * 100:F1}%");
        
        if (erasedPixels >= cleanThreshold && !isFullyCleaned)
        {
            isFullyCleaned = true;
            OnFullyCleaned();
        }
    }
    
    void OnFullyCleaned()
    {
        // DirtCleaner'a haber ver
        DirtCleaner cleaner = FindObjectOfType<DirtCleaner>();
        if (cleaner != null)
        {
            cleaner.OnDirtCleanedByBrush(transform);
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