using Signals;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject WinPanel;
    public GameObject FailPanel;
    public GameObject GameplayPanel;
    
    public void Init()
    {
        UISignals.OnWinPanelActive.AddListener(OnWinPanelActive);
        UISignals.OnFailPanelActive.AddListener(OnFailPanelActive);
        UISignals.OnGameplayPanelActive.AddListener(OnGameplayPanelActive);
    }
    public void Disable()
    {
        UISignals.OnWinPanelActive.RemoveListener(OnWinPanelActive);
        UISignals.OnFailPanelActive.RemoveListener(OnFailPanelActive);
        UISignals.OnGameplayPanelActive.RemoveListener(OnGameplayPanelActive);
    }

    private void OnGameplayPanelActive()
    {
        WinPanel.SetActive(false);
        FailPanel.SetActive(false);
        GameplayPanel.SetActive(true);
    }

    private void OnFailPanelActive()
    {
        GameplayPanel.SetActive(false);
        FailPanel.SetActive(true);
    }

    private void OnWinPanelActive()
    {
        GameplayPanel.SetActive(false);
        WinPanel.SetActive(true);
    }
}