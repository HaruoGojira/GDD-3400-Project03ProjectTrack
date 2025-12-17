using UnityEngine;
using Cinemachine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Cinemachine3rdPersonAim _ThirdPersonAim;
    [SerializeField] ShootMechanic _ShootMechanic;

    [Header("Health")]
    [SerializeField] private int _MaxHealth = 100;
    [SerializeField] private float _invincibilityTime = 0.2f;

    // varibles for health and death
    private int _currentHealth;
    private bool _IsDead = false;
    private float _invincibilityTimer = 0f;

    public void Awake()
    {
        // Initialize health
        _currentHealth = _MaxHealth;
    }

    public void Update()
    {
        if (_ThirdPersonAim == null || _ShootMechanic == null) return;

        // Pass in the aim target point to the shoot mechanic
        _ShootMechanic.AimTargetPoint = _ThirdPersonAim.AimTarget;

        // Start and stop the shoot action based on the shoot action input
        if (Mouse.current.leftButton.wasPressedThisFrame) PerformShoot();

        // Update invincibility timer
        if (_invincibilityTimer > 0)
        {
            _invincibilityTimer -= Time.deltaTime;
        }

        // check for death
        if (_IsDead) return;
    }
    private void PerformShoot()
    {
        // Perform the shoot action
        _ShootMechanic.PerformShoot(_ShootMechanic.AimTargetPoint);

        // Look at the aim target, this helps make the character look more natural when shooting
        this.transform.LookAt(_ThirdPersonAim.AimTarget);
        this.transform.rotation = Quaternion.Euler(0, this.transform.rotation.eulerAngles.y, 0);
    }

    public void TakeDamage(int damage)
    {
        // Handle player taking damage
        Debug.Log("Player took damage: " + damage);
        
        // Implement health reduction and death check here
        if (_IsDead) return;
        if (_invincibilityTimer > 0) return;

        // Reduce health
        _currentHealth -= damage;
        _invincibilityTimer = _invincibilityTime;

        Debug.Log("Player health: " + _currentHealth);

        // Check for death
        if (_currentHealth <= 0)
        {
            // call the die method
            Die();
        }

    }

    //private void Die()
    private void Die()
    {
        // Handle player death
        Debug.Log("Player has died");

        // implement death behavior here
        _IsDead = true;
        _currentHealth = 0;

        // For now, just reload the current scene
        StartCoroutine(RestartLevelAfterDelay(2f));
    }

    // This restarts the level
    private IEnumerator RestartLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}
