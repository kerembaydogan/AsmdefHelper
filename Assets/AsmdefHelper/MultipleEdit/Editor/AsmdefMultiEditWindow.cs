﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;
using UnityEngine;

namespace AsmdefHelper.MultipleEdit.Editor {
    public class AsmdefMultiEditWindow : EditorWindow {
        [MenuItem("Window/Asmdef Helper/Find all asmdef in project")]
        public static void Search() {
            var projectBrowser = GetWindow<ProjectBrowser>();
            projectBrowser.SetSearch("t:AssemblyDefinitionAsset");
        }

        [MenuItem("Window/Asmdef Helper/Open selected asmdef inspector view")]
        public static void Open() {
            var asmdefs = Selection.GetFiltered(typeof(AssemblyDefinitionAsset), SelectionMode.TopLevel);
            if (!asmdefs.Any()) {
                Debug.Log("no AssemblyDefinitionAsset");
                return;
            }

            foreach (var adf in asmdefs) {
                Selection.objects = new[] { adf };
                var w = CreateWindow<InspectorWindow>();
                // LockすることでInspectorWindowの表示を固定する
                w.isLocked = true;
            }
        }
    }
}
