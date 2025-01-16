using Cysharp.Threading.Tasks;
using Signals;
using UnityEngine;

public class InputManager
{
    private bool isActive;
    
    public void Init()
    {
        CheckInput().Forget();
    }

    private async UniTask CheckInput()
    {
        isActive = true;

        while (isActive)
        {
            if (Input.GetMouseButtonDown(0))
            {
                InputSignals.OnInputGetMouseDown?.Dispatch(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                InputSignals.OnInputGetMouseUp?.Dispatch(Input.mousePosition);
            }

            await UniTask.Yield();
        }
    }
    
    public void DisableInput() => isActive = false;
}
