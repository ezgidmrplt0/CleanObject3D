using UnityEngine;

public class MusteriAI : MonoBehaviour
{
    public float speed = 3f;

    [HideInInspector] public MusteriManager manager;
    [HideInInspector] public Transform silinmePoint;
    [HideInInspector] public int queueIndex;

    bool leaving = false;
    Vector3 currentTarget;
    float fixedY;

    void Start()
    {
        // Müþterinin sabit yüksekliði
        fixedY = transform.position.y;
    }

    void Update()
    {
        if (manager == null) return;

        if (leaving)
        {
            // Silinme noktasýnýn Y'sini de sabitle
            Vector3 target = silinmePoint.position;
            target.y = fixedY;

            MoveTowards(target);

            // Mesafeyi de sabitlenmiþ hedefe göre ölç
            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                manager.NotifyCustomerDestroyed(this);
                Destroy(gameObject);
            }
        }
        else
        {
            MoveTowards(currentTarget);
        }
    }

    void MoveTowards(Vector3 hedef)
    {
        // Y ekseni sabit kalsýn
        hedef.y = fixedY;

        Vector3 newPos = Vector3.MoveTowards(
            transform.position,
            hedef,
            speed * Time.deltaTime
        );

        newPos.y = fixedY;
        transform.position = newPos;
    }

    public void UpdateTarget()
    {
        if (!leaving && manager != null)
        {
            currentTarget = manager.GetQueuePosition(queueIndex);
        }
    }

    public void StartLeaving()
    {
        leaving = true;
    }
}
