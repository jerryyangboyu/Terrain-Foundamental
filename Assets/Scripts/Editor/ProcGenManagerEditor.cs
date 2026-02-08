using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(ProcGenManager))]
public class ProcGenManagerEditor: Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Regenerate World"))
        {
            ProcGenManager targetManager = serializedObject.targetObject as ProcGenManager;
            targetManager.RegenerateWorld();
        }

        if (GUILayout.Button("Regenerate HeightMap Only"))
        {
            ProcGenManager targetManager = serializedObject.targetObject as ProcGenManager;
            targetManager.RegenerateHeightMapOnly();
        }
    }
}
