using ScpParty.Combat;
using ScpParty.Weapons;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScpParty.Player
{
    /// <summary>
    /// 推人 / 扑倒（空手时的攻击）：拥有者检测前方可受击对象并向服务器请求，服务器校验后通过 Health 结算击退。
    /// 冲刺中按攻击会向前扑出，命中时把对方扑倒并造成少量伤害。手持武器时不生效。
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerPush : NetworkBehaviour
    {
        [SerializeField] float range = 1.6f;
        [SerializeField] float radius = 0.45f;
        [SerializeField] float cooldown = 0.6f;

        [Header("普通推")]
        [SerializeField] float pushForce = 9f;
        [SerializeField] float pushUpForce = 3f;

        [Header("冲刺扑倒")]
        [SerializeField] float tackleForce = 12f;
        [SerializeField] float tackleUpForce = 4.5f;
        [SerializeField] float tackleLunge = 7f;
        [SerializeField] float tackleCooldown = 1.2f;
        [SerializeField] int tackleDamage = 5;

        [Tooltip("服务器端校验距离时给延迟留的余量")]
        [SerializeField] float serverRangeTolerance = 2f;

        PlayerController player;
        PlayerWeapons weapons;
        InputAction attackAction;
        float nextPushTime;
        float serverNextPushTime;

        static readonly RaycastHit[] Hits = new RaycastHit[16];

        void Awake()
        {
            player = GetComponent<PlayerController>();
            weapons = GetComponent<PlayerWeapons>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                attackAction = InputSystem.actions.FindAction("Player/Attack", true);
        }

        void Update()
        {
            if (!IsOwner || !IsSpawned || !player.CanAct || (weapons != null && weapons.HasWeapon))
                return;
            if (Time.time < nextPushTime || !attackAction.WasPressedThisFrame())
                return;

            bool tackle = player.IsSprinting;
            nextPushTime = Time.time + (tackle ? tackleCooldown : cooldown);

            Vector3 forward = Flatten(player.Head.forward, transform.forward);
            if (tackle)
                player.ApplyImpulse(forward * tackleLunge, false);

            Health target = FindTarget(player.Head.forward);
            if (target != null)
                RequestPushRpc(target.NetworkObject, forward, tackle);
        }

        Health FindTarget(Vector3 aimDirection)
        {
            Vector3 origin = player.Head.position;
            int count = Physics.SphereCastNonAlloc(origin, radius, aimDirection, Hits, range,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            Health best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var other = Hits[i].collider.GetComponentInParent<Health>();
                if (other == null || other.transform == transform || Hits[i].distance >= bestDistance)
                    continue;
                best = other;
                bestDistance = Hits[i].distance;
            }
            return best;
        }

        [Rpc(SendTo.Server)]
        void RequestPushRpc(NetworkObjectReference targetRef, Vector3 direction, bool tackle, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;
            if (!targetRef.TryGet(out NetworkObject targetObject) || targetObject == NetworkObject)
                return;
            if (!targetObject.TryGetComponent(out Health target))
                return;

            // 简单防作弊：冷却与距离
            float minInterval = (tackle ? tackleCooldown : cooldown) * 0.8f;
            if (Time.time < serverNextPushTime)
                return;
            serverNextPushTime = Time.time + minInterval;

            float maxDistance = range + radius * 2f + serverRangeTolerance + (tackle ? tackleLunge * 0.3f : 0f);
            if (Vector3.Distance(transform.position, target.transform.position) > maxDistance)
                return;

            Vector3 dir = Flatten(direction, transform.forward);
            Vector3 impulse = tackle
                ? dir * tackleForce + Vector3.up * tackleUpForce
                : dir * pushForce + Vector3.up * pushUpForce;
            target.ApplyDamage(new DamageInfo
            {
                Amount = tackle ? tackleDamage : 0,
                Point = target.transform.position + Vector3.up * 1.2f,
                Knockback = impulse,
                KnockDown = tackle,
                AttackerId = OwnerClientId,
            });
        }

        static Vector3 Flatten(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : fallback;
        }
    }
}
