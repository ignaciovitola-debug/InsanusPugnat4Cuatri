namespace GladiusAI
{
    // Única forma de mostrar una cantidad de denarios en pantalla: el ícono de la moneda + "N denarios".
    // Así, si algún día cambia el ícono o la palabra, se cambia solo acá.
    //
    // El ícono es un "sprite" dentro del texto (rich text de TextMeshPro): la etiqueta <sprite> busca el
    // Sprite Asset "Monedas" en Assets/Resources/Sprite Assets/ y dibuja el cuadro llamado "Denario".
    public static class DenariiText
    {
        private const string CoinIcon = "<sprite=\"Monedas\" name=\"Denario\">";

        // Ej: Amount(100) → "(moneda) 100 denarios"; Amount(50, showPlus: true) → "(moneda) +50 denarios".
        public static string Amount(int amount, bool showPlus = false)
            => $"{CoinIcon} {(showPlus && amount > 0 ? "+" : "")}{amount} denarios";
    }
}
