using UnityEngine;

namespace GladiusAI
{
    public interface IGladiatorFactory
    {
        GladiatorNPC CreatePlayer(Vector3 position, Quaternion rotation);
        GladiatorNPC CreateEnemy(Vector3 position, Quaternion rotation);
    }
}
