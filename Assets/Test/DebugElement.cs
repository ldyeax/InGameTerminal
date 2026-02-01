using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InGameTerminal.Elements
{
    [ExecuteAlways]
    public class DebugElement : Element
    {
        private const string RootMenu = "GameObject/InGameTerminal/";
        [MenuItem(RootMenu + "Debug", false, 10)]
        public static void CreateDebugElement(MenuCommand cmd)
        {
            var go = new GameObject("DebugElement", typeof(DebugElement));
            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Debug Element");
            Selection.activeObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        public float UV_X_Numerator = 1.0f;
        public float UV_X_Denominator = 32.0f;
        public float UV_Y_Numerator = 2.0f;
        public float UV_Y_Denominator = 12.0f;
        public float UV_X_2_Numerator = 2.0f;
        public float UV_X_2_Denominator = 32.0f;
        public float UV_Y_2_Numerator = 3.0f;
        public float UV_Y_2_Denominator = 12.0f;

        public float UV_X => UV_X_Numerator / UV_X_Denominator;
        public float UV_Y => UV_Y_Numerator / UV_Y_Denominator;
        public float UV_X_2 => UV_X_2_Numerator / UV_X_2_Denominator;
        public float UV_Y_2 => UV_Y_2_Numerator / UV_Y_2_Denominator;
    }
}
