using System;
using UnityEngine;
using VContainer;

[DisallowMultipleComponent]
public class FarmBoxMergeActionBudget : MonoBehaviour
{
    private int _remainingAddCardUses;
    private int _remainingTrashUses;
    private IFarmBoxMergeSettingsService _settings;

    public int RemainingAddCardUses => _remainingAddCardUses;
    public int RemainingTrashUses => _remainingTrashUses;
    public bool CanAddCard => _remainingAddCardUses > 0;
    public bool CanUseTrash => _remainingTrashUses > 0;

    public event Action Changed;

    [Inject]
    public void Construct(IFarmBoxMergeSettingsService settings)
    {
        _settings = settings;
        ResetForAttempt();
    }

    public void ResetForAttempt()
    {
        _remainingAddCardUses = Mathf.Max(0, _settings != null ? _settings.AddCardUses : 3);
        _remainingTrashUses = Mathf.Max(0, _settings != null ? _settings.TrashUses : 3);
        Changed?.Invoke();
    }

    public bool TryConsumeAddCardUse()
    {
        return TryConsume(ref _remainingAddCardUses);
    }

    public bool TryConsumeTrashUse()
    {
        return TryConsume(ref _remainingTrashUses);
    }

    public void GrantAddCardUses(int amount = 1)
    {
        Grant(ref _remainingAddCardUses, amount);
    }

    public void GrantTrashUses(int amount = 1)
    {
        Grant(ref _remainingTrashUses, amount);
    }

    private bool TryConsume(ref int remainingUses)
    {
        if (remainingUses <= 0)
        {
            return false;
        }

        remainingUses--;
        Changed?.Invoke();
        return true;
    }

    private void Grant(ref int remainingUses, int amount)
    {
        int grantedAmount = Mathf.Max(0, amount);
        if (grantedAmount == 0)
        {
            return;
        }

        remainingUses += grantedAmount;
        Changed?.Invoke();
    }
}
