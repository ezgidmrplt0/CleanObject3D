using System.Collections.Generic;
using UnityEngine;

// Basit seviyeler: Inspector'dan oda prefab'larını sırayla ver
// Level index'i arttıkça prefab index'i (level-1) % roomPrefabs.Length ile döner.
// Örnek: 1→oda0, 2→oda1, 3→oda2, 4→oda0 ...
public class RoomLevelManager : MonoBehaviour
{
    [Header("Oda Prefab'ları (sırayla)")]
    public GameObject[] roomPrefabs;

    [Header("Oda Parent'ı (opsiyonel)")]
    public Transform roomParent;

    [Header("Oda Transform Ayarları")]
    public Vector3 spawnPosition = new Vector3(-1.4f, 4.5f, -4f);
    public Vector3 spawnRotation = new Vector3(0f, -131.175f, 0f);
    public Vector3 spawnScale = new Vector3(0.6f, 0.6f, 0.6f);

    [Header("Level Bilgisi")]
    [Min(1)] public int currentLevel = 1;

    [Header("DirtCleaner Bağlantısı (opsiyonel)")]
    [Tooltip("Her yeni oda yüklendiğinde dirtItems listesini otomatik doldurmak için.")]
    public DirtCleaner dirtCleaner;

    GameObject currentRoomInstance;

    void Awake()
    {
        if (dirtCleaner == null)
            dirtCleaner = FindObjectOfType<DirtCleaner>();
    }

    void Start()
    {
        // Oyun başlarken mevcut level'i yükle
        LoadLevel(currentLevel);
    }

    // Dışarıdan belirli bir level yüklemek için
    public void LoadLevel(int level)
    {
        if (roomPrefabs == null || roomPrefabs.Length == 0)
        {
            Debug.LogWarning("[RoomLevelManager] roomPrefabs boş, level yüklenemedi.");
            return;
        }

        if (level < 1)
            level = 1;

        currentLevel = level;

        int index = (currentLevel - 1) % roomPrefabs.Length;
        GameObject prefab = roomPrefabs[index];
        if (prefab == null)
        {
            Debug.LogWarning("[RoomLevelManager] roomPrefabs[" + index + "] null.");
            return;
        }

        // Eski odayı sil
        if (currentRoomInstance != null)
        {
            Destroy(currentRoomInstance);
            currentRoomInstance = null;
        }

        // Yeni odayı instantiate et
        Transform parent = roomParent != null ? roomParent : null;
        currentRoomInstance = Instantiate(prefab, parent);

        // Belirlenen transform ayarlarını uygula
        currentRoomInstance.transform.position = spawnPosition;
        currentRoomInstance.transform.rotation = Quaternion.Euler(spawnRotation);
        currentRoomInstance.transform.localScale = spawnScale;

        SetupDirtCleanerForRoom();
    }

    // Sıradaki level'e geç
    public void LoadNextLevel()
    {
        LoadLevel(currentLevel + 1);
    }

    void SetupDirtCleanerForRoom()
    {
        if (dirtCleaner == null)
            return;
        if (currentRoomInstance == null)
            return;

        // Oda içindeki tüm BrushErasableDirt'leri bul ve DirtCleaner'a ver
        var dirtList = new List<Transform>();
        var brushDirts = currentRoomInstance.GetComponentsInChildren<BrushErasableDirt>(true);
        foreach (var bd in brushDirts)
        {
            dirtList.Add(bd.transform);
        }

        dirtCleaner.dirtItems = dirtList;
        dirtCleaner.RecalculateTotalsFromList();
    }
}
