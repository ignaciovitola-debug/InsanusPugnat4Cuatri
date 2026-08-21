using System;

namespace GladiusAI
{
    /// <summary>
    /// Nodo de decisión ("pregunta"). Evalúa una condición y deriva la
    /// ejecución a la rama True o False — es el equivalente, dentro del
    /// árbol, a un if/else.
    ///
    /// Ejemplo: "¿El enemigo está en rango de ataque?" → sí: atacar,
    /// no (o sin rama False): seguir evaluando otras opciones.
    /// </summary>
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
