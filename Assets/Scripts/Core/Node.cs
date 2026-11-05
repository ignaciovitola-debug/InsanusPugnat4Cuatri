namespace GladiusAI
{
    // Estados que devuelve un nodo al evaluarse (Tick): Success, Failure o Running.
    // Running significa que sigue en progreso (ej: moviéndose hacia un objetivo).
    public enum NodeState
    {
        Success,
        Failure,
        Running
    }

    // Clase base de todos los nodos del árbol de comportamiento.
    // Cada nodo nuevo hereda de acá e implementa Tick().
    public abstract class Node
    {
        protected readonly string label;

        protected Node(string label = "")
        {
            this.label = string.IsNullOrEmpty(label) ? GetType().Name : label;
        }

        // Evalúa el nodo. Se llama una vez por "decisión" del NPC.
        public abstract NodeState Tick(Blackboard bb);

        public string Label => label;
    }
}
