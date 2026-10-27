using UnityEngine;

namespace ScpParty.Weapons
{
    /// <summary>武器刷新点：服务器在这里放一把指定武器，被捡走后延迟刷新。</summary>
    public class WeaponSpawnPoint : MonoBehaviour
    {
        [SerializeField] int weaponIndex;
        [SerializeField] float respawnDelay = 8f;

        public int WeaponIndex => weaponIndex;
        public float RespawnDelay => respawnDelay;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.6f, new Vector3(0.6f, 0.6f, 0.6f));
        }
    }
}
