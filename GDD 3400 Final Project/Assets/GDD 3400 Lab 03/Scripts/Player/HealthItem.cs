using UnityEngine;

public class HealthItem : MonoBehaviour
{
    // variable for health item
    [SerializeField] private int _HealthAmount = 50;

    /// <summary>
    /// OnTriggerEnter is called when the collider enters a trigger
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the trigger is the player
        if (other.CompareTag("Player"))
        {
            // Get the PlayerController component from the player
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Heal the player
                playerController.Heal(_HealthAmount);
            }
            // Destroy the health item after it has been collected
            Destroy(this.gameObject);
        }
    }
}
