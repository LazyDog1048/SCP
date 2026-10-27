using System;
using System.Collections.Generic;
using ScpParty.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace ScpParty
{
    /// <summary>
    /// 服务器启动时生成场景里的网络物体：每个生成组把一个 prefab 放到 points 的每个子物体位置（木桩、门等）；
    /// 武器刷新点单独处理，被捡走后按刷新点的间隔重新出现。
    /// </summary>
    public class ServerSpawner : MonoBehaviour
    {
        [Serializable]
        public class SpawnGroup
        {
            public NetworkObject prefab;
            public Transform points;
        }

        [SerializeField] NetworkManager network;
        [SerializeField] SpawnGroup[] groups;
        [SerializeField] WeaponPickup pickupPrefab;
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
            foreach (var group in groups)
            {
                foreach (Transform point in group.points)
                {
                    var obj = Instantiate(group.prefab, point.position, point.rotation);
                    obj.name = point.name;
                    obj.Spawn(true);
                }
            }
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
