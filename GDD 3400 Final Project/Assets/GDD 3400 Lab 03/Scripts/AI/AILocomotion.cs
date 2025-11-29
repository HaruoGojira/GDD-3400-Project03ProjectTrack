using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class AILocomotion : MonoBehaviour
{
    private Animator _animator;
    private NavMeshAgent _navMeshAgent;

    // Jumping
    private bool _isJumping = false;
    private float _baseOffset = 0f;

    private float _speed = 0f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _navMeshAgent = GetComponent<NavMeshAgent>();

        _animator.SetFloat("MotionSpeed", 1f);
        _animator.SetBool("Grounded", true);

        _baseOffset = _navMeshAgent.baseOffset;
    }

    private void Update()
    {
        // Smooth the speed of the animation to avoid jittering when repathing
        _speed = Mathf.Lerp(_speed, _navMeshAgent.velocity.magnitude, Time.deltaTime * 6f);
        _animator.SetFloat("Speed", _speed);

        if (_navMeshAgent.isOnOffMeshLink)
        {
            // Only start the jump if we're not already jumping
            if (!_isJumping) OnJumpStart();
        }
        else
        {
            // Only end the jump if we're already jumping
            if (_isJumping) OnJumpEnd();            
        }
    }

    public void OnJumpStart()
    {        
        _isJumping = true;

        _animator.SetBool("Jump", true);
        _animator.SetBool("Grounded", false);

        _animator.SetFloat("MotionSpeed", 0.5f);

        StartCoroutine(LerpBaseOffset(.65f, .25f));
    }

    public void OnJumpEnd()
    {
        _isJumping = false;

        _animator.SetBool("Jump", false);
        _animator.SetBool("Grounded", true);

        _animator.SetFloat("MotionSpeed", 1f);

        StartCoroutine(LerpBaseOffset(_baseOffset, .5f));
    }

    IEnumerator LerpBaseOffset(float yOffset, float duration)
    {
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            _navMeshAgent.baseOffset = Mathf.Lerp(0, yOffset, t);
            yield return null;
        }

        _navMeshAgent.baseOffset = yOffset;
    }

    public void OnFootstep()
    {
        Debug.Log("AI Footstep");
    }

    public void OnLand()
    {
        Debug.Log("AI Land");
    }
}
