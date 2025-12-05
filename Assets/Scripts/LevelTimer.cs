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
    public float timeFor3Stars = 30f;
    public float timeFor2Stars = 50f;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public Slider timerSlider;

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

    [Tooltip("Her 5 levelde bir artan zorluk adımında brushSize'e uygulanacak çarpan (0.9-1 arası, küçültmek için <1).")]
    public float brushSizeFactorPerStep = 0.95f;

    [Tooltip("Temizlenme eşiği için üst sınır.")]
    public float maxCleanThreshold = 0.98f;

    [Tooltip("Fırça boyutu için alt sınır.")]
    public float minBrushSize = 0.15f;

    [Header("Level Bittiğinde")]
    [Tooltip("Level bitince kaç saniye beklenecek (yıldızları görmek için)")]
    public float delayBeforeNextLevel = 2f;

    float elapsed = 0f;
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

        elapsed = 0f;
        running = true;
        UpdateUI();

        if (starsImage) starsImage.enabled = false;
    }

    void Update()
    {
        if (!running || finished) return;

        elapsed += Time.deltaTime;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (timerText)
        {
            int sec = Mathf.FloorToInt(elapsed);
            timerText.text = sec + "s";
        }

        if (timerSlider)
        {
            float maxForSlider = timeFor2Stars;
            if (maxForSlider <= 0f) maxForSlider = 1f;
            timerSlider.value = Mathf.Clamp01(elapsed / maxForSlider);
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
        // Timer'ı sıfırla
        elapsed = 0f;
        running = true;
        finished = false;
        
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
        
        Debug.Log("[LevelTimer] Yeni level için sıfırlandı.");
    }

    int CalculateStars()
    {
        if (elapsed <= timeFor3Stars) return 3;
        if (elapsed <= timeFor2Stars) return 2;
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
            float sizeFactor = Mathf.Pow(brushSizeFactorPerStep, step);
            dirt.brushSize = Mathf.Max(minBrushSize, dirt.brushSize * sizeFactor);
        }
    }

    // Şimdilik: level bitince açılan butondan çağırılacak, aynı leveli yeniden yükler
    public void RestartLevel()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}