using UnityEngine;

namespace ScpParty
{
    /// <summary>
    /// 出生点集合：子物体即出生点，按玩家编号轮流分配。
    /// </summary>
    public class SpawnPoints : MonoBehaviour
    {
        static SpawnPoints instance;

        void Awake() => instance = this;

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static Transform Get(ulong index)
        {
            if (instance == null || instance.transform.childCount == 0)
                return null;
            return instance.transform.GetChild((int)(index % (ulong)instance.transform.childCount));
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in transform)
            {
                Gizmos.DrawWireSphere(point.position + Vector3.up * 0.9f, 0.4f);
                Gizmos.DrawLine(point.position + Vector3.up, point.position + Vector3.up + point.forward);
            }
        }
    }
}
