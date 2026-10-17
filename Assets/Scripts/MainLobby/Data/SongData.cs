using UnityEngine;
using System.Collections.Generic;
// using RhythmCore.Data; // Nhớ dùng namespace chứa ChartData của ông

namespace MainLobby.Data
{
    // Enum định danh độ khó để dễ quản lý
    public enum DifficultyType { Easy, Normal, Hard, Special }

    [System.Serializable]
    public class DifficultyVariant
    {
        public DifficultyType type;   // Loại độ khó (để code dễ check)
        public string difficultyName; // Tên hiển thị (VD: "Future", "Beyond")
        public int difficultyLevel;   // Level: 9, 10, 10+
        public Color themeColor;      // Màu đặc trưng

        [Header("Chart Connection")]
        // Kéo file JSON (đã save từ Editor) vào đây. 
        // Khi chơi game, ta sẽ đọc text từ file này để Parse ra ChartData
        public TextAsset chartJsonFile;

        // Hoặc nếu ông dùng StreamingAssets thì dùng string path
        // public string jsonFileName; 
    }

    [CreateAssetMenu(fileName = "NewSong", menuName = "RhythmGame/Song Data")]
    public class SongData : ScriptableObject
    {
        [Header("Metadata")]
        public string songID;
        public string songName;
        public string artistName;
        public Sprite songCoverArt;

        [Header("Audio")]
        public AudioClip fullTrack;
        public float previewStartTime;
        public float previewDuration;

        [Header("Difficulty Configuration")]
        public List<DifficultyVariant> difficulties;

        // Hàm tiện ích: Lấy thông tin theo độ khó
        public DifficultyVariant GetDifficultyData(DifficultyType type)
        {
            return difficulties.Find(x => x.type == type);
        }
    }
}