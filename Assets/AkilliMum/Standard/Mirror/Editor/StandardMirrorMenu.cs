using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AkilliMum.Standard.Mirror
{
    public static class StandardMirrorMenu
    {
        [MenuItem("Tools / Akilli Mum / Mirror Legacy / Force Draw")]
        static void ForceDraw()
        {
            var mirrorManagers = new List<MirrorManager>();
            var objects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var gameObject in objects)
            {
                var found = gameObject.GetComponentsInChildren<MirrorManager>(true);
                if (found != null)
                    mirrorManagers.AddRange(found.ToList());
            }
            if (mirrorManagers.Count <= 0)
            {
                EditorUtility.DisplayDialog("Error", "Can not find any active MirrorManager in scene!", "OK");
                return;
            }

            foreach (var mirrorManager in mirrorManagers)
            {
                if (mirrorManager.IsEnabled)
                {
                    mirrorManager.InitializeMirror();
                    mirrorManager.Render();
                }
            }
        }
    }
}