using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ClippedRenderer))]
public class ClippedRendererEditor : Editor {
    public override void OnInspectorGUI() {
        serializedObject.Update();

        bool needsRepaint = false;
        ClippedRenderer cr = (ClippedRenderer)target;

        EditorGUI.BeginChangeCheck();
        cr.material = (Material)EditorGUILayout.ObjectField("Material", cr.material, typeof(Material), false);
        
        EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
        cr.shareMaterialProperties = EditorGUILayout.Toggle(new GUIContent("Share Properties", "Whether the script should set the properties on the original material"), cr.shareMaterialProperties);
        cr.useWorldSpace = EditorGUILayout.Toggle(new GUIContent("Use World Space", "Draw the clip plane in world space"), cr.useWorldSpace);

        EditorGUILayout.LabelField("Plane", EditorStyles.boldLabel);
        cr.planeNormal = EditorGUILayout.Vector3Field("Normal", cr.planeNormal);
        cr.planePoint = EditorGUILayout.Vector3Field("Point", cr.planePoint);
        cr.planeVector = EditorGUILayout.Vector4Field("Vector", cr.planeVector);

        EditorGUILayout.LabelField("Shadows", EditorStyles.boldLabel);
        
        cr.castShadows = EditorGUILayout.Toggle("Cast Shadows", cr.castShadows);

        int count = serializedObject.FindProperty("_shadowCastingLights").arraySize;
        EditorGUILayout.LabelField("Casting from " + count + " light" + (count == 1 ? "" : "s"));
        if (GUILayout.Button("Gather Shadowing Lights")) {
            cr.SetLights(FindObjectsOfType<Light>());
            needsRepaint = true;
        }
        if (GUILayout.Button("Clear Shadowing Lights")) {
            cr.ClearLights();
            needsRepaint = true;
        }

        if (EditorGUI.EndChangeCheck() || needsRepaint) RepaintAll();
    }
    
    void RepaintAll() {
        // Repaint all views, including the game and scene editor view
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
}
