using ScpParty.Combat;
using ScpParty.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace ScpParty.Player
{
    /// <summary>
    /// 玩家死亡与复活（服务器）：血量归零时丢下武器，倒地若干秒后满血复活并回到出生点。
    /// </summary>
    [RequireComponent(typeof(Health), typeof(PlayerController))]
    public class PlayerLife : NetworkBehaviour
    {
        [SerializeField] float respawnDelay = 3f;

        Health health;
        PlayerController player;
        PlayerWeapons weapons;
        float reviveAt = -1f;

        /// <summary>本地：距离复活还剩多少秒（HUD 用，仅服务器准确，客户端为估算）。</summary>
        public float RespawnRemaining => Mathf.Max(0f, localReviveAt - Time.time);
        float localReviveAt;

        void Awake()
        {
            health = GetComponent<Health>();
            player = GetComponent<PlayerController>();
            weapons = GetComponent<PlayerWeapons>();
        }

        public override void OnNetworkSpawn()
        {
            health.IsDead.OnValueChanged += OnDeadChanged;
            if (IsServer)
                health.ServerDied += OnServerDied;
        }

        public override void OnNetworkDespawn()
        {
            health.IsDead.OnValueChanged -= OnDeadChanged;
            health.ServerDied -= OnServerDied;
        }

        void OnDeadChanged(bool previous, bool dead)
        {
            if (dead)
                localReviveAt = Time.time + respawnDelay;
            if (Debug.isDebugBuild)
                Debug.Log($"[PlayerLife] player {OwnerClientId} isDead={dead} (local view)");
        }

        void OnServerDied(ulong attackerId)
        {
            Debug.Log($"[PlayerLife] player {OwnerClientId} died (by {attackerId})");
            if (weapons != null)
                weapons.ServerDrop(transform.position + Vector3.up * 0.2f);
            reviveAt = Time.time + respawnDelay;
        }

        void Update()
        {
            if (!IsServer || reviveAt < 0f || Time.time < reviveAt)
                return;
            reviveAt = -1f;
            health.Revive();
            player.RespawnRpc();
            Debug.Log($"[PlayerLife] player {OwnerClientId} revived");
        }
    }
}
