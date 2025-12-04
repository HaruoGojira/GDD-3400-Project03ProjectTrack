using UnityEngine;
using UnityEngine.AI;

public class AISmallEnemy : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Patrol,
        Chase,
        Search,
        HeatSeaking
    }

    [Header("Health")]
    [SerializeField] int _Health = 100;

    [Header("Navigation")]
    [SerializeField] float _ReNavigateInterval = 1f;

    [Header("Patrol Settings")]
    [SerializeField] private float _PatrolSpeed = 2f;
    [SerializeField] private Transform[] _PatrolPoints;     // Possible patrol/search locations
    [SerializeField] private float _IdleAtPatrolPointTime = 2f;

    [Header("Chase Settings")]
    [SerializeField] private float _ChaseSpeed = 5f;
    [SerializeField] private float _PreferredDistance = 8f;
    [SerializeField] private float _DistanceTolerance = 2f;
    [SerializeField] private float _ChaseBuffer = 1f; // The buffer time to wait before the chase state exits to the search state

    [Header("Search Settings")]
    [SerializeField] private float _TimeToRememberPlayer = 3f;

    [Header("Attack Settings")]
    [SerializeField] private bool _AttackOnSight = true;
    [SerializeField] private float _InitialAttackInterval = 1f; // The time to wait before the first attack after entering the chase state
    [SerializeField] private float _RegularAttackInterval = 2f; // Time between regular attacks
    [SerializeField] private float _regularAttackTimer = 0f;

    [Header("Ultimate Attack Settings")]
    [SerializeField] private float _HeatSeakingAttackInterval = 10f; // Time between ultimate attacks
    [SerializeField] private float _heatSeakingAttackTimer = 0f;
    [SerializeField] private GameObject _HeatSeakingProjectilePrefab; // The ultimate attack projectile prefab

    // Internal references
    PlayerController _player;
    AINavigation _navigation;
    AIPerception _perception;
    ShootMechanic _shootMechanic;

    // Internal state
    AIState _currentState = AIState.Idle;
    private float _stateTimer = 0f;
    private float _chaseBuffer = 0f;

    private Transform _currentPatrolPoint;
    private Vector3 _chaseStartPosition;
    private Vector3 _lastKnownPlayerPosition;

    #region Unity Lifecycle

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        // Find the player, navigation, shoot mechanic, and perception components
        _player = FindFirstObjectByType<PlayerController>();
        _navigation = this.GetComponent<AINavigation>();
        _shootMechanic = this.GetComponent<ShootMechanic>();
        _perception = this.GetComponent<AIPerception>();

        // Start the agent in the idle state
        SwitchState(AIState.Idle);
    }


    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    private void Update()
    {
        // Update the state timer
        _stateTimer += Time.deltaTime;

        //this is for the regular and ultimate attack timers
        _regularAttackTimer += Time.deltaTime;
        _heatSeakingAttackTimer += Time.deltaTime;

        // Set the name of the agent to the current state for debugging purposes
        this.name = "AI Agent: " + _currentState;

        // Update the state of the agent
        switch (_currentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;

            case AIState.Patrol:
                UpdatePatrol();
                break;

            case AIState.Chase:
                UpdateChase();
                break;

            case AIState.Search:
                UpdateSearch();
                break;
            case AIState.HeatSeaking:
                UpdateHeatSeaking();
                break;
        }
    }

    #endregion

    #region State Updates

    private void SwitchState(AIState newState)
    {

        // First call the exit state method to clean up the current state
        OnExitState(_currentState);

        // Then set the new state
        _currentState = newState;

        // Finally call the enter state method to initialize the new state
        OnEnterState(newState);
    }

    // Called once when the state is entered
    private void OnEnterState(AIState state)
    {
        _stateTimer = 0f;

        switch (state)
        {
            case AIState.Idle:
                break;

            case AIState.Patrol:
                _navigation.SetSpeed(_PatrolSpeed);
                PickNewPatrolPoint();
                break;

            case AIState.Chase:
                _chaseStartPosition = this.transform.position;
                _navigation.SetSpeed(_ChaseSpeed);
                break;

            case AIState.Search:
                _navigation.SetSpeed(_PatrolSpeed);
                _navigation.SetDestination(_lastKnownPlayerPosition, true);
                break;
            case AIState.HeatSeaking:
                _navigation.SetSpeed(0f); // Stop moving during ultimate attack
                break;
        }
    }

    // Called once when the state is exited
    private void OnExitState(AIState state)
    {
        switch (state)
        {
            case AIState.Idle:
                break;

            case AIState.Patrol:
                break;

            case AIState.Chase:
                break;

            case AIState.Search:
                break;
            case AIState.HeatSeaking:
                break;
        }
    }

    #endregion

    #region State Updates
    private void UpdateIdle()
    {
        // If we can see the player, start chasing.
        if (_perception.CanSeePlayer)
        {
            SwitchState(AIState.Chase);
        }

        // If player is not even in detection range, start searching.
        else
        {
            SwitchState(AIState.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        // If we see the player at any time, start chasing.
        if (_perception.CanSeePlayer)
        {
            SwitchState(AIState.Chase);
            return;
        }

        // If no search points, just go back to idle.
        if (_PatrolPoints == null || _PatrolPoints.Length == 0)
        {
            SwitchState(AIState.Idle);
            return;
        }

        // Make sure we have somewhere to go.
        if (_currentPatrolPoint == null)
        {
            PickNewPatrolPoint();
        }

        // If we've reached the destination, set the state timer to the idle time.
        if (_navigation.IsAtDestination())
        {
            _currentPatrolPoint = null;
            SwitchState(AIState.Idle);
        }
    }

    /// <summary>
    /// When enemy is in chase state
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
                _navigation.SetDestination(Vector3.Lerp(_player.transform.position, _chaseStartPosition, 0.5f));
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
                _navigation.SetDestination(this.transform.position);
            }

            // Perform the shoot action
            if (_AttackOnSight && _regularAttackTimer >= _RegularAttackInterval)
            {
                _shootMechanic.PerformShoot(_perception.GetPlayerCenterPosition());
                _regularAttackTimer = 0f; // Reset the regular attack timer
            }

            // Begin the ultimate attack if the timer has reached the interval
            if (_heatSeakingAttackTimer >= _HeatSeakingAttackInterval)
            {
                Debug.Log("Switching to Ultimate Attack State");
                SwitchState(AIState.HeatSeaking);
                return;
            }

        }
        else
        {
            _chaseBuffer += Time.deltaTime;

            // If we have been chasing for too long, start searching.
            if (_chaseBuffer >= _ChaseBuffer)
            {
                _lastKnownPlayerPosition = _player.transform.position;
                _chaseBuffer = 0f;

                SwitchState(AIState.Search);
            }
        }
    }

    private void UpdateSearch()
    {
        // If we have been searching for too long, go back to patrol.
        if (_stateTimer >= _TimeToRememberPlayer)
        {
            // Move to last known location.
            _navigation.SetDestination(_lastKnownPlayerPosition);

            // Once we arrive and still can't see them, go back to search.
            if (_navigation.IsAtDestination())
            {
                SwitchState(AIState.Patrol);
            }
        }

        // If we can see the player again, start chasing.
        if (_perception.CanSeePlayer)
        {
            SwitchState(AIState.Chase);
        }
    }

    // Ultimate attack state update
    private void UpdateHeatSeaking()
    {
        // Perform ultimate attack action
        if (_stateTimer >= 0.5f)
        {
            Debug.Log("Performing Ultimate Attack!");
            LaunchHeatSeakingAttack();
            // Here you would add the logic for the ultimate attack
            _heatSeakingAttackTimer = 0f; // Reset the ultimate attack timer
            SwitchState(AIState.Chase); // Return to chase state after ultimate attack
        }
    }

    /// <summary>
    /// When the ultimate attack is launched
    /// </summary>
    private void LaunchHeatSeakingAttack()
    {
        //checks if the ultimate projectile prefab is assigned
        if (_HeatSeakingProjectilePrefab == null)
        {
            Debug.LogWarning("Ultimate Projectile Prefab is not assigned!");
            return;
        }
        // Instantiate the ultimate projectile at the shoot point
        GameObject _heatSeakingProjectile = Instantiate(_HeatSeakingProjectilePrefab, _shootMechanic.ShootPoint.position, Quaternion.identity);
        // This will make sure the projectile doesn't kill the enemy that shot it
        Vector3 _directionToPlayer = (_perception.GetPlayerCenterPosition() - _shootMechanic.ShootPoint.position).normalized;
        _heatSeakingProjectile.GetComponent<HeatSeakingProjectile>().InitializeProjectile(_directionToPlayer, this.gameObject.tag);

    }


    #endregion

    private void PickNewPatrolPoint()
    {
        if (_PatrolPoints == null || _PatrolPoints.Length == 0)
        {
            return;
        }

        int index = Random.Range(0, _PatrolPoints.Length);
        _currentPatrolPoint = _PatrolPoints[index];

        _navigation.SetDestination(_currentPatrolPoint.position);
    }

    public void TakeDamage(int damage)
    {
        _Health -= damage;
        if (_Health <= 0)
        {
            Destroy(this.gameObject);
        }
    }
}
