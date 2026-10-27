using System;
using Unity.Netcode;
using UnityEngine;

namespace ScpParty.Combat
{
    /// <summary>
    /// 血量与受击。只有服务器能扣血，结果通过 NetworkVariable 同步；受击表现通过 Rpc 广播到所有端。
    /// 玩家和木桩共用。木桩设置 canDie=false、regenerateWhenIdle=true。
    /// </summary>
    public class Health : NetworkBehaviour
    {
        [SerializeField] int maxHealth = 100;
        [SerializeField] bool canDie = true;
        [SerializeField] bool regenerateWhenIdle;
        [SerializeField] float regenerateDelay = 3f;

        public readonly NetworkVariable<int> Current = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<bool> IsDead = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public int Max => maxHealth;

        /// <summary>任一 Health 受击（所有端触发）：目标、伤害、受击点、击退方向、攻击者。</summary>
        public static event Action<Health, int, Vector3, Vector3, ulong> AnyDamaged;

        /// <summary>本对象受击（所有端触发）。</summary>
        public event Action<int, Vector3, Vector3, ulong> Damaged;

        /// <summary>死亡（仅服务器触发），参数为攻击者。</summary>
        public event Action<ulong> ServerDied;

        float lastDamageTime;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                Current.Value = maxHealth;
                IsDead.Value = false;
            }
        }

        void Update()
        {
            if (!IsServer || !regenerateWhenIdle || IsDead.Value)
                return;
            if (Current.Value < maxHealth && Time.time - lastDamageTime > regenerateDelay)
                Current.Value = maxHealth;
        }

        /// <summary>服务器调用：结算伤害与击退。</summary>
        public void ApplyDamage(in DamageInfo info)
        {
            if (!IsServer || !IsSpawned || IsDead.Value)
                return;

            if (info.Amount > 0)
            {
                Current.Value = Mathf.Max(0, Current.Value - info.Amount);
                lastDamageTime = Time.time;
            }

            DamagedRpc(info.Amount, info.Point, info.Knockback, info.AttackerId);

            bool dies = canDie && info.Amount > 0 && Current.Value == 0;
            if (info.Knockback != Vector3.zero || info.KnockDown)
            {
                foreach (var receiver in GetComponents<IKnockbackReceiver>())
                    receiver.ServerApplyKnockback(info.Knockback, info.KnockDown && !dies);
            }

            if (dies)
            {
                IsDead.Value = true;
                ServerDied?.Invoke(info.AttackerId);
            }
        }

        /// <summary>服务器调用：满血复活。</summary>
        public void Revive()
        {
            if (!IsServer)
                return;
            Current.Value = maxHealth;
            IsDead.Value = false;
        }

        [Rpc(SendTo.Everyone)]
        void DamagedRpc(int amount, Vector3 point, Vector3 knockback, ulong attackerId)
        {
            if (amount > 0)
                DamagePopup.Spawn(point, amount);
            if (Debug.isDebugBuild)
                Debug.Log($"[Health] {name} took {amount} from {attackerId} -> {Current.Value}/{maxHealth}");
            Damaged?.Invoke(amount, point, knockback, attackerId);
            AnyDamaged?.Invoke(this, amount, point, knockback, attackerId);
        }
    }
}
