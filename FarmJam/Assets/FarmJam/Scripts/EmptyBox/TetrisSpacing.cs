using UnityEngine;

public class TetrisSpacing : MonoBehaviour
{
    public float TargetAngle;
    
    // Açılara göre kapladığı alan (Örn: 2x1 yatayken 2, dikeyken 1)
    public Vector2 sizeAt0 = Vector2.one;
    public Vector2 sizeAt90 = Vector2.one;
    public Vector2 sizeAt180 = Vector2.one;
    public Vector2 sizeAt270 = Vector2.one;

    public Vector2 GetSizeAtRotation(int rotation)
    {
        rotation %= 360; // 0, 90, 180, 270 döngüsüne sok
        switch (rotation)
        {
            case 0: return sizeAt0;
            case 90: return sizeAt90;
            case 180: return sizeAt180;
            case 270: return sizeAt270;
            default: return Vector2.one; // Varsayılan 1
        }
    }
}