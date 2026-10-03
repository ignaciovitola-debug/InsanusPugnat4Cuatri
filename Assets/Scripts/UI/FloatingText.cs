using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GladiusAI
{
    /// Cartel flotante sobre un gladiador ("¡En guardia!", "¡A la carga!"...) que sube y se desvanece.
    /// Uso: FloatingText.Spawn(posicion, "texto", color). No hace falta armar nada en la escena.
    ///
    /// Optimización para mobile (object pooling): en vez de crear y destruir un objeto por cada cartel,
    /// cuando un cartel termina se desactiva y se guarda en un "pool". El próximo Spawn lo reutiliza.
    /// Crear/destruir objetos en cada orden genera basura en memoria, y cuando el recolector de basura
    /// (GC) la limpia el juego se traba unos milisegundos: en un celular eso se nota como un tirón.
    public class FloatingText : MonoBehaviour
    {
        private const float Lifetime = 1.2f;       // segundos que dura en pantalla
        private const float RiseSpeed = 1.2f;      // unidades por segundo que sube
        private const float FontSize = 7f;
        private const float StackSpacing = 0.9f;   // separación vertical entre carteles apilados
        private const float StackRadius = 1.5f;    // a esta distancia se considera "el mismo gladiador"

        // Carteles visibles ahora (para apilarlos) y carteles apagados listos para reutilizar.
        private static readonly List<FloatingText> Active = new List<FloatingText>();
        private static readonly Stack<FloatingText> Pool = new Stack<FloatingText>();

        // Un único material con contorno negro, compartido por todos los carteles. Si cambiáramos
        // el contorno en cada TextMeshPro, TMP crearía una copia del material por cartel.
        private static Material outlinedMaterial;

        private TextMeshPro label;
        private Color baseColor;
        private float age;

        /// Muestra un cartel en esa posición (reutilizando uno del pool si hay).
        public static void Spawn(Vector3 position, string message, Color color)
        {
            FloatingText floating = TakeFromPool();
            if (floating == null)
                floating = Create();

            floating.Show(position, message, color);
        }

        // Los static sobreviven entre sesiones de Play si "Enter Play Mode" no recarga el dominio:
        // los limpiamos al arrancar para no arrastrar referencias viejas.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
            Pool.Clear();
            outlinedMaterial = null;
        }

        /// Saca un cartel del pool. Al cambiar de escena los carteles guardados se destruyen junto con
        /// ella, así que los que ya no existen (Unity los compara como null) se descartan.
        private static FloatingText TakeFromPool()
        {
            while (Pool.Count > 0)
            {
                FloatingText pooled = Pool.Pop();
                if (pooled != null)
                    return pooled;
            }
            return null;
        }

        /// Crea un cartel nuevo con todo lo que no cambia entre usos (fuente, tamaño, material).
        private static FloatingText Create()
        {
            var go = new GameObject("FloatingText");

            var label = go.AddComponent<TextMeshPro>();
            label.fontSize = FontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSharedMaterial = GetOutlinedMaterial(label.fontSharedMaterial);
            label.GetComponent<MeshRenderer>().sortingOrder = 100; // por delante de los sprites

            var floating = go.AddComponent<FloatingText>();
            floating.label = label;
            return floating;
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

        /// Si ya hay un cartel sobre el mismo gladiador, el nuevo se ubica más arriba en vez de encimarse.
        /// Comparamos solo la distancia horizontal (X/Z): la altura es justamente lo que vamos a cambiar.
        /// "self" se ignora: un cartel recién creado ya está en Active (Unity llama a OnEnable al crearlo).
        private static Vector3 StackAbove(Vector3 position, FloatingText self)
        {
            foreach (var other in Active)
            {
                if (other == self) continue;

                Vector3 otherPosition = other.transform.position;
                Vector3 offset = otherPosition - position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= StackRadius * StackRadius && otherPosition.y + StackSpacing > position.y)
                    position.y = otherPosition.y + StackSpacing;
            }
            return position;
        }

        private void Show(Vector3 position, string message, Color color)
        {
            transform.position = StackAbove(position, this);
            label.text = message;
            label.color = color;
            baseColor = color;
            age = 0f;
            gameObject.SetActive(true);
            FaceCamera();
        }

        // Active se mantiene solo: un cartel entra al activarse y sale al apagarse (o destruirse).
        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        // LateUpdate (y no Update) para orientarlo después de que la cámara ya se movió en este frame.
        private void LateUpdate()
        {
            age += Time.deltaTime;
            if (age >= Lifetime)
            {
                // En vez de Destroy: se apaga y vuelve al pool para el próximo Spawn.
                gameObject.SetActive(false);
                Pool.Push(this);
                return;
            }

            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);
            FaceCamera();

            // Totalmente visible la primera mitad de su vida; en la segunda mitad se desvanece.
            float alpha = Mathf.Clamp01((Lifetime - age) / (Lifetime * 0.5f));
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        /// Lo gira igual que la cámara para que el texto siempre se lea de frente (billboard).
        private void FaceCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                transform.rotation = mainCamera.transform.rotation;
        }
    }
}
