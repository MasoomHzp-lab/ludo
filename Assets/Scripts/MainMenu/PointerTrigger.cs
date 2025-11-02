using RTLTMPro;
using UnityEngine;

public class PointerTrigger : MonoBehaviour
{
    [Tooltip("نقطه‌ای که برای تشخیص بخش زیر pointer چک می‌شود. معمولاً یک child کوچک در نوک فلش.")]
    public Transform pointerTip;

    [Tooltip("لِیِری که بخش‌های گردونه (segments) در آن قرار دارند تا تنها آن‌ها چک شوند.")]
    public LayerMask segmentLayer;

    [Tooltip("برای جلوگیری از تکرار، وقتی یک جایزه داده شد true می‌شود.")]
    public bool rewardGiven = false;
    public RTLTextMeshPro scoreText;

    // این متد را می‌خواهیم به Spinner.OnStopped وصل کنیم (در Inspector یا از طریق کد)
    public void HandleStop()
    {
        // اگر جایزه قبلاً داده شده، کاری نکن
        if (rewardGiven) return;

        // اگر pointerTip تنظیم نشده، از transform خود pointer استفاده کن
        Vector2 checkPos = (pointerTip != null) ? (Vector2)pointerTip.position : (Vector2)transform.position;

        // OverlapPoint دقیق‌تره برای موقعیت ثابت pointer
        Collider2D hit = Physics2D.OverlapPoint(checkPos, segmentLayer);
        if (hit != null)
        {
            Debug.Log("Pointer detected segment: " + hit.gameObject.name);

            // بررسی تَگ یا هر کامپوننت دیگری که برای تعیین نوع جایزه استفاده می‌کنی
            if (hit.CompareTag("coin"))
            {
                GiveCoin(hit.gameObject);
            }
            else if (hit.CompareTag("gift"))
            {
                GiveGift(hit.gameObject);
            }
            else if (hit.CompareTag("not"))
            {
                HandleNotReward(hit.gameObject);
            }
            else
            {
                Debug.Log("Unknown segment tag: " + hit.tag);
            }

            // بعد از دادن جایزه، جلوگیری از اجرای مجدد تا ریست نشود
            rewardGiven = true;
        }
        else
        {
            Debug.Log("Pointer didn't overlap any segment. checkPos: " + checkPos);
        }
    }

    void GiveCoin(GameObject segment)
    {
        Debug.Log("You got a coin from: " + segment.name);
        scoreText.text =segment.name+ "سکه گرفتی" ;
    }

    void GiveGift(GameObject segment)
    {
        Debug.Log("You got a gift from: " + segment.name);
        scoreText.text = " یه هدیه گرفتی"  ;
    }

    void HandleNotReward(GameObject segment)
    {
        Debug.Log("No reward segment: " + segment.name);
        scoreText.text = " پوچ بود";
    }

    public void ResetReward()
    {
        rewardGiven = false;
    }
}

