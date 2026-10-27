using ScpParty.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScpParty.Interaction
{
    /// <summary>
    /// 玩家的交互：拥有者每帧从视线找 Interactable，按 E 发给服务器。看着可交互物时，E 不再用于拾取武器。
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : NetworkBehaviour
    {
        [SerializeField] float rayLength = 3f;
        [Tooltip("服务器校验距离时的余量")]
        [SerializeField] float serverTolerance = 1.5f;

        /// <summary>本地：当前看着的可交互物。</summary>
        public Interactable Target { get; private set; }

        PlayerController player;
        InputAction interactAction;

        static readonly RaycastHit[] Hits = new RaycastHit[8];

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                interactAction = InputSystem.actions.FindAction("Player/Interact", true);
        }

        void Update()
        {
            if (!IsOwner || !IsSpawned)
                return;

            Target = player.CanAct ? FindTarget() : null;
            if (Target != null && interactAction.WasPressedThisFrame())
                InteractRpc(Target.NetworkObject);
        }

        Interactable FindTarget()
        {
            int count = Physics.RaycastNonAlloc(player.Head.position, player.Head.forward, Hits, rayLength,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            Interactable result = null;
            for (int i = 0; i < count; i++)
            {
                if (Hits[i].distance >= best || Hits[i].collider.transform.IsChildOf(transform))
                    continue;
                best = Hits[i].distance;
                // 最近的碰撞体如果不是可交互物（例如墙），就算被挡住
                result = Hits[i].collider.GetComponentInParent<Interactable>();
            }
            return result != null && best <= result.MaxDistance ? result : null;
        }

        [Rpc(SendTo.Server)]
        void InteractRpc(NetworkObjectReference targetRef, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;
            if (!targetRef.TryGet(out NetworkObject obj) || !obj.TryGetComponent(out Interactable target))
                return;
            ServerTryInteract(target);
        }

        /// <summary>服务器调用：校验后交互（测试代码也可直接调用）。</summary>
        public bool ServerTryInteract(Interactable target)
        {
            if (!IsServer || target == null || !target.IsSpawned || player.IsDown)
                return false;
            var col = target.GetComponentInChildren<Collider>();
            Vector3 closest = col != null ? col.ClosestPoint(player.Head.position) : target.transform.position;
            if (Vector3.Distance(closest, player.Head.position) > target.MaxDistance + serverTolerance)
                return false;
            target.ServerInteract(NetworkObject);
            return true;
        }
    }
}
