#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ItemManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace ItemManager.Editor
{
    // ────────────────────────────────────────────────────────────────────────────
    // Item World JSON Editor Window
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Editor window for creating and editing <c>items.json</c> in StreamingAssets.
    /// Open via <b>JSON Editors → Item Manager</b> or via the Manager Inspector button.
    /// </summary>
    public class ItemJsonEditorWindow : EditorWindow
    {
        private const string JsonFolderName   = "items";
        private const string JsonSaveFileName = "items.json";

        private ItemEditorBridge         _bridge;
        private UnityEditor.Editor       _bridgeEditor;
        private Vector2                  _scroll;
        private string                   _status;
        private bool                     _statusError;

        [MenuItem("JSON Editors/Item Manager")]
        public static void ShowWindow() =>
            GetWindow<ItemJsonEditorWindow>("World Items JSON");

        private void OnEnable()
        {
            _bridge = CreateInstance<ItemEditorBridge>();
            Load();
        }

        private void OnDisable()
        {
            if (_bridgeEditor != null) DestroyImmediate(_bridgeEditor);
            if (_bridge      != null) DestroyImmediate(_bridge);
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusError ? MessageType.Error : MessageType.Info);

            if (_bridge == null) return;
            if (_bridgeEditor == null)
                _bridgeEditor = UnityEditor.Editor.CreateEditor(_bridge);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _bridgeEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(
                $"StreamingAssets/{JsonFolderName}/",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50))) Load();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
            EditorGUILayout.EndHorizontal();
        }

        private void Load()
        {
            string folderPath = Path.Combine(Application.streamingAssetsPath, JsonFolderName);
            try
            {
                var list = new List<ItemWorldJson>();
                if (Directory.Exists(folderPath))
                {
                    foreach (var file in Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var w = JsonUtility.FromJson<ItemJsonWrapper>(File.ReadAllText(file));
                        if (w?.items != null) list.AddRange(w.items);
                    }
                }
                else
                {
                    Directory.CreateDirectory(folderPath);
                    File.WriteAllText(Path.Combine(folderPath, JsonSaveFileName), JsonUtility.ToJson(new ItemJsonWrapper(), true));
                    AssetDatabase.Refresh();
                }
                _bridge.items = list;
                if (_bridgeEditor != null) { DestroyImmediate(_bridgeEditor); _bridgeEditor = null; }
                _status = $"Loaded {list.Count} items from {JsonFolderName}/.";
                _statusError = false;
            }
            catch (Exception e) { _status = $"Load error: {e.Message}"; _statusError = true; }
        }

        private void Save()
        {
            try
            {
                string folderPath = Path.Combine(Application.streamingAssetsPath, JsonFolderName);
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                var w = new ItemJsonWrapper { items = new List<ItemWorldJson>(_bridge.items) };
                var path = Path.Combine(folderPath, JsonSaveFileName);
                File.WriteAllText(path, JsonUtility.ToJson(w, true));
                AssetDatabase.Refresh();
                _status = $"Saved {_bridge.items.Count} items to {JsonFolderName}/{JsonSaveFileName}.";
                _statusError = false;
            }
            catch (Exception e) { _status = $"Save error: {e.Message}"; _statusError = true; }
        }
    }

    // ── ScriptableObject bridge ──────────────────────────────────────────────
    internal class ItemEditorBridge : ScriptableObject
    {
        public List<ItemWorldJson> items = new List<ItemWorldJson>();
    }
}
#endif
