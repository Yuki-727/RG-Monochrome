using RhythmCore.Data;
using System.Collections.Generic;
using System.Linq; // Để dùng LINQ sort/filter cho lẹ

namespace MainLobby.Data
{
    // Class này đại diện cho 1 bài hát (Chứa list các chart của nó)
    public class SongEntry
    {
        public string SongID; // ID chung để gom nhóm (VD: "freedom_dive")

        public string JsonFileName;

        // Dictionary lưu trữ chart theo độ khó
        // Key: Difficulty Level (hoặc Enum) -> Value: ChartData (đọc từ Json)
        public Dictionary<string, ChartData> Charts = new Dictionary<string, ChartData>();

        // Hàm lấy Metadata dựa trên độ khó đang chọn
        public ChartMetadata GetMetadata(string difficultyKey)
        {
            if (Charts.ContainsKey(difficultyKey))
            {
                return Charts[difficultyKey].metadata;
            }

            // Fallback: Nếu không tìm thấy độ khó Hard, lấy đại cái đầu tiên tìm thấy để hiển thị
            return Charts.Values.First().metadata;
        }

        // Hàm kiểm tra xem bài này có độ khó này không
        public bool HasDifficulty(string difficultyKey)
        {
            return Charts.ContainsKey(difficultyKey);
        }
    }
}