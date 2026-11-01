using TMPro;
using UnityEngine;

namespace GladiusAI
{
    // La tienda: arma una fila por cada ShopItem dentro del ScrollView y cobra con los denarios.
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private ShopItem[] items = new ShopItem[0];
        [SerializeField] private RectTransform content;
        [Tooltip("Fila de molde: queda apagada y se copia una vez por item.")]
        [SerializeField] private ShopRow rowTemplate;
        [SerializeField] private TMP_Text messageLabel;

        private void Start()
        {
            rowTemplate.gameObject.SetActive(false);

            foreach (var item in items)
            {
                if (item == null) continue;

                var row = Instantiate(rowTemplate, content);
                row.gameObject.SetActive(true);
                row.Setup(item, TryBuy);
            }
        }

        private void OnEnable() => ShowMessage(string.Empty);

        private void TryBuy(ShopItem item)
        {
            if (!item.CanBuy(out string reason))
            {
                ShowMessage(reason);
                return;
            }

            if (!Denarii.TrySpend(item.Price))
            {
                ShowMessage($"Te faltan {item.Price - Denarii.Balance} denarios");
                return;
            }

            item.Apply();
            SoundManager.Play(SoundId.CrowdCheer);
            ShowMessage($"¡Compraste {item.DisplayName}!");
        }

        private void ShowMessage(string text)
        {
            if (messageLabel != null)
                messageLabel.text = text;
        }
    }
}
