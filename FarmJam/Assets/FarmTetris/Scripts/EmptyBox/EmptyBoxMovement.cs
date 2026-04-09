namespace FarmTetris
{
    using DG.Tweening;
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
            rotatePoint.transform.localPosition = Vector3.zero;
            
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
        /// KullanÄ±cÄ± bir objeye tÄ±kladÄ±ÄŸÄ±nda Ã§alÄ±ÅŸÄ±r.
        /// Objeyi **direkt olarak mouse'un olduÄŸu noktaya yerleÅŸtirir** ve takibe baÅŸlar.
        /// </summary>
        public void HandleMouseDown(Vector3 mousePosition)
        {
            isMouseDown = true;
            clickTimer = clickTimerDuration;
            isActive = true; // Objeyi mouse ile sÃ¼rÃ¼kleyebiliriz

            ScaleTweenTo(clickScale, 0.15f);
            
            // **Objeyi direkt mouse'un olduÄŸu noktaya taÅŸÄ±!**
            lastMouseWorldPosition = GetMouseWorldPosition(mousePosition);
            transform.position = new Vector3(lastMouseWorldPosition.x, maxYPos, lastMouseWorldPosition.z);
        }

        /// <summary>
        /// KullanÄ±cÄ± mouse'u basÄ±lÄ± tutuyorsa, obje mouse'u takip eder.
        /// </summary>
        private void HandleMouseHold(Vector3 pos)
        {
            if (!isActive) return;

            clickTimer = Mathf.Max(clickTimer - Time.deltaTime, 0);

            Vector3 currentMouseWorldPos = GetMouseWorldPosition(pos);
            Vector3 velocity = currentMouseWorldPos - lastMouseWorldPosition;
            velocity.y = 0; // Y ekseninde hareket engellendi

            transform.position += velocity; // **Hareket farkÄ± kadar pozisyon gÃ¼ncellendi**
            lastMouseWorldPosition = currentMouseWorldPos; // Yeni mouse konumu kaydedildi
        }

        /// <summary>
        /// KullanÄ±cÄ± mouse'u bÄ±raktÄ±ÄŸÄ±nda Ã§alÄ±ÅŸÄ±r.
        /// EÄŸer kÄ±sa sÃ¼rede bÄ±rakÄ±lmÄ±ÅŸsa, objeyi dÃ¶ndÃ¼r.
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
                .DORotateQuaternion(Quaternion.Euler(0, rotatePoint.eulerAngles.y + 90f, 0), 0f)
                .SetEase(Ease.Linear)
                .OnComplete(() => EmptyBoxSignals.OnUpdateTetrisLayout?.Dispatch());
        }

        private void ResetObjectState()
        {
            isMouseDown = false;
            EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition?.Dispatch(this);
            rotatePoint.transform.localPosition = Vector3.zero;
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
}

