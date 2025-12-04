using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FoamTransitionController : MonoBehaviour
{
    public static FoamTransitionController Instance { get; private set; }

    [Header("References")]
    public Image foamImage;

    [Header("Settings")]
    public float defaultDuration = 0.7f;
    public float maxScale = 2.5f;   // köpüðün ne kadar büyüyeceði

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (foamImage == null)
            foamImage = GetComponentInChildren<Image>();

        if (foamImage == null)
        {
            Debug.LogError("[FoamTransitionController] foamImage atanmadý!");
            return;
        }

        // Baþlangýçta görünmez ve küçük
        var c = foamImage.color;
        c.a = 0f;
        foamImage.color = c;

        foamImage.rectTransform.localScale = Vector3.zero;
    }

    /// <summary>
    /// Ekraný köpükle kapatma (transition OUT gibi düþünebilirsin)
    /// </summary>
    public Tween FoamClose(float duration = -1f)
    {
        if (foamImage == null) return null;
        if (duration <= 0f) duration = defaultDuration;

        RectTransform rt = foamImage.rectTransform;

        // Her seferinde baþlangýç durumuna çek
        rt.localScale = Vector3.zero;
        var c = foamImage.color;
        c.a = 0f;
        foamImage.color = c;

        Sequence seq = DOTween.Sequence();

        // 1) Alfa açýlýrken köpük büyüsün
        seq.Append(foamImage.DOFade(1f, duration * 0.4f));
        seq.Join(rt.DOScale(maxScale, duration)
            .SetEase(Ease.OutQuad));

        // TimeScale=0 olsa bile çalýþsýn
        seq.SetUpdate(true);

        return seq;
    }

    /// <summary>
    /// Ekrandaki köpüðü geri çekme (transition IN gibi)
    /// </summary>
    public Tween FoamOpen(float duration = -1f)
    {
        if (foamImage == null) return null;
        if (duration <= 0f) duration = defaultDuration;

        RectTransform rt = foamImage.rectTransform;

        // Close bittiðinde zaten maxScale + alpha=1 olacak
        Sequence seq = DOTween.Sequence();

        // Biraz daha büyütüp silinebilir, ya da direkt küçültebilirsin.
        seq.Append(rt.DOScale(maxScale * 1.1f, duration * 0.3f));

        // Sonra alfa kapat + scale küçült
        seq.Append(foamImage.DOFade(0f, duration * 0.7f));
        seq.Join(rt.DOScale(0f, duration * 0.7f)
            .SetEase(Ease.InQuad));

        seq.SetUpdate(true);

        return seq;
    }
}
