using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;


[CustomEditor(typeof(ProcGenManager))]
public class ProcGenManagerEditor: Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProcGenManager targetManager = serializedObject.targetObject as ProcGenManager;

        if (GUILayout.Button("Regenerate"))
        {
            targetManager.RegenerateWorld();
        }

        if (GUILayout.Button("Adjust Initial Player Location"))
        {
            FirstPersonPlayerController playerController = FindFirstObjectByType<FirstPersonPlayerController>();
            if (playerController == null)
            {
                Debug.LogWarning("No FirstPersonPlayerController found in the active scene.");
                return;
            }

            Camera sceneCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            Undo.RegisterFullObjectHierarchyUndo(playerController.gameObject, "Adjust Initial Player Location");
            if (sceneCamera != null && sceneCamera.gameObject != playerController.gameObject)
            {
                Undo.RegisterFullObjectHierarchyUndo(sceneCamera.gameObject, "Adjust Initial Player Location");
            }

            playerController.AdjustInitialPlayerLocation();
            EditorUtility.SetDirty(playerController);
            EditorSceneManager.MarkSceneDirty(playerController.gameObject.scene);
        }
    }
}
