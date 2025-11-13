using UnityEngine;

public class RoomSetup : MonoBehaviour
{
    [Header("Oda Elemanları")]
    public GameObject duvar1; // Ön duvar (Z+)
    public GameObject duvar2; // Sağ duvar (X+)
    public GameObject duvar3; // Arka duvar (Z-)
    public GameObject duvar4; // Sol duvar (X-)
    public GameObject zemin;

    [Header("Oda Ayarları")] 
    [Min(0.01f)] public float odaGenisligi = 10f;
    [Min(0.01f)] public float odaDerinligi = 10f;
    [Min(0.01f)] public float duvarYuksekligi = 5f;
    [Min(0.001f)] public float duvarKalinligi = 0.1f;
    [Min(0.001f)] public float zeminKalinligi = 0.1f;
    [Header("Zemin Ölçek Modu")]
    [Tooltip("Zemini, bağlı olduğu Mesh'in gerçek boyutlarına göre ölçeklendir (örn. Unity Plane 10x10 olduğu için otomatik düzeltir).")]
    public bool zeminMeshBazliOlcek = true;
    [Tooltip("Oda merkez konumu. Boşsa bu GameObject'in konumu kullanılır.")]
    public Transform odaMerkeziReferansi;
    [Header("Grid / Hizalama")]
    public bool gridSnapAktif = false;
    public float gridAdimi = 1f;

    [Header("Kamera")] 
    public Camera kamera;                     // Boşsa Start'ta Camera.main atanır
    public bool otomatikAnaKamerayiBul = true; // true: kamera yoksa Camera.main kullan
    public bool izometrikKameraKullan = true; // ortho izometrik görünüm
    public float orthoBoyutu = 8f;            // izometrik için orthographic size
    public float kameraMesafesi = 15f;
    public float kameraYuksekligi = 10f;
    [Range(0.1f, 20f)] public float gecisHizi = 5f; // Lerp hızı
    [Header("Kamera Otomatik Kadraj")] 
    [Tooltip("Kamera, odayı ekrana sığdırmak için orthographic size'ı otomatik hesaplasın.")]
    public bool otomatikKadraj = true;
    [Tooltip("Kadraja eklenen çerçeve payı (dünya birimi)." )]
    public float kadrajBosluk = 0.25f;
    [Tooltip("Kamera her zaman oda merkezini hedef alsın (ekranda ortalasın).")]
    public bool odaMerkezineBak = true;
    [Tooltip("Merkez hedefe eklenen opsiyonel offset (sağ/sol/yukarı aşağı).")]
    public Vector3 bakisOffset = Vector3.zero;

    [Header("Görünüm")]
    public bool duvarGizlemeAktif = true;     // bakış açısına göre ön duvarları gizle

    // Dahili durum
    private int kameraKosesi = 0; // 0: Arka-Sağ, 1: Arka-Sol, 2: Ön-Sol, 3: Ön-Sağ
    private Vector3 hedefPozisyon;
    private Quaternion hedefRotasyon;
    private bool kameraHareketEdiyor = false;
    private bool ilkKameraYerlesimiYapildi = false;

    void Start()
    {
        if (!kamera && otomatikAnaKamerayiBul) kamera = Camera.main;

        OdayiDuzenle();

        if (kamera)
        {
            if (izometrikKameraKullan)
            {
                IzometrikKameraAyarla();
            }
            else
            {
                // Perspektif kullanım için de ilk hedefe yerleştir
                HedefKameraPozisyonunuAyarla();
                kamera.transform.position = hedefPozisyon;
                kamera.transform.rotation = hedefRotasyon;
            }

            if (otomatikKadraj)
            {
                FitCameraOrthoToRoomImmediate();
            }
            ilkKameraYerlesimiYapildi = true;
        }

        DuvarlariGuncelle();
        LogKameraAcisi();
    }

    void Update()
    {
        // Köşe değişimi – dokunma/drag ile (soldan sağa / sağdan sola swipe)
        HandleSwipeInput();

        // Smooth kamera hareketi
        if (kameraHareketEdiyor && kamera)
        {
            kamera.transform.position = Vector3.Lerp(kamera.transform.position, hedefPozisyon, Time.deltaTime * gecisHizi);
            kamera.transform.rotation = Quaternion.Lerp(kamera.transform.rotation, hedefRotasyon, Time.deltaTime * gecisHizi);

            if (Vector3.Distance(kamera.transform.position, hedefPozisyon) < 0.01f)
            {
                kamera.transform.position = hedefPozisyon;
                kamera.transform.rotation = hedefRotasyon;
                kameraHareketEdiyor = false;

                if (otomatikKadraj && izometrikKameraKullan)
                {
                    FitCameraOrthoToRoomImmediate();
                }
            }
        }
    }

    // ---- Swipe Girişi ----
    [Header("Swipe Girişi")]
    public float swipeThresholdPixels = 80f;     // bir köşe değiştirmek için gereken min. yatay sürükleme
    public float swipeCooldown = 0.25f;          // ikinci kez tetiklenmeden önce bekleme
    private Vector2 swipeStartPos;
    private bool swipeActive = false;
    private float lastSwipeTime = -999f;

    private void HandleSwipeInput()
    {
    // Obje inceleme modunda oda döndürme devre dışı
    if (ObjectInspectionSystem.Instance != null &&
        ObjectInspectionSystem.Instance.CurrentState == ObjectInspectionSystem.GameState.Inspecting)
        return;
#if UNITY_EDITOR || UNITY_STANDALONE
        // Mouse ile emülasyon: sol tık bas-bırak yatay kaydırma
        if (Input.GetMouseButtonDown(0))
        {
            swipeStartPos = Input.mousePosition;
            swipeActive = true;
        }
        else if (Input.GetMouseButtonUp(0) && swipeActive)
        {
            Vector2 end = Input.mousePosition;
            TrySwipe(end - swipeStartPos);
            swipeActive = false;
        }
#else
        if (Input.touchCount > 0)
        {
            Touch t = Input.touches[0];
            if (t.phase == TouchPhase.Began)
            {
                swipeStartPos = t.position;
                swipeActive = true;
            }
            else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && swipeActive)
            {
                TrySwipe(t.position - swipeStartPos);
                swipeActive = false;
            }
        }
#endif
    }

    private void TrySwipe(Vector2 delta)
    {
        if (Time.time - lastSwipeTime < swipeCooldown) return;
        if (Mathf.Abs(delta.x) < swipeThresholdPixels) return; // yeterli yatay sürükleme yok

        if (delta.x > 0)
            NextCorner();   // sağa doğru sürükleme → saat yönünde
        else
            PrevCorner();   // sola doğru sürükleme → saat yönünün tersine

        lastSwipeTime = Time.time;
    }

    public void OdayiDuzenle()
    {
        Vector3 merkez = odaMerkeziReferansi ? odaMerkeziReferansi.position : transform.position;

        if (zemin)
        {
            // Zemin – merkezde, yatay
            zemin.transform.position = Snap(merkez);
            zemin.transform.rotation = Quaternion.identity;

            // Hedef dünya boyutu → ebeveyn scale'ından bağımsız localScale hesapla
            Vector3 hedefDunya = new Vector3(odaGenisligi, Mathf.Max(0.001f, zeminKalinligi), odaDerinligi);
            ApplyWorldSize(zemin, hedefDunya, ignoreYIfFlatMesh: true);
        }

        if (duvar1)
        {
            // Ön duvar (Z+)
            duvar1.transform.position = Snap(new Vector3(merkez.x, merkez.y + duvarYuksekligi / 2f, merkez.z + odaDerinligi / 2f));
            duvar1.transform.rotation = Quaternion.identity;
            ApplyWorldSize(duvar1, new Vector3(odaGenisligi, duvarYuksekligi, Mathf.Max(0.001f, duvarKalinligi)), ignoreYIfFlatMesh: false);
        }

        if (duvar2)
        {
            // Sağ duvar (X+)
            duvar2.transform.position = Snap(new Vector3(merkez.x + odaGenisligi / 2f, merkez.y + duvarYuksekligi / 2f, merkez.z));
            duvar2.transform.rotation = Quaternion.Euler(0, 90, 0);
            ApplyWorldSize(duvar2, new Vector3(odaDerinligi, duvarYuksekligi, Mathf.Max(0.001f, duvarKalinligi)), ignoreYIfFlatMesh: false);
        }

        if (duvar3)
        {
            // Arka duvar (Z-)
            duvar3.transform.position = Snap(new Vector3(merkez.x, merkez.y + duvarYuksekligi / 2f, merkez.z - odaDerinligi / 2f));
            duvar3.transform.rotation = Quaternion.identity;
            ApplyWorldSize(duvar3, new Vector3(odaGenisligi, duvarYuksekligi, Mathf.Max(0.001f, duvarKalinligi)), ignoreYIfFlatMesh: false);
        }

        if (duvar4)
        {
            // Sol duvar (X-)
            duvar4.transform.position = Snap(new Vector3(merkez.x - odaGenisligi / 2f, merkez.y + duvarYuksekligi / 2f, merkez.z));
            duvar4.transform.rotation = Quaternion.Euler(0, 90, 0);
            ApplyWorldSize(duvar4, new Vector3(odaDerinligi, duvarYuksekligi, Mathf.Max(0.001f, duvarKalinligi)), ignoreYIfFlatMesh: false);
        }
    }

    public void IzometrikKameraAyarla()
    {
        if (!kamera) return;

        HedefKameraPozisyonunuAyarla();

        // İlk başlatmada direkt konumla
        kamera.transform.position = hedefPozisyon;
        kamera.transform.rotation = hedefRotasyon;

        // Orthographic izometrik görünüm
        kamera.orthographic = true;
        kamera.orthographicSize = Mathf.Max(0.01f, orthoBoyutu);

        if (otomatikKadraj)
        {
            FitCameraOrthoToRoomImmediate();
        }
    }

    private void HedefKameraPozisyonunuAyarla()
    {
        Vector3 kameraPozisyonu = Vector3.zero;

        switch (kameraKosesi)
        {
            case 0: // Arka-Sağ
                kameraPozisyonu = new Vector3(kameraMesafesi, kameraYuksekligi, kameraMesafesi);
                break;
            case 1: // Arka-Sol
                kameraPozisyonu = new Vector3(-kameraMesafesi, kameraYuksekligi, kameraMesafesi);
                break;
            case 2: // Ön-Sol
                kameraPozisyonu = new Vector3(-kameraMesafesi, kameraYuksekligi, -kameraMesafesi);
                break;
            case 3: // Ön-Sağ
                kameraPozisyonu = new Vector3(kameraMesafesi, kameraYuksekligi, -kameraMesafesi);
                break;
        }

        hedefPozisyon = kameraPozisyonu;
        Vector3 merkez = odaMerkeziReferansi ? odaMerkeziReferansi.position : transform.position;
        Vector3 hedef = (merkez + bakisOffset);
        hedefRotasyon = Quaternion.LookRotation(hedef - kameraPozisyonu);
        kameraHareketEdiyor = true;
    }

    private void DuvarlariGuncelle()
    {
        // Tüm duvarları aç
        if (duvar1) duvar1.SetActive(true);
        if (duvar2) duvar2.SetActive(true);
        if (duvar3) duvar3.SetActive(true);
        if (duvar4) duvar4.SetActive(true);

        if (!duvarGizlemeAktif) return;

        // Bakış açısına göre ön duvarları gizle
        switch (kameraKosesi)
        {
            case 0: // Arka-Sağ: Ön (duvar1) ve Sağ (duvar2) gizle
                if (duvar1) duvar1.SetActive(false);
                if (duvar2) duvar2.SetActive(false);
                break;
            case 1: // Arka-Sol: Ön (duvar1) ve Sol (duvar4) gizle
                if (duvar1) duvar1.SetActive(false);
                if (duvar4) duvar4.SetActive(false);
                break;
            case 2: // Ön-Sol: Arka (duvar3) ve Sol (duvar4) gizle
                if (duvar3) duvar3.SetActive(false);
                if (duvar4) duvar4.SetActive(false);
                break;
            case 3: // Ön-Sağ: Sağ (duvar2) ve Arka (duvar3) gizle
                if (duvar2) duvar2.SetActive(false);
                if (duvar3) duvar3.SetActive(false);
                break;
        }
    }

    private void LogKameraAcisi()
    {
        string acisiAdi = "";
        string gizliDuvarlar = "";

        switch (kameraKosesi)
        {
            case 0:
                acisiAdi = "A (Arka-Sağ)";
                gizliDuvarlar = "Duvar1 (Ön) ve Duvar2 (Sağ)";
                break;
            case 1:
                acisiAdi = "B (Arka-Sol)";
                gizliDuvarlar = "Duvar1 (Ön) ve Duvar4 (Sol)";
                break;
            case 2:
                acisiAdi = "C (Ön-Sol)";
                gizliDuvarlar = "Duvar3 (Arka) ve Duvar4 (Sol)";
                break;
            case 3:
                acisiAdi = "D (Ön-Sağ)";
                gizliDuvarlar = "Duvar2 (Sağ) ve Duvar3 (Arka)";
                break;
        }

        Debug.Log($"=== KAMERA AÇISI: {acisiAdi} ===\nGizli Duvarlar: {gizliDuvarlar}");
    }

    // UI butonlarından da çağrılabilsin
    public void NextCorner()
    {
        kameraKosesi = (kameraKosesi + 1) % 4;
        HedefKameraPozisyonunuAyarla();
        DuvarlariGuncelle();
        LogKameraAcisi();
        if (ilkKameraYerlesimiYapildi && otomatikKadraj && izometrikKameraKullan) FitCameraOrthoToRoomImmediate();
    }

    public void PrevCorner()
    {
        kameraKosesi = (kameraKosesi - 1 + 4) % 4;
        HedefKameraPozisyonunuAyarla();
        DuvarlariGuncelle();
        LogKameraAcisi();
        if (ilkKameraYerlesimiYapildi && otomatikKadraj && izometrikKameraKullan) FitCameraOrthoToRoomImmediate();
    }

    public void SetCorner(int kose)
    {
        kameraKosesi = Mathf.Clamp(kose, 0, 3);
        HedefKameraPozisyonunuAyarla();
        DuvarlariGuncelle();
        LogKameraAcisi();
        if (ilkKameraYerlesimiYapildi && otomatikKadraj && izometrikKameraKullan) FitCameraOrthoToRoomImmediate();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            OdayiDuzenle();
            if (izometrikKameraKullan && kamera)
            {
                IzometrikKameraAyarla();
            }
        }
    }

    // ----------------- Yardımcılar -----------------
    private Vector3 Snap(Vector3 p)
    {
        if (!gridSnapAktif || gridAdimi <= 0f) return p;
        p.x = Mathf.Round(p.x / gridAdimi) * gridAdimi;
        p.y = Mathf.Round(p.y / gridAdimi) * gridAdimi;
        p.z = Mathf.Round(p.z / gridAdimi) * gridAdimi;
        return p;
    }

    private void FitCameraOrthoToRoomImmediate()
    {
        if (!kamera || !kamera.orthographic) return;

        Vector3 merkez = odaMerkeziReferansi ? odaMerkeziReferansi.position : transform.position;
        float yarimGenislik = odaGenisligi * 0.5f;
        float yarimDerinlik = odaDerinligi * 0.5f;

        // Odanın tabanındaki 4 köşe
        Vector3[] koseDunya = new Vector3[4]
        {
            new Vector3(merkez.x - yarimGenislik, merkez.y, merkez.z - yarimDerinlik),
            new Vector3(merkez.x - yarimGenislik, merkez.y, merkez.z + yarimDerinlik),
            new Vector3(merkez.x + yarimGenislik, merkez.y, merkez.z - yarimDerinlik),
            new Vector3(merkez.x + yarimGenislik, merkez.y, merkez.z + yarimDerinlik)
        };

        // Kamera uzayına çevir ve up/right eksenlerindeki kapsama alanını bul
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

        for (int i = 0; i < koseDunya.Length; i++)
        {
            Vector3 camLocal = kamera.transform.InverseTransformPoint(koseDunya[i]);
            if (camLocal.x < minX) minX = camLocal.x;
            if (camLocal.x > maxX) maxX = camLocal.x;
            if (camLocal.y < minY) minY = camLocal.y;
            if (camLocal.y > maxY) maxY = camLocal.y;
        }

        float yarimGenislikEkran = (maxX - minX) * 0.5f + kadrajBosluk;
        float yarimYukseklikEkran = (maxY - minY) * 0.5f + kadrajBosluk;

        float gerekenOrtho = Mathf.Max(yarimYukseklikEkran, yarimGenislikEkran / Mathf.Max(0.0001f, kamera.aspect));
        kamera.orthographicSize = Mathf.Max(0.01f, gerekenOrtho);
    }

    // Belirtilen GameObject'in localScale'ını, ebeveyn scale'ından bağımsız şekilde
    // hedef DÜNYA boyutuna getirecek şekilde hesaplar.
    private void ApplyWorldSize(GameObject go, Vector3 hedefDunyaBoyutu, bool ignoreYIfFlatMesh)
    {
        if (!go) return;

        // Parent'ın world scale'ı (localScale hariç)
        Vector3 lossy = go.transform.lossyScale;
        Vector3 local = go.transform.localScale;
        Vector3 parentScale = new Vector3(
            SafeDiv(lossy.x, local.x),
            SafeDiv(lossy.y, local.y),
            SafeDiv(lossy.z, local.z)
        );

        // Mesh boyutu (local uzay). Yoksa Renderer world bounds'ından yaklaşıkla.
        Vector3 meshLocalSize;
        bool hasMesh = TryGetMeshLocalSize(go, out meshLocalSize);

        if (!hasMesh)
        {
            // Mesh yoksa 1 birim kabul edip parent scale'ına göre düzelt.
            meshLocalSize = Vector3.one;
        }

        // Plane gibi Y kalınlığı 0 olan meshlerde Y'yi yoksay
        if (ignoreYIfFlatMesh && meshLocalSize.y < 1e-5f)
        {
            meshLocalSize.y = 1f; // kalınlık görsel değil; ölçek hesaplamasında 1 kabul
        }

        float sx = SafeDiv(hedefDunyaBoyutu.x, meshLocalSize.x * parentScale.x);
        float sy = SafeDiv(hedefDunyaBoyutu.y, meshLocalSize.y * parentScale.y);
        float sz = SafeDiv(hedefDunyaBoyutu.z, meshLocalSize.z * parentScale.z);

        // Eğer y'yi yoksayacaksak mevcut y scale'ını koru
        if (ignoreYIfFlatMesh)
            sy = local.y;

        go.transform.localScale = new Vector3(sx, sy, sz);
    }

    private static float SafeDiv(float a, float b)
    {
        return a / (Mathf.Abs(b) < 1e-6f ? 1e-6f : b);
    }

    private bool TryGetMeshLocalSize(GameObject go, out Vector3 size)
    {
        size = Vector3.zero;
        var mf = go.GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh)
        {
            size = mf.sharedMesh.bounds.size;
            return true;
        }

        var rend = go.GetComponent<Renderer>();
        if (rend)
        {
            // world bounds → local eksenlere yaklaşık varsayım; parent scale düzeltilirken yeterli olur
            size = rend.bounds.size; // world
            // world boyutunu parent scale'dan arındırıp local kabul et
            Vector3 parentScale = go.transform.parent ? go.transform.parent.lossyScale : Vector3.one;
            size = new Vector3(
                SafeDiv(size.x, parentScale.x),
                SafeDiv(size.y, parentScale.y),
                SafeDiv(size.z, parentScale.z)
            );
            return true;
        }
        return false;
    }
}
