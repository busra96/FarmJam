using DG.Tweening;
using Signals;
using UnityEngine;

namespace FarmBlast
{
    public class FB_EmptyUnitBoxParentMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LayerMask planeLayer;

        [Header("Settings")]
        [SerializeField] private float maxYPos = 1.25f;
        [SerializeField] private float returnDuration = 0.2f;
        [SerializeField] private float clickTimerDuration = 0.25f;
        [SerializeField] private Vector3 initialScale = Vector3.one * 0.7f;
        [SerializeField] private Vector3 clickScale = Vector3.one;

        private Camera mainCamera;
        private bool isMouseDown;
        private bool isActive;
        private float clickTimer;
        private Vector3 lastMouseWorldPosition;
        private Vector3 dragOffset;
        private Vector3 startWorldPosition;
        private Quaternion startWorldRotation;
        private bool hasStartPose;
        private Tween moveTween;
        private Tween scaleTween;

        public void Init()
        {
            mainCamera = Camera.main;
            CacheStartPose();
            ResetScale();

            InputSignals.OnInputGetMouseHold.AddListener(HandleMouseHold);
        }

        public void Disable()
        {
            moveTween?.Kill();
            scaleTween?.Kill();
            InputSignals.OnInputGetMouseHold.RemoveListener(HandleMouseHold);
        }

        public void CacheStartPose()
        {
            if (hasStartPose)
            {
                return;
            }

            startWorldPosition = transform.position;
            startWorldRotation = transform.rotation;
            hasStartPose = true;
        }

        private void ResetScale()
        {
            transform.localScale = initialScale;
        }

        public void HandleMouseDown(Vector3 mousePosition)
        {
            CacheStartPose();
            isMouseDown = true;
            isActive = true;
            clickTimer = clickTimerDuration;

            moveTween?.Kill();
            ScaleTweenTo(clickScale, 0.05f);

            if (!TryGetMouseWorldPosition(mousePosition, out lastMouseWorldPosition))
            {
                lastMouseWorldPosition = new Vector3(transform.position.x, 0f, transform.position.z);
            }

            Vector3 liftedPosition = new Vector3(transform.position.x, maxYPos, transform.position.z);
            Vector3 pointerAlignedPosition = new Vector3(lastMouseWorldPosition.x, maxYPos, lastMouseWorldPosition.z);

            dragOffset = liftedPosition - pointerAlignedPosition;
            dragOffset.y = 0f;

            // First selection should only lift the piece, not snap it to the mouse hit point.
            transform.position = liftedPosition;
        }

        private void HandleMouseHold(Vector3 mousePosition)
        {
            if (!isActive)
            {
                return;
            }

            clickTimer = Mathf.Max(clickTimer - Time.deltaTime, 0f);

            if (!TryGetMouseWorldPosition(mousePosition, out Vector3 currentMouseWorldPosition))
            {
                return;
            }

            Vector3 targetPosition = currentMouseWorldPosition + dragOffset;
            targetPosition.y = maxYPos;

            transform.position = targetPosition;
            lastMouseWorldPosition = currentMouseWorldPosition;
        }

        public void HandleMouseUp()
        {
            if (!isActive)
            {
                return;
            }

            isActive = false;
            dragOffset = Vector3.zero;

            ResetObjectState();
        }

        public void ReturnToStart()
        {
            CacheStartPose();

            isActive = false;
            isMouseDown = false;
            dragOffset = Vector3.zero;

            moveTween?.Kill();

            if ((transform.position - startWorldPosition).sqrMagnitude <= 0.0001f)
            {
                transform.position = startWorldPosition;
                transform.rotation = startWorldRotation;
                ResetObjectState();
                return;
            }

            moveTween = transform.DOMove(startWorldPosition, returnDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.rotation = startWorldRotation;
                    ResetObjectState();
                });
        }

        private void ResetObjectState()
        {
            isMouseDown = false;
            FB_EmptyBoxSignals.OnTheBoxHasCompletedTheMovementToTheStartingPosition?.Dispatch(this);
            ScaleTweenTo(initialScale, 0.05f);
        }

        private void ScaleTweenTo(Vector3 targetScale, float duration)
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(targetScale, duration).SetEase(Ease.Linear);
        }

        private bool TryGetMouseWorldPosition(Vector3 mousePosition, out Vector3 worldPosition)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                worldPosition = default;
                return false;
            }

            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, planeLayer))
            {
                worldPosition = hitInfo.point;
                return true;
            }

            worldPosition = default;
            return false;
        }
    }
}
