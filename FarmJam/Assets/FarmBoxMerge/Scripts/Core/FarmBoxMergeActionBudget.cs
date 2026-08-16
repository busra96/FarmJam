using System;
using UnityEngine;

[DisallowMultipleComponent]
public class FarmBoxMergeActionBudget : MonoBehaviour
{
    [Header("Uses Per Attempt")]
    [SerializeField, Min(0)] private int addCardUsesPerAttempt = 3;
    [SerializeField, Min(0)] private int trashUsesPerAttempt = 3;

    private int _remainingAddCardUses;
    private int _remainingTrashUses;

    public int RemainingAddCardUses => _remainingAddCardUses;
    public int RemainingTrashUses => _remainingTrashUses;
    public bool CanAddCard => _remainingAddCardUses > 0;
    public bool CanUseTrash => _remainingTrashUses > 0;

    public event Action Changed;

    private void Awake()
    {
        ResetForAttempt();
    }

    private void OnValidate()
    {
        addCardUsesPerAttempt = Mathf.Max(0, addCardUsesPerAttempt);
        trashUsesPerAttempt = Mathf.Max(0, trashUsesPerAttempt);
    }

    public void ResetForAttempt()
    {
        _remainingAddCardUses = addCardUsesPerAttempt;
        _remainingTrashUses = trashUsesPerAttempt;
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
