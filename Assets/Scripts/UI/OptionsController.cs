using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Panel de opciones: volumen general, musica, efectos, brillo, vibracion, 60 FPS y calidad grafica.
    // Es el mismo panel (prefab PanelOpciones) en el Menu y en la pausa de los niveles.
    public class OptionsController : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Toggle vibrationToggle;
        [SerializeField] private Toggle fpsToggle;
        [SerializeField] private TextMeshProUGUI qualityLabel;

        private void Awake()
        {
            masterSlider.onValueChanged.AddListener(value => GameSettings.Master = value);
            musicSlider.onValueChanged.AddListener(value => GameSettings.Music = value);
            sfxSlider.onValueChanged.AddListener(value => GameSettings.Sfx = value);
            brightnessSlider.onValueChanged.AddListener(value => GameSettings.Brightness = value);
            vibrationToggle.onValueChanged.AddListener(value => GameSettings.Vibration = value);
            fpsToggle.onValueChanged.AddListener(value => GameSettings.Fps60 = value);

            brightnessSlider.minValue = GameSettings.MinBrightness;
            brightnessSlider.maxValue = 1f;
        }

        // Cada vez que se abre se leen los valores guardados (otro panel pudo haberlos cambiado).
        private void OnEnable()
        {
            masterSlider.SetValueWithoutNotify(GameSettings.Master);
            musicSlider.SetValueWithoutNotify(GameSettings.Music);
            sfxSlider.SetValueWithoutNotify(GameSettings.Sfx);
            brightnessSlider.SetValueWithoutNotify(GameSettings.Brightness);
            vibrationToggle.SetIsOnWithoutNotify(GameSettings.Vibration);
            fpsToggle.SetIsOnWithoutNotify(GameSettings.Fps60);
            RefreshQualityLabel();
        }

        public void NextQuality() => ChangeQuality(1);
        public void PreviousQuality() => ChangeQuality(-1);

        private void ChangeQuality(int direction)
        {
            int count = QualitySettings.names.Length;
            GameSettings.QualityIndex = (GameSettings.QualityIndex + direction + count) % count;
            RefreshQualityLabel();
        }

        private void RefreshQualityLabel()
        {
            if (qualityLabel != null)
                qualityLabel.text = QualitySettings.names[GameSettings.QualityIndex];
        }
    }
}
