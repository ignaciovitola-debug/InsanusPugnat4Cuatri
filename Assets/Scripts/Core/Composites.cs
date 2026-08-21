namespace GladiusAI
{
    /// <summary>
    /// Selector (OR lógico / "prioridad"): evalúa a sus hijos EN ORDEN y
    /// devuelve apenas uno tenga éxito. Es el nodo típico para elegir
    /// "qué hacer ahora" entre varias opciones ordenadas por prioridad.
    ///
    /// Ejemplo: Atacar (si puedo) > Perseguir (si lo veo) > Patrullar.
    /// </summary>
    public class Selector : Node
    {
        private readonly Node[] children;

        public Selector(string label, params Node[] children) : base(label)
        {
            this.children = children;
        }

        public override NodeState Tick(Blackboard bb)
        {
            for (int i = 0; i < children.Length; i++)
            {
                var state = children[i].Tick(bb);
                if (state != NodeState.Failure)
                    return state;
            }
            return NodeState.Failure;
        }
    }

    /// <summary>
    /// Sequence (AND lógico): evalúa a sus hijos en orden y devuelve
    /// apenas uno falle. Útil para una cadena de pasos que TODOS tienen
    /// que cumplirse (ej: estar en rango -> tener stamina -> atacar).
    /// No lo usamos todavía en la versión básica, pero lo dejamos listo.
    /// </summary>
    public class Sequence : Node
    {
        private readonly Node[] children;

        public Sequence(string label, params Node[] children) : base(label)
        {
            this.children = children;
        }

        public override NodeState Tick(Blackboard bb)
        {
            for (int i = 0; i < children.Length; i++)
            {
                var state = children[i].Tick(bb);
                if (state != NodeState.Success)
                    return state;
            }
            return NodeState.Success;
        }
    }
}
