using System;

namespace GladiusAI
{
    // Nodo hoja: ejecuta una acción concreta (moverse, atacar, patrullar...).
    // Recibe la lógica como un delegado, así no hace falta una clase por cada acción.
    public class ActionNode : Node
    {
        private readonly Func<Blackboard, NodeState> action;

        public ActionNode(string label, Func<Blackboard, NodeState> action) : base(label)
        {
            this.action = action;
        }

        public override NodeState Tick(Blackboard bb) => action(bb);
    }
}
