using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectile2D : MonoBehaviour
{
    [SerializeField] private bool rotateToTravelDirection = true;

    private Rigidbody2D _rigidbody2D;
    private Vector2 _direction = Vector2.right;
    private float _speed;
    private float _destroyTime;
    private bool _launched;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!_launched)
            return;

        if (Time.time >= _destroyTime)
        {
            Destroy(gameObject);
            return;
        }

        if (_rigidbody2D == null)
            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);

        if (rotateToTravelDirection && _direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
    }

    public void Launch(Vector2 direction, float speed, float lifetime)
    {
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _speed = Mathf.Max(0f, speed);
        _destroyTime = Time.time + Mathf.Max(0.1f, lifetime);
        _launched = true;

        if (_rigidbody2D != null)
            _rigidbody2D.linearVelocity = _direction * _speed;

        if (rotateToTravelDirection)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
    }
}