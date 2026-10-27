using Unity.Netcode;
using UnityEngine;

namespace ScpParty.Interaction
{
    /// <summary>
    /// 联网的门：服务器切换开关，状态同步给所有端；门板随铰链转动，碰撞体跟着门板走，关着时挡路。
    /// </summary>
    public class Door : Interactable
    {
        [SerializeField] Transform hinge;
        [SerializeField] float openAngle = -100f;
        [SerializeField] float swingSpeed = 220f;
        [SerializeField] bool startOpen;

        public readonly NetworkVariable<bool> IsOpen = new(false);

        public override string Prompt => IsOpen.Value ? "关门" : "开门";

        float angle;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                IsOpen.Value = startOpen;
            angle = IsOpen.Value ? openAngle : 0f;
            ApplyAngle();
            IsOpen.OnValueChanged += OnOpenChanged;
        }

        public override void OnNetworkDespawn()
        {
            IsOpen.OnValueChanged -= OnOpenChanged;
        }

        void OnOpenChanged(bool previous, bool open)
        {
            if (Debug.isDebugBuild)
                Debug.Log($"[Door] {name} isOpen={open}");
        }

        public override void ServerInteract(NetworkObject player)
        {
            IsOpen.Value = !IsOpen.Value;
        }

        void Update()
        {
            float target = IsOpen.Value ? openAngle : 0f;
            if (Mathf.Approximately(angle, target))
                return;
            angle = Mathf.MoveTowards(angle, target, swingSpeed * Time.deltaTime);
            ApplyAngle();
        }

        void ApplyAngle() => hinge.localRotation = Quaternion.Euler(0f, angle, 0f);
    }
}
