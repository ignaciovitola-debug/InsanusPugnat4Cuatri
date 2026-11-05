using System;

namespace GladiusAI
{
    // Nodo de decisión: evalúa una condición y sigue por la rama True o False (como un if/else).
    // Ejemplo: ¿el enemigo está en rango? Sí: atacar. No: seguir con otras opciones.
    public class QuestionNode : Node
    {
        private readonly Func<Blackboard, bool> condition;
        private readonly Node onTrue;
        private readonly Node onFalse;

        public QuestionNode(string label, Func<Blackboard, bool> condition, Node onTrue, Node onFalse = null)
            : base(label)
        {
            this.condition = condition;
            this.onTrue = onTrue;
            this.onFalse = onFalse;
        }

        public override NodeState Tick(Blackboard bb)
        {
            bool result = condition(bb);

            if (result)
                return onTrue.Tick(bb);

            // Sin rama False, se considera Failure (útil dentro de un Selector).
            return onFalse != null ? onFalse.Tick(bb) : NodeState.Failure;
        }
    }
}
