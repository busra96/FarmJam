using System;
using DG.Tweening;
using UnityEngine;

public class EmptyBoxMovement : MonoBehaviour
{
    public Transform RotatePoint;
    public Camera mainCamera;
    public LayerMask planeLayer;
    public float MaxYPos = 1.25f;

    private bool _isMouseDown = false;
    private float _defaultTimer = .5f;
    private float _timer;
    
    private Ray _currentRay; // Ray'i saklamak için bir değişken
    private bool _isRayValid = false; // Ray'in geçerli olup olmadığını kontrol etmek için
    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        if (_isMouseDown)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _timer = 0;
                _isMouseDown = false;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            _isMouseDown = true;
            _timer = _defaultTimer;
        }
        
        if (Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            _currentRay = ray; // Ray'i kaydediyoruz
            _isRayValid = true;
            
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, planeLayer))
            {
                Vector3 hitPoint = hitInfo.point;
               transform.position = new Vector3(hitPoint.x, MaxYPos, hitPoint.z);
            }
        }
        else
        {
            _isRayValid = false; // Ray'i gizlemek için geçersiz yapıyoruz
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_isMouseDown && _timer > 0)
            {
                _timer = 0;
                _isMouseDown = false;
                
                float yPos = RotatePoint.eulerAngles.y;
                Debug.Log("  ypos 0 " + yPos);
                yPos += 90;
                Debug.Log("  ypos " + yPos);
                RotatePoint.DORotateQuaternion( Quaternion.Euler(new Vector3(0, yPos, 0)), 0.1f).SetEase(Ease.Linear);
            }

            transform.DOMove(_startPos, .1f).SetEase(Ease.Linear);
        }

      
        
    }
    
    // Gizmos çizimi
    private void OnDrawGizmos()
    {
        if (_isRayValid)
        {
            Gizmos.color = Color.red; // Ray'in rengini kırmızı yapıyoruz
            Gizmos.DrawRay(_currentRay.origin, _currentRay.direction * 100); // Ray'i çiziyoruz
        }
    }
    
}