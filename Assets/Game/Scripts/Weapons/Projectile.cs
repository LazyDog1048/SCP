using ScpParty.Combat;
using UnityEngine;

namespace ScpParty.Weapons
{
    /// <summary>
    /// 实体子弹（水弹）。不是网络对象：服务器上的那颗负责结算伤害，其他端只生成表现用的同轨迹子弹。
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        static readonly RaycastHit[] Hits = new RaycastHit[8];

        Vector3 velocity;
        float gravity;
        float radius;
        float dieAt;
        bool authoritative;
        Transform ignoreRoot;
        WeaponDefinition weapon;
        ulong attackerId;

        public static void Launch(WeaponDefinition def, Vector3 origin, Vector3 direction, Transform shooter, ulong attackerId, bool authoritative)
        {
            GameObject go = def.projectileModel != null
                ? Instantiate(def.projectileModel, origin, Quaternion.LookRotation(direction))
                : new GameObject("Projectile");
            go.transform.position = origin;
            var p = go.AddComponent<Projectile>();
            p.velocity = direction.normalized * def.projectileSpeed;
            p.gravity = def.projectileGravity;
            p.radius = def.projectileRadius;
            p.dieAt = Time.time + def.projectileLifetime;
            p.authoritative = authoritative;
            p.ignoreRoot = shooter;
            p.weapon = def;
            p.attackerId = attackerId;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            velocity += Vector3.down * gravity * dt;
            Vector3 step = velocity * dt;

            int count = Physics.SphereCastNonAlloc(transform.position, radius, step.normalized, Hits, step.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            int nearest = -1;
            for (int i = 0; i < count; i++)
            {
                if (ignoreRoot != null && Hits[i].collider.transform.IsChildOf(ignoreRoot))
                    continue;
                if (nearest < 0 || Hits[i].distance < Hits[nearest].distance)
                    nearest = i;
            }

            if (nearest >= 0)
            {
                var hit = Hits[nearest];
                Vector3 point = hit.point == Vector3.zero ? transform.position : hit.point;
                if (authoritative)
                    WeaponHits.ServerApply(hit.collider, weapon, point, velocity, attackerId);
                CombatFx.Impact(point, weapon.kind);
                Destroy(gameObject);
                return;
            }

            transform.position += step;
            if (velocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(velocity);
            if (Time.time >= dieAt)
                Destroy(gameObject);
        }
    }

    /// <summary>服务器端把武器命中转换成伤害。</summary>
    public static class WeaponHits
    {
        public static bool ServerApply(Collider collider, WeaponDefinition weapon, Vector3 point, Vector3 direction, ulong attackerId)
        {
            var health = collider.GetComponentInParent<Health>();
            if (health == null)
                return false;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
            health.ApplyDamage(new DamageInfo
            {
                Amount = weapon.damage,
                Point = point,
                Knockback = direction * weapon.knockback + Vector3.up * weapon.knockbackUp,
                KnockDown = weapon.knockDownOnHit,
                AttackerId = attackerId,
            });
            return true;
        }
    }
}
