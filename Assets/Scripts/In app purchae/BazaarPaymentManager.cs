using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Bazaar.Poolakey;
using Bazaar.Poolakey.Data;

public class BazaarPaymentManager : MonoBehaviour
{
   public static BazaarPaymentManager Instance;
    
    [TextArea(3, 10)]
    public string applicationPublicKey;

    private Payment payment;

    private async void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await ConnectToBazaar();
    }

   private async Task ConnectToBazaar()
{
    try
    {
        var securityCheck = SecurityCheck.Enable(applicationPublicKey);
        var config = new PaymentConfiguration(securityCheck);

        payment = new Payment(config);

        var result = await payment.Connect();

        // فقط برای دیباگ
        Debug.Log($"[Poolakey] Connect result: {result.message}");

        // اگر به اینجا رسیده یعنی متد بدون خطای جدی اجرا شده
        Debug.Log("[Poolakey] Connected to Bazaar (no exception).");
    }
    catch (System.Exception e)
    {
        Debug.LogError("[Poolakey] Connect exception: " + e.Message);
    }
}


    // ---- خرید تم ----
    public async void BuyTheme(string productId, string themeId)
{
    if (payment == null)
    {
        Debug.LogError("[Poolakey] Payment is null. Maybe not connected yet?");
        return;
    }

    try
    {
        var result = await payment.Purchase(productId);

        // فقط برای لاگ
        Debug.Log($"[Poolakey] Purchase result: {result.message}");

        // اگر اینجا رسیده، یعنی متد اجرا شده
        Debug.Log("[Poolakey] Purchase finished, unlocking theme...");

        PlayerPrefs.SetInt("Theme_" + themeId, 1);
        PlayerPrefs.Save();
    }
    catch (System.Exception e)
    {
        Debug.LogError("[Poolakey] Purchase exception: " + e.Message);
    }
}

}
