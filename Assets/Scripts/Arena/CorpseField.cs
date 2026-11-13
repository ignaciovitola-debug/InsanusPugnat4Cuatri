using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GladiusAI
{
    // Dibuja en la arena los cuerpos de combates anteriores y deja un cuerpo nuevo cada vez que alguien cae.
    // Lo crea CombatStarter. El cuerpo es una copia del dibujo del gladiador (sprite + sombra) congelada en
    // el último cuadro de su animación de muerte, con un charco de sangre debajo. No tiene física ni lógica,
    // así que no cuesta casi nada.
    public class CorpseField
    {
        private static readonly int DeadParam = Animator.StringToHash("Dead");

        // Lo que tarda la animación de muerte (Dead dura 0,75 s) antes de dejar el cuerpo fijo.
        private const float DeathAnimationSeconds = 0.9f;

        private readonly string scene;
        private readonly GameObject template;   // prefab del gladiador, de ahí se copian sprite y sombra
        private readonly Transform root;
        private readonly Sprite[] puddles;
        private readonly Material bloodMaterial;
        private readonly RaycastHit[] hits = new RaycastHit[8];
        private readonly Dictionary<Vector3, GameObject> shown = new Dictionary<Vector3, GameObject>();

        public CorpseField(GladiatorNPC prefab)
        {
            scene = SceneManager.GetActiveScene().name;
            template = prefab != null ? prefab.gameObject : null;
            root = new GameObject("Cuerpos").transform;

            // Charcos de la hoja de sangre (los mismos que usa BloodField).
            var list = new List<Sprite>();
            foreach (var s in Resources.LoadAll<Sprite>("Effects/Sangre"))
                if (s.name.StartsWith("Charco")) list.Add(s);
            puddles = list.ToArray();
            bloodMaterial = Resources.Load<Material>("Effects/SangreMat");

            // Cuenta este combate: si ya van 3 en esta arena, se limpian los cuerpos antes de dibujarlos.
            CorpseRegistry.RegisterCombat(scene);
            foreach (var record in CorpseRegistry.InScene(scene))
                Show(record);
        }

        // Llamar desde una corrutina del dueño cuando un gladiador cae.
        public IEnumerator OnGladiatorDied(GladiatorNPC fallen)
        {
            if (fallen == null || template == null) yield break;

            var sprite = fallen.CharacterSprite;
            Vector3 spot = CorpseRegistry.FindFreeSpot(scene, fallen.transform.position);
            var record = new CorpseRecord
            {
                scene = scene,
                x = spot.x, y = spot.y, z = spot.z,
                skin = fallen.SkinIndex,
                flipX = sprite != null && sprite.flipX,
                puddleRotation = Random.Range(0f, 360f),
            };

            // Se anota ya, así si cae otro enseguida no elige el mismo lugar.
            var removed = CorpseRegistry.Add(record);
            if (removed.HasValue) Hide(removed.Value);

            yield return new WaitForSeconds(DeathAnimationSeconds);

            // Termina la animación: aparece el cuerpo fijo y se esconde el gladiador (si todavía es el mismo muerto:
            // un enemigo del pool podría haberse reusado ya para el próximo rival).
            Show(record);
            if (fallen != null && fallen.IsDead && fallen.gameObject.activeInHierarchy)
                fallen.SetBodyVisible(false);
        }

        private void Show(CorpseRecord record)
        {
            if (template == null) return;

            var position = new Vector3(record.x, record.y, record.z);
            if (shown.ContainsKey(position)) return;

            var corpse = new GameObject("Cuerpo");
            corpse.transform.SetParent(root, false);
            corpse.transform.SetPositionAndRotation(position, template.transform.rotation);
            corpse.transform.localScale = template.transform.localScale;

            // Copia los dibujos del prefab (el personaje animado y la sombra) con su misma ubicación.
            foreach (var source in template.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (source.gameObject == template) continue;
                var copy = Object.Instantiate(source.gameObject, corpse.transform, false);
                copy.name = source.name;
                copy.transform.localPosition = source.transform.localPosition;
                copy.transform.localRotation = source.transform.localRotation;
                copy.transform.localScale = source.transform.localScale;

                // Solo se quiere el dibujo: cualquier otro componente que traiga el hijo se apaga.
                foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
                    behaviour.enabled = false;
                foreach (var collider in copy.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;

                var animator = copy.GetComponent<Animator>();
                if (animator != null)
                    FreezeOnDeathPose(animator, copy.GetComponent<SpriteRenderer>(), record);
            }

            AddPuddle(corpse.transform, position, record.puddleRotation);
            shown[position] = corpse;
        }

        // Charco de sangre debajo del cuerpo, apoyado en el piso. Se va junto con el cuerpo.
        private void AddPuddle(Transform corpse, Vector3 position, float rotation)
        {
            if (puddles.Length == 0) return;

            var go = new GameObject("Charco");
            go.transform.SetParent(corpse, true);
            go.transform.SetPositionAndRotation(new Vector3(position.x, FloorHeight(position) + 0.012f, position.z),
                Quaternion.Euler(90f, rotation, 0f));
            go.transform.localScale = Vector3.one * 1.7f / Mathf.Max(0.01f, corpse.lossyScale.x);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = puddles[Mathf.Abs((int)rotation) % puddles.Length];
            if (bloodMaterial != null) sr.sharedMaterial = bloodMaterial;
            sr.sortingOrder = -9;   // debajo del cuerpo y de las sombras, apenas encima de la sangre suelta
            sr.color = new Color(0.7f, 0.7f, 0.7f, 0.95f);
        }

        // Pone el aspecto, salta la animación de muerte hasta el final y apaga el Animator: queda el último cuadro.
        private static void FreezeOnDeathPose(Animator animator, SpriteRenderer sprite, CorpseRecord record)
        {
            var controller = GladiatorSkins.Get(record.skin);
            if (controller != null)
                animator.runtimeAnimatorController = controller;

            animator.Rebind();
            animator.SetBool(DeadParam, true);
            for (int i = 0; i < 4; i++)
                animator.Update(0.5f);   // entra a Dead y la recorre entera (dura 0,75 s)
            animator.enabled = false;

            if (sprite != null)
                sprite.flipX = record.flipX;
        }

        private void Hide(CorpseRecord record)
        {
            var position = new Vector3(record.x, record.y, record.z);
            if (shown.TryGetValue(position, out var corpse))
            {
                if (corpse != null) Object.Destroy(corpse);
                shown.Remove(position);
            }
        }

        // Altura del piso debajo del cuerpo (se ignoran los gladiadores).
        private float FloorHeight(Vector3 from)
        {
            int count = Physics.RaycastNonAlloc(from + Vector3.up, Vector3.down, hits, 10f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue, floor = from.y - 0.45f;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.GetComponentInParent<GladiatorNPC>() != null) continue;
                if (hits[i].distance < best) { best = hits[i].distance; floor = hits[i].point.y; }
            }
            return floor;
        }
    }
}
