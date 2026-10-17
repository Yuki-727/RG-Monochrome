#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MainLobby.Data;

namespace MainLobby.Editor
{
    public class AutoAnimScanner : EditorWindow
    {
        private InteractionVisualData targetSO;
        private string animFolderPath = "Assets/Anim/MainLobby/Animations/Clips";

        [MenuItem("Tools/MainLobby/Auto Scan Animations")]
        public static void ShowWindow()
        {
            GetWindow<AutoAnimScanner>("Auto Anim Scanner");
        }

        private void OnGUI()
        {
            GUILayout.Label("SCAN CONFIG", EditorStyles.boldLabel);
            targetSO = (InteractionVisualData)EditorGUILayout.ObjectField("Target SO", targetSO, typeof(InteractionVisualData), false);
            animFolderPath = EditorGUILayout.TextField("Anim Folder", animFolderPath);

            EditorGUILayout.Space();

            if (GUILayout.Button("SCAN NOW", GUILayout.Height(40)))
            {
                ScanAndImport();
            }
        }

        private void ScanAndImport()
        {
            if (targetSO == null)
            {
                EditorUtility.DisplayDialog("Error", "Chưa kéo file Main_VisualData vào kìa ông!", "OK");
                return;
            }

            // 1. Tìm file trong folder
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animFolderPath });

            // Dictionary tạm
            Dictionary<string, InteractionVisualData.InteractionClipSet> tempMap = new Dictionary<string, InteractionVisualData.InteractionClipSet>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                // Tên file: Hana_Sofa_Intro
                string[] parts = clip.name.Split('_');

                if (parts.Length < 3) continue; // Bỏ qua file đặt tên sai

                string npcID = parts[0];   // Hana
                string objID = parts[1];   // Sofa
                string type = parts[2].ToLower(); // intro/loop/outro

                string pairKey = $"{npcID}_{objID}";

                // [FIX LỖI CS1612 Ở ĐÂY]
                // Thay vì sửa trực tiếp, ta lấy ra -> sửa -> nhét lại
                if (!tempMap.ContainsKey(pairKey))
                {
                    tempMap[pairKey] = new InteractionVisualData.InteractionClipSet();
                }

                // Lấy bản copy ra biến tạm
                var currentSet = tempMap[pairKey];

                // Gán dữ liệu
                if (type.Contains("intro")) currentSet.introClip = clip;
                else if (type.Contains("loop")) currentSet.loopClip = clip;
                else if (type.Contains("outro")) currentSet.outroClip = clip;

                // Nhét ngược lại vào Dictionary
                tempMap[pairKey] = currentSet;
            }

            // 2. Ghi vào ScriptableObject
            // Clear dữ liệu cũ
            targetSO.visualPairs.Clear();

            // Đổ dữ liệu mới vào
            foreach (var kvp in tempMap)
            {
                string[] keys = kvp.Key.Split('_'); 

                var newPair = new InteractionVisualData.VisualPair();
                newPair.npcID = keys[0];
                newPair.objectID = keys[1];
                newPair.clipSet = kvp.Value;

                targetSO.visualPairs.Add(newPair);
            }

            // Lưu file
            EditorUtility.SetDirty(targetSO);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success", $"Đã import xong {targetSO.visualPairs.Count} bộ hành động!", "Ngon");
        }
    }
}
#endif