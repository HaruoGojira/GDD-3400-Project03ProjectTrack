using UnityEngine;

public class Projectile : MonoBehaviour
    {
    [SerializeField] float _Speed = 10f;
    [SerializeField] float _InitialColliderDelay = 0.2f; // This is the delay before the collider is enabled
    [SerializeField] float _LifeTime = 10f;
    [SerializeField] int _Damage = 10;

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

    /// <summary>
    /// This method enables the collider
    /// </summary>
    void EnableCollider()
    {
        _Collider.enabled = true;
    }

    /// <summary>
    /// is called when the collider enters a trigger
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerEnter(Collider other)
    {
        // If the projectile hits the parent, don't do anything
        if (other.gameObject.tag == _parentTag) return;

        // If the projectile hits another projectile, don't do anything
        if (other.CompareTag("Projectile")) return;

        // If the projectile hits something else, play the impact particles and destroy the projectile
        if (_ImpactParticles != null)
        {
            Vector3 direction = (this.transform.position - _startPosition).normalized;
            ParticleSystem impactParticles = Instantiate(_ImpactParticles, this.transform.position - direction * 1f, Quaternion.identity);
            impactParticles.Play();
        }

        // getting components to help with health/damage and healthbars
        // Try to get the components of the enemies
        AIBigEnemy _bigEnemy = other.GetComponentInParent<AIBigEnemy>();
        AISmallEnemy smallEnemy = other.GetComponentInParent<AISmallEnemy>();

        // If an enemy component is found, apply damage
        if (_bigEnemy != null)
        {
            _bigEnemy.TakeDamage(_Damage);
        }
        else if (smallEnemy != null)
        {
            smallEnemy.TakeDamage(_Damage);
        }

        //get component for player
        else if (other.GetComponentInParent<PlayerController>() != null)
        {
            PlayerController _player = other.GetComponentInParent<PlayerController>();
            _player.TakeDamage(_Damage);
        }

        if (_ApplyExplosionForce) Explode(this.transform.position);

        // If the projectile can destroy breakables
        if (other.gameObject.CompareTag("Breakable"))
        {
            // Send a message to break the object
            other.gameObject.SendMessageUpwards("BreakObject", SendMessageOptions.DontRequireReceiver);
            // Destroys the object with the "Breakable" tag
            Destroy(other.gameObject);

            // if the player shoots a breakable, we don't want the Breakable object to break
            if (_parentTag == "Player")
            {
                return;
            }

        }

        // Destroy the projectile
        DestroyProjectile();
    }

    /// <summary>
    /// This method applies explosion force to nearby rigidbodies
    /// </summary>
    /// <param name="explosionPosition"></param>
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
