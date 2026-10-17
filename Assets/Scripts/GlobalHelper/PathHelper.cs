using System.IO;
using UnityEngine;

public static class PathHelper
{
    public static string FixResourcePath(string rawPath)
    {
        if (string.IsNullOrEmpty(rawPath)) return "";

        string path = rawPath.Replace('\\', '/');

        // Nếu lỡ trong JSON ghi Assets/Resources/... thì cắt nó đi
        // Resources.Load chỉ cần đoạn sau chữ Resources/
        if (path.Contains("Resources/"))
        {
            int resIndex = path.IndexOf("Resources/");
            path = path.Substring(resIndex + "Resources/".Length);
        }
        else if (path.StartsWith("Assets/")) // Trường hợp ông lỡ ghi Assets/...
        {
            // Cái này không chạy được đâu, nhưng cứ cắt cho sạch
            Debug.LogError("Lỗi Path: File phải nằm trong thư mục Resources! Path hiện tại: " + rawPath);
            return "";
        }

        // Cắt đuôi file (extension)
        int lastDot = path.LastIndexOf('.');
        if (lastDot > -1)
        {
            path = path.Substring(0, lastDot);
        }

        return path;
    }
}