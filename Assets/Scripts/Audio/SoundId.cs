namespace GladiusAI
{
    /// <summary>
    /// Nombre de cada sonido del juego. El código pide sonidos por este nombre y la SoundLibrary
    /// decide qué archivo(s) suenan: cambiar un sonido no requiere tocar código.
    /// Agregar valores siempre al final, para no desordenar los ya asignados en la SoundLibrary.
    /// </summary>
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
