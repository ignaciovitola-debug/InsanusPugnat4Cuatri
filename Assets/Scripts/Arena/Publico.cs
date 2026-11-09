using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    // Público de fondo: una multitud de personitas pixel art que se balancean, saltan y levantan los brazos.
    // No usa ningún sprite del proyecto: los dibujos se generan por código al arrancar (16 personas distintas,
    // cada una con brazos abajo y brazos arriba) en una sola textura chica, así todo el público comparte
    // material y Unity lo dibuja junto (barato en el celular).
    //
    // Se pone detrás de una empalizada o muro: la parte de abajo queda tapada y asoman cabezas y hombros.
    // Si hay combate, se entusiasma con cada golpe y festeja al terminar.
    public class Publico : MonoBehaviour
    {
        public enum Modo
        {
            Tribuna,   // en las arenas: miran el combate, saltan y festejan los golpes
            Pueblo,    // en la Ludus: pasan caminando y hacen lo suyo, sin darle importancia a nada
        }

        [Tooltip("Tribuna = festejan el combate. Pueblo = gente que pasa caminando.")]
        [SerializeField] private Modo modo = Modo.Tribuna;
        [Tooltip("Color que multiplica a toda la gente (más oscuro = más metida en el fondo).")]
        [SerializeField] private Color tinte = Color.white;

        [Header("Lugar")]
        [Tooltip("Ancho de la tribuna, en unidades del mundo (centrado en este objeto).")]
        [SerializeField] private float width = 24f;
        [Tooltip("Filas de gente; cada fila va un poco más atrás y más arriba.")]
        [SerializeField] private int rows = 2;
        [SerializeField] private float rowDepth = 0.35f;
        [SerializeField] private float rowRise = 0.12f;
        [Tooltip("Personas por fila.")]
        [SerializeField] private int peoplePerRow = 38;

        [Header("Aspecto")]
        [Tooltip("Tamaño de cada píxel en el mundo. 0,03 = mismo tamaño de píxel que los gladiadores.")]
        [SerializeField] private float pixelSize = 0.03f;
        [Tooltip("Las filas de atrás se oscurecen un poco para dar profundidad.")]
        [SerializeField] private float backRowShade = 0.75f;
        [SerializeField] private int seed = 7;
        [Tooltip("Orden de dibujo de la gente. Negativo = se dibuja antes que los muros y decorados (sprites en 0), " +
                 "así queda siempre detrás aunque por la inclinación de la cámara parezca \"más cerca\".")]
        [SerializeField] private int sortingOrder = -20;

        [Header("Ánimo")]
        [Tooltip("Entusiasmo de base (0 = quietos, 1 = todos festejando).")]
        [Range(0f, 1f)] [SerializeField] private float baseExcitement = 0.25f;
        [Tooltip("Cuánto se calma por segundo después de un festejo.")]
        [SerializeField] private float calmDownPerSecond = 0.25f;

        private const int CellW = 18, CellH = 24, Variants = 16;

        private class Person
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector3 basePosition;
            public int variant;
            public float phase;
            public float speed;
            public float nextArmChange;
            public bool armsUp;

            // Solo en modo Pueblo.
            public float walkSpeed;      // con signo: positivo camina a la derecha
            public float stopUntil;      // si es mayor que el tiempo actual, está parado
            public float nextDecision;   // cuándo decide si para, sigue o se da vuelta
        }

        private readonly List<Person> people = new List<Person>();
        private Sprite[] armsDown;
        private Sprite[] armsUp;
        private float excitement;

        private void Awake()
        {
            excitement = baseExcitement;
            BuildSprites();
            BuildCrowd();
        }

        private void OnEnable()
        {
            EventManager.Subscribe<GladiatorHitEvent>(OnHit);
            EventManager.Subscribe<CombatEndedEvent>(OnCombatEnded);
        }

        private void OnDisable()
        {
            EventManager.Unsubscribe<GladiatorHitEvent>(OnHit);
            EventManager.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        }

        // En modo Pueblo nadie le presta atención al combate.
        private void OnHit(GladiatorHitEvent e)
        {
            if (modo == Modo.Tribuna) excitement = Mathf.Min(1f, excitement + (e.killingBlow ? 0.8f : 0.25f));
        }

        private void OnCombatEnded(CombatEndedEvent e)
        {
            if (modo == Modo.Tribuna) excitement = 1f;
        }

        private void Update()
        {
            if (modo == Modo.Pueblo) UpdateTownsfolk();
            else UpdateCrowd();
        }

        // Gente que pasa: camina a su ritmo, a veces se para un rato, a veces se da vuelta.
        // Al salir por un costado de la franja vuelve a entrar por el otro.
        private void UpdateTownsfolk()
        {
            float t = Time.time, dt = Time.deltaTime, half = width / 2f;
            foreach (var p in people)
            {
                bool walking = t >= p.stopUntil;

                if (t >= p.nextDecision)
                {
                    float roll = Random.value;
                    if (roll < 0.25f) p.stopUntil = t + Random.Range(1f, 4f);        // se para (charla, mira algo)
                    else if (roll < 0.4f) p.walkSpeed = -p.walkSpeed;                  // se da vuelta
                    p.nextDecision = t + Random.Range(2f, 6f);

                    // Muy de vez en cuando alguien estira los brazos o saluda a otro.
                    p.armsUp = !walking && Random.value < 0.15f;
                    p.renderer.sprite = p.armsUp ? armsUp[p.variant] : armsDown[p.variant];
                }

                if (walking)
                {
                    p.basePosition.x += p.walkSpeed * dt;
                    if (p.basePosition.x > half) p.basePosition.x -= width;
                    else if (p.basePosition.x < -half) p.basePosition.x += width;
                    p.renderer.flipX = p.walkSpeed < 0f;
                }

                // Al caminar se nota el paso (un rebote cortito); parado, quieto.
                float step = walking ? Mathf.Abs(Mathf.Sin(t * Mathf.Abs(p.walkSpeed) * 9f + p.phase)) * 0.035f : 0f;
                p.transform.localPosition = p.basePosition + new Vector3(0f, step, 0f);
            }
        }

        private void UpdateCrowd()
        {
            float t = Time.time;
            excitement = Mathf.MoveTowards(excitement, baseExcitement, calmDownPerSecond * Time.deltaTime);

            // Saltan más alto y más seguido cuanto más entusiasmados están.
            // Saltitos cortos y no muy rápidos: con más fuerza parecía que la tribuna vibraba.
            float jump = Mathf.Lerp(0.015f, 0.09f, excitement);
            foreach (var p in people)
            {
                float hop = Mathf.Abs(Mathf.Sin(t * p.speed * (1f + 0.65f * excitement) + p.phase)) * jump;
                p.transform.localPosition = p.basePosition + new Vector3(0f, hop, 0f);

                if (t >= p.nextArmChange)
                {
                    // Con más entusiasmo, más gente con los brazos arriba y por más tiempo.
                    p.armsUp = Random.value < Mathf.Lerp(0.1f, 0.85f, excitement);
                    p.renderer.sprite = p.armsUp ? armsUp[p.variant] : armsDown[p.variant];
                    p.nextArmChange = t + Random.Range(0.55f, 1.8f);
                }
            }
        }

        // ---------- armado ----------

        private void BuildCrowd()
        {
            var material = Resources.Load<Material>("Effects/PublicoMat");
            var random = new System.Random(seed);
            float step = width / peoplePerRow;

            for (int row = rows - 1; row >= 0; row--)   // de atrás para adelante
            {
                float shade = Mathf.Lerp(1f, backRowShade, rows > 1 ? row / (float)(rows - 1) : 0f);
                for (int i = 0; i < peoplePerRow; i++)
                {
                    float x = -width / 2f + step * (i + 0.5f) + (float)(random.NextDouble() - 0.5) * step * 0.6f;
                    // Las filas se intercalan para que no queden uno detrás del otro.
                    if (row % 2 == 1) x += step * 0.5f;
                    float y = row * rowRise + (float)random.NextDouble() * 0.05f;
                    float z = row * rowDepth;

                    var go = new GameObject("Persona");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(x, y, z);

                    var sr = go.AddComponent<SpriteRenderer>();
                    int variant = random.Next(Variants);
                    sr.sprite = armsDown[variant];
                    sr.flipX = random.Next(2) == 0;
                    sr.color = new Color(tinte.r * shade, tinte.g * shade, tinte.b * shade, tinte.a);
                    if (material != null) sr.sharedMaterial = material;
                    sr.sortingOrder = sortingOrder;

                    float walk = 0.25f + (float)random.NextDouble() * 0.45f;
                    people.Add(new Person
                    {
                        transform = go.transform,
                        renderer = sr,
                        basePosition = go.transform.localPosition,
                        variant = variant,
                        phase = (float)random.NextDouble() * Mathf.PI * 2f,
                        speed = 2.1f + (float)random.NextDouble() * 1.9f,
                        nextArmChange = (float)random.NextDouble() * 1.5f,
                        walkSpeed = random.Next(2) == 0 ? walk : -walk,
                        stopUntil = random.Next(3) == 0 ? (float)random.NextDouble() * 3f : 0f,
                        nextDecision = 1f + (float)random.NextDouble() * 4f,
                    });
                }
            }
        }

        // Dibuja las 16 personas (brazos abajo y brazos arriba) en una textura de 8x4 celdas de 18x24 píxeles.
        private void BuildSprites()
        {
            var skins = new[] { C(0xF1C9A5), C(0xD9A37A), C(0xB07A52), C(0x7A4E32), C(0x5A3A24) };
            var tunics = new[] { C(0xE8E2D0), C(0xA8322D), C(0xC9A04A), C(0x3F5F8C), C(0x5E7A3A), C(0x7A5232), C(0x6B3F7A), C(0x8A8A8A), C(0xD07A3A) };
            var hairs = new[] { C(0x1E1612), C(0x4A2E1A), C(0x8A6A3A), C(0xB8B0A0), C(0x2A1E14) };
            var outline = C(0x1A120C);

            int texW = CellW * 8, texH = CellH * 4;
            var pixels = new Color32[texW * texH];
            var random = new System.Random(seed * 31 + 1);
            armsDown = new Sprite[Variants];
            armsUp = new Sprite[Variants];

            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Publico (generado)",
            };

            for (int v = 0; v < Variants; v++)
            {
                var skin = skins[random.Next(skins.Length)];
                var tunic = tunics[random.Next(tunics.Length)];
                var hair = hairs[random.Next(hairs.Length)];
                bool bald = random.Next(6) == 0;
                bool hood = !bald && random.Next(7) == 0;   // algunos con capucha/pañuelo del color de la túnica

                for (int frame = 0; frame < 2; frame++)
                {
                    int cell = v * 2 + frame;
                    int ox = (cell % 8) * CellW, oy = (cell / 8) * CellH;
                    var grid = new Color32?[CellW, CellH];

                    // Torso (de 0 a 11), con hombros redondeados.
                    for (int y = 0; y <= 11; y++)
                        for (int x = 4; x <= 13; x++)
                            if (!(y == 11 && (x == 4 || x == 13))) grid[x, y] = tunic;
                    // Cuello y cabeza.
                    grid[8, 12] = skin; grid[9, 12] = skin;
                    for (int y = 13; y <= 19; y++)
                        for (int x = 6; x <= 11; x++)
                            if (!((y == 13 || y == 19) && (x == 6 || x == 11))) grid[x, y] = skin;
                    // Pelo / capucha.
                    if (hood)
                    {
                        for (int y = 16; y <= 20; y++) for (int x = 5; x <= 12; x++) if (y >= 19 || x <= 6 || x >= 11) grid[x, y] = tunic;
                    }
                    else if (!bald)
                    {
                        for (int x = 6; x <= 11; x++) { grid[x, 19] = hair; grid[x, 18] = hair; }
                        grid[6, 17] = hair; grid[11, 17] = hair;
                    }
                    // Ojos.
                    grid[7, 16] = outline; grid[10, 16] = outline;

                    // Brazos: abajo a los costados, o arriba festejando.
                    if (frame == 0)
                    {
                        for (int y = 2; y <= 10; y++) { grid[3, y] = skin; grid[14, y] = skin; }
                    }
                    else
                    {
                        for (int y = 10; y <= 22; y++) { grid[3, y] = skin; grid[14, y] = skin; }
                        grid[2, 22] = skin; grid[15, 22] = skin;   // manos abiertas
                    }

                    // Contorno oscuro de 1 píxel, como el pixel art de los gladiadores.
                    for (int x = 0; x < CellW; x++)
                        for (int y = 0; y < CellH; y++)
                        {
                            Color32 c;
                            if (grid[x, y].HasValue) c = grid[x, y].Value;
                            else if (Touches(grid, x, y)) c = outline;
                            else c = new Color32(0, 0, 0, 0);
                            pixels[(oy + y) * texW + ox + x] = c;
                        }

                    var sprite = Sprite.Create(tex, new Rect(ox, oy, CellW, CellH), new Vector2(0.5f, 0f), 1f / pixelSize, 0, SpriteMeshType.FullRect);
                    if (frame == 0) armsDown[v] = sprite; else armsUp[v] = sprite;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
        }

        private static bool Touches(Color32?[,] grid, int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < grid.GetLength(0) && ny < grid.GetLength(1) && grid[nx, ny].HasValue) return true;
                }
            return false;
        }

        private static Color32 C(int hex) => new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255);

        // En el editor se ve una caja con el lugar que ocupa el público.
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;
            float h = CellH * pixelSize + (rows - 1) * rowRise;
            float d = Mathf.Max(0.1f, (rows - 1) * rowDepth);
            Gizmos.DrawWireCube(new Vector3(0f, h / 2f, d / 2f), new Vector3(width, h, d));
        }
    }
}
