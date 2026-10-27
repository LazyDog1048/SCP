using UnityEngine;

namespace ScpParty.Weapons
{
    /// <summary>
    /// 武器表。网络上只同步下标，所以各端必须是同一份资源（放在 Resources/WeaponDatabase）。
    /// 调整顺序会改变已有下标，联机双方需使用相同版本。
    /// </summary>
    [CreateAssetMenu(menuName = "SCP/Weapon Database", fileName = "WeaponDatabase")]
    public class WeaponDatabase : ScriptableObject
    {
        public WeaponDefinition[] weapons;

        static WeaponDatabase instance;

        static WeaponDatabase Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<WeaponDatabase>("WeaponDatabase");
                return instance;
            }
        }

        public static int Count => Instance != null ? Instance.weapons.Length : 0;

        public static WeaponDefinition Get(int index)
        {
            var db = Instance;
            if (db == null || index < 0 || index >= db.weapons.Length)
                return null;
            return db.weapons[index];
        }
    }
}
