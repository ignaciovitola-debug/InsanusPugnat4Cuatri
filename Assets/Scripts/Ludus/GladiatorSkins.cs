using UnityEngine;

namespace GladiusAI
{
    // Los distintos "aspectos" de gladiador (Gladiator 1-3, Amazon 1-3): cada uno es un controlador de animacion
    // con sus propios sprites. Se edita desde el Inspector en Assets/Resources/GladiatorSkins.
    [CreateAssetMenu(fileName = "GladiatorSkins", menuName = "Insanus Pugnat/Gladiator Skins")]
    public class GladiatorSkins : ScriptableObject
    {
        private const string ResourcePath = "GladiatorSkins";

        [SerializeField] private RuntimeAnimatorController[] controllers = new RuntimeAnimatorController[0];

        private static GladiatorSkins instance;

        private static GladiatorSkins Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<GladiatorSkins>(ResourcePath);
                return instance;
            }
        }

        public static int Count => Instance != null ? Instance.controllers.Length : 0;

        public static RuntimeAnimatorController Get(int index)
            => Instance != null && index >= 0 && index < Instance.controllers.Length ? Instance.controllers[index] : null;

        // Un aspecto al azar, evitando el indicado (para que los dos rivales no se vean iguales).
        public static int RandomIndex(int exclude = -1)
        {
            int count = Count;
            if (count <= 1) return 0;

            int index = Random.Range(0, count);
            if (index == exclude)
                index = (index + 1 + Random.Range(0, count - 1)) % count;
            return index;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;
    }
}
