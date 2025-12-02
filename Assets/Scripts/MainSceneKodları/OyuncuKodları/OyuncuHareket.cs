using UnityEngine;

public class OyuncuHareket : MonoBehaviour
{
    public float speed = 5f;
    public float minDragDistance = 20f; // çok küçük kaymalarý yok say

    Vector2 touchStartPos;
    bool isDragging = false;

    void Update()
    {
        Vector3 move = Vector3.zero;

        // --- PC / KLAVYE (Editör + Standalone) ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            move = new Vector3(h, 0, v);
        }

        // --- DOKUNMATÝK / JOYSTICK BENZERÝ KONTROL ---
#if UNITY_EDITOR
        // Editörde test için mouse'u dokunma gibi kullan
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.mousePosition.x < Screen.width * 0.5f)
            {
                isDragging = true;
                touchStartPos = Input.mousePosition;
            }
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 current = Input.mousePosition;
            Vector2 delta = current - touchStartPos;

            if (delta.magnitude > minDragDistance)
            {
                delta.Normalize();
                move = new Vector3(delta.x, 0, delta.y);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
#else
        // Gerçek cihaz (Android / iOS)
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                // Ekranýn sol yarýsý joystick alaný
                if (t.position.x < Screen.width * 0.5f)
                {
                    isDragging = true;
                    touchStartPos = t.position;
                }
            }
            else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && isDragging)
            {
                Vector2 delta = t.position - touchStartPos;

                if (delta.magnitude > minDragDistance)
                {
                    delta.Normalize();
                    move = new Vector3(delta.x, 0, delta.y);
                }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }
#endif

        // Çok hýzlý gitmesin diye normalize et
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        transform.Translate(move * speed * Time.deltaTime, Space.World);
    }
}
