using UnityEngine;

namespace ScpParty
{
    /// <summary>
    /// 鼠标锁定即视为“在游戏中”：锁定时角色响应输入，解锁时（菜单 / Esc）忽略输入。
    /// </summary>
    public static class InputGate
    {
        public static bool GameplayEnabled => Cursor.lockState == CursorLockMode.Locked;

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
