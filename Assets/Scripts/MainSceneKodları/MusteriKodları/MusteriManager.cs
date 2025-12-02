using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusteriManager : MonoBehaviour
{
    [Header("Referanslar")]
    public GameObject musteriPrefab;   // Müþteri prefab
    public Transform spawnPoint;       // Müþterinin doðacaðý nokta
    public Transform kasaPoint;        // Müþterinin DURACAÐI kasa noktasý (MusteriKasa)
    public Transform silinmePoint;     // Yok olacaðý nokta

    [Header("Kuyruk Ayarlarý")]
    public float distanceBetweenCustomers = 1f; // müþteriler arasý mesafe
    public float spawnInterval = 2f;            // her kaç saniyede 1 müþteri

    [Tooltip("0 veya negatif olursa sýnýrsýz müþteri birikebilir")]
    public int maxInQueue = 0;  // 0 => limitsiz, >0 => en fazla þu kadar müþteri

    [Header("Spawn Y Offset (müþteri aþaðýdan doðuyorsa yükselt)")]
    public float spawnYOffset = 0.0f;

    [HideInInspector] public List<MusteriAI> customers = new List<MusteriAI>();

    void Start()
    {
        // Inspector'dan atamayý unutursan tag ile bul
        if (kasaPoint == null)
        {
            GameObject k = GameObject.FindGameObjectWithTag("MusteriKasa");
            if (k != null) kasaPoint = k.transform;
        }

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            bool queueFull = maxInQueue > 0 && customers.Count >= maxInQueue;

            if (!queueFull)
            {
                SpawnCustomer();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnCustomer()
    {
        Vector3 pos = spawnPoint.position + Vector3.up * spawnYOffset;

        GameObject go = Instantiate(
            musteriPrefab,
            pos,
            spawnPoint.rotation
        );

        MusteriAI ai = go.GetComponent<MusteriAI>();

        ai.manager = this;
        ai.queueIndex = customers.Count;
        ai.silinmePoint = silinmePoint;

        customers.Add(ai);
        ai.UpdateTarget();   // sýradaki pozisyona git
    }

    // Kuyruktaki index'e göre hedef pozisyonu verir
    public Vector3 GetQueuePosition(int index)
    {
        // 0. müþteri direkt kasanýn önünde dursun
        Vector3 basePos = kasaPoint.position;

        // Kuyruk kasanýn arkasýna doðru gitsin
        Vector3 backDir = -kasaPoint.forward;

        Vector3 pos = basePos + backDir * distanceBetweenCustomers * index;
        pos.y = basePos.y; // Y sabit
        return pos;
    }

    // Oyuncu satýþ alanýna girince çaðrýlýr
    public void SellFrontCustomer()
    {
        if (customers.Count == 0) return;

        // En öndeki müþteriyi kuyruktan HEMEN çýkar
        MusteriAI leavingCustomer = customers[0];
        customers.RemoveAt(0);

        // Arkadakileri öne kaydýr
        for (int i = 0; i < customers.Count; i++)
        {
            customers[i].queueIndex = i;
            customers[i].UpdateTarget();   // yeni hedefleri: 0 -> kasa, 1 -> arka, vs.
        }

        // Çýkan müþteri görsel olarak silinme noktasýna yürüsün
        leavingCustomer.StartLeaving();
    }

    // Artýk listeden önceden çýkardýðýmýz için burada büyük iþ yapmamýza gerek yok
    public void NotifyCustomerDestroyed(MusteriAI customer)
    {
        // Güvenlik için; eðer hala listede kalmýþsa çýkar
        int index = customers.IndexOf(customer);
        if (index != -1)
        {
            customers.RemoveAt(index);

            for (int i = index; i < customers.Count; i++)
            {
                customers[i].queueIndex = i;
                customers[i].UpdateTarget();
            }
        }
    }
}
