using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GladiusAI
{
    /// <summary>Volumen general y calidad gráfica del panel de Opciones. Persiste con PlayerPrefs.</summary>
    public class OptionsController : MonoBehaviour
    {
        private const string VolumeKey = "MasterVolume";
        private const string QualityKey = "QualityLevel";

        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI qualityLabel;

        private int qualityIndex;

        /// <summary>Usado por la herramienta de Editor al armar el panel — asigna las referencias una sola vez.</summary>
        public void Configure(Slider slider, TextMeshProUGUI label)
        {
            volumeSlider = slider;
            qualityLabel = label;
        }

        private void Awake()
        {
            float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);

            AudioListener.volume = savedVolume;
            QualitySettings.SetQualityLevel(qualityIndex);

            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(savedVolume);
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }

            RefreshQualityLabel();
        }

        public void SetVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(VolumeKey, value);
        }

        public void NextQuality() => ChangeQuality(1);
        public void PreviousQuality() => ChangeQuality(-1);

        private void ChangeQuality(int direction)
        {
            int count = QualitySettings.names.Length;
            qualityIndex = (qualityIndex + direction + count) % count;

            QualitySettings.SetQualityLevel(qualityIndex);
            PlayerPrefs.SetInt(QualityKey, qualityIndex);
            RefreshQualityLabel();
        }

        private void RefreshQualityLabel()
        {
            if (qualityLabel != null)
                qualityLabel.text = QualitySettings.names[qualityIndex];
        }
    }
}
