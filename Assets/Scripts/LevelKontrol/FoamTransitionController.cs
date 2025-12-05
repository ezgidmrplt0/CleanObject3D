using UnityEngine;
using DG.Tweening;

public class FoamTransitionController : MonoBehaviour
{
    public static FoamTransitionController Instance { get; private set; }

    [Header("Particle References")]
    [Tooltip("Ekraný köpükle kapatan ana patlama sistemi")]
    public ParticleSystem closeFoamSystem;

    [Tooltip("Ýstersen açýlýþta farklý bir efekt kullanmak için ikinci sistem")]
    public ParticleSystem openFoamSystem; // boþ býrakýlýrsa closeFoamSystem kullanýlýr

    [Header("Settings")]
    [Tooltip("Geçiþ süresi (FoamClose/FoamOpen için temel süre)")]
    public float defaultDuration = 2.5f;   // 2–3 sn dedin ya, buna yakýn tut

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (closeFoamSystem != null)
        {
            closeFoamSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (openFoamSystem != null)
        {
            openFoamSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    /// <summary>
    /// Ekraný köpükle kapatma (OUT transition)
    /// </summary>
    public Tween FoamClose(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;
        if (closeFoamSystem == null) return null;

        var main = closeFoamSystem.main;
        main.loop = false;
        main.duration = duration;
        main.useUnscaledTime = true;

        // Bubbles'ýn ölme süresini duration'a göre ayarla (isteðe baðlý)
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            duration * 0.6f,
            duration * 1.0f
        );

        closeFoamSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        closeFoamSystem.Play(true);

        // DOTween sequence sadece süreyi beklemek için
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(duration);
        seq.SetUpdate(true); // timescale'den baðýmsýz

        return seq;
    }

    /// <summary>
    /// Köpüklerin patlayýp yok olduðu, ekranýn açýldýðý kýsým (IN transition)
    /// </summary>
    public Tween FoamOpen(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;

        ParticleSystem ps = openFoamSystem != null ? openFoamSystem : closeFoamSystem;
        if (ps != null)
        {
            var main = ps.main;
            main.loop = false;
            main.duration = duration;
            main.useUnscaledTime = true;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(duration);
        seq.SetUpdate(true);

        return seq;
    }
}
