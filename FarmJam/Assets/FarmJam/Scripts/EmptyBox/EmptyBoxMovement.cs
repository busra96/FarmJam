using System;
using DG.Tweening;
using Signals;
using UnityEngine;

public class EmptyBoxMovement : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] private Transform rotatePoint;
    private Camera mainCamera;
    [SerializeField] private LayerMask planeLayer;

    [Header("Settings")]
    [SerializeField] private float maxYPos = 1.25f;
    [SerializeField] private float moveSpeed = 5f; 
    [SerializeField] private float clickTimerDuration = 0.25f;
    [SerializeField] private Vector3 initialScale = Vector3.one * 0.8f;
    [SerializeField] private Vector3 clickScale = Vector3.one;

    private bool isMouseDown;
    private float clickTimer;
    private Vector3 startPosition;
    private Vector3 movingPosition;
    private Vector3 lastMouseWorldPosition;
    private Tween scaleTween;
    
    private bool isActive;

    public void Init()
    {
        mainCamera = Camera.main;
        InitializeValues();
        
        InputSignals.OnInputGetMouseHold.AddListener(HandleMouseHold);
    }

    public void Disable()
    {
        InputSignals.OnInputGetMouseHold.RemoveListener(HandleMouseHold);
    }

    private void InitializeValues()
    {
        startPosition = transform.position;
        movingPosition = startPosition;
        transform.localScale = initialScale;
    }

    public void HandleMouseDown(Vector3 mousePosition)
    {
        isMouseDown = true;
        clickTimer = clickTimerDuration;

        ScaleTweenTo(clickScale, 0.25f);

        movingPosition = transform.position;
        lastMouseWorldPosition = GetMouseWorldPosition(mousePosition);
        
        isActive = true;
    }

    private void HandleMouseHold(Vector3 pos)
    {
        if(!isActive) return;
        
        clickTimer = Mathf.Max(clickTimer - Time.deltaTime, 0);

        Vector3 currentMouseWorldPos = GetMouseWorldPosition(pos);

        Vector3 velocity = currentMouseWorldPos - lastMouseWorldPosition;
        velocity.y = 0; 

        movingPosition += velocity;

        Vector3 targetPos = new Vector3(movingPosition.x, maxYPos, movingPosition.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        lastMouseWorldPosition = currentMouseWorldPos;
    }

    public void HandleMouseUp()
    {
        isActive = false;
        
        if (isMouseDown && clickTimer > 0)
        {
            RotateObject();
        }

        ResetObjectPositionAndScale();
    }

    private void RotateObject()
    {
        isMouseDown = false;

        float currentYRotation = rotatePoint.eulerAngles.y;
        float newYRotation = currentYRotation + 90f;

        rotatePoint.DORotateQuaternion(Quaternion.Euler(0, newYRotation, 0), 0.1f).SetEase(Ease.Linear);
    }

    private void ResetObjectPositionAndScale()
    {
        isMouseDown = false;
        transform.DOMove(startPosition, 0.1f).SetEase(Ease.Linear).OnComplete(()=> EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition?.Dispatch(this));
        ScaleTweenTo(initialScale, 0.1f);
    }

    private void ScaleTweenTo(Vector3 targetScale, float duration)
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(targetScale, duration).SetEase(Ease.Linear);
    }

    private Vector3 GetMouseWorldPosition(Vector3 mousePos)
    {
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, planeLayer))
        {
            return hitInfo.point;
        }

        return lastMouseWorldPosition; // Eğer bir yere vurmazsa son pozisyonu döndür
    }
}