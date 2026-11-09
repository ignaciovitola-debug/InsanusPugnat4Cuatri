using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace GladiusAI
{
    // Mancha el piso de la arena con cada golpe que saca vida, y al entrar dibuja la sangre de combates anteriores.
    // Lo crea CombatStarter. Cada mancha es un sprite plano apoyado en el piso: sin física ni lógica, y todas
    // comparten material y textura, así Unity las dibuja juntas (barato en el celular).
    public class BloodField : IDisposable
    {
        private const string SpritesPath = "Effects/Sangre";
        private const string MaterialPath = "Effects/SangreMat";
        private const int SortingOrder = -10;          // debajo de las sombras (-1) y de los gladiadores (0)
        private const float AboveFloor = 0.01f;        // un pelito arriba del piso para que no parpadee

        private readonly string scene;
        private readonly Transform root;
        private readonly Material material;
        private readonly Sprite[] sprites;              // ordenados por nombre: el índice se guarda en el archivo
        private readonly List<int> splashes = new List<int>();
        private readonly List<int> puddles = new List<int>();
        private readonly List<int> drops = new List<int>();
        private readonly Dictionary<BloodStain, GameObject> shown = new Dictionary<BloodStain, GameObject>();
        private readonly RaycastHit[] hits = new RaycastHit[8];
        private int drawn;

        public BloodField()
        {
            scene = SceneManager.GetActiveScene().name;
            root = new GameObject("Sangre").transform;
            material = Resources.Load<Material>(MaterialPath);
            sprites = Resources.LoadAll<Sprite>(SpritesPath);
            Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));

            for (int i = 0; i < sprites.Length; i++)
            {
                string name = sprites[i].name;
                if (name.StartsWith("Salpicon")) splashes.Add(i);
                else if (name.StartsWith("Charco")) puddles.Add(i);
                else drops.Add(i);
            }

            foreach (var stain in BloodRegistry.InScene(scene))
                Show(stain);

            EventManager.Subscribe<GladiatorHitEvent>(OnHit);
        }

        public void Dispose() => EventManager.Unsubscribe<GladiatorHitEvent>(OnHit);

        // Golpe: un salpicón que sale para el lado contrario al atacante y unas gotas más lejos.
        // Golpe final: además, un charco debajo del cuerpo.
        private void OnHit(GladiatorHitEvent e)
        {
            if (e.victim == null || sprites.Length == 0) return;

            Vector3 victim = e.victim.transform.position;
            Vector3 away = victim - e.attackerPosition;
            away.y = 0f;
            away = away.sqrMagnitude > 0.0001f ? away.normalized : Random.insideUnitSphere.WithY(0f).normalized;
            float floorY = FloorHeight(victim);

            Add(splashes, victim + away * Random.Range(0.15f, 0.6f) + Jitter(0.2f), floorY, Random.Range(0.8f, 1.3f));

            int dropCount = Random.Range(0, 3);   // 0 a 2 gotas: unas 2 manchas por golpe en promedio
            for (int i = 0; i < dropCount; i++)
                Add(drops, victim + away * Random.Range(0.6f, 1.4f) + Jitter(0.45f), floorY, Random.Range(1f, 1.6f));

            if (e.killingBlow)
                Add(puddles, victim + Jitter(0.15f), floorY, Random.Range(1.2f, 1.6f));
        }

        private void Add(List<int> group, Vector3 position, float floorY, float scale)
        {
            if (group.Count == 0) return;

            var stain = new BloodStain
            {
                scene = scene,
                x = position.x,
                y = floorY,
                z = position.z,
                sprite = group[Random.Range(0, group.Count)],
                rotation = Random.Range(0f, 360f),
                scale = scale,
            };

            var removed = BloodRegistry.Add(stain);
            if (removed.HasValue) Hide(removed.Value);
            Show(stain);
        }

        private void Show(BloodStain stain)
        {
            if (stain.sprite < 0 || stain.sprite >= sprites.Length || shown.ContainsKey(stain)) return;

            var go = new GameObject("Mancha");
            var t = go.transform;
            t.SetParent(root, false);
            // Cada mancha nueva queda apenas más arriba que la anterior: si se pisan, no parpadean.
            float lift = AboveFloor + (drawn++ % 50) * 0.0004f;
            t.SetPositionAndRotation(new Vector3(stain.x, stain.y + lift, stain.z), Quaternion.Euler(90f, stain.rotation, 0f));
            t.localScale = Vector3.one * stain.scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[stain.sprite];
            if (material != null) sr.sharedMaterial = material;
            sr.sortingOrder = SortingOrder;
            // La sangre vieja y la nueva no son del mismo rojo: un poco de variación la hace más creíble.
            float shade = 0.65f + (Mathf.Abs(stain.rotation) % 30f) / 100f;
            sr.color = new Color(shade, shade, shade, 0.92f);

            shown[stain] = go;
        }

        private void Hide(BloodStain stain)
        {
            if (!shown.TryGetValue(stain, out var go)) return;
            if (go != null) Object.Destroy(go);
            shown.Remove(stain);
        }

        // Altura del piso debajo del gladiador (se ignoran los propios gladiadores).
        private float FloorHeight(Vector3 from)
        {
            int count = Physics.RaycastNonAlloc(from + Vector3.up, Vector3.down, hits, 10f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            float floor = from.y - 0.45f;   // por si no hay piso con collider
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.GetComponentInParent<GladiatorNPC>() != null) continue;
                if (hits[i].distance < best) { best = hits[i].distance; floor = hits[i].point.y; }
            }
            return floor;
        }

        private static Vector3 Jitter(float radius)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            return new Vector3(r.x, 0f, r.y);
        }
    }

    internal static class Vector3Extensions
    {
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    }
}
