using UnityEngine;

namespace GladiusAI
{
    /// Caras del instructor (Máximo). El orden tiene que coincidir con la lista "Portraits" de TutorialDialogue,
    /// porque la cara se busca por posición: Calm es la 0, Talk la 1, etc.
    public enum PortraitExpression
    {
        Calm,
        Talk,
        Smile,
        Aggression,
        Sadness,
        Special,
    }

    /// Qué parte de la interfaz resalta un mensaje mientras se lee.
    /// Los valores nuevos van siempre al final: Unity guarda el número, no el nombre.
    public enum DialogueHighlight
    {
        None,
        OrderButtons,       // los tres: Atacá, Defiéndete y Ríndete
        AttackButton,       // solo Atacá
        DefendButton,       // solo Defiéndete
        SurrenderButton,    // solo Ríndete
    }

    /// Un mensaje del instructor: qué cara pone, qué dice y qué resalta.
    /// Es un struct [Serializable] para poder escribir los diálogos desde el Inspector del CombatStarter.
    [System.Serializable]
    public struct DialogueLine
    {
        [Tooltip("Cara al aparecer el mensaje y cuando termina de hablar.")]
        public PortraitExpression expression;
        [Tooltip("Mientras aparece el texto usa la cara Talk (parece que habla) y al terminar vuelve a la de arriba.")]
        public bool talkWhileWriting;
        [Tooltip("Cierra el cuadro y lo vuelve a abrir antes de este mensaje (el juego sigue en pausa).")]
        public bool appearAgain;
        [Tooltip("Parte de la interfaz que se resalta mientras se lee este mensaje.")]
        public DialogueHighlight highlight;
        [TextArea(2, 5)] public string text;
    }
}
