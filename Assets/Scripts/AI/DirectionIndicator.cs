using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Arma una flechita simple con primitivas de Unity (sin necesitar ningún
    /// asset) y la deja como hija del agente. Como el agente ya rota hacia su
    /// velocidad (ver SteeringBehaviors.FaceDirection), la flecha "viaja" con
    /// esa rotación sin necesitar código extra por frame. Pedido del profesor
    /// para poder ver la dirección/trayectoria de cada NPC en el build.
    /// </summary>
    public static class DirectionIndicator
    {
        public static void Attach(Transform parent, Color color, float heightOffset = 0.6f, float scale = 1f)
        {
            var root = new GameObject("DirectionIndicator");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, heightOffset, 0f);

            CreatePart(root.transform, "Shaft", new Vector3(0.07f, 0.07f, 0.55f) * scale,
                new Vector3(0f, 0f, 0.35f) * scale, Quaternion.identity, color);

            CreatePart(root.transform, "Head", new Vector3(0.18f, 0.07f, 0.18f) * scale,
                new Vector3(0f, 0f, 0.65f) * scale, Quaternion.Euler(0f, 45f, 0f), color);
        }

        private static void CreatePart(Transform parent, string name, Vector3 localScale, Vector3 localPos, Quaternion localRot, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            Object.Destroy(part.GetComponent<Collider>());

            part.transform.SetParent(parent, false);
            part.transform.localScale = localScale;
            part.transform.localPosition = localPos;
            part.transform.localRotation = localRot;

            part.GetComponent<Renderer>().material.color = color;
        }
    }
}
