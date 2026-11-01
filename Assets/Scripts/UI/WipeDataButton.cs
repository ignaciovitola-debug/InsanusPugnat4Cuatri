using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Boton "Borrar datos" con confirmacion: el primer toque lo arma, el segundo (dentro de unos segundos) borra.
    [RequireComponent(typeof(Button))]
    public class WipeDataButton : MonoBehaviour
    {
        private const string IdleText = "Borrar datos";
        private const string ArmedText = "¿Seguro? Tocá de nuevo";

        [SerializeField] private MenuController menu;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float confirmSeconds = 3f;

        private float armedUntil;

        private void Awake() => GetComponent<Button>().onClick.AddListener(OnClick);

        private void OnEnable() => Disarm();

        private void Update()
        {
            if (armedUntil > 0f && Time.unscaledTime > armedUntil)
                Disarm();
        }

        private void OnClick()
        {
            if (armedUntil > 0f)
            {
                menu.WipeSaveData();
                return;
            }

            armedUntil = Time.unscaledTime + confirmSeconds;
            label.text = ArmedText;
        }

        private void Disarm()
        {
            armedUntil = 0f;
            if (label != null)
                label.text = IdleText;
        }
    }
}
