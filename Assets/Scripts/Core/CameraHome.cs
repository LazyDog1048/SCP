using UnityEngine;

namespace ScpParty
{
    /// <summary>
    /// 记录主相机在场景中的初始位置（菜单俯瞰视角），本地玩家离开后相机回到这里。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraHome : MonoBehaviour
    {
        Vector3 homePosition;
        Quaternion homeRotation;

        void Awake()
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        public static void Restore(Transform cameraTransform)
        {
            if (cameraTransform != null && cameraTransform.TryGetComponent(out CameraHome home))
                cameraTransform.SetPositionAndRotation(home.homePosition, home.homeRotation);
        }
    }
}
