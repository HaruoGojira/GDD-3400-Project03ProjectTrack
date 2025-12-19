using UnityEngine;

public class AIBigEnemy : MonoBehaviour
{
    // Enum for AI States
    public enum AIState
    {
        Idle,
        Chase,
        HeatSeakingUltimate,
        CombustUltimate
    }

    [Header("Health")]
    [SerializeField] int _Health = 500;
    [SerializeField] private Healthbar _healthbar; 
    private int _maxHealth;

    [Header("Navigation")]
    [SerializeField] float _ReNavigateInterval = 1f;

    [Header("Chase Settings")]
    [SerializeField] private float _ChaseSpeed = 5f;
    [SerializeField] private float _PreferredDistance = 8f;
    [SerializeField] private float _DistanceTolerance = 2f;
    [SerializeField] private float _ChaseBuffer = 1f; // The buffer time to wait before the chase state exits to the search state

    [Header("Attack Settings")]
    [SerializeField] private bool _AttackOnSight = true;
    [SerializeField] private float _InitialAttackInterval = 3f; // The time to wait before the first attack after entering the chase state
    float _bossAttackTimer = 0f;

    [Header("Ultimate Settings")]
    [SerializeField] float _heatSeakingAttackInterval = 5f;
    [SerializeField] float _combustAttackInterval = 15f;

    // Ultimate timers
    float _heatSeakingAttackTimer = 0f;
    float _combustAttackTimer = 0f;

    // Prefabs for ultimates
    [SerializeField] GameObject _heatSeakingProjectilePrefab;
    [SerializeField] GameObject _combustProjectilePrefab;

    //Combustion parameters
    [SerializeField] int _combustProjectileCount = 50;
    [SerializeField] float _combustSpreadAngle = 45f;
    [SerializeField] float _combustProjectileInterval = 0.1f;
    float _combustProjectileTimer = 0f;

    // Internal references
    PlayerController _player;
    AINavigation _navigation;
    AIPerception _perception;
    ShootMechanic _shootMechanic;

    // Internal state
    AIState _currentState = AIState.Idle;
    private float _stateTimer = 0f;

    #region Unity Lifecycle

    /// <summary>
    /// The Awake method is called when the script instance is being loaded
    /// </summary>
    private void Awake()
    {
        // Find the player, navigation, shoot mechanic, and perception components
        _player = FindFirstObjectByType<PlayerController>();
        _navigation = this.GetComponent<AINavigation>();
        _shootMechanic = this.GetComponent<ShootMechanic>();
        _perception = this.GetComponent<AIPerception>();

        // Initialize healthbar
        _maxHealth = _Health;

        if (_healthbar != null)
        {
            _healthbar.Initialize(_maxHealth);
        }

        // Start the agent in the idle state
        SwitchState(AIState.Idle);
    }

    // Update is called once per frame
    private void Update()
    {
        // Update the state timer
        _stateTimer += Time.deltaTime;

        // Update the attack timer
        _bossAttackTimer += Time.deltaTime;
        _heatSeakingAttackTimer += Time.deltaTime;
        _combustAttackTimer += Time.deltaTime;


        // Set the name of the agent to the current state for debugging purposes
        this.name = "BOSS STATE: " + _currentState;

        // Update the state of the agent
        switch (_currentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;

            case AIState.Chase:
                UpdateChase();
                break;

            case AIState.HeatSeakingUltimate:
                UpdateHeatSeakingUltimate();
                break;
            
            case AIState.CombustUltimate:
                UpdateCombustUltimate();
                break;

        }
    }

    #endregion

    #region State Updates

    /// <summary>
    /// This method switches the AI to a new state.
    /// </summary>
    /// <param name="newState"></param>
    private void SwitchState(AIState newState)
    {
        // Then set the new state
        _currentState = newState;
        _stateTimer = 0f;

        // boss attack timer reset when entering chase state
        if (newState == AIState.Chase)
        {
            _bossAttackTimer = 0f;
        }

    }

    #endregion

    #region State Updates

    /// <summary>
    /// This method updates the idle behavior of the AI.
    /// </summary>
    private void UpdateIdle()
    {
        // If we can see the player, start chasing.
        if (_perception.CanSeePlayer)
        {
            SwitchState(AIState.Chase);
        }

    }

    /// <summary>
    /// This method updates the chase behavior of the AI.
    /// </summary>
    private void UpdateChase()
    {
        float distanceToPlayer = Vector3.Distance(this.transform.position, _player.transform.position);

        if (_perception.CanSeePlayer)
        {
            // Update last known position whenever we see the player.
            //_lastKnownPlayerPosition = _player.transform.position;

            // Too far: move closer.
            if (distanceToPlayer > _PreferredDistance + _DistanceTolerance)
            {
                Debug.Log("TOO FAR, moving closer");
                Vector3 directionToPlayer = (_player.transform.position - this.transform.position).normalized;
                Vector3 targetPosition = _player.transform.position - directionToPlayer * _PreferredDistance;
                _navigation.SetDestination(targetPosition);
            }
            // Too close: back away to our preferred distance.
            else if (distanceToPlayer < _PreferredDistance - _DistanceTolerance)
            {
                Debug.Log("TOO CLOSE, moving away");
                Vector3 directionAway = (this.transform.position - _player.transform.position).normalized;
                Vector3 targetPosition = _player.transform.position + directionAway * _PreferredDistance;

                _navigation.SetDestination(targetPosition);
            }

            // Within target distance, stay still.
            else
            {
                Debug.Log("WITHIN TARGET DISTANCE, staying still");
            }


            // Perform the shoot action
            if (_AttackOnSight && _bossAttackTimer >= _InitialAttackInterval)
            {
                _shootMechanic.PerformShoot(_perception.GetPlayerCenterPosition());
                _bossAttackTimer = 0f; // Reset the attack timer
            }
        }
        else
        {
           // boss doesn't engage
           _navigation.SetDestination(this.transform.position);
        }

        // Check for ultimate conditions
        if (_heatSeakingAttackTimer >= _heatSeakingAttackInterval)
        {
            SwitchState(AIState.HeatSeakingUltimate);
            return;
        }
        else if (_combustAttackTimer >= _combustAttackInterval)
        {
            SwitchState(AIState.CombustUltimate);
            return;
        }

    }

    /// <summary>
    /// Heat-Seaking Ultimate State Update
    /// </summary>
    void UpdateHeatSeakingUltimate()
    {
        // Timer for player to react
        if (_stateTimer < 1f)
            return;

        // Switches to heat-seaking attack
        GameObject projectile = Instantiate(_heatSeakingProjectilePrefab, _shootMechanic.ShootPoint.position, Quaternion.identity);

        // Initialize the projectile to home in on the player
        Vector3 directionToPlayer = (_player.transform.position - _shootMechanic.ShootPoint.position).normalized;
        projectile.GetComponent<HeatSeakingProjectile>().InitializeProjectile(directionToPlayer, this.tag);

        // Reset the timer and switch back to chase state
        _heatSeakingAttackTimer = 0f;
        SwitchState(AIState.Chase);

    }

    /// <summary>
    /// Combust Ultimate State Update
    /// </summary>
    void UpdateCombustUltimate()
    {
       //Timer for player to react
        if (_stateTimer < 1f)
            return;

        //timer for projectile interval
        _combustProjectileTimer += Time.deltaTime;

        // Launches combust projectiles after the timer
        if (_combustProjectileTimer > _combustProjectileInterval)
        {
            //Aims at the player position
            Vector3 _directionToPlayer = (_player.transform.position - _shootMechanic.ShootPoint.position).normalized;
            this.transform.rotation = Quaternion.LookRotation(_directionToPlayer);

            // Launches all combust projectiles in a spread pattern
            for (int i = 0; i < _combustProjectileCount; i++)
            {
                // Calculate spread angle for each projectile
                float _angle = -_combustSpreadAngle / 2 + (_combustSpreadAngle / (_combustProjectileCount - 1)) * i;
                Quaternion _rotation = Quaternion.AngleAxis(_angle, Vector3.up);
                Vector3 _spreadDirection = _rotation * _directionToPlayer;

                //Offset the projectile spawn position slightly to avoid overlap
                Vector3 _spawnPosition = _shootMechanic.ShootPoint.position;
                _spawnPosition.y += Random.Range(- 2.1f, 2.1f);

                // Instantiate and initialize projectile
                GameObject _projectile = Instantiate(_combustProjectilePrefab, _spawnPosition, Quaternion.identity);
                _projectile.GetComponent<Projectile>().InitializeProjectile(_spreadDirection, this.tag);
            }

            // Reset the projectile timer
            _combustProjectileTimer = 0f;
        }

        // Reset the timer and switch back to chase state
        if (_stateTimer >= 2f)
        {
            _combustAttackTimer = 0f;
            SwitchState(AIState.Chase);
        }
    }

    #endregion

    /// <summary>
    /// If the enemy takes damage, reduce health and check for death.
    /// </summary>
    /// <param name="damage"></param>
    public void TakeDamage(int damage)
    {
        // Reduce health
        _Health -= damage;

        //set up healthbar
        if (_healthbar != null)
        {
            _healthbar.SetHealth(_Health);
        }

        // Check for death
        if (_Health <= 0)
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// if the big enemy walks into breakable object, destroy it
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Breakable"))
        {
            Destroy(other.gameObject);
        }
    }

}
