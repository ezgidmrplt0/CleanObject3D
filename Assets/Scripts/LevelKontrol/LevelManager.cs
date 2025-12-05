using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Level Objeleri")]
    [Tooltip("Sahnedeki aktif level instance'ları")]
    public List<GameObject> levels = new List<GameObject>();
    
    [Header("Orijinal Prefablar")]
    [Tooltip("Sıfırlama için orijinal prefablar (levels ile aynı sırada olmalı)")]
    public List<GameObject> originalPrefabs = new List<GameObject>();

    [Header("UI")]
    public TMP_Text levelText;   // "LEVEL 1" "LEVEL 2" yazan TextMeshPro

    [Header("Geçiş Ayarları")]
    public float transitionDuration = 0.7f; // köpük geçiş süresi
    
    [Header("Level Ayarları")]
    [Tooltip("PlayerPrefs'te level bilgisinin saklandığı key")]
    public string playerPrefsLevelKey = "CurrentLevel";

    int currentIndex = 0;
    bool isTransitioning = false;

    void Start()
    {
        // Kayıtlı level'e göre hangi prefab açılacak hesapla
        int savedLevel = PlayerPrefs.GetInt(playerPrefsLevelKey, 1);
        currentIndex = GetPrefabIndexForLevel(savedLevel);
        
        Debug.Log("[LevelManager] Level " + savedLevel + " için Prefab index: " + currentIndex);
        
        // Başlangıçta doğru prefab açık olsun
        for (int i = 0; i < levels.Count; i++)
            levels[i].SetActive(i == currentIndex);

        UpdateLevelText();
    }
    
    /// <summary>
    /// Level numarasına göre hangi prefab index'inin açılacağını hesaplar
    /// Örnek (5 prefab):
    /// Level 1, 6, 11, 16, 21... → Prefab 0 (Bathroom)
    /// Level 2, 7, 12, 17, 22... → Prefab 1
    /// Level 3, 8, 13, 18, 23... → Prefab 2
    /// Level 4, 9, 14, 19, 24... → Prefab 3
    /// Level 5, 10, 15, 20, 25... → Prefab 4
    /// </summary>
    int GetPrefabIndexForLevel(int level)
    {
        if (levels.Count == 0) return 0;
        
        // (level - 1) % prefab sayısı
        // Level 1 → (1-1) % 5 = 0 → Prefab 0
        // Level 2 → (2-1) % 5 = 1 → Prefab 1
        // Level 5 → (5-1) % 5 = 4 → Prefab 4
        // Level 6 → (6-1) % 5 = 0 → Prefab 0
        int prefabIndex = (level - 1) % levels.Count;
        
        return prefabIndex;
    }

    public void LoadNextLevel()
    {
        // Kayıtlı level'e göre bir sonraki prefab'ı hesapla
        int savedLevel = PlayerPrefs.GetInt(playerPrefsLevelKey, 1);
        int nextPrefabIndex = GetPrefabIndexForLevel(savedLevel);
        
        Debug.Log("[LevelManager] LoadNextLevel - Level: " + savedLevel + ", Prefab Index: " + nextPrefabIndex);
        
        // Eğer aynı prefab'taysa sadece DirtCleaner sıfırlanacak (LevelTimer hallediyor)
        // Farklı prefab'a geçiş gerekiyorsa LoadLevel çağır
        if (nextPrefabIndex != currentIndex)
        {
            LoadLevel(nextPrefabIndex);
        }
        else
        {
            Debug.Log("[LevelManager] Aynı prefab'ta devam ediliyor.");
        }
    }

    public void LoadLevel(int index)
    {
        if (isTransitioning) return;
        if (index < 0 || index >= levels.Count) return;

        isTransitioning = true;

        // Eğer FoamTransitionController yoksa, direkt geçiş yap (güvenlik için)
        if (FoamTransitionController.Instance == null)
        {
            Debug.LogWarning("[LevelManager] FoamTransitionController bulunamadı, direkt level değiştiriliyor.");
            SwitchLevel(index);
            isTransitioning = false;
            return;
        }

        // DOTween Sequence: köpükle kapan -> level değiş -> köpükle aç
        Sequence seq = DOTween.Sequence();

        // 1) Ekranı köpükle kapat
        seq.Append(FoamTransitionController.Instance.FoamClose(transitionDuration));

        // 2) Level değiş
        seq.AppendCallback(() =>
        {
            SwitchLevel(index);
        });

        // 3) Köpüğü aç (kaybolsun)
        seq.Append(FoamTransitionController.Instance.FoamOpen(transitionDuration));

        // 4) Bittiğinde transition kilidini kaldır
        seq.OnComplete(() =>
        {
            isTransitioning = false;
        });
    }

    void SwitchLevel(int index)
    {
        if (levels.Count == 0) return;

        // Önceki prefab'ı sıfırla (kirleri geri getir)
        ResetPrefab(levels[currentIndex]);
        
        levels[currentIndex].SetActive(false);
        currentIndex = index;
        levels[currentIndex].SetActive(true);
        UpdateLevelText();
    }
    
    /// <summary>
    /// Prefab'ı sıfırlar - destroy edilmiş objeleri geri getirir
    /// </summary>
    void ResetPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        
        int prefabIndex = levels.IndexOf(prefab);
        if (prefabIndex < 0 || prefabIndex >= originalPrefabs.Count) return;
        
        GameObject originalPrefab = originalPrefabs[prefabIndex];
        if (originalPrefab == null)
        {
            Debug.LogWarning("[LevelManager] Orijinal prefab atanmamış, index: " + prefabIndex);
            return;
        }
        
        // Prefab'ın pozisyon ve rotasyonunu kaydet
        Vector3 pos = prefab.transform.position;
        Quaternion rot = prefab.transform.rotation;
        Vector3 scale = prefab.transform.localScale;
        Transform parent = prefab.transform.parent;
        
        Debug.Log("[LevelManager] Prefab sıfırlanıyor: " + prefab.name);
        
        // Eski prefab'ı yok et
        Destroy(prefab);
        
        // Yeni instance oluştur
        GameObject newInstance = Instantiate(originalPrefab, pos, rot, parent);
        newInstance.name = originalPrefab.name; // (Clone) kaldır
        newInstance.transform.localScale = scale;
        newInstance.SetActive(false); // Başlangıçta kapalı
        levels[prefabIndex] = newInstance;
        
        Debug.Log("[LevelManager] Prefab yeniden oluşturuldu: " + newInstance.name);
    }

    void UpdateLevelText()
    {
        if (levelText != null)
        {
            int savedLevel = PlayerPrefs.GetInt(playerPrefsLevelKey, 1);
            levelText.text = "LEVEL " + savedLevel;
        }
    }
    
    /// <summary>
    /// Mevcut prefab index'ini döndürür
    /// </summary>
    public int GetCurrentPrefabIndex()
    {
        return currentIndex;
    }
}
