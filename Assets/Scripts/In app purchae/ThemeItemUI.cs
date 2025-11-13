using UnityEngine;
using UnityEngine.UI;

public class ThemeItemUI : MonoBehaviour
{
    public string themeId;     // مثلا Forest — Galaxy — Sea
    public string productId;   // مثلا theme_forest (از بازار)
    public Button buyButton;

    private void Start()
    {
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        BazaarPaymentManager.Instance.BuyTheme(productId, themeId);
    }
}
