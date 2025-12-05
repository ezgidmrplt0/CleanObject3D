using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameStartController : MonoBehaviour
{
    [Header("UI Elemanları")]
    [Tooltip("Play butonu")]
    public Button playButton;
    
    [Tooltip("Play butonunun bulunduğu panel (başlangıç ekranı)")]
    public GameObject startPanel;
    
    [Tooltip("Güncel level'i gösteren text")]
    public TextMeshProUGUI currentLevelText;
    
    [Header("Level Ayarları")]
    [Tooltip("PlayerPrefs'te level bilgisinin saklandığı key")]
    public string playerPrefsLevelKey = "CurrentLevel";
    
    [Header("Geçiş Ayarları")]
    [Tooltip("Köpük geçiş süresi")]
    public float transitionDuration = 0.7f;
    
    [Header("Oyun Başladığında Aktif Olacaklar")]
    [Tooltip("Oyun başladığında aktif edilecek UI elemanları (timer, brush button vs.)")]
    public GameObject[] gameUIElements;
    
    [Header("Bağlantılar")]
    public DirtCleaner dirtCleaner;
    public LevelTimer levelTimer;
    
    bool gameStarted = false;
    
    void Start()
    {
        // Oyun başlangıcında UI'ları gizle
        SetGameUIActive(false);
        
        // Start panelini göster
        if (startPanel != null)
            startPanel.SetActive(true);
        
        // Güncel level'i göster
        UpdateCurrentLevelText();
        
        // Play butonuna listener ekle
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        
        // DirtCleaner'ı başlangıçta pasif yap
        if (dirtCleaner != null)
            dirtCleaner.enabled = false;
        
        // LevelTimer'ı başlangıçta pasif yap
        if (levelTimer != null)
            levelTimer.enabled = false;
    }
    
    void UpdateCurrentLevelText()
    {
        if (currentLevelText != null)
        {
            int level = PlayerPrefs.GetInt(playerPrefsLevelKey, 1);
            currentLevelText.text = "Current Level: Level " + level;
        }
    }
    
    void OnPlayButtonClicked()
    {
        if (gameStarted) return;
        gameStarted = true;
        
        Debug.Log("[GameStartController] Play butonuna basıldı!");
        
        // Butonu devre dışı bırak
        if (playButton != null)
            playButton.interactable = false;
        
        // Köpük efekti ile geçiş yap
        StartGameWithTransition();
    }
    
    void StartGameWithTransition()
    {
        // FoamTransitionController varsa köpük efekti kullan
        if (FoamTransitionController.Instance != null)
        {
            Sequence seq = DOTween.Sequence();
            
            // 1) Ekranı köpükle kapat
            seq.Append(FoamTransitionController.Instance.FoamClose(transitionDuration));
            
            // 2) UI değişikliklerini yap
            seq.AppendCallback(() =>
            {
                // Start panelini gizle
                if (startPanel != null)
                    startPanel.SetActive(false);
                
                // Oyun UI'larını göster
                SetGameUIActive(true);
                
                // Oyun sistemlerini aktif et
                EnableGameSystems();
            });
            
            // 3) Köpüğü aç
            seq.Append(FoamTransitionController.Instance.FoamOpen(transitionDuration));
            
            seq.OnComplete(() =>
            {
                Debug.Log("[GameStartController] Oyun başladı!");
            });
        }
        else
        {
            // FoamTransitionController yoksa direkt başlat
            Debug.LogWarning("[GameStartController] FoamTransitionController bulunamadı, direkt başlatılıyor.");
            
            if (startPanel != null)
                startPanel.SetActive(false);
            
            SetGameUIActive(true);
            EnableGameSystems();
        }
    }
    
    void SetGameUIActive(bool active)
    {
        foreach (var uiElement in gameUIElements)
        {
            if (uiElement != null)
                uiElement.SetActive(active);
        }
    }
    
    void EnableGameSystems()
    {
        // DirtCleaner'ı aktif et
        if (dirtCleaner == null)
            dirtCleaner = FindObjectOfType<DirtCleaner>();
        
        if (dirtCleaner != null)
        {
            dirtCleaner.enabled = true;
            dirtCleaner.FindAllDirts(); // Kirleri bul
        }
        
        // LevelTimer'ı aktif et
        if (levelTimer == null)
            levelTimer = FindObjectOfType<LevelTimer>();
        
        if (levelTimer != null)
        {
            levelTimer.enabled = true;
        }
    }
    
    /// <summary>
    /// Oyunu yeniden başlatmak için (opsiyonel)
    /// </summary>
    public void ResetToStartScreen()
    {
        gameStarted = false;
        
        SetGameUIActive(false);
        
        if (startPanel != null)
            startPanel.SetActive(true);
        
        if (playButton != null)
            playButton.interactable = true;
        
        if (dirtCleaner != null)
            dirtCleaner.enabled = false;
        
        if (levelTimer != null)
            levelTimer.enabled = false;
    }
}
