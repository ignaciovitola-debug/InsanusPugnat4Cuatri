namespace GladiusAI
{
    // Selector (OR): evalúa a sus hijos en orden y devuelve apenas uno tenga éxito.
    // Ejemplo: Atacar (si puedo) > Perseguir (si lo veo) > Patrullar.
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

    // Sequence (AND): evalúa a sus hijos en orden y devuelve apenas uno falle.
    // Sirve para una cadena de pasos que se tienen que cumplir todos.
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
