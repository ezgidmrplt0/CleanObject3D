using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelTimer : MonoBehaviour
{
    [Header("Para Ayarları")]
public int coinsFor3Stars = 30;
public int coinsFor2Stars = 20;
public int coinsFor1Star = 10;

[Header("Para UI (opsiyonel)")]
public TextMeshProUGUI coinsText;   // Ekranda toplam parayı göstermek istersen

    [Header("Süre Ayarları (saniye)")]
    [Tooltip("Toplam süre (geri sayım başlangıcı)")]
    public float totalTime = 30f;
    
    [Tooltip("Bu süreden önce biterse 3 yıldız")]
    public float timeFor3Stars = 10f;
    
    [Tooltip("Bu süreden önce biterse 2 yıldız")]
    public float timeFor2Stars = 20f;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public Slider timerSlider;
    
    [Header("Level Failed UI")]
    [Tooltip("Level failed olduğunda gösterilecek panel/image")]
    public GameObject failedPanel;
    [Tooltip("Level failed sprite (opsiyonel, starsImage üzerine)")]
    public Sprite spriteFailedStar;

    [Header("Yıldız Spriteları")]
    public Image starsImage;        // Üzerine yıldız spriteları gelecek Image
    public Sprite sprite1Star;
    public Sprite sprite2Stars;
    public Sprite sprite3Stars;

    [Header("Bağlantılar")]
    public DirtCleaner dirtCleaner;
    public LevelManager levelManager;

    [Header("Level Takibi")]
    public TextMeshProUGUI levelText; // Ekranda gösterilecek level yazısı
    public string playerPrefsLevelKey = "CurrentLevel";

    [Header("Zorluk Ayarları")]
    [Tooltip("Her 5 levelde bir artan zorluk adımında cleanThreshold'a eklenecek değer.")]
    public float thresholdPerStep = 0.03f;

    [Tooltip("Her 5 levelde bir artan zorluk adımında fırça boyutundan düşülecek pixel miktarı.")]
    public int brushSizeReducePerStep = 3;

    [Tooltip("Temizlenme eşiği için üst sınır.")]
    public float maxCleanThreshold = 0.98f;

    [Tooltip("Fırça boyutu için alt sınır (pixel).")]
    public int minBrushPixelRadius = 15;

    [Header("Level Bittiğinde")]
    [Tooltip("Level bitince kaç saniye beklenecek (yıldızları görmek için)")]
    public float delayBeforeNextLevel = 2f;

    float remainingTime = 0f;
    bool running = false;
    bool finished = false;

    void Start()
    {
        // Level bilgisini yükle ve UI'ı güncelle
        int currentLevel = PlayerPrefs.GetInt(playerPrefsLevelKey, 1);
        if (levelText)
            levelText.text = "Level " + currentLevel;

        ApplyDifficultyForLevel(currentLevel);

        UpdateCoinsUI();
        if (!dirtCleaner) dirtCleaner = FindObjectOfType<DirtCleaner>();
        if (dirtCleaner != null)
        {
            dirtCleaner.onAllCleaned.AddListener(OnAllCleaned);
            Debug.Log("[LevelTimer] DirtCleaner bulundu, onAllCleaned dinleniyor.");
        }
        else
        {
            Debug.LogError("[LevelTimer] DirtCleaner BULUNAMADI!");
        }

        remainingTime = totalTime;
        UpdateUI();

        if (starsImage) starsImage.enabled = false;
        if (failedPanel) failedPanel.SetActive(false);
        
        // NOT: running durumu OnEnable'da ayarlanıyor
    }
    
    void OnEnable()
    {
        // Script aktif edildiğinde timer'ı başlat
        StartTimer();
    }
    
    /// <summary>
    /// Timer'ı başlatır veya yeniden başlatır
    /// </summary>
    public void StartTimer()
    {
        remainingTime = totalTime;
        running = true;
        finished = false;
        UpdateUI();
        Debug.Log("[LevelTimer] Timer başlatıldı. Süre: " + totalTime + " saniye");
    }

    void Update()
    {
        if (!running || finished) return;

        remainingTime -= Time.deltaTime;
        UpdateUI();
        
        // Süre bitti mi kontrol et
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            OnTimerExpired();
        }
    }

    void UpdateUI()
    {
        if (timerText)
        {
            int sec = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
            timerText.text = sec + "s";
        }

        if (timerSlider)
        {
            // Slider: kalan süre / toplam süre (1'den 0'a doğru azalır)
            timerSlider.value = Mathf.Clamp01(remainingTime / totalTime);
        }
    }
    
    /// <summary>
    /// Süre dolduğunda çağrılır - Level Failed
    /// </summary>
    void OnTimerExpired()
    {
        if (finished) return;
        finished = true;
        running = false;
        
        Debug.Log("[LevelTimer] SÜRE DOLDU! Level Failed.");
        
        // Failed görseli göster
        if (failedPanel != null)
            failedPanel.SetActive(true);
        
        if (starsImage != null && spriteFailedStar != null)
        {
            starsImage.sprite = spriteFailedStar;
            starsImage.enabled = true;
        }
        
        // Aynı level'i tekrar yükle (level artmadan)
        StartCoroutine(ReloadSameLevelAfterDelay(delayBeforeNextLevel));
    }
    
    System.Collections.IEnumerator ReloadSameLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Failed panelini gizle
        if (failedPanel != null) failedPanel.SetActive(false);
        if (starsImage != null) starsImage.enabled = false;
        
        // LevelManager ile aynı level'i tekrar yükle
        if (levelManager == null)
            levelManager = FindObjectOfType<LevelManager>();
        
        if (levelManager != null)
        {
            // Aynı prefab'ı sıfırla ve yeniden yükle
            int currentPrefabIndex = levelManager.GetCurrentPrefabIndex();
            levelManager.LoadLevel(currentPrefabIndex);
            Debug.Log("[LevelTimer] Aynı level tekrar yükleniyor...");
            
            yield return new WaitForSeconds(levelManager.transitionDuration + 0.2f);
            
            ResetForNewLevel();
        }
        else
        {
            // LevelManager yoksa sahneyi yeniden yükle
            Debug.LogWarning("[LevelTimer] LevelManager bulunamadı, sahne yeniden yükleniyor.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void OnAllCleaned()
    {
        Debug.Log("[LevelTimer] OnAllCleaned çağrıldı! Level tamamlandı.");
        
        if (finished) return;
        finished = true;
        running = false;

        int stars = CalculateStars();
        Debug.Log("[LevelTimer] Yıldız sayısı: " + stars);
        ApplyStarSprite(stars);
        AddCoinsForStars(stars);

        // Level'i artır
        int currentLevel = PlayerPrefs.GetInt(playerPrefsLevelKey, 1);
        currentLevel++;
        PlayerPrefs.SetInt(playerPrefsLevelKey, currentLevel);
        PlayerPrefs.Save();

        Debug.Log("[LevelTimer] Yeni level: " + currentLevel + " - " + delayBeforeNextLevel + " saniye sonra geçiş yapılacak...");

        // Bekle, sonra otomatik olarak sahneyi yeniden yükle (yeni level için)
        StartCoroutine(LoadNextLevelAfterDelay(delayBeforeNextLevel));
    }

    System.Collections.IEnumerator LoadNextLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Yıldız görüntüsünü gizle
        if (starsImage) starsImage.enabled = false;
        
        // LevelManager ile bir sonraki prefab'a geç
        if (levelManager == null)
            levelManager = FindObjectOfType<LevelManager>();
        
        if (levelManager != null)
        {
            levelManager.LoadNextLevel();
            Debug.Log("[LevelTimer] LevelManager.LoadNextLevel() çağrıldı.");
            
            // Biraz bekle (level geçiş animasyonu için), sonra DirtCleaner'ı sıfırla
            yield return new WaitForSeconds(levelManager.transitionDuration + 0.2f);
            
            ResetForNewLevel();
        }
        else
        {
            Debug.LogError("[LevelTimer] LevelManager bulunamadı!");
        }
    }
    
    void ResetForNewLevel()
    {
        // Timer'ı sıfırla (geri sayım başa dönsün)
        remainingTime = totalTime;
        running = true;
        finished = false;
        
        // Failed panelini gizle
        if (failedPanel != null) failedPanel.SetActive(false);
        if (starsImage != null) starsImage.enabled = false;
        
        // DirtCleaner'ı yeni level için sıfırla
        if (dirtCleaner == null)
            dirtCleaner = FindObjectOfType<DirtCleaner>();
        
        if (dirtCleaner != null)
        {
            dirtCleaner.FindAllDirts(); // Yeni level'deki kirleri bul
        }
        
        // Level text güncelle
        int currentLevel = PlayerPrefs.GetInt(playerPrefsLevelKey, 1);
        if (levelText)
            levelText.text = "Level " + currentLevel;
        
        // Zorluk ayarla
        ApplyDifficultyForLevel(currentLevel);
        
        UpdateUI();
        Debug.Log("[LevelTimer] Yeni level için sıfırlandı. Süre: " + totalTime + " saniye");
    }

    int CalculateStars()
    {
        // Harcanan süre = toplam süre - kalan süre
        float elapsedTime = totalTime - remainingTime;
        
        // 10 saniyeden önce biterse 3 yıldız
        // 20 saniyeden önce biterse 2 yıldız
        // 20-30 arası 1 yıldız
        if (elapsedTime <= timeFor3Stars) return 3;
        if (elapsedTime <= timeFor2Stars) return 2;
        return 1;
    }

    void AddCoinsForStars(int stars)
{
    int add = 0;
    if (stars == 3) add = coinsFor3Stars;
    else if (stars == 2) add = coinsFor2Stars;
    else add = coinsFor1Star;

    int current = PlayerPrefs.GetInt("Coins", 0);
    current += add;
    PlayerPrefs.SetInt("Coins", current);
    PlayerPrefs.Save();

    UpdateCoinsUI();
}

void UpdateCoinsUI()
{
    if (!coinsText) return;
    int current = PlayerPrefs.GetInt("Coins", 0);
    coinsText.text = current.ToString();
}

    void ApplyStarSprite(int starCount)
    {
        if (!starsImage) return;

        Sprite s = null;
        if (starCount == 3) s = sprite3Stars;
        else if (starCount == 2) s = sprite2Stars;
        else s = sprite1Star;

        starsImage.sprite = s;
        starsImage.enabled = (s != null);
    }

    void ApplyDifficultyForLevel(int level)
    {
        // Her 5 levelde bir zorluk artsın: kirler daha zor silinsin
        int step = Mathf.Max(0, (level - 1) / 5);
        if (step == 0) return; // ilk 5 level temel ayar

        BrushErasableDirt[] dirts = FindObjectsOfType<BrushErasableDirt>();

        foreach (var dirt in dirts)
        {
            if (!dirt) continue;

            // Temizlenme eşiğini artır (daha çok silmek gereksin)
            float extraThreshold = thresholdPerStep * step;
            dirt.cleanThreshold = Mathf.Clamp01(dirt.cleanThreshold + extraThreshold);
            dirt.cleanThreshold = Mathf.Min(dirt.cleanThreshold, maxCleanThreshold);

            // Fırça boyutunu biraz küçült (her stroke daha az alan silsin)
            int reduceAmount = brushSizeReducePerStep * step;
            dirt.brushPixelRadius = Mathf.Max(minBrushPixelRadius, dirt.brushPixelRadius - reduceAmount);
        }
    }

    // Şimdilik: level bitince açılan butondan çağırılacak, aynı leveli yeniden yükler
    public void RestartLevel()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}