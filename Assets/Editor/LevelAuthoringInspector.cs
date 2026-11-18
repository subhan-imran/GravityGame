using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GravityGame.Editor
{
    [CustomEditor(typeof(LevelAuthoring))]
    public sealed class LevelAuthoringInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Edit planets, pickups, the Player Rig and Mobile Interface directly in the Hierarchy. New active children are registered automatically on Play. Refresh to see references before playing.", MessageType.Info);
            DrawDefaultInspector();
            var level = (LevelAuthoring)target;
            if (GUILayout.Button("Refresh Level References"))
            {
                Undo.RegisterFullObjectHierarchyUndo(level.gameObject, "Refresh level bindings");
                level.RefreshReferences();
                foreach (var item in level.GetComponentsInChildren<Component>(true))
                    if (item != null && PrefabUtility.IsPartOfPrefabInstance(item)) PrefabUtility.RecordPrefabInstancePropertyModifications(item);
                EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
            }
            if (GUILayout.Button("Validate Level"))
            {
                try { PrefabAuthoring.Validate(level); Debug.Log("Level references are valid.", level); }
                catch (System.Exception error) { Debug.LogError(error.Message, level); }
            }
            if (GUILayout.Button("Duplicate This Level")) PrefabAuthoring.DuplicateLevel();
        }
    }

    [CustomEditor(typeof(CollectibleStar)), CanEditMultipleObjects]
    public sealed class CollectibleStarInspector : UnityEditor.Editor
    {
        private float height = 1.35f;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            height = EditorGUILayout.FloatField("Height above surface", height);
            if (!GUILayout.Button("Snap Selected Stars To Their Planet")) return;
            foreach (CollectibleStar star in targets)
            {
                if (star.planet == null) { Debug.LogWarning("Assign this star's Planet first.", star); continue; }
                Undo.RecordObject(star.transform, "Snap star to surface");
                Vector3 up = star.planet.UpAt(star.transform.position);
                star.transform.SetPositionAndRotation(star.planet.transform.position + up * (star.planet.Radius + height),
                    Quaternion.FromToRotation(Vector3.up, up));
                if (PrefabUtility.IsPartOfPrefabInstance(star.transform)) PrefabUtility.RecordPrefabInstancePropertyModifications(star.transform);
            }
        }
    }

    [CustomEditor(typeof(MobileGameUI))]
    public sealed class MobileGameUIInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Edit static text, images, fonts and RectTransforms in the Canvas. Edit dynamic HUD and tutorial text/colors in the Settings asset. Authored Layout prevents creation helpers from overwriting this interface.", MessageType.Info);
            DrawDefaultInspector();
            var ui = (MobileGameUI)target;
            if (ui.settings != null && GUILayout.Button("Select Dynamic UI Settings")) Selection.activeObject = ui.settings;
            if (!ui.HasRequiredReferences()) EditorGUILayout.HelpBox("One or more required UI bindings are missing. Assign references before playing.", MessageType.Error);
        }
    }

    [CustomEditor(typeof(PlanetGravity)), CanEditMultipleObjects]
    public sealed class PlanetGravityInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var planet = (PlanetGravity)target;
            EditorGUI.BeginChangeCheck();
            float radius = EditorGUILayout.FloatField("Surface Radius", planet.Radius);
            if (EditorGUI.EndChangeCheck() && radius > 0f)
            {
                foreach (PlanetGravity source in targets)
                {
                    Undo.RecordObject(source.transform, "Resize planet");
                    if (source.Radius > 0f) source.transform.localScale *= radius / source.Radius;
                    if (PrefabUtility.IsPartOfPrefabInstance(source.transform)) PrefabUtility.RecordPrefabInstancePropertyModifications(source.transform);
                    var boundary = source.GetComponent<GravityBoundary>();
                    if (boundary != null) boundary.Refresh();
                }
            }
            EditorGUILayout.HelpBox("Move the planet in the Scene view. Keep its scale uniform. After resizing, reposition or snap its stars to the surface. Edit its MeshRenderer material to change appearance.", MessageType.Info);
        }
    }
}
