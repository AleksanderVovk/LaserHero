using TMPro;
using UnityEngine;

namespace LaserHero
{
    public class LaserCurrencyOffer : MonoBehaviour
    {
        public LaserIAPStore store;
        public LaserStoreKind kind;
        public int index;
        public bool featured;
        public TMP_Text amountText, priceText;
        public UnityEngine.UI.Button purchaseButton;
        public void Buy()
        {
            if (store != null)
                store.Buy(kind, index);
        }

        void OnEnable()
        {
            Refresh();
        }

        void Update()
        {
            Refresh();
        }

        void Refresh()
        {
            if (store == null || store.catalog == null)
                return;
            var item = store.catalog.Find(kind, index);
            if (item == null)
                return;
            string amount = "+" + LaserProgress.Format(LaserAmount.Parse(item.grantAmount)) + (featured
                ? (kind == LaserStoreKind.Money ? " ДЕНЕГ" : " энергии") : "");
            if (amountText.text != amount)
                amountText.text = amount;
            string price = store.Price(kind, index);
            if (priceText.text != price)
                priceText.text = price;
            purchaseButton.interactable = store.CanBuy(kind, index);
        }
    }
}
