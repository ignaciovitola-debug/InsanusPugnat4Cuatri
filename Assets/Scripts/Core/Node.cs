namespace GladiusAI
{
    /// Estados posibles que devuelve un nodo al ser evaluado (Tick).
    /// - Success: la acción/condición se cumplió.
    /// - Failure: no se cumplió.
    /// - Running: sigue en progreso (ej: moviéndose hacia un objetivo).
    public enum NodeState
    {
        Success,
        Failure,
        Running
    }

    /// Clase base de TODOS los nodos del árbol de comportamiento.
    /// Cualquier nodo nuevo que armen (ActionNode, QuestionNode, Selector...)
    /// hereda de acá y tiene que implementar Tick().
    public abstract class Node
    {
        protected readonly string label;

        protected Node(string label = "")
        {
            this.label = string.IsNullOrEmpty(label) ? GetType().Name : label;
        }

        /// Evalúa el nodo. Se llama una vez por "decisión" del NPC.
        public abstract NodeState Tick(Blackboard bb);

        public string Label => label;
    }
}
