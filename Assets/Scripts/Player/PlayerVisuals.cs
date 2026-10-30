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
        [SerializeField] Animator animator;
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
        Vector3 previousPosition;
        static readonly int MovingId = Animator.StringToHash("Moving");

        public static Color ColorFor(ulong clientId) => Palette[(int)(clientId % (ulong)Palette.Length)];

        void Awake()
        {
            player = GetComponent<PlayerController>();
            previousPosition = transform.position;
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
            if (!IsSpawned) return;
            bool down = player.IsDown;
            var delta = transform.position - previousPosition;
            previousPosition = transform.position;
            delta.y = 0f;
            if (animator != null)
                animator.SetBool(MovingId, !down && delta.magnitude > Time.deltaTime * .2f);
            float target = down ? knockdownAngle : 0f;
            tilt = Mathf.MoveTowards(tilt, target, (down ? fallSpeed : getUpSpeed) * Time.deltaTime);
            body.localRotation = Quaternion.Euler(tilt, 0f, 0f);
        }
    }
}
