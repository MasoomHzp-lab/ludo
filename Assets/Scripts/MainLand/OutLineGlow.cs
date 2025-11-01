using UnityEngine;

public class OutLineGlow : MonoBehaviour
{

    [Header("Glow Material")]
    public Material glowMaterial;   // متریال درخشش (Outline Shader)
    public Material GeneralMaterial;
    private Material instanceMat;   // نسخه‌ی اختصاصی برای هر مهره
    private SpriteRenderer sr;

    private bool glowing = false;   // آیا درخشش فعال است؟
    private float glowSpeed = 2f;   // سرعت پالس درخشش

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        if (glowMaterial != null)
        {
            // ✅ هر مهره باید متریال خودش را داشته باشد تا رنگ جداگانه بگیرد
            instanceMat = new Material(glowMaterial);
            

            // 🔹 تلاش برای یافتن Token و رنگ بازیکن
            Token token = GetComponent<Token>();
            if (token != null && token.owner != null)
            {
                Color playerColor = token.owner.playerColor; // از PlayerController گرفته می‌شود
                instanceMat.SetColor("_OutLineColor", playerColor);
            }
            else
            {
                // رنگ پیش‌فرض در صورتی که Token یا Owner موجود نباشد
                instanceMat.SetColor("_OutLineColor", Color.white);
            }

            // درخشش در ابتدا خاموش باشد
            Color c = instanceMat.GetColor("_OutLineColor");
            c.a = 0f;
            instanceMat.SetColor("_OutLineColor", c);
        }
    }

    void Update()
    {
        if (glowing && instanceMat != null)
        {
            // 🔥 انیمیشن پالس برای درخشش (روشن و خاموش شدن نرم)
            float intensity = 0.5f + Mathf.Sin(Time.time * glowSpeed) * 0.5f;
            Color baseColor = instanceMat.GetColor("_OutLineColor");
            baseColor.a = Mathf.Lerp(0.2f, 0.8f, intensity);
            sr.material = instanceMat;
            instanceMat.SetColor("_OutLineColor", baseColor);
        }
    }

    /// <summary>
    /// فعال یا غیرفعال کردن درخشش
    /// </summary>
    public void SetGlow(bool enable)
    {
        glowing = enable;

        if (!enable && instanceMat != null)
        {
            // خاموش کردن درخشش فوری
            Color c = instanceMat.GetColor("_OutLineColor");
            c.a = 0f;
             sr.material = GeneralMaterial;
            instanceMat.SetColor("_OutLineColor", c);
        }
    }
}

