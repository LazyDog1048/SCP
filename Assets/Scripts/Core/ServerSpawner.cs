using System.Collections.Generic;
using ScpParty.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace ScpParty
{
    /// <summary>
    /// 服务器启动时生成场景里的网络物体：训练木桩、武器刷新点。武器被捡走后按刷新点的间隔重新出现。
    /// </summary>
    public class ServerSpawner : MonoBehaviour
    {
        [SerializeField] NetworkManager network;
        [SerializeField] NetworkObject dummyPrefab;
        [SerializeField] WeaponPickup pickupPrefab;
        [SerializeField] Transform dummyPoints;
        [SerializeField] Transform weaponPoints;

        readonly List<(WeaponSpawnPoint point, float respawnAt)> pending = new();

        void OnEnable()
        {
            network.OnServerStarted += OnServerStarted;
            network.OnServerStopped += OnServerStopped;
        }

        void OnDisable()
        {
            if (network == null)
                return;
            network.OnServerStarted -= OnServerStarted;
            network.OnServerStopped -= OnServerStopped;
        }

        void OnServerStarted()
        {
            pending.Clear();
            foreach (Transform point in dummyPoints)
                Instantiate(dummyPrefab, point.position, point.rotation).Spawn(true);
            foreach (var point in weaponPoints.GetComponentsInChildren<WeaponSpawnPoint>())
                SpawnAt(point);
        }

        void OnServerStopped(bool wasHost) => pending.Clear();

        void SpawnAt(WeaponSpawnPoint point)
        {
            var def = WeaponDatabase.Get(point.WeaponIndex);
            if (def == null)
                return;
            var pickup = WeaponPickup.ServerSpawn(pickupPrefab, point.WeaponIndex, def.magazineSize, point.transform.position, dropped: false);
            pickup.ServerTaken += () => pending.Add((point, Time.time + point.RespawnDelay));
        }

        void Update()
        {
            if (!network.IsServer)
                return;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].respawnAt)
                    continue;
                var point = pending[i].point;
                pending.RemoveAt(i);
                SpawnAt(point);
            }
        }
    }
}
