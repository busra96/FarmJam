using Signals;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private bool isActive;

    public void Init()
    {
        isActive = true;
    }

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            InputSignals.OnInputGetMouseDown?.Dispatch(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            InputSignals.OnInputGetMouseHold?.Dispatch(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            InputSignals.OnInputGetMouseUp?.Dispatch(Input.mousePosition);
        }
    }

    public void DisableInput() => isActive = false;
}
