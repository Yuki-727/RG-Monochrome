using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    private ParticleSystem _ps;

    void Start()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        // Nếu particle system đã dừng hoạt động (chạy hết lifetime)
        if (_ps != null && !_ps.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}