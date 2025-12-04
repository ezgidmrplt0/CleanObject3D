using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // 0 = default fýrça, 1 = marketten aldýðýn fýrça, 2,3... vs.
    public int currentBrushId = 0;

    // Fýrça þu an elde mi? (butonla aç/kapa için)
    public bool brushEquipped = false;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);  // Tüm sahnelerde kalsýn

        // --- EK: PlayerPrefs'ten mevcut fýrça bilgilerini çek ---
        currentBrushId = PlayerPrefs.GetInt("CurrentBrushId", 0);
        brushEquipped = PlayerPrefs.GetInt("BrushEquipped", 0) == 1;
    }
}
