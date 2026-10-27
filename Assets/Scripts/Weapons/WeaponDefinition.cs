using UnityEngine;

namespace ScpParty.Weapons
{
    public enum WeaponKind
    {
        Melee,
        Hitscan,
        Projectile,
    }

    /// <summary>武器参数。新增武器：建一个资源并加入 WeaponDatabase。</summary>
    [CreateAssetMenu(menuName = "SCP/Weapon Definition", fileName = "Weapon")]
    public class WeaponDefinition : ScriptableObject
    {
        public string displayName = "武器";
        public WeaponKind kind;
        [Tooltip("手持与地上展示用的模型（不含网络组件和碰撞体）")]
        public GameObject model;

        [Header("通用")]
        public int damage = 20;
        public float knockback = 6f;
        public float knockbackUp = 2f;
        public bool knockDownOnHit;
        [Tooltip("两次攻击的最小间隔（秒）")]
        public float fireInterval = 0.5f;
        [Tooltip("按住连发")]
        public bool automatic;
        public float range = 2.2f;

        [Header("近战")]
        [Tooltip("前方扇形角度")]
        public float meleeAngle = 100f;

        [Header("枪械（弹匣 0 表示不用子弹）")]
        public int magazineSize;
        public float reloadTime = 1.2f;
        [Tooltip("散布角度（度）")]
        public float spread;

        [Header("实体子弹")]
        public GameObject projectileModel;
        public float projectileSpeed = 25f;
        public float projectileGravity = 9.81f;
        public float projectileRadius = 0.12f;
        public float projectileLifetime = 3f;

        public bool UsesAmmo => magazineSize > 0;
    }
}
