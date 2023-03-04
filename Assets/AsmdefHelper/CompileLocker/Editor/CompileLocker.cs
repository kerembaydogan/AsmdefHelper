using UnityEngine;
using UnityEditor;

namespace AsmdefHelper.CompileLocker.Editor
{
    public static class CompileLocker
    {
        private const string MenuPath = "AsmdefHelper/Compile Lock";


        [MenuItem(MenuPath, false, 1)]
        private static void Lock()
        {
            var isLocked = Menu.GetChecked(MenuPath);
            if (isLocked)
            {
                Debug.Log("Compile Unlocked");
                EditorApplication.UnlockReloadAssemblies();
                Menu.SetChecked(MenuPath, false);
            }
            else
            {
                Debug.Log("Compile Locked");
                EditorApplication.LockReloadAssemblies();
                Menu.SetChecked(MenuPath, true);
            }
        }
    }
}