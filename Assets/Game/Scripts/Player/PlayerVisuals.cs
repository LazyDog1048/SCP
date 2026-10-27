using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScpParty.Player
{
    /// <summary>
    /// 角色表现：按玩家编号上色，隐藏本地玩家自己的身体（只保留影子），被扑倒时身体向后倒下。
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerVisuals : NetworkBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Renderer[] tintedRenderers;
        [SerializeField] float knockdownAngle = -80f;
        [SerializeField] float fallSpeed = 400f;
        [SerializeField] float getUpSpeed = 160f;

        static readonly Color[] Palette =
        {
            new(0.95f, 0.35f, 0.30f), // 红
            new(0.25f, 0.55f, 0.95f), // 蓝
            new(0.98f, 0.80f, 0.25f), // 黄
            new(0.35f, 0.80f, 0.40f), // 绿
            new(0.70f, 0.45f, 0.90f), // 紫
            new(1.00f, 0.55f, 0.20f), // 橙
            new(0.30f, 0.85f, 0.85f), // 青
            new(0.95f, 0.50f, 0.75f), // 粉
        };

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        PlayerController player;
        float tilt;

        public static Color ColorFor(ulong clientId) => Palette[(int)(clientId % (ulong)Palette.Length)];

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        public override void OnNetworkSpawn()
        {
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, ColorFor(OwnerClientId));
            foreach (var r in tintedRenderers)
                r.SetPropertyBlock(block);

            if (IsOwner)
            {
                foreach (var r in body.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }

        void LateUpdate()
        {
            bool down = player.IsDown;
            float target = down ? knockdownAngle : 0f;
            tilt = Mathf.MoveTowards(tilt, target, (down ? fallSpeed : getUpSpeed) * Time.deltaTime);
            body.localRotation = Quaternion.Euler(tilt, 0f, 0f);
        }
    }
}
