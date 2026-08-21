using System;

namespace GladiusAI
{
    /// <summary>
    /// Nodo hoja: ejecuta una acción concreta (moverse, atacar, patrullar...).
    /// Recibe la lógica como un delegado (una función) en vez de obligar a
    /// crear una clase nueva por cada acción — así el NPC define sus
    /// acciones como simples métodos.
    /// </summary>
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
