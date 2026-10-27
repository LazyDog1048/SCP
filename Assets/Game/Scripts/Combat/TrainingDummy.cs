using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ScpParty.Combat
{
    /// <summary>
    /// 训练木桩：不会死，停手几秒后回满；受击时摇晃，头顶显示血量、最近一次伤害和 DPS。
    /// 显示与摇晃都在各端本地计算，数据来自 Health 的同步血量和受击广播。
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class TrainingDummy : NetworkBehaviour
    {
        [SerializeField] Transform wobblePivot;
        [SerializeField] Transform labelAnchor;
        [SerializeField] float wobbleStrength = 6f;
        [SerializeField] float maxWobbleAngle = 35f;
        [SerializeField] float spring = 60f;
        [SerializeField] float damping = 7f;
        [SerializeField] float dpsWindow = 3f;

        Health health;
        TextMesh label;
        Vector2 angle;      // x: 绕 X 轴，y: 绕 Z 轴
        Vector2 angularVelocity;
        int lastHit;
        readonly Queue<(float time, int amount)> recentHits = new();

        void Awake()
        {
            health = GetComponent<Health>();
        }

        public override void OnNetworkSpawn()
        {
            health.Damaged += OnDamaged;
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(labelAnchor, false);
            label = DamagePopup.CreateText(labelGo, "", Color.white, 0.035f);
        }

        public override void OnNetworkDespawn()
        {
            health.Damaged -= OnDamaged;
        }

        void OnDamaged(int amount, Vector3 point, Vector3 knockback, ulong attackerId)
        {
            Vector3 push = knockback;
            if (push.sqrMagnitude < 0.01f)
                push = (transform.position - point).normalized * 2f;
            push.y = 0f;
            Vector3 local = transform.InverseTransformDirection(push);
            angularVelocity += new Vector2(local.z, -local.x) * wobbleStrength * 10f;

            if (amount <= 0)
                return;
            lastHit = amount;
            recentHits.Enqueue((Time.time, amount));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            angularVelocity += (-angle * spring - angularVelocity * damping) * dt;
            angle += angularVelocity * dt;
            angle = Vector2.ClampMagnitude(angle, maxWobbleAngle);
            wobblePivot.localRotation = Quaternion.Euler(angle.x, 0f, angle.y);

            while (recentHits.Count > 0 && Time.time - recentHits.Peek().time > dpsWindow)
                recentHits.Dequeue();
            int total = 0;
            foreach (var hit in recentHits)
                total += hit.amount;

            if (label != null)
                label.text = $"HP {health.Current.Value}/{health.Max}\nLast {lastHit}   DPS {total / dpsWindow:0}";
        }
    }
}
