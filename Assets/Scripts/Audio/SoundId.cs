namespace GladiusAI
{
    /// Lista de todos los sonidos del juego, identificados por nombre.
    /// El código nunca nombra archivos de audio: pide, por ejemplo, SoundManager.Play(SoundId.SwordHit),
    /// y la SoundLibrary decide qué clip(s) suenan. Así, cambiar un sonido no requiere tocar código.
    ///
    /// IMPORTANTE: los valores tienen número fijo porque Unity guarda en la SoundLibrary el número,
    /// no el nombre. Los sonidos nuevos se agregan siempre al final, con el número siguiente.
    public enum SoundId
    {
        None = 0,

        // Combate
        SwordHit = 1,       // golpe que conecta
        ShieldBlock = 2,    // golpe parado con el escudo
        KillingBlow = 3,    // golpe que mata

        // Público
        CrowdCheer = 4,     // ganaste el combate
        CrowdBoo = 5,       // perdiste o te rendiste

        // Ambientes en loop
        ArenaCrowd = 6,     // público de fondo durante las peleas
        Rain = 7,           // lluvia (Menu y Ludus)
        DistantStadium = 8, // estadio de lejos (Menu y Ludus, junto con la lluvia)
    }
}
