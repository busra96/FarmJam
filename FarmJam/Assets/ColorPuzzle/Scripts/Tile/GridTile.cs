using UnityEngine;

namespace ColorPuzzle
{
    public enum ColorEnum
    {
        None,
        Blue,
        Red,
        Green,
        Yellow,
        White
    }
    
    public class GridTile : MonoBehaviour
    {
        public Tile Tile;
        public ColorEnum ColorEnum;

        public MeshRenderer _MeshRenderer;

        private Material DefaultMat;
        private bool isSelected;

        public bool GetIsSelected() => isSelected;
      

        private void OnEnable()
        {
        }
        private void OnDisable()
        {
        }

        public void SetMaterial(Material material)
        {
             DefaultMat = material;
            _MeshRenderer.material = material;
        }

        public void SetColor(Color color,ColorEnum colorEnum)
        {
            if (colorEnum == ColorEnum.None)
            {
                ColorEnum = colorEnum;
                _MeshRenderer.material.color = Color.white;
            }
            else
            {
                ColorEnum = colorEnum;
                _MeshRenderer.material.color =color;
            }
            
          /*  if (isSelected)
            {
                isSelected = false;
                ColorEnum = ColorEnum.None;
                _MeshRenderer.material.color = Color.white;
            }
            else
            {
                isSelected = true;
                ColorEnum = colorEnum;
                _MeshRenderer.material.color = color;
            }*/
        }

        public void SetDefaultMat()
        {
            _MeshRenderer.material = DefaultMat;
        }
    }
}