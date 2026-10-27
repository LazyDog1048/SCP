using UnityEngine;

namespace ScpParty.Combat
{
    /// <summary>一次伤害或击退。Amount 可以为 0（例如推人只有击退）。</summary>
    public struct DamageInfo
    {
        public int Amount;
        public Vector3 Point;
        public Vector3 Knockback;
        public bool KnockDown;
        public ulong AttackerId;
    }

    /// <summary>能被击退的对象（服务器端调用）。</summary>
    public interface IKnockbackReceiver
    {
        void ServerApplyKnockback(Vector3 impulse, bool knockDown);
    }
}
