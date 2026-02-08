using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(ProcGenManager))]
public class ProcGenManagerEditor: Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Regenerate"))
        {
            ProcGenManager targetManager = serializedObject.targetObject as ProcGenManager;
            targetManager.RegenerateWorld();
        }
    }
}
