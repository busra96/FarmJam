using UnityEngine;

namespace Signals
{
    public class InputSignals
    {
        public static Signal<Vector3> OnInputGetMouseDown = new Signal<Vector3>();
        public static Signal<Vector3> OnInputGetMouseUp = new Signal<Vector3>();
    }
}