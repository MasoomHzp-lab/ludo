using Unity.VisualScripting;
using UnityEngine;

public class PieceAnimator : MonoBehaviour
{

    [Header("Pulse Settings")]
    public float pulseSpeed = 2f;       // سرعت پالس
    public float pulseAmount = 0.05f;   // مقدار تغییر اندازه (۵٪)


    private bool isActive = false;

    void Start()
    {

     
    }

    void Update()
    {
        if (isActive)
        {
            // --- پالس اندازه ---
            float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = Vector3.one* scale;

        }
        else
        {
            // بازگشت به حالت عادی
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 5f);

        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }
}

