using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Panel simple en pantalla (OnGUI, sin Canvas/UI) que muestra el estado
    /// de cada cazador y la cantidad de comida activa. Sirve para que la
    /// consigna se pueda ver funcionando en el build (los Gizmos no se ven
    /// fuera del Editor). Poner este script en cualquier GameObject de la escena.
    /// </summary>
    public class AIDebugHUD : MonoBehaviour
    {
        [SerializeField] private int fontSize = 16;

        private GUIStyle style;

        private void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label) { fontSize = fontSize, normal = { textColor = Color.white } };

            float y = 10f;
            GUI.Label(new Rect(10, y, 500, 30), "== Insanus Pugnat: IA (TP1) ==", style);
            y += 24f;

            for (int i = 0; i < Hunter.All.Count; i++)
            {
                Hunter hunter = Hunter.All[i];
                GUI.Label(new Rect(10, y, 600, 24),
                    $"Cazador [{hunter.name}] - Estado: {hunter.StateName} - Energía: {hunter.EnergyRatio * 100f:F0}%",
                    style);
                y += 22f;
            }

            if (FoodManager.Instance != null)
            {
                GUI.Label(new Rect(10, y, 400, 24), $"Comida activa: {FoodManager.Instance.ActiveFoodCount}", style);
                y += 22f;
            }

            GUI.Label(new Rect(10, y, 400, 24), $"Boids vivos: {Boid.All.Count}", style);
        }
    }
}
