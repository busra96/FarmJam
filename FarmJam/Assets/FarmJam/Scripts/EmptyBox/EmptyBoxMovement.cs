using System;
using DG.Tweening;
using UnityEngine;

public class EmptyBoxMovement : MonoBehaviour
{
    public Transform RotatePoint;
    public Camera mainCamera;
    public LayerMask planeLayer;
    public float MaxYPos = 1.25f;
    public float moveSpeed = 5f; // Objenin hareket hızı
    
    private bool _isMouseDown = false;
    private float _defaultTimer = .5f;
    private float _timer;
    
    private Ray _currentRay; // Ray'i saklamak için bir değişken
    private bool _isRayValid = false; // Ray'in geçerli olup olmadığını kontrol etmek için
    private Vector3 _startPos;
    
    private Vector3 _targetPos; // Objenin hedef pozisyonu (mouse'un işaret ettiği yer)
    private Tween ScaleTween;

    private void Start()
    {
        _startPos = transform.position;
        _targetPos = _startPos;
        transform.localScale = Vector3.one * .8f;
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
            if(ScaleTween != null) ScaleTween.Kill();
            ScaleTween = transform.DOScale(Vector3.one, .25f).SetEase(Ease.Linear);
        }
        
        if (Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            _currentRay = ray; // Ray'i kaydediyoruz
            _isRayValid = true;
          
          
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, planeLayer))
            {
                Vector3 hitPoint = hitInfo.point;
                _targetPos  = new Vector3(hitPoint.x, MaxYPos, hitPoint.z);
            }
        }
        else
        {
            _isRayValid = false; // Ray'i gizlemek için geçersiz yapıyoruz
        }

        transform.position = Vector3.MoveTowards(transform.position, _targetPos, moveSpeed * Time.deltaTime);
        
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

            // Objeyi başlangıç pozisyonuna geri döndürme
            _targetPos = _startPos;
            transform.DOMove(_startPos, .1f).SetEase(Ease.Linear);
            if(ScaleTween != null) ScaleTween.Kill();
            ScaleTween = transform.DOScale(Vector3.one * .8f, .1f).SetEase(Ease.Linear);
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