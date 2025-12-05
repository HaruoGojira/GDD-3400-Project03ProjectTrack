using UnityEngine;

public class HeatSeakingProjectile : MonoBehaviour
{
    //variables for the projectile
    [SerializeField] float _Speed = 12f;
    [SerializeField] float _InitialColliderDelay = 0.2f; // This is the delay before the collider is enabled
    [SerializeField] float _RotationSpeed = 6f;
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
    Rigidbody _rb;
    Transform _target;

    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    void Awake()
    {
        _rb = this.GetComponent<Rigidbody>();
    }

    // Initialize the projectile
    public void InitializeProjectile(Vector3 direction, string parentTag)
    {
        //calls parent tag
        _parentTag = parentTag;
        //finds the target with the "Player" tag
        _target = GameObject.FindGameObjectWithTag("Player").transform;
        // aims at the player's position
        Vector3 _initialDirection = (_target.position - this.transform.position).normalized;
        this.transform.rotation = Quaternion.LookRotation(_initialDirection);

        // Set the initial velocity
        _rb.velocity = _initialDirection * _Speed;

        // Enable the collider after the initial delay
        Invoke("EnableCollider", _InitialColliderDelay);

        // Destroy the projectile after the lifetime
        Invoke("DestroyProjectile", _LifeTime);

        // Keep track of the start position and parent tag
        _startPosition = this.transform.position;
    }

    void EnableCollider()
    {
        _Collider.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        // If there is a target, rotate towards it
        if (_target != null)
        {
            // Calculate the direction to the target
            Vector3 directionToTarget = (_target.position - this.transform.position).normalized;
            // Calculate the rotation step
            float step = _RotationSpeed * Time.deltaTime;
            // Rotate the projectile towards the target
            Vector3 newDirection = Vector3.RotateTowards(this.transform.forward, directionToTarget, step, 0.0f);
            this.transform.rotation = Quaternion.LookRotation(newDirection);
            // Update the velocity to match the new forward direction
            _rb.velocity = this.transform.forward * _Speed;
        }
    }

    /// <summary>
    /// this function is called when the projectile collides with another collider
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerEnter(Collider other)
    {
        // If the projectile hits the parent, don't do anything
        if (other.gameObject.tag == _parentTag) return;

        //breaks objects with the "Breakable" tag
        if (other.gameObject.tag == "Breakable")
        {
            // Send a message to break the object
            other.gameObject.SendMessageUpwards("BreakObject", SendMessageOptions.DontRequireReceiver);
            // destroys the object with the "Breakable" tag
            Destroy(other.gameObject);
        }

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

    /// <summary>
    /// this function applies explosion force to nearby rigidbodies
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

        // breaks objects with the "Breakable" tag within the explosion radius
        foreach (var col in colliders)
        {
            if (col.gameObject.tag == "Breakable")
            {
                col.gameObject.SendMessageUpwards("BreakObject", SendMessageOptions.DontRequireReceiver);
            }
        }

    }

    // Destroy the projectile
    void DestroyProjectile()
    {
        Destroy(this.gameObject);
    }
}
