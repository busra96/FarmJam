using DG.Tweening;
using Signals;
using UnityEngine;

public class EmptyBoxMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rotatePoint;
    [SerializeField] private LayerMask planeLayer;
    
    private Camera mainCamera;

    [Header("Settings")]
    [SerializeField] private float maxYPos = 1.25f;
    [SerializeField] private float moveSpeed = 5f; 
    [SerializeField] private float clickTimerDuration = 0.25f;
    [SerializeField] private Vector3 initialScale = Vector3.one * 0.7f;
    [SerializeField] private Vector3 clickScale = Vector3.one;

    private bool isMouseDown;
    private float clickTimer;
    private Vector3 movingPosition;
    private Vector3 lastMouseWorldPosition;
    private Tween scaleTween;
    
    private bool isActive;

     public void Init()
    {
        mainCamera = Camera.main;
        ResetScale();

        InputSignals.OnInputGetMouseHold.AddListener(HandleMouseHold);
    }

    public void Disable()
    {
        InputSignals.OnInputGetMouseHold.RemoveListener(HandleMouseHold);
    }

    private void ResetScale()
    {
        transform.localScale = initialScale;
    }

    /// <summary>
    /// Kullanıcı bir objeye tıkladığında çalışır.
    /// Objeyi **direkt olarak mouse'un olduğu noktaya yerleştirir** ve takibe başlar.
    /// </summary>
    public void HandleMouseDown(Vector3 mousePosition)
    {
        isMouseDown = true;
        clickTimer = clickTimerDuration;
        isActive = true; // Objeyi mouse ile sürükleyebiliriz

        ScaleTweenTo(clickScale, 0.15f);
        
        // **Objeyi direkt mouse'un olduğu noktaya taşı!**
        lastMouseWorldPosition = GetMouseWorldPosition(mousePosition);
        transform.position = new Vector3(lastMouseWorldPosition.x, maxYPos, lastMouseWorldPosition.z);
    }

    /// <summary>
    /// Kullanıcı mouse'u basılı tutuyorsa, obje mouse'u takip eder.
    /// </summary>
    private void HandleMouseHold(Vector3 pos)
    {
        if (!isActive) return;

        clickTimer = Mathf.Max(clickTimer - Time.deltaTime, 0);

        Vector3 currentMouseWorldPos = GetMouseWorldPosition(pos);
        Vector3 velocity = currentMouseWorldPos - lastMouseWorldPosition;
        velocity.y = 0; // Y ekseninde hareket engellendi

        transform.position += velocity; // **Hareket farkı kadar pozisyon güncellendi**
        lastMouseWorldPosition = currentMouseWorldPos; // Yeni mouse konumu kaydedildi
    }

    /// <summary>
    /// Kullanıcı mouse'u bıraktığında çalışır.
    /// Eğer kısa sürede bırakılmışsa, objeyi döndür.
    /// </summary>
    public void HandleMouseUp()
    {
        if (!isActive) return;

        isActive = false;

        if (isMouseDown && clickTimer > 0)
        {
            RotateObject();
        }

        ResetObjectState();
    }

    private void RotateObject()
    {
        isMouseDown = false;

        rotatePoint
            .DORotateQuaternion(Quaternion.Euler(0, rotatePoint.eulerAngles.y + 90f, 0), 0.1f)
            .SetEase(Ease.Linear)
            .OnComplete(() => EmptyBoxSignals.OnUpdateTetrisLayout?.Dispatch());
    }

    private void ResetObjectState()
    {
        isMouseDown = false;
        EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition?.Dispatch(this);
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

        return Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, planeLayer) 
            ? hitInfo.point 
            : lastMouseWorldPosition;
    }
}