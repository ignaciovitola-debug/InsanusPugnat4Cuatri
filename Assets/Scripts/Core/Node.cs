namespace GladiusAI
{
    /// <summary>
    /// Estados posibles que devuelve un nodo al ser evaluado (Tick).
    /// - Success: la acción/condición se cumplió.
    /// - Failure: no se cumplió.
    /// - Running: sigue en progreso (ej: moviéndose hacia un objetivo).
    /// </summary>
    public enum NodeState
    {
        Success,
        Failure,
        Running
    }

    /// <summary>
    /// Clase base de TODOS los nodos del árbol de comportamiento.
    /// Cualquier nodo nuevo que armen (ActionNode, QuestionNode, Selector...)
    /// hereda de acá y tiene que implementar Tick().
    /// </summary>
    public abstract class Node
    {
        protected readonly string label;

        protected Node(string label = "")
        {
            this.label = string.IsNullOrEmpty(label) ? GetType().Name : label;
        }

        /// <summary>Evalúa el nodo. Se llama una vez por "decisión" del NPC.</summary>
        public abstract NodeState Tick(Blackboard bb);

        public string Label => label;
    }
}
