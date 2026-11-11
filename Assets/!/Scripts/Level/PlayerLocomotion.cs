using System;
using NUnit.Framework;
using Unity.Hierarchy.Editor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLocomotion : MonoBehaviour
{
    InputManager inputManager;

    Vector3 moveDirection;
    Transform cameraObject;
    Rigidbody playerRigidbody;
    AnimatorManager animatorManager;

    [Header("Movement Flags")]
    public bool isSprinting;
    public bool isJumping;

    [Header("Movement Speeds")]
    public float walkingSpeed = 10;
    public float runningSpeed = 15;
    public float sprintingSpeed = 20;
    public float rotationSpeed = 6;
    
    [Header("Jump Speeds")]
    public float jumpHeight = 3;
    public float gravityIntensity = -15;

    [Header("Ground Check")]
    public bool isGrounded;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;

    private void Awake()
    {
        inputManager = GetComponent<InputManager>();
        playerRigidbody = GetComponent<Rigidbody>();
        cameraObject = Camera.main.transform;
        animatorManager = GetComponent<AnimatorManager>();
    }

    public void HandleAllMovement()
    {
        CheckGrounded();
        HandleMovement();
        HandleRotation();

        if (inputManager.jump_Input)
        {
            inputManager.jump_Input = false;
            HandleJumping();
        }
    }

    private void HandleMovement()
    {
        moveDirection = cameraObject.forward * inputManager.verticalInput;
        moveDirection = moveDirection + cameraObject.right * inputManager.horizontalInput;
        moveDirection.Normalize();
        moveDirection.y = 0;

        if(isSprinting)
        {
            moveDirection = moveDirection * sprintingSpeed;
        }
        else
        {
            if(inputManager.moveAmount >= 0.5)
            {
                moveDirection = moveDirection * runningSpeed;
            }
            else
            {
                moveDirection = moveDirection * walkingSpeed;
            }
        }

        Vector3 movementVelocity = moveDirection;
        movementVelocity.y = playerRigidbody.linearVelocity.y;

        playerRigidbody.linearVelocity = movementVelocity;
    }

    private void HandleRotation()
    {
        if(isJumping)
            return;

        Vector3 targetDirection = Vector3.zero;

        targetDirection = cameraObject.forward * inputManager.verticalInput;
        targetDirection = targetDirection + cameraObject.right * inputManager.horizontalInput;
        targetDirection.Normalize();
        targetDirection.y = 0;

        if (targetDirection == Vector3.zero)
            targetDirection = transform.forward;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        transform.rotation = playerRotation; 
    }

    private void CheckGrounded()
    {
        float radius = 0.2f;

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        isGrounded = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    public void HandleJumping()
    {
        if (!isGrounded)
            return;

        animatorManager.PlayTargetAnimation("Jump", false);

        float jumpingVelocity = Mathf.Sqrt(
            -2f * gravityIntensity * jumpHeight
        );

        Vector3 playerVelocity = playerRigidbody.linearVelocity;
        playerVelocity.y = jumpingVelocity;

        playerRigidbody.linearVelocity = playerVelocity;

        isGrounded = false;
    }
}
