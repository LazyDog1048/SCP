using Unity.Netcode;
using UnityEngine;

namespace ScpParty.Interaction
{
    /// <summary>
    /// 可以用 E 交互的网络物体（门、以后的开关、箱子等）。玩家看着它的碰撞体按 E，服务器校验距离后调用 ServerInteract。
    /// </summary>
    public abstract class Interactable : NetworkBehaviour
    {
        [SerializeField] float maxDistance = 3f;

        public float MaxDistance => maxDistance;

        /// <summary>HUD 提示文字（不含按键），例如“开门”。</summary>
        public abstract string Prompt { get; }

        /// <summary>服务器调用：玩家交互。</summary>
        public abstract void ServerInteract(NetworkObject player);
    }
}
