using MainLobby.UI;
using RhythmCore.Data; // Namespace chứa ChartData của ông
using System.Collections.Generic;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace MainLobby.Data
{
    public class SongLibraryManager : MonoBehaviour
    {
        public static SongLibraryManager Instance;

        // Đây là cái List tổng mà LeftZone sẽ đọc
        public List<SongEntry> Library = new List<SongEntry>();

        private void Awake()
        {
            Instance = this;
            LoadAllSongs();
        }

        private void LoadAllSongs()
        {
            Library.Clear();
            Dictionary<string, SongEntry> tempDict = new Dictionary<string, SongEntry>();

            // Load file
            TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>("Charts");
            Debug.Log($"[SongLibrary] Tìm thấy {jsonFiles.Length} file JSON trong Resources/Charts");

            foreach (var file in jsonFiles)
            {
                try
                {
                    ChartData chart = JsonUtility.FromJson<ChartData>(file.text);

                    // --- DEBUG QUAN TRỌNG ---
                    // In ra ID thô từ file để xem có ông nào bị lệch không
                    // Debug.Log($"[File Scan] File: {file.name} -> ID trong JSON: '{chart.metadata.id}'"); 

                    string id = chart.metadata.id;

                    chart.metadata.difficultyType = chart.metadata.difficultyType.ToLower();
                    string diff = chart.metadata.difficultyType;

                    if (!tempDict.ContainsKey(id))
                    {
                        SongEntry newEntry = new SongEntry { SongID = id };

                        newEntry.JsonFileName = file.name;

                        tempDict.Add(id, newEntry);
                    }

                    tempDict[id].Charts[diff] = chart;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Lỗi đọc file {file.name}: {e.Message}");
                }
            }

            foreach (var entry in tempDict.Values)
            {
                Library.Add(entry);
            }

            // --- CHỐT SỔ ---
            Debug.LogWarning($"[SongLibrary] TỔNG KẾT: Load được {Library.Count} bài hát (Unique ID).");
            for (int i = 0; i < Library.Count; i++)
            {
                Debug.LogWarning($"[SongLibrary] Bài {i}: ID='{Library[i].SongID}' | Tên='{Library[i]}'");
            }
        }

        // Helper để lấy list bài hát (đã sort nếu cần)
        public List<SongEntry> GetSongList()
        {
            return Library;
        }
    }
}