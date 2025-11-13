using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Referanslar")]
    public RoomSetup room;
    public DirtCleaner cleaner;

    [Header("Dirt Spawner")]
    public Transform dirtParent;           // boş bırakılırsa otomatik oluşturulur
    public GameObject dirtPrefab;          // boşsa Cube primitive oluşturulur

    [Header("Seviye Ayarları")] 
    public int currentLevel = 1;           // 1 tabanlı seviye
    public int baseRows = 6;               // başlangıç grid satırı
    public int baseCols = 6;               // başlangıç grid sütunu
    public int rowsPerLevel = 0;           // her seviye için ek satır
    public int colsPerLevel = 0;           // her seviye için ek sütun
    public float edgePadding = 0.3f;       // odanın kenarlarından güvenlik payı (dünya birimi)
    public float tileScaleFactor = 0.8f;   // hücre boyutunun yüzde kaçı kadar tile ölçeği
    public float tileHeightOffset = 0.05f; // zeminin üstüne hafif yükseltme

    [Header("Seviye Akışı")] 
    public bool autoNextLevel = true;
    public float nextLevelDelay = 0.8f;

    void Awake()
    {
        if (!room) room = FindObjectOfType<RoomSetup>();
        if (!cleaner) cleaner = FindObjectOfType<DirtCleaner>();
    }

    void Start()
    {
        if (cleaner != null)
        {
            // Seviye bittiğinde bir sonrakine geç
            cleaner.onAllCleaned.AddListener(OnLevelComplete);
        }
        BuildLevel(currentLevel);
    }

    void OnDestroy()
    {
        if (cleaner != null)
            cleaner.onAllCleaned.RemoveListener(OnLevelComplete);
    }

    public void BuildLevel(int level)
    {
        if (!room || !cleaner) return;

        // Grid boyutlarını belirle
        int rows = Mathf.Max(1, baseRows + (level - 1) * rowsPerLevel);
        int cols = Mathf.Max(1, baseCols + (level - 1) * colsPerLevel);

        // Oda ölçülerini olduğu gibi kullanıyoruz; gerekirse seviyeye göre değiştirebilirsin
        // room.odaGenisligi / room.odaDerinligi değerleri Inspector'da
        room.OdayiDuzenle();
        if (room.izometrikKameraKullan) room.IzometrikKameraAyarla();

        // Eski dirtleri temizle
        ClearDirt();

        // Spawn
        var spawned = SpawnGridOnFloor(rows, cols);

        // Cleaner'ı yeni listeyle sıfırla
        cleaner.ResetAll(spawned);
    }

    private void ClearDirt()
    {
        if (!dirtParent)
        {
            var found = GameObject.Find("Dirt");
            if (found) dirtParent = found.transform;
        }
        if (dirtParent)
        {
            for (int i = dirtParent.childCount - 1; i >= 0; i--)
            {
                var ch = dirtParent.GetChild(i);
                if (Application.isPlaying) Destroy(ch.gameObject); else DestroyImmediate(ch.gameObject);
            }
        }
    }

    private List<Transform> SpawnGridOnFloor(int rows, int cols)
    {
        if (!dirtParent)
        {
            var go = new GameObject("Dirt");
            dirtParent = go.transform;
        }

        var list = new List<Transform>();

        // Merkez ve ölçüler
        Vector3 center = room.odaMerkeziReferansi ? room.odaMerkeziReferansi.position : room.transform.position;
        float width = Mathf.Max(0.01f, room.odaGenisligi - 2f * edgePadding);
        float depth = Mathf.Max(0.01f, room.odaDerinligi - 2f * edgePadding);

        float cellX = width / Mathf.Max(1, cols);
        float cellZ = depth / Mathf.Max(1, rows);

        // Sol-arka köşe (yerleşim merkezden simetrik)
        Vector3 origin = new Vector3(center.x - width * 0.5f + cellX * 0.5f,
                                      center.y + tileHeightOffset,
                                      center.z - depth * 0.5f + cellZ * 0.5f);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = new Vector3(origin.x + c * cellX, origin.y, origin.z + r * cellZ);
                var t = CreateDirtTile(pos, cellX, cellZ);
                t.SetParent(dirtParent, true);
                list.Add(t);
            }
        }
        return list;
    }

    private Transform CreateDirtTile(Vector3 position, float cellX, float cellZ)
    {
        GameObject go;
        if (dirtPrefab)
        {
            go = Instantiate(dirtPrefab, position, Quaternion.identity);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = position;
            // BoxCollider zaten geliyor
        }

        // Görsel boyut: hücrenin bir kısmı
        float sx = cellX * tileScaleFactor;
        float sz = cellZ * tileScaleFactor;
        float sy = Mathf.Min(sx, sz) * 0.2f; // ince bir yükseklik

        // Parent ölçeğinden bağımsız dünya boyutu için localScale'ı ayarlıyoruz
        go.transform.localScale = Vector3.one; // önce bir reset
        ApplyWorldSize(go, new Vector3(Mathf.Max(0.01f, sx), Mathf.Max(0.01f, sy), Mathf.Max(0.01f, sz)));

        return go.transform;
    }

    // Parent ölçeğinden bağımsız şekilde hedef dünya boyutuna getir
    private void ApplyWorldSize(GameObject go, Vector3 targetWorldSize)
    {
        var rend = go.GetComponent<Renderer>();
        Vector3 baseLocal = Vector3.one;
        if (rend)
        {
            // World bounds → parent scale'dan arındırıp local kabul
            Vector3 worldSize = rend.bounds.size;
            Vector3 parentScale = go.transform.parent ? go.transform.parent.lossyScale : Vector3.one;
            baseLocal = new Vector3(
                SafeDiv(worldSize.x, parentScale.x),
                SafeDiv(worldSize.y, parentScale.y),
                SafeDiv(worldSize.z, parentScale.z)
            );
            // Eğer çok küçük çıkarsa 1 kabul et
            if (baseLocal.x < 1e-4f) baseLocal.x = 1f;
            if (baseLocal.y < 1e-4f) baseLocal.y = 1f;
            if (baseLocal.z < 1e-4f) baseLocal.z = 1f;
        }
        Vector3 parentS = go.transform.parent ? go.transform.parent.lossyScale : Vector3.one;
        float sx = SafeDiv(targetWorldSize.x, baseLocal.x * parentS.x);
        float sy = SafeDiv(targetWorldSize.y, baseLocal.y * parentS.y);
        float sz = SafeDiv(targetWorldSize.z, baseLocal.z * parentS.z);
        go.transform.localScale = new Vector3(sx, sy, sz);
    }

    private float SafeDiv(float a, float b) => a / (Mathf.Abs(b) < 1e-6f ? 1e-6f : b);

    private void OnLevelComplete()
    {
        if (!autoNextLevel) return;
        Invoke(nameof(NextLevel), nextLevelDelay);
    }

    public void NextLevel()
    {
        currentLevel = Mathf.Max(1, currentLevel + 1);
        BuildLevel(currentLevel);
    }
}
