using ScpParty.Combat;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScpParty.Player
{
    /// <summary>
    /// 第一人称角色控制：移动、跳跃、冲刺、视角，以及被推/被扑倒后的击退。
    /// 移动由拥有者本地计算（NetworkTransform 为 Owner 权限），其他端只看同步结果。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour, IKnockbackReceiver
    {
        [Header("移动")]
        [SerializeField] float walkSpeed = 5f;
        [SerializeField] float sprintSpeed = 8.5f;
        [SerializeField] float acceleration = 45f;
        [SerializeField, Range(0f, 1f)] float airControl = 0.35f;
        [SerializeField] float jumpHeight = 1.3f;
        [SerializeField] float gravity = -25f;

        [Header("视角")]
        [SerializeField] Transform head;
        [SerializeField] float mouseSensitivity = 0.1f;   // 度/像素
        [SerializeField] float gamepadSensitivity = 180f; // 度/秒
        [SerializeField] float pitchLimit = 85f;

        [Header("击退 / 扑倒")]
        [SerializeField] float knockbackDamping = 4f;
        [SerializeField] float knockdownDuration = 1.6f;
        [SerializeField] float knockdownCameraRoll = 25f;
        [SerializeField] float fallResetHeight = -20f;

        /// <summary>是否处于被扑倒状态。拥有者写入，所有端读取以播放倒地表现。</summary>
        public readonly NetworkVariable<bool> IsKnockedDown = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public Transform Head => head;
        public bool IsSprinting { get; private set; }

        /// <summary>被扑倒或已死亡（所有端可读，用于表现）。</summary>
        public bool IsDown => IsKnockedDown.Value || (health != null && health.IsDead.Value);

        /// <summary>本地拥有者是否可以响应游戏输入（鼠标已锁定，且未倒地/死亡）。</summary>
        public bool CanAct => IsOwner && InputGate.GameplayEnabled && !IsDown;

        CharacterController controller;
        NetworkTransform networkTransform;
        Health health;
        InputAction moveAction, lookAction, jumpAction, sprintAction;

        Vector3 planarVelocity;
        Vector3 externalVelocity;
        float verticalVelocity;
        float pitch;
        float roll;
        float knockdownTimer;
        Transform attachedCamera;
        bool needsPhysicsSync;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            networkTransform = GetComponent<NetworkTransform>();
            health = GetComponent<Health>();
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"[Player] spawned owner={OwnerClientId} isLocal={IsOwner} pos={transform.position}");
            if (!IsOwner)
                return;

            var actions = InputSystem.actions;
            moveAction = actions.FindAction("Player/Move", true);
            lookAction = actions.FindAction("Player/Look", true);
            jumpAction = actions.FindAction("Player/Jump", true);
            sprintAction = actions.FindAction("Player/Sprint", true);
            actions.FindActionMap("Player", true).Enable();

            // 客户端是先实例化再移到出生点，CharacterController 需要同步一次，否则第一次 Move 会被拉回原点
            needsPhysicsSync = true;
            AttachCamera();
            InputGate.SetCursorLocked(true);
        }

        public override void OnNetworkDespawn()
        {
            DetachCamera();
        }

        public override void OnDestroy()
        {
            DetachCamera();
            base.OnDestroy();
        }

        void Update()
        {
            if (!IsOwner || !IsSpawned)
                return;

            if (needsPhysicsSync)
            {
                Physics.SyncTransforms();
                needsPhysicsSync = false;
            }

            float dt = Time.deltaTime;
            UpdateKnockdown(dt);
            UpdateLook(dt);
            UpdateMovement(dt);

            if (transform.position.y < fallResetHeight)
                TeleportToSpawn();
        }

        void UpdateLook(float dt)
        {
            if (InputGate.GameplayEnabled)
            {
                Vector2 look = lookAction.ReadValue<Vector2>();
                bool fromGamepad = lookAction.activeControl?.device is Gamepad;
                look *= fromGamepad ? gamepadSensitivity * dt : mouseSensitivity;

                transform.Rotate(0f, look.x, 0f, Space.Self);
                pitch = Mathf.Clamp(pitch - look.y, -pitchLimit, pitchLimit);
            }

            float targetRoll = IsDown ? knockdownCameraRoll : 0f;
            roll = Mathf.MoveTowards(roll, targetRoll, 120f * dt);
            head.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }

        void UpdateMovement(float dt)
        {
            bool canAct = CanAct;
            Vector2 input = canAct ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            IsSprinting = canAct && sprintAction.IsPressed() && input.y > 0.1f;

            Vector3 wish = transform.right * input.x + transform.forward * input.y;
            if (wish.sqrMagnitude > 1f)
                wish.Normalize();
            wish *= IsSprinting ? sprintSpeed : walkSpeed;

            bool grounded = controller.isGrounded;
            float accel = grounded ? acceleration : acceleration * airControl;
            planarVelocity = Vector3.MoveTowards(planarVelocity, wish, accel * dt);

            if (grounded)
            {
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;
                if (canAct && jumpAction.WasPressedThisFrame())
                    verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
            }
            verticalVelocity += gravity * dt;

            // 地面上摩擦更大，空中的击退可以飞得更远
            float damping = grounded ? knockbackDamping * 2f : knockbackDamping * 0.5f;
            externalVelocity *= Mathf.Exp(-damping * dt);

            Vector3 velocity = planarVelocity + externalVelocity + Vector3.up * verticalVelocity;
            CollisionFlags flags = controller.Move(velocity * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;
        }

        void UpdateKnockdown(float dt)
        {
            if (!IsKnockedDown.Value)
                return;
            knockdownTimer -= dt;
            if (knockdownTimer <= 0f)
                IsKnockedDown.Value = false;
        }

        /// <summary>拥有者本地施加冲量（向上的分量作为起跳速度）。</summary>
        public void ApplyImpulse(Vector3 impulse, bool knockDown)
        {
            externalVelocity += new Vector3(impulse.x, 0f, impulse.z);
            if (impulse.y > 0f)
                verticalVelocity = Mathf.Max(verticalVelocity, impulse.y);

            if (knockDown)
            {
                knockdownTimer = knockdownDuration;
                planarVelocity = Vector3.zero;
                IsKnockedDown.Value = true;
            }
        }

        /// <summary>服务器结算伤害后调用：击退由拥有者执行（移动是拥有者权威）。</summary>
        public void ServerApplyKnockback(Vector3 impulse, bool knockDown) => ReceiveKnockbackRpc(impulse, knockDown);

        [Rpc(SendTo.Owner)]
        void ReceiveKnockbackRpc(Vector3 impulse, bool knockDown)
        {
            ApplyImpulse(impulse, knockDown);
        }

        /// <summary>服务器在复活时调用，让拥有者回到出生点。</summary>
        [Rpc(SendTo.Owner)]
        public void RespawnRpc()
        {
            IsKnockedDown.Value = false;
            TeleportToSpawn();
        }

        public void TeleportToSpawn()
        {
            Transform spawn = SpawnPoints.Get(OwnerClientId);
            Vector3 position = spawn != null ? spawn.position : Vector3.up;
            Quaternion rotation = spawn != null ? spawn.rotation : Quaternion.identity;
            TeleportTo(position, rotation);
        }

        /// <summary>拥有者调用：瞬移（不插值）。</summary>
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            planarVelocity = externalVelocity = Vector3.zero;
            verticalVelocity = 0f;

            controller.enabled = false;
            networkTransform.Teleport(position, rotation, transform.localScale);
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        void AttachCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Player] 场景中没有 MainCamera，第一人称相机未挂载。");
                return;
            }
            attachedCamera = cam.transform;
            attachedCamera.SetParent(head, false);
            attachedCamera.localPosition = Vector3.zero;
            attachedCamera.localRotation = Quaternion.identity;
        }

        void DetachCamera()
        {
            if (attachedCamera == null)
                return;
            attachedCamera.SetParent(null, true);
            CameraHome.Restore(attachedCamera);
            attachedCamera = null;
            InputGate.SetCursorLocked(false);
        }
    }
}
