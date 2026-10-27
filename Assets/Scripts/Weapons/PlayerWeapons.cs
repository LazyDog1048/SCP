using ScpParty.Combat;
using ScpParty.Interaction;
using ScpParty.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScpParty.Weapons
{
    /// <summary>
    /// 玩家手上的武器：拾取、丢弃、换弹、攻击。
    /// 拥有者负责输入和即时表现（挥动、后坐、弹道），服务器负责判定命中、扣子弹和伤害。
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerWeapons : NetworkBehaviour
    {
        [SerializeField] Transform weaponAnchor;
        [SerializeField] WeaponPickup pickupPrefab;
        [SerializeField] float pickupRange = 2.5f;
        [Tooltip("服务器端校验攻击起点与头部位置的最大偏差")]
        [SerializeField] float originTolerance = 1.5f;

        public readonly NetworkVariable<int> HeldIndex = new(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<int> Ammo = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<bool> Reloading = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public WeaponDefinition Held => WeaponDatabase.Get(HeldIndex.Value);
        public bool HasWeapon => Held != null;

        /// <summary>本地：视线前方可以捡的武器（HUD 提示用）。</summary>
        public WeaponPickup LookedAtPickup { get; private set; }

        PlayerController player;
        PlayerInteractor interactor;
        InputAction attackAction, interactAction, dropAction, reloadAction;
        GameObject heldModel;
        Transform muzzle;
        Vector3 anchorBasePosition;
        Quaternion anchorBaseRotation;
        float kick;         // 后坐/挥动动画进度 0..1
        WeaponKind kickKind;
        float nextAttackTime;
        float serverNextAttackTime;
        float serverReloadDoneTime;

        static readonly RaycastHit[] Hits = new RaycastHit[16];
        static readonly Collider[] Overlaps = new Collider[32];

        void Awake()
        {
            player = GetComponent<PlayerController>();
            interactor = GetComponent<PlayerInteractor>();
            anchorBasePosition = weaponAnchor.localPosition;
            anchorBaseRotation = weaponAnchor.localRotation;
        }

        public override void OnNetworkSpawn()
        {
            HeldIndex.OnValueChanged += OnHeldChanged;
            OnHeldChanged(-1, HeldIndex.Value);

            if (IsOwner)
            {
                var actions = InputSystem.actions;
                attackAction = actions.FindAction("Player/Attack", true);
                interactAction = actions.FindAction("Player/Interact", true);
                dropAction = actions.FindAction("Player/Drop", true);
                reloadAction = actions.FindAction("Player/Reload", true);
            }
        }

        public override void OnNetworkDespawn()
        {
            HeldIndex.OnValueChanged -= OnHeldChanged;
        }

        void OnHeldChanged(int previous, int current)
        {
            if (heldModel != null)
                Destroy(heldModel);
            heldModel = null;
            muzzle = null;
            var def = WeaponDatabase.Get(current);
            if (def != null && def.model != null)
            {
                heldModel = Instantiate(def.model, weaponAnchor, false);
                muzzle = heldModel.transform.Find("Muzzle");
            }
        }

        void Update()
        {
            AnimateAnchor();

            if (IsServer)
                ServerUpdateReload();

            if (!IsOwner || !IsSpawned)
                return;

            // 看着门等可交互物时，E 交给 PlayerInteractor
            bool interactorBusy = interactor != null && interactor.Target != null;
            LookedAtPickup = player.CanAct && !interactorBusy ? WeaponPickup.FindBest(player.Head.position, player.Head.forward, pickupRange) : null;
            if (!player.CanAct)
                return;

            if (interactAction.WasPressedThisFrame() && LookedAtPickup != null)
                RequestPickupRpc(LookedAtPickup.NetworkObject);

            var def = Held;
            if (def == null)
                return;

            if (dropAction.WasPressedThisFrame())
            {
                RequestDropRpc();
                return;
            }

            if (reloadAction.WasPressedThisFrame() && def.UsesAmmo && Ammo.Value < def.magazineSize && !Reloading.Value)
                RequestReloadRpc();

            bool trigger = def.automatic ? attackAction.IsPressed() : attackAction.WasPressedThisFrame();
            if (!trigger || Time.time < nextAttackTime)
                return;

            if (def.UsesAmmo && (Reloading.Value || Ammo.Value <= 0))
            {
                if (!Reloading.Value && attackAction.WasPressedThisFrame())
                    RequestReloadRpc();
                return;
            }

            nextAttackTime = Time.time + def.fireInterval;
            Vector3 origin = player.Head.position;
            Vector3 direction = ApplySpread(player.Head.forward, def.spread);
            PlayAttackFx(def, origin, direction, isOwnerPrediction: true);
            AttackRpc(origin, direction);
        }

        // ---------- 服务器 ----------

        [Rpc(SendTo.Server)]
        void RequestPickupRpc(NetworkObjectReference pickupRef, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;
            if (pickupRef.TryGet(out NetworkObject obj) && obj.TryGetComponent(out WeaponPickup pickup))
                ServerTryPickup(pickup);
        }

        /// <summary>服务器调用：尝试捡起地上的武器（手上有武器会先丢在原地）。</summary>
        public bool ServerTryPickup(WeaponPickup pickup)
        {
            if (!IsServer || pickup == null || !pickup.IsSpawned || !ServerCanAct())
                return false;
            if (Vector3.Distance(pickup.transform.position, transform.position) > pickupRange + 1.5f)
                return false;

            if (HasWeapon)
                ServerDrop(pickup.transform.position);

            int index = pickup.WeaponIndex.Value;
            int ammo = pickup.Ammo.Value;
            pickup.ServerTake();
            ServerGive(index, ammo);
            return true;
        }

        [Rpc(SendTo.Server)]
        void RequestDropRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId || !HasWeapon)
                return;
            ServerDrop(DropPosition());
        }

        [Rpc(SendTo.Server)]
        void RequestReloadRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;
            ServerStartReload();
        }

        [Rpc(SendTo.Server)]
        void AttackRpc(Vector3 origin, Vector3 direction, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;
            ServerAttack(origin, direction);
        }

        /// <summary>服务器调用：给玩家一把武器（拾取、测试用）。</summary>
        public void ServerGive(int weaponIndex, int ammo)
        {
            var def = WeaponDatabase.Get(weaponIndex);
            if (!IsServer || def == null)
                return;
            HeldIndex.Value = weaponIndex;
            Ammo.Value = def.UsesAmmo ? Mathf.Clamp(ammo, 0, def.magazineSize) : 0;
            Reloading.Value = false;
            serverNextAttackTime = 0f;
        }

        /// <summary>服务器调用：把手上的武器丢在指定位置。</summary>
        public void ServerDrop(Vector3 position)
        {
            if (!IsServer || !HasWeapon)
                return;
            if (pickupPrefab != null)
                WeaponPickup.ServerSpawn(pickupPrefab, HeldIndex.Value, Ammo.Value, position, dropped: true);
            HeldIndex.Value = -1;
            Ammo.Value = 0;
            Reloading.Value = false;
        }

        /// <summary>服务器调用：执行一次攻击（输入来自拥有者，测试代码也可以直接调用）。</summary>
        public void ServerAttack(Vector3 origin, Vector3 direction)
        {
            var def = Held;
            if (!IsServer || def == null || !ServerCanAct())
                return;
            if (Time.time < serverNextAttackTime)
                return;
            if (def.UsesAmmo && (Reloading.Value || Ammo.Value <= 0))
                return;

            serverNextAttackTime = Time.time + def.fireInterval * 0.8f;
            if (def.UsesAmmo)
                Ammo.Value--;

            if ((origin - player.Head.position).sqrMagnitude > originTolerance * originTolerance)
                origin = player.Head.position;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : player.Head.forward;

            switch (def.kind)
            {
                case WeaponKind.Melee:
                    ServerMelee(def, origin, direction);
                    AttackFxRpc(origin, direction, origin);
                    break;
                case WeaponKind.Hitscan:
                    Vector3 end = ServerHitscan(def, origin, direction);
                    AttackFxRpc(origin, direction, end);
                    break;
                case WeaponKind.Projectile:
                    Projectile.Launch(def, origin + direction * 0.4f, direction, transform, OwnerClientId, authoritative: true);
                    AttackFxRpc(origin, direction, origin);
                    break;
            }

            if (def.UsesAmmo && Ammo.Value == 0)
                ServerStartReload();
        }

        void ServerMelee(WeaponDefinition def, Vector3 origin, Vector3 direction)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, def.range, Overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var damaged = new System.Collections.Generic.HashSet<Health>();
            for (int i = 0; i < count; i++)
            {
                var col = Overlaps[i];
                if (col.transform.IsChildOf(transform))
                    continue;
                var health = col.GetComponentInParent<Health>();
                if (health == null || !damaged.Add(health))
                    continue;

                // CharacterController 不支持 Collider.ClosestPoint，用包围盒近似
                Vector3 point = col.bounds.ClosestPoint(origin);
                Vector3 to = point - origin;
                Vector3 flatTo = new Vector3(to.x, 0f, to.z);
                Vector3 flatDir = new Vector3(direction.x, 0f, direction.z);
                if (flatTo.sqrMagnitude > 0.0001f && Vector3.Angle(flatDir, flatTo) > def.meleeAngle * 0.5f)
                    continue;
                WeaponHits.ServerApply(col, def, point, direction, OwnerClientId);
            }
        }

        Vector3 ServerHitscan(WeaponDefinition def, Vector3 origin, Vector3 direction)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, Hits, def.range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            int nearest = -1;
            for (int i = 0; i < count; i++)
            {
                if (Hits[i].collider.transform.IsChildOf(transform))
                    continue;
                if (nearest < 0 || Hits[i].distance < Hits[nearest].distance)
                    nearest = i;
            }
            if (nearest < 0)
                return origin + direction * def.range;

            var hit = Hits[nearest];
            WeaponHits.ServerApply(hit.collider, def, hit.point, direction, OwnerClientId);
            ImpactRpc(hit.point);
            return hit.point;
        }

        void ServerStartReload()
        {
            var def = Held;
            if (def == null || !def.UsesAmmo || Reloading.Value || Ammo.Value >= def.magazineSize)
                return;
            Reloading.Value = true;
            serverReloadDoneTime = Time.time + def.reloadTime;
        }

        void ServerUpdateReload()
        {
            if (!Reloading.Value || Time.time < serverReloadDoneTime)
                return;
            var def = Held;
            if (def != null)
                Ammo.Value = def.magazineSize;
            Reloading.Value = false;
        }

        bool ServerCanAct() =>
            !player.IsKnockedDown.Value && !(TryGetComponent(out Health h) && h.IsDead.Value);

        Vector3 DropPosition()
        {
            Vector3 forward = transform.forward;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            float distance = Physics.Raycast(origin, forward, out RaycastHit hit, 1.2f) ? hit.distance - 0.3f : 1.2f;
            return transform.position + forward * Mathf.Max(0.3f, distance);
        }

        // ---------- 表现 ----------

        [Rpc(SendTo.NotOwner)]
        void AttackFxRpc(Vector3 origin, Vector3 direction, Vector3 end)
        {
            var def = Held;
            if (def == null)
                return;
            PlayAttackFx(def, origin, direction, isOwnerPrediction: false, end);
        }

        [Rpc(SendTo.Everyone)]
        void ImpactRpc(Vector3 point)
        {
            CombatFx.Impact(point, WeaponKind.Hitscan);
        }

        void PlayAttackFx(WeaponDefinition def, Vector3 origin, Vector3 direction, bool isOwnerPrediction, Vector3 end = default)
        {
            kick = 1f;
            kickKind = def.kind;
            Vector3 from = muzzle != null ? muzzle.position : origin;

            switch (def.kind)
            {
                case WeaponKind.Hitscan:
                    if (isOwnerPrediction)
                    {
                        end = origin + direction * def.range;
                        int count = Physics.RaycastNonAlloc(origin, direction, Hits, def.range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                        float best = float.MaxValue;
                        for (int i = 0; i < count; i++)
                        {
                            if (!Hits[i].collider.transform.IsChildOf(transform) && Hits[i].distance < best)
                            {
                                best = Hits[i].distance;
                                end = Hits[i].point;
                            }
                        }
                    }
                    CombatFx.Tracer(from, end);
                    break;
                case WeaponKind.Projectile:
                    // 服务器上的那颗就是可见的，其余端各自生成表现用子弹
                    if (!IsServer)
                        Projectile.Launch(def, from, direction, transform, OwnerClientId, authoritative: false);
                    break;
            }
        }

        void AnimateAnchor()
        {
            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * (kickKind == WeaponKind.Melee ? 3.5f : 8f));
            float k = Mathf.Sin(kick * Mathf.PI * 0.5f);
            if (kickKind == WeaponKind.Melee)
            {
                // 挥动：从右上往左下
                weaponAnchor.localPosition = anchorBasePosition + new Vector3(-0.25f * k, -0.05f * k, 0.15f * k);
                weaponAnchor.localRotation = anchorBaseRotation * Quaternion.Euler(70f * k, -50f * k, 0f);
            }
            else
            {
                weaponAnchor.localPosition = anchorBasePosition + new Vector3(0f, 0.02f * k, -0.08f * k);
                weaponAnchor.localRotation = anchorBaseRotation * Quaternion.Euler(-12f * k, 0f, 0f);
            }
        }

        static Vector3 ApplySpread(Vector3 direction, float spreadDegrees)
        {
            if (spreadDegrees <= 0f)
                return direction;
            Vector2 r = Random.insideUnitCircle * spreadDegrees;
            return Quaternion.AngleAxis(r.x, Vector3.up) * Quaternion.AngleAxis(r.y, Vector3.right) * direction;
        }
    }
}
