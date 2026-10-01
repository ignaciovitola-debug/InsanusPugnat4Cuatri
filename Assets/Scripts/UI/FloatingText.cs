using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GladiusAI
{
    /// <summary>
    /// Cartel que aparece sobre un gladiador ("¡En guardia!", "¡A la carga!"...), sube y se desvanece.
    /// Se crea solo con FloatingText.Spawn(...), no hace falta armar nada en la escena.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        private const float Lifetime = 1.2f;
        private const float RiseSpeed = 1.2f;
        private const float FontSize = 7f;
        private const float StackSpacing = 0.9f;
        private const float StackRadius = 1.5f;

        private static readonly List<FloatingText> Active = new List<FloatingText>();

        // Un solo material con contorno para todos los carteles: setear outlineWidth en cada
        // TextMeshPro creaba una copia del material por cartel que nunca se liberaba.
        private static Material outlinedMaterial;

        private TextMeshPro label;
        private Color baseColor;
        private float age;

        public static void Spawn(Vector3 position, string message, Color color)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = StackAbove(position);

            var label = go.AddComponent<TextMeshPro>();
            label.text = message;
            label.fontSize = FontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = color;
            label.fontSharedMaterial = GetOutlinedMaterial(label.fontSharedMaterial);
            label.GetComponent<MeshRenderer>().sortingOrder = 100;

            var floating = go.AddComponent<FloatingText>();
            floating.label = label;
            floating.baseColor = color;
            floating.FaceCamera();
        }

        private static Material GetOutlinedMaterial(Material fontMaterial)
        {
            if (outlinedMaterial != null) return outlinedMaterial;

            outlinedMaterial = new Material(fontMaterial) { name = "FloatingText Outline" };
            outlinedMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            outlinedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
            outlinedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            return outlinedMaterial;
        }

        /// <summary>Si ya hay un cartel sobre el mismo gladiador, el nuevo aparece arriba en vez de encimarse.</summary>
        private static Vector3 StackAbove(Vector3 position)
        {
            foreach (var other in Active)
            {
                Vector3 offset = other.transform.position - position;
                offset.y = 0f;
                if (offset.magnitude <= StackRadius && other.transform.position.y + StackSpacing > position.y)
                    position.y = other.transform.position.y + StackSpacing;
            }
            return position;
        }

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        private void LateUpdate()
        {
            age += Time.deltaTime;
            if (age >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * RiseSpeed * Time.deltaTime;
            FaceCamera();

            // Se mantiene opaco la primera mitad y despues se desvanece.
            float fade = Mathf.Clamp01((Lifetime - age) / (Lifetime * 0.5f));
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, fade);
        }

        private void FaceCamera()
        {
            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;
        }
    }
}
