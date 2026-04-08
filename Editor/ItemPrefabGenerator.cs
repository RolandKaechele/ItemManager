#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ItemManager.Editor
{
    /// <summary>
    /// <b>ItemPrefabGenerator</b> reads <c>items.json</c> from <c>StreamingAssets/</c>
    /// (or a custom path) and auto-generates item pickup prefabs under
    /// <c>Assets/Resources/Items/</c>.
    ///
    /// <para>Open via <b>Generate Prefabs &gt; Item Prefabs from JSON</b>.</para>
    ///
    /// <para>Each prefab gets:
    /// <list type="bullet">
    ///   <item>A <see cref="ItemManager.Runtime.ItemPickup"/> component.</item>
    ///   <item>A <see cref="SphereCollider"/> set as trigger.</item>
    ///   <item>A placeholder <see cref="MeshRenderer"/> (sphere primitive) named after the definition id.</item>
    /// </list>
    /// The generated prefab path is written back into the definition's <c>prefabResource</c>
    /// field so it matches the expected <c>Resources.Load</c> path.
    /// </para>
    /// </summary>
    public class ItemPrefabGenerator : EditorWindow
    {
        private string _jsonPath    = "items.json";
        private string _outputPath  = "Assets/Resources/Items";
        private bool   _overwrite   = false;
        private Vector2 _scroll;

        private List<string> _log = new List<string>();

        [MenuItem("Generate Prefabs/Item Prefabs from JSON")]
        public static void ShowWindow()
            => GetWindow<ItemPrefabGenerator>("Item Prefab Generator");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Item Prefab Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _jsonPath   = EditorGUILayout.TextField(
                new GUIContent("JSON Path", "Path relative to StreamingAssets/"),
                _jsonPath);

            _outputPath = EditorGUILayout.TextField(
                new GUIContent("Output Path", "Assets/ relative path for generated prefabs"),
                _outputPath);

            _overwrite  = EditorGUILayout.Toggle(
                new GUIContent("Overwrite Existing", "Regenerate prefabs that already exist"),
                _overwrite);

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Generate Prefabs"))
                Generate();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(200));
            foreach (var line in _log)
                EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }

        private void Generate()
        {
            _log.Clear();

            string fullJsonPath = Path.Combine(Application.streamingAssetsPath, _jsonPath);
            if (!File.Exists(fullJsonPath))
            {
                _log.Add($"ERROR: JSON not found at '{fullJsonPath}'");
                Repaint();
                return;
            }

            string raw = File.ReadAllText(fullJsonPath);
            ItemManager.Runtime.ItemJsonWrapper wrapper;
            try { wrapper = JsonUtility.FromJson<ItemManager.Runtime.ItemJsonWrapper>(raw); }
            catch (System.Exception ex) { _log.Add($"ERROR: JSON parse failed — {ex.Message}"); Repaint(); return; }

            if (wrapper?.items == null || wrapper.items.Count == 0)
            {
                _log.Add("No item entries found in JSON.");
                Repaint();
                return;
            }

            // Ensure output directory
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
                AssetDatabase.Refresh();
            }

            int created = 0, skipped = 0;

            foreach (var entry in wrapper.items)
            {
                if (string.IsNullOrEmpty(entry.id)) continue;

                string prefabName = $"ItemPickup_{SanitizeName(entry.id)}";
                string assetPath  = $"{_outputPath.TrimEnd('/')}/{prefabName}.prefab";

                if (!_overwrite && File.Exists(assetPath))
                {
                    _log.Add($"Skipped (exists): {assetPath}");
                    skipped++;
                    continue;
                }

                // Build hierarchy
                var root   = new GameObject(prefabName);
                var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform);
                visual.transform.localScale = Vector3.one * 0.4f;

                // Remove boxcollider from primitive — add shared trigger on root
                var primCol = visual.GetComponent<SphereCollider>();
                if (primCol != null) DestroyImmediate(primCol);

                var col          = root.AddComponent<SphereCollider>();
                col.isTrigger    = true;
                col.radius       = entry.collectRadius > 0f ? entry.collectRadius : 1f;

                root.AddComponent<ItemManager.Runtime.ItemPickup>();

                // Save prefab
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                DestroyImmediate(root);

                if (prefab != null)
                {
                    _log.Add($"Created: {assetPath}");
                    created++;
                }
                else
                {
                    _log.Add($"FAILED: {assetPath}");
                }
            }

            AssetDatabase.Refresh();
            _log.Add($"Done — {created} created, {skipped} skipped.");
            Repaint();
        }

        private static string SanitizeName(string id)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in id)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            return sb.ToString();
        }
    }
}
#endif
