using UnityEngine;

namespace RhythmCore.Gameplay
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject tapHitPrefab;   
        public GameObject holdSparkPrefab; 

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void PlayTapVFX(Vector3 position)
        {
            if (tapHitPrefab == null) return;

            Instantiate(tapHitPrefab, position, Quaternion.identity);
        }

        public GameObject StartHoldVFX(Vector3 position, Transform parent)
        {
            if (holdSparkPrefab == null) return null;

            GameObject vfx = Instantiate(holdSparkPrefab, parent);
            vfx.transform.localPosition = Vector3.zero;
            return vfx;
        }

        public void StopHoldVFX(GameObject vfxInstance)
        {
            if (vfxInstance == null) return;

            var ps = vfxInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Cách dừng nhẹ nhàng: Tắt bộ phát hạt, để hạt cũ bay nốt rồi tắt
                var emission = ps.emission;
                emission.enabled = false;

                // (Tùy chọn) Tắt ánh sáng nếu có
                // var lights = vfxInstance.GetComponent<Light>(); ...
            }

            // Hủy object sau 1 giây (đủ thời gian cho hạt cũ biến mất)
            Destroy(vfxInstance, 1.0f);
        }
    }
}