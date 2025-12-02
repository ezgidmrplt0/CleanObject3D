using UnityEngine;
using UnityEngine.EventSystems;

public class MovableInCleanMode : MonoBehaviour
{
    [Header("Hedef Hareket")]
    [Tooltip("Objenin gideceği hedef nokta (genelde child boş obje)")]
    public Transform targetPoint;
    [Tooltip("Hedefe giderken kullanılacak süre (sn)")]
    [Min(0f)] public float moveDuration = 0.3f;
    [Tooltip("Sadece kamera hareketi AKTİFKEN (controlsEnabled = true) çalışsın")]
    public bool onlyWhenCameraActive = true;

    Camera cam;
    CleanModeCamera cleanController;
    Vector3 originalPosition;
    bool atTarget = false;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cleanController) cleanController = FindObjectOfType<CleanModeCamera>();
        originalPosition = transform.position;
    }

    void Update()
    {
        // Kamera durumu kontrolü
        // onlyWhenCameraActive == true  ise: sadece controlsEnabled == true iken çalış
        // onlyWhenCameraActive == false ise: kamera durumuna bakmadan çalış
        if (onlyWhenCameraActive && cleanController && !cleanController.controlsEnabled)
            return;

        // PC & mobil için ortak: tek tıklama ile hedefe git / geri dön
        bool clicked = false;
        Vector2 screenPos = Vector2.zero;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
            clicked = true;
            screenPos = Input.mousePosition;
        }
        else if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(t.fingerId)) return;
                clicked = true;
                screenPos = t.position;
            }
        }

        if (!clicked) return;

        if (!cam) cam = Camera.main;
        if (!cam) return;

        // Raycast ile bu objeye tıklandı mı kontrol et
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                ToggleMove();
            }
        }
    }

    void ToggleMove()
    {
        if (targetPoint == null)
            return;

        Vector3 toPos = atTarget ? originalPosition : targetPoint.position;
        atTarget = !atTarget;

        if (moveDuration <= 0f)
        {
            transform.position = toPos;
        }
        else
        {
            // Basit Lerp benzeri: coroutine yerine burada doğrudan tween kullanmak istersen
            // DOTween ekliyse burayı DOTween ile değiştirebilirsin.
            StopAllCoroutines();
            StartCoroutine(MoveRoutine(toPos));
        }
    }

    System.Collections.IEnumerator MoveRoutine(Vector3 target)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, moveDuration);
            float k = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(start, target, k);
            yield return null;
        }
        transform.position = target;
    }
}
