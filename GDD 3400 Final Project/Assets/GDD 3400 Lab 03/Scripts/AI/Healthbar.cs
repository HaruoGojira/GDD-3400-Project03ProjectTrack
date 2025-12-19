using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    [SerializeField] private Image _healthbarSprite;
    private int _maxHealth;

    // initialize the healthbar with maximum health
    public void Initialize(int maxHealth)
    {
        _maxHealth = maxHealth;
        SetHealth(maxHealth);
    }

    // set the healthbar to reflect current health
    public void SetHealth(int currentHealth)
    {
        float healthPercentage = (float)currentHealth / _maxHealth;
        _healthbarSprite.fillAmount = healthPercentage;
    }

}
