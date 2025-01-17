using DG.Tweening;
using UnityEngine;

public class EmptyBoxMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rotatePoint;
    [SerializeField] private Camera mainCamera;
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

    private void Start()
    {
        InitializeValues();
    }

    private void Update()
    {
        HandleMouseDown();
        HandleMouseHold();
        HandleMouseUp();
    }

    private void InitializeValues()
    {
        startPosition = transform.position;
        movingPosition = startPosition;
        transform.localScale = initialScale;
    }

    private void HandleMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
            clickTimer = clickTimerDuration;

            ScaleTweenTo(clickScale, 0.25f);

            movingPosition = transform.position;
            lastMouseWorldPosition = GetMouseWorldPosition();
        }
    }

    private void HandleMouseHold()
    {
        if (!Input.GetMouseButton(0)) return;

        clickTimer = Mathf.Max(clickTimer - Time.deltaTime, 0);

        Vector3 currentMouseWorldPos = GetMouseWorldPosition();

        Vector3 velocity = currentMouseWorldPos - lastMouseWorldPosition;
        velocity.y = 0; // Sadece x ve z eksenlerinde hareket

        movingPosition += velocity;

        Vector3 targetPos = new Vector3(movingPosition.x, maxYPos, movingPosition.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        lastMouseWorldPosition = currentMouseWorldPos;
    }

    private void HandleMouseUp()
    {
        if (!Input.GetMouseButtonUp(0)) return;

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
        transform.DOMove(startPosition, 0.1f).SetEase(Ease.Linear);
        ScaleTweenTo(initialScale, 0.1f);
    }

    private void ScaleTweenTo(Vector3 targetScale, float duration)
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(targetScale, duration).SetEase(Ease.Linear);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, planeLayer))
        {
            return hitInfo.point;
        }

        return lastMouseWorldPosition; // Eğer bir yere vurmazsa son pozisyonu döndür
    }
}