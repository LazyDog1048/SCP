using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScpParty.Player
{
    /// <summary>
    /// 推人 / 扑倒：拥有者检测前方玩家并向服务器请求，服务器校验后通知被推玩家的拥有者施加击退。
    /// 冲刺中按攻击会向前扑出，命中时把对方扑倒。
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

        [Tooltip("服务器端校验距离时给延迟留的余量")]
        [SerializeField] float serverRangeTolerance = 2f;

        PlayerController player;
        InputAction attackAction;
        float nextPushTime;
        float serverNextPushTime;

        static readonly RaycastHit[] Hits = new RaycastHit[16];

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                attackAction = InputSystem.actions.FindAction("Player/Attack", true);
        }

        void Update()
        {
            if (!IsOwner || !IsSpawned || !player.CanAct)
                return;
            if (Time.time < nextPushTime || !attackAction.WasPressedThisFrame())
                return;

            bool tackle = player.IsSprinting;
            nextPushTime = Time.time + (tackle ? tackleCooldown : cooldown);

            Vector3 forward = Flatten(player.Head.forward, transform.forward);
            if (tackle)
                player.ApplyImpulse(forward * tackleLunge, false);

            PlayerController target = FindTarget(player.Head.forward);
            if (target != null)
                RequestPushRpc(target.NetworkObject, forward, tackle);
        }

        PlayerController FindTarget(Vector3 aimDirection)
        {
            Vector3 origin = player.Head.position;
            int count = Physics.SphereCastNonAlloc(origin, radius, aimDirection, Hits, range,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            PlayerController best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var other = Hits[i].collider.GetComponentInParent<PlayerController>();
                if (other == null || other == player || Hits[i].distance >= bestDistance)
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
            if (!targetObject.TryGetComponent(out PlayerController target))
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
            target.ReceivePushRpc(impulse, tackle);
        }

        static Vector3 Flatten(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : fallback;
        }
    }
}
