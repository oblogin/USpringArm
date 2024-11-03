using UnityEditor;
using UnityEngine;

public class SpringArm : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] internal Transform target;
    
    [Header("Spring Arm Settings")]
    [SerializeField] public float targetArmLength = 4;
    [SerializeField] public Vector3 targetOffset = new Vector3(0, 0, 0);
    [SerializeField] public Vector3 socketOffset = new Vector3(0, 0, 0);
    
    [Header("Target Collision Settings")]
    [SerializeField] bool doCollisionTest = true;
    [SerializeField] float probeSize = 0.16f;
    [SerializeField] LayerMask probeChannel;
    
    [Header("Target Rotation Settings")]
    [SerializeField] bool autoFocusOnTarget = false;
        
    [SerializeField] bool useSpringArmRotation = false;
    [SerializeField] bool useSpringArmPitch = false;
    [SerializeField] bool useSpringArmYaw = false;
    [SerializeField] bool useSpringArmRoll = false;

    enum TargetLagMode
    {
        TransformPosition,
        TargetMovement
    }
    
    [Header("Target Lag Position settings")]
    [SerializeField] bool enableTargetLag = true;
    [SerializeField] TargetLagMode targetLagMode = TargetLagMode.TransformPosition;
    [SerializeField] float targetLagSpeed = 2.0f;
    [SerializeField] float targetLagMaxDistance = 0.0f;
    
    [Header("Target Lag Rotation settings")]
    [SerializeField] bool targetRotationLag = false;
    [SerializeField] float targetRotationLagSpeed = 2.0f;
    
    private Vector3 _targetPosition;
    private Vector3 _desiredPosition;
    private Vector3 _desiredRotation;
    
    private bool _collisionPreviouslyTriggered = false;

    void Awake()
    {
        if (target)
        {
            _desiredPosition = transform.position;
        }
    }

    void LateUpdate()
    {
        UpdateTarget();
    }
    
    void OnDrawGizmos()
    {
        if (Application.isPlaying || !target)
        {
            return;
        }
        
        UpdateTarget();
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(target.transform.position, 0.125f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, target.transform.position);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(target.transform.position, 0.125f);
    }

    void UpdateTarget()
    {
        if (!target)
        {
            return;
        }
        
        UpdateTargetPosition();
        UpdateTargetRotation();
    }

    void UpdateTargetPosition()
    {
        if (enableTargetLag)
        {
            switch (targetLagMode)
            {
                case TargetLagMode.TransformPosition:
                    TransformPosition();
                    break;
                case TargetLagMode.TargetMovement:
                    TargetMovement();
                    break;
            }
        }
        else
        {
            _targetPosition = transform.position;
            _targetPosition += targetOffset;
            _targetPosition += transform.rotation * socketOffset;
            _targetPosition += transform.forward * -targetArmLength;
            
            if (doCollisionTest)
            {
                Vector3 raycastDirection = _targetPosition - transform.position;
            
                if (Physics.SphereCast(transform.position, probeSize, raycastDirection.normalized, out var hit, raycastDirection.magnitude, probeChannel))
                {
                    _targetPosition = hit.point + hit.normal * probeSize;
                }
            }
            
            target.transform.position = _targetPosition;
        }
    }
    
    void TransformPosition()
    {
        _desiredPosition = Vector3.Lerp(_desiredPosition, transform.position, targetLagSpeed * Time.deltaTime);
            
        if (targetLagMaxDistance > 0)
        {
            if (Vector3.Distance(transform.position, _desiredPosition) > targetLagMaxDistance)
            {
                _desiredPosition = transform.position + (_desiredPosition - transform.position).normalized * targetLagMaxDistance;
            } 
        }

        _targetPosition = _desiredPosition;
        _targetPosition += targetOffset;
        _targetPosition += transform.rotation * socketOffset;
        _targetPosition += transform.forward * -targetArmLength;

        if (doCollisionTest)
        {
            Vector3 raycastDirection = _targetPosition - transform.position;
            
            if (Physics.SphereCast(transform.position, probeSize / 2, raycastDirection.normalized, out var hit, raycastDirection.magnitude, probeChannel))
            {
                _targetPosition = hit.point + hit.normal * probeSize / 2;
            }
        }
        
        target.transform.position = _targetPosition;
    }
    
    void TargetMovement()
    {
        _targetPosition = transform.position;
        _targetPosition += targetOffset;
        _targetPosition += transform.rotation * socketOffset;
        _targetPosition += transform.forward * -targetArmLength;

        bool collisionOccurred = false; // Did we hit something?
        
        if (doCollisionTest)
        {
            Vector3 raycastDirection = _targetPosition - transform.position;
            
            if (Physics.SphereCast(transform.position, probeSize, raycastDirection.normalized, out var hit, raycastDirection.magnitude, probeChannel))
            {
                _targetPosition = hit.point + hit.normal * probeSize;
                collisionOccurred = true;
            }
        }
        
        bool collisionJustTriggered = collisionOccurred && !_collisionPreviouslyTriggered; // Did we just hit something?
        _collisionPreviouslyTriggered = collisionOccurred;
            
        Vector3 currentPosition = target.transform.position;
            
        if (collisionJustTriggered)
        {
            currentPosition = _targetPosition;
        }
            
        Vector3 laggedPosition = Vector3.Lerp(currentPosition, _targetPosition, targetLagSpeed * Time.deltaTime);

        if (targetLagMaxDistance > 0)
        {
            if (Vector3.Distance(currentPosition, laggedPosition) > targetLagMaxDistance)
            {
                laggedPosition = currentPosition + (laggedPosition - currentPosition).normalized * targetLagMaxDistance;
            } 
        }

        target.transform.position = laggedPosition;
    }
    
    void UpdateTargetRotation()
    {
        _desiredRotation = target.transform.rotation.eulerAngles;

        if (autoFocusOnTarget)
        {
            _desiredRotation = Quaternion.LookRotation(transform.position - target.transform.position).eulerAngles;
        }
        
        if (useSpringArmRotation)
        {
            if (useSpringArmPitch)
            {
                _desiredRotation.x = transform.rotation.eulerAngles.x;
            }

            if (useSpringArmYaw)
            {
                _desiredRotation.y = transform.rotation.eulerAngles.y;
            }

            if (useSpringArmRoll)
            {
                _desiredRotation.z = transform.rotation.eulerAngles.z;
            }
        }

        if (targetRotationLag)
        {
            Quaternion currentRotation = target.transform.rotation;
            Quaternion desiredRotation = Quaternion.Euler(_desiredRotation);
            target.transform.rotation = Quaternion.Slerp(currentRotation, desiredRotation, targetRotationLagSpeed * Time.deltaTime);
        }
        else
        {
            target.transform.rotation = Quaternion.Euler(_desiredRotation);
        };
    }
}
