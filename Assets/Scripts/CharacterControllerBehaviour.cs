using System.Collections;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class CharacterControllerBehaviour : MonoBehaviour
{
    [Header("Locomotion Parameters")]
    [SerializeField]
    private float _mass = 150; // [kg]

    [SerializeField]
    private float _acceleration = 0.1f; // [m/s^2]

    [SerializeField]
    private float _dragOnGround = 0.1f; // []

    [SerializeField]
    private float _maxRunningSpeed = (20f * 1000) / (60 * 60); // [m/s], 30 km/h

    [SerializeField]
    private float _jumpHeight = 5; // [m]

    [Header("Dependencies")]
    [SerializeField, Tooltip("What should determine the absolute forward when a player presses forward.")]
    private Transform _absoluteForward;

    private Animator _animator;
    private CharacterController _characterController;

    private Vector3 _velocity = Vector3.zero;

    private Vector3 _movement;
    private bool _jump;
    private Vector3 _aim;
    private bool _aiming;


    private int _horizontalVelocityAnimationParameter = Animator.StringToHash("HorizontalVelocity");
    private int _verticalVelocityAnimationParameter = Animator.StringToHash("VerticalVelocity");

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

#if DEBUG
        Assert.IsNotNull(_characterController, "Dependency Error: This component needs a CharachterController to work.");
        Assert.IsNotNull(_absoluteForward, "Dependency Error: Set the Absolute Forward field.");
#endif      
    }

    void Update()
    {
        _movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        if (Input.GetButtonDown("Jump"))
        {
            _jump = true;
        }
        if (Input.GetButtonDown("Fire1"))
        {
            _aiming = !_aiming;
        }

    }

    void FixedUpdate()
    {

        ApplyGround();
        ApplyGravity();
        ApplyRotation();
        ApplyMovement();
        ApplyGroundDrag();
        ApplyJump();

        LimitMaximumRunningSpeed();
        Vector3 velocityXZ = Vector3.Scale(_velocity, new Vector3(1, 0, 1));
        Vector3 localVelocity = gameObject.transform.InverseTransformDirection(velocityXZ);

        _animator.SetFloat(_verticalVelocityAnimationParameter, localVelocity.z);
        _animator.SetFloat(_horizontalVelocityAnimationParameter, localVelocity.x);

        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void ApplyGround()
    {
        if (_characterController.isGrounded)
        {
            _velocity -= Vector3.Project(_velocity, Physics.gravity.normalized);
        }
    }

    private void ApplyGravity()
    {      
            _velocity += Physics.gravity * Time.deltaTime; // g[m/s^2] * t[s]        
    }

    private void ApplyMovement()
    {
        if (_characterController.isGrounded)
        {
            Vector3 xzAbsoluteForward = Vector3.Scale(_absoluteForward.forward, new Vector3(1, 0, 1)); //Scale multiplies 2 vector -> dotproduct

            Quaternion forwardRotation =
                Quaternion.LookRotation(xzAbsoluteForward);

            Vector3 relativeMovement = forwardRotation * _movement; //Forward in richting van camera

            _velocity += relativeMovement * _mass * _acceleration * Time.deltaTime; // F(= m.a) [m/s^2] * t [s]
        }

    }
    void ApplyRotation()
    {
        Vector3 xzAbsoluteForward = Vector3.Scale(_absoluteForward.forward, new Vector3(1, 0, 1));
        Quaternion ForwardRotation = Quaternion.LookRotation(xzAbsoluteForward);
        Vector3 relativeMovement = ForwardRotation * _movement;

        if (_movement.magnitude > 0.020f)
            transform.rotation = Quaternion.LookRotation(relativeMovement, Vector3.up);
    }

    private void ApplyGroundDrag()
    {    
        if (_characterController.isGrounded)
        {
            _velocity = _velocity * (1 - 0.1f * _dragOnGround);
        }
    }

    private void ApplyJump()
    {
        //https://en.wikipedia.org/wiki/Equations_of_motion
        //v^2 = v0^2  + 2*a(r - r0)
        //v = 0
        //v0 = ?
        //a = 9.81
        //r = 1
        //r0 = 0
        //v0 = sqrt(2 * 9.81 * 1) 
        //but => g is inverted

        if (_jump && _characterController.isGrounded)
        {
            
            _velocity += -Physics.gravity.normalized * Mathf.Sqrt(2 * Physics.gravity.magnitude * _jumpHeight);
            _jump = false;
        }
        

    }

    private void LimitMaximumRunningSpeed()
    {
        Vector3 yVelocity = Vector3.Scale(_velocity, new Vector3(0, 1, 0));

        Vector3 xzVelocity = Vector3.Scale(_velocity, new Vector3(1, 0, 1));
        Vector3 clampedXzVelocity = Vector3.ClampMagnitude(xzVelocity, _maxRunningSpeed);

        _velocity = yVelocity + clampedXzVelocity;
    }
}