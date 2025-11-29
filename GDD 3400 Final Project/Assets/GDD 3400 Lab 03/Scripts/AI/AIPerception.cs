using UnityEngine;

public class AIPerception : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] bool _DrawDebugLines = true;
    [SerializeField] bool _DrawDebugCone = true;

    [Header("References")]
    [SerializeField] Transform _HeadPoint;

    [Header("Vision")]
    [SerializeField] float _VisionUpdateInterval = 0.1f;
    [SerializeField] float _VisionRange = 10f;
    [SerializeField] float _VisionConeAngle = 120f;
    
    PlayerController _player;

    bool _canSeePlayer = false;
    public bool CanSeePlayer => _canSeePlayer;

    float _timeSinceLastVisionUpdate = 0f;

    void Awake()
    {
        _player = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        _timeSinceLastVisionUpdate += Time.deltaTime;
        if (_timeSinceLastVisionUpdate >= _VisionUpdateInterval)
        {
            _timeSinceLastVisionUpdate = 0f;
            UpdateVision();
        }
    }

    public bool UpdateVision()
    {
        // STEP 1: First check if the player is within vision range
        if (!WithinVisionRange())
        {
            _canSeePlayer = false;
            return false;
        }

        // STEP 2: Check if the player is in the vision cone
        if (!WithinVisionCone())
        {
            _canSeePlayer = false;
            return false;
        }

        // STEP 3: Check if the player is in line of sight
        if (!InLineOfSight())
        {
            _canSeePlayer = false;
            return false;
        }

        _canSeePlayer = true;
        if (_DrawDebugLines) Debug.DrawLine(_HeadPoint.position, GetPlayerCenterPosition(), Color.green, _VisionUpdateInterval);

        // If we have visibility, return true
        return true;
    }

    // Check if the player is within the vision range using the distance between the head point and the player's position
    public bool WithinVisionRange()
    {
        return Vector3.Distance(_HeadPoint.position, GetPlayerCenterPosition()) <= _VisionRange;
    }

    // Check if the player is within the vision cone using the dot product of the head point forward and the direction to the player
    public bool WithinVisionCone()
    {
        // Get the direction to the player's center
        Vector3 directionToPlayer = GetPlayerCenterPosition() - _HeadPoint.position;
        directionToPlayer.Normalize();

        // Calculate the minimum dot product for the vision cone
        float dotProduct = Vector3.Dot(_HeadPoint.forward, directionToPlayer);
        float minDot = Mathf.Cos(_VisionConeAngle * 0.5f * Mathf.Deg2Rad);

        // If the dot product is less than the minimum dot product, the player is outside of the vision cone
        if (dotProduct < minDot)
        {
            return false;
        }

        return true;
    }

    // Check if the player is in line of sight, this check for any colliders between the agents head point and the center of the player's body (one unit above the player root)
    public bool InLineOfSight()
    {
        // Perform a linecast from the head point to the player's center, this is more efficient than a raycast
        RaycastHit hit;
        if (Physics.Linecast(_HeadPoint.position, GetPlayerCenterPosition(), out hit))
        {
            // If the object hit is the player, return true
            if (hit.transform.gameObject.tag == "Player")
            {
                return true;
            }
            else if (_DrawDebugLines) Debug.DrawLine(_HeadPoint.position, hit.point, Color.red, _VisionUpdateInterval);
        }

        return false;
    }

    public Vector3 GetPlayerCenterPosition()
    {
        // By default, the player center is one unit above the player root, this can be changed 
        return _player.transform.position + Vector3.up;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_HeadPoint == null || _player == null || !_DrawDebugCone)
            return;

        // Draw boundary lines
        Quaternion leftRotation = Quaternion.Euler(0, -_VisionConeAngle / 2, 0);
        Quaternion rightRotation = Quaternion.Euler(0, _VisionConeAngle / 2, 0);
        Vector3 leftDir = leftRotation * _HeadPoint.forward;
        Vector3 rightDir = rightRotation * _HeadPoint.forward;

        leftDir.y = 0;
        rightDir.y = 0;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(_HeadPoint.position, _HeadPoint.position + leftDir * _VisionRange);
        Gizmos.DrawLine(_HeadPoint.position, _HeadPoint.position + rightDir * _VisionRange);

        // Draw vision cone arc
        UnityEditor.Handles.color = _canSeePlayer ? Color.red * 0.25f : Color.yellow * 0.25f;
        UnityEditor.Handles.DrawSolidArc(_HeadPoint.position, Vector3.up, leftDir, _VisionConeAngle, _VisionRange);
    }
#endif
}
