using UnityEngine;
using DG.Tweening;

public class FoamTransitionController : MonoBehaviour
{
    public static FoamTransitionController Instance { get; private set; }

    [Header("Particle References")]
    [Tooltip("Ekran� k�p�kle kapatan ana patlama sistemi")]
    public ParticleSystem closeFoamSystem;

    [Tooltip("�stersen a��l��ta farkl� bir efekt kullanmak i�in ikinci sistem")]
    public ParticleSystem openFoamSystem; // bo� b�rak�l�rsa closeFoamSystem kullan�l�r

    [Header("Settings")]
    [Tooltip("Ge�i� s�resi (FoamClose/FoamOpen i�in temel s�re)")]
    public float defaultDuration = 2.5f;   // 2�3 sn dedin ya, buna yak�n tut

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Kameraya sabitleme
        if (Camera.main != null)
        {
            transform.SetParent(Camera.main.transform);
            transform.localPosition = new Vector3(0, 0, 1f); // Kameranın 1 birim önünde
            transform.localRotation = Quaternion.identity;
        }

        if (closeFoamSystem != null)
        {
            closeFoamSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (openFoamSystem != null)
        {
            openFoamSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void LateUpdate()
    {
        // Her karede kameranın önüne sabitle
        if (Camera.main != null)
        {
            transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1f; // 1 birim önünde
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    /// <summary>
    /// Ekranı köpükle kapatma (OUT transition)
    /// </summary>
    public Tween FoamClose(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;
        if (closeFoamSystem == null) return null;

        var main = closeFoamSystem.main;
        main.loop = false;
        main.duration = duration;
        main.useUnscaledTime = true;

        // Bubbles'�n �lme s�resini duration'a g�re ayarla (iste�e ba�l�)
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            duration * 0.6f,
            duration * 1.0f
        );

        closeFoamSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        closeFoamSystem.Play(true);

        // DOTween sequence sadece s�reyi beklemek i�in
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(duration);
        seq.SetUpdate(true); // timescale'den ba��ms�z

        return seq;
    }

    /// <summary>
    /// K�p�klerin patlay�p yok oldu�u, ekran�n a��ld��� k�s�m (IN transition)
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
