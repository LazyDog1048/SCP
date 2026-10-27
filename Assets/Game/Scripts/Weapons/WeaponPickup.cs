using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ScpParty.Weapons
{
    /// <summary>
    /// 地上的武器。只同步武器下标和剩余子弹，模型在各端本地生成。
    /// 被捡走时由服务器销毁；玩家丢下的武器一段时间后自动消失。
    /// </summary>
    public class WeaponPickup : NetworkBehaviour
    {
        [SerializeField] Transform modelRoot;
        [SerializeField] float spinSpeed = 90f;
        [SerializeField] float bobHeight = 0.12f;
        [SerializeField] float droppedLifetime = 30f;

        public readonly NetworkVariable<int> WeaponIndex = new(-1);
        public readonly NetworkVariable<int> Ammo = new(0);

        public static readonly List<WeaponPickup> Active = new();

        public WeaponDefinition Definition => WeaponDatabase.Get(WeaponIndex.Value);

        /// <summary>服务器端：被捡走时通知来源（出生点）。</summary>
        public event Action ServerTaken;

        GameObject model;
        float despawnAt = -1f;
        float phase;

        /// <summary>服务器调用：生成一把地上的武器。</summary>
        public static WeaponPickup ServerSpawn(WeaponPickup prefab, int weaponIndex, int ammo, Vector3 position, bool dropped)
        {
            var pickup = Instantiate(prefab, position, Quaternion.identity);
            pickup.WeaponIndex.Value = weaponIndex;
            pickup.Ammo.Value = ammo;
            pickup.NetworkObject.Spawn(true);
            if (dropped)
                pickup.despawnAt = Time.time + pickup.droppedLifetime;
            return pickup;
        }

        public override void OnNetworkSpawn()
        {
            Active.Add(this);
            phase = UnityEngine.Random.value * 10f;
            var def = Definition;
            if (def != null && def.model != null)
                model = Instantiate(def.model, modelRoot, false);
        }

        public override void OnNetworkDespawn()
        {
            Active.Remove(this);
        }

        void Update()
        {
            modelRoot.localRotation = Quaternion.Euler(0f, (Time.time + phase) * spinSpeed, 0f);
            modelRoot.localPosition = new Vector3(0f, 0.6f + Mathf.Sin((Time.time + phase) * 2f) * bobHeight, 0f);

            if (IsServer && despawnAt > 0f && Time.time >= despawnAt)
                NetworkObject.Despawn(true);
        }

        /// <summary>服务器调用：被玩家捡走。</summary>
        public void ServerTake()
        {
            if (!IsServer || !IsSpawned)
                return;
            ServerTaken?.Invoke();
            NetworkObject.Despawn(true);
        }

        /// <summary>找视线前方最合适的武器（本地，用于拾取提示和请求）。</summary>
        public static WeaponPickup FindBest(Vector3 origin, Vector3 forward, float maxDistance)
        {
            WeaponPickup best = null;
            float bestScore = float.MaxValue;
            foreach (var pickup in Active)
            {
                Vector3 to = pickup.transform.position + Vector3.up * 0.6f - origin;
                float distance = to.magnitude;
                if (distance > maxDistance)
                    continue;
                float angle = Vector3.Angle(forward, to);
                if (angle > 70f && distance > 1.2f)
                    continue;
                float score = angle + distance * 10f;
                if (score < bestScore)
                {
                    best = pickup;
                    bestScore = score;
                }
            }
            return best;
        }
    }
}
