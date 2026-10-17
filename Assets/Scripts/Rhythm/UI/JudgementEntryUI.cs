using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmCore.UI
{
    public class JudgementEntryUI : MonoBehaviour
    {
        public TMP_Text labelText; // Chữ "Perfect"
        public TMP_Text countText; // Số lượng "150"

        // Hàm này chỉ nhiệm vụ set số thôi
        public void SetCount(int count)
        {
            if (countText != null) countText.text = count.ToString();
        }
    }
}