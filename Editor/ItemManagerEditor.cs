#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ItemManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="ItemManager.Runtime.ItemManager"/>.
    /// Adds runtime spawn controls, live instance view, and pause/resume buttons.
    /// </summary>
    [CustomEditor(typeof(ItemManager.Runtime.ItemManager))]
    public class ItemManagerEditor : UnityEditor.Editor
    {
        private string _spawnId   = "";
        private string _collectId = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Open JSON Editor")) ItemJsonEditorWindow.ShowWindow();

            var mgr = (ItemManager.Runtime.ItemManager)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use runtime controls.", MessageType.Info);
                return;
            }

            // Status
            EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Live Pickups", mgr.LiveCount);
            EditorGUILayout.Toggle("Interaction Paused", mgr.IsInteractionPaused);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);

            // Pause / Resume
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(mgr.IsInteractionPaused);
            if (GUILayout.Button("Pause Interaction"))  mgr.PauseInteraction();
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(!mgr.IsInteractionPaused);
            if (GUILayout.Button("Resume Interaction")) mgr.ResumeInteraction();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Spawn by definition id
            EditorGUILayout.LabelField("Spawn by Definition Id", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _spawnId = EditorGUILayout.TextField("Definition Id", _spawnId);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_spawnId));
            if (GUILayout.Button("Spawn", GUILayout.Width(70)))
                mgr.SpawnItem(_spawnId);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Collect by instance id
            EditorGUILayout.LabelField("Collect by Instance Id", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _collectId = EditorGUILayout.TextField("Instance Id", _collectId);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_collectId));
            if (GUILayout.Button("Collect", GUILayout.Width(70)))
                mgr.CollectItem(_collectId);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Clear all
            if (GUILayout.Button("Clear ALL Pickups"))
                mgr.ClearAllPickups();

            EditorGUILayout.Space(4);

            // Definition list
            EditorGUILayout.LabelField("Registered Definitions", EditorStyles.miniBoldLabel);
            foreach (var id in mgr.GetAllDefinitionIds())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {id}");
                if (GUILayout.Button("Spawn", GUILayout.Width(60)))
                    mgr.SpawnItem(id);
                if (GUILayout.Button("Collect All", GUILayout.Width(80)))
                    mgr.CollectAllItems(id);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif
