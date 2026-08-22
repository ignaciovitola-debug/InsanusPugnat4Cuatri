using UnityEngine;

public class BlodShadow : MonoBehaviour
{
    [SerializeField] private Transform target;   // el objeto que proyecta sombra
    [SerializeField] private float yOffset = 0.02f; // pegadito al piso, evita z-fighting
    [SerializeField] private float groundY = 0f;

    private void LateUpdate()
    {
        Vector3 pos = target.position;
        pos.y = groundY + yOffset;
        transform.position = pos;
    }
}
