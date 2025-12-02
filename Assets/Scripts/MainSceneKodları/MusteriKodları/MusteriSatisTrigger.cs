using UnityEngine;

public class MusteriSatisTrigger : MonoBehaviour
{
    public MusteriManager manager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Oyuncu"))
        {
            manager.SellFrontCustomer();
        }
    }
}
