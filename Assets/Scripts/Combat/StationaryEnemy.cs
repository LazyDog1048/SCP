using Unity.Netcode;
using UnityEngine;

namespace ScpParty.Combat
{
    [RequireComponent(typeof(Health))]
    public class StationaryEnemy : NetworkBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] Collider hitCollider;
        Health health;
        static readonly int Dead = Animator.StringToHash("Dead");
        static readonly int Hit = Animator.StringToHash("Hit");

        void Awake() => health = GetComponent<Health>();

        public override void OnNetworkSpawn()
        {
            health.Damaged += OnDamaged;
            health.IsDead.OnValueChanged += OnDeathChanged;
            ApplyDeath(health.IsDead.Value);
            Debug.Log($"[Enemy] spawned health={health.Current.Value} dead={health.IsDead.Value} server={IsServer}");
        }

        public override void OnNetworkDespawn()
        {
            health.Damaged -= OnDamaged;
            health.IsDead.OnValueChanged -= OnDeathChanged;
        }

        void OnDamaged(int amount, Vector3 point, Vector3 knockback, ulong attacker)
        {
            if (amount > 0 && !health.IsDead.Value) animator.SetTrigger(Hit);
        }

        void OnDeathChanged(bool previous, bool current) => ApplyDeath(current);

        void ApplyDeath(bool dead)
        {
            hitCollider.enabled = !dead;
            animator.SetBool(Dead, dead);
            if (dead) Debug.Log($"[Enemy] death applied server={IsServer} collider={hitCollider.enabled}");
        }
    }
}
