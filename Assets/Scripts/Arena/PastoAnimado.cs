using UnityEngine;

namespace GladiusAI
{
    // Mata de pasto que se mece con el viento: va pasando por sus cuadros (ida y vuelta) a su propio ritmo.
    // Cada mata arranca en un momento distinto y puede salir espejada, así un grupo de pastos no se mueve
    // todo al mismo tiempo ni se ve repetido.
    [RequireComponent(typeof(SpriteRenderer))]
    public class PastoAnimado : MonoBehaviour
    {
        [Tooltip("Cuadros del movimiento, en orden.")]
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [Tooltip("Cuadros por segundo.")]
        [SerializeField] private float fps = 5f;
        [Tooltip("Variación al azar de la velocidad entre matas (0,2 = ±20%).")]
        [Range(0f, 0.5f)] [SerializeField] private float speedVariation = 0.25f;
        [Tooltip("Si está activo, cada mata sale mirando para un lado al azar.")]
        [SerializeField] private bool randomFlip = true;
        [Tooltip("Desactivado = queda quieto en el primer cuadro.")]
        [SerializeField] private bool animate = true;

        private SpriteRenderer sr;
        private float speed;
        private float time;
        private int shown = -1;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            speed = fps * (1f + Random.Range(-speedVariation, speedVariation));
            time = Random.Range(0f, 10f);
            if (randomFlip) sr.flipX = Random.value < 0.5f;
            Show(0);
        }

        private void Update()
        {
            if (!animate || frames.Length < 2) return;

            // Ida y vuelta (0,1,2,3,2,1,0...) para que el vaivén sea suave y no salte del último al primero.
            time += Time.deltaTime * speed;
            int cycle = frames.Length * 2 - 2;
            int step = (int)time % cycle;
            Show(step < frames.Length ? step : cycle - step);
        }

        private void Show(int index)
        {
            if (index == shown || frames.Length == 0) return;
            shown = index;
            sr.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }
    }
}
