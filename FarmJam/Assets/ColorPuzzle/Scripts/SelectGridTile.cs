using System;
using UnityEngine;

namespace ColorPuzzle
{
    public class SelectGridTile : MonoBehaviour
    {
        
        public Camera RaycastCamera;         // Boş bırakırsan Awake'te Camera.main alınacak
        public LayerMask GridTileLayerMask;  // GridTile'ların olduğu layer'ı buraya ver (zorunlu değil ama faydalı)

        private Ray _lastRay;
        private bool _hasHit;
        private Vector3 _hitPoint;
        private GridTile _lastGridTile;
        private GridTile _beforeLastGridTile;

        public ColorEnum ColorEnum;
        private Color Color;

        private void Start()
        {
            RaycastCamera = Camera.main;
            SetColor();
        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                SelectGrid();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _lastGridTile = null;
                _beforeLastGridTile = null;
            }
        }

        private void SelectGrid()
        {
            _lastRay = RaycastCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(_lastRay, out hit, 100f, GridTileLayerMask.value == 0 ? ~0 : GridTileLayerMask))
            {
                _hitPoint = hit.point;

                var currentGridTile = hit.collider.GetComponent<GridTile>();
                if (currentGridTile != null && currentGridTile != _lastGridTile)
                {
                    Debug.Log("GridTile vuruldu: " + currentGridTile.name);

                    if (currentGridTile.ColorEnum != ColorEnum.None) // Boyanmıs 
                    {
                        if (currentGridTile.ColorEnum == ColorEnum)
                        {
                            if (_lastGridTile != null)
                            {
                                _lastGridTile.SetColor(Color, ColorEnum.None);
                            }
                            else
                            {
                                currentGridTile.SetColor(Color, ColorEnum.None);
                            }
                            _lastGridTile = currentGridTile;
                            //_lastGridTile.SetColor(Color, ColorEnum.None);
                        }
                        else
                        {
                            _lastGridTile = currentGridTile;
                            _lastGridTile.SetColor(Color, ColorEnum);
                        }
                    }
                    else // boyanmamıs
                    {
                        _lastGridTile = currentGridTile;
                        _lastGridTile.SetColor(Color, ColorEnum);
                    }
                    
                   /* if (_lastGridTile != null)
                    {
                        if (currentGridTile.GetIsSelected())
                        {
                            Debug.Log(" 2 ");
                            _lastGridTile.SetColor(Color.white);
                            _lastGridTile = currentGridTile;
                          
                        }
                        else
                        {
                            _lastGridTile = currentGridTile;
                            _lastGridTile.SetColor(Color.blue);
                        }
                       /* Debug.Log(" 1 ");
                        if (_beforeLastGridTile != null)
                        {
                            Debug.Log(" 1  --- 0");
                            if (currentGridTile == _beforeLastGridTile)
                            {
                                Debug.Log(" 1  --- 1");
                                _beforeLastGridTile = _lastGridTile;
                                _lastGridTile = currentGridTile;
                                _beforeLastGridTile.SetColor(Color.white);
                            }
                            else
                            {
                                Debug.Log(" 1  --- 2 ");
                                _beforeLastGridTile = _lastGridTile;
                                _lastGridTile = currentGridTile;
                                _lastGridTile.SetColor(Color.blue);
                            }
                        }
                        else
                        {
                            Debug.Log(" 1  --- 3 ");
                            _beforeLastGridTile = _lastGridTile;
                            _lastGridTile = currentGridTile;
                            _lastGridTile.SetColor(Color.blue);
                       
                        }
                    }
                    else
                    {
                        Debug.Log(" 2 ");
                        _lastGridTile = currentGridTile;
                        _lastGridTile.SetColor(Color.blue);
                    }*/
                }
            }
        }

        [ContextMenu(" SET COLOR  ")]
        public void SetColor()
        {
            if (ColorEnum == ColorEnum.Blue)
            {
               Color = Color.blue;
            }
            else if (ColorEnum == ColorEnum.Green)
            {
                Color = Color.green;
            }
            else if (ColorEnum == ColorEnum.Red)
            {
                Color = Color.red;
            }
            else if (ColorEnum == ColorEnum.Yellow)
            {
                Color = Color.yellow;
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(_lastRay.origin, _lastRay.direction * 100f);

            if (_hasHit)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_hitPoint, 0.1f);
            }
        }
    }

}
