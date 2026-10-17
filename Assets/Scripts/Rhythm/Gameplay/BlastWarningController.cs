using UnityEngine;
using RhythmCore.Logic; // Chứa Conductor

namespace RhythmCore.Gameplay
{
    public class BlastWarningController : MonoBehaviour
    {
        private double _startBeat;    
        private double _targetBeat;   
        private Vector3 _startScale = new Vector3(3f, 3f, 1f);
        private Vector3 _endScale; 

        public void Initialize(double startBeat, double targetBeat, float targetScale)
        {
            _startBeat = startBeat;
            _targetBeat = targetBeat;

            _endScale = new Vector3(targetScale, targetScale, 1f);
            _startScale = _endScale * 3f;

            transform.localScale = _startScale;
        }

        void Update()
        {
            double currentBeat = Conductor.Instance.CurrentBeat;

            // Progress = (Hiện tại - Bắt đầu) / (Tổng thời gian)
            float t = (float)((currentBeat - _startBeat) / (_targetBeat - _startBeat));

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.localScale = Vector3.Lerp(_startScale, _endScale, t);
        }
    }
}