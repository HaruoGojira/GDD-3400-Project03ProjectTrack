using UnityEngine;

public class Projectile : MonoBehaviour
    {
    [SerializeField] float _Speed = 10f;
    [SerializeField] float _InitialColliderDelay = 0.2f; // This is the delay before the collider is enabled
    [SerializeField] float _LifeTime = 10f;
    [SerializeField] int _Damage = 100;

    [Header("Explosion Settings")]
    [SerializeField] bool _ApplyExplosionForce = false;
    [SerializeField] float _ImpactRadius = 2f;
    [SerializeField] float _ImpactForce = 4f;
    [SerializeField] float _UpwardsModifier = .5f;

    [SerializeField] Collider _Collider;
    [SerializeField] ParticleSystem _ImpactParticles;

    Vector3 _startPosition;
    string _parentTag;

    // Initialize the projectile
    public void InitializeProjectile(Vector3 direction, string parentTag)
    {
        // Set the direction of the projectile and add force to it
        this.transform.forward = direction;
        this.GetComponent<Rigidbody>().AddForce(direction * _Speed, ForceMode.Impulse);

        // Enable the collider after the initial delay
        Invoke("EnableCollider", _InitialColliderDelay);

        // Destroy the projectile after the lifetime
        Invoke("DestroyProjectile", _LifeTime);

        // Keep track of the start position and parent tag
        _startPosition = this.transform.position;
        _parentTag = parentTag;
    }

    void EnableCollider()
    {
        _Collider.enabled = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // If the projectile hits the parent, don't do anything
        if (other.gameObject.tag == _parentTag) return;

        // If the projectile hits something else, play the impact particles and destroy the projectile
        if (_ImpactParticles != null)
        {
            Vector3 direction = (this.transform.position - _startPosition).normalized;
            ParticleSystem impactParticles = Instantiate(_ImpactParticles, this.transform.position - direction * 1f, Quaternion.identity);
            impactParticles.Play();
        }

        // Tell the parent to take damage
        other.gameObject.SendMessageUpwards("TakeDamage", _Damage, SendMessageOptions.DontRequireReceiver);

        if (_ApplyExplosionForce) Explode(this.transform.position);

        // Destroy the projectile
        DestroyProjectile();
    }

    public void Explode(Vector3 explosionPosition)
    {
        // Find nearby colliders
        Collider[] colliders = Physics.OverlapSphere(explosionPosition, _ImpactRadius);

        foreach (var col in colliders)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb == null) continue;

            rb.AddExplosionForce(
                _ImpactForce,
                explosionPosition,
                _ImpactRadius,
                _UpwardsModifier,
                ForceMode.Impulse
            );
        }
    }

    // Destroy the projectile
    void DestroyProjectile()
    {
        Destroy(this.gameObject);
    }
}
