using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Level Objeleri")]
    public List<GameObject> levels = new List<GameObject>();

    [Header("UI")]
    public TMP_Text levelText;   // "LEVEL 1" "LEVEL 2" yazan TextMeshPro

    [Header("Geçiş Ayarları")]
    public float transitionDuration = 0.7f; // köpük geçiş süresi

    int currentIndex = 0;
    bool isTransitioning = false;

    void Start()
    {
        // Başlangıçta sadece ilk level açık olsun
        for (int i = 0; i < levels.Count; i++)
            levels[i].SetActive(i == 0);

        UpdateLevelText();
    }

    public void LoadNextLevel()
    {
        LoadLevel(currentIndex + 1);
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

        levels[currentIndex].SetActive(false);
        currentIndex = index;
        levels[currentIndex].SetActive(true);
        UpdateLevelText();
    }

    void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = "LEVEL " + (currentIndex + 1);
        }
    }
}
