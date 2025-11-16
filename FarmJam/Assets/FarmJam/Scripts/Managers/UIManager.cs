using Signals;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject WinPanel;
    public GameObject FailPanel;
    public GameObject GameplayPanel;
    public Button NextLevelButton;
    public Button RetryLevelButton;
    
    public void Init()
    {
        UISignals.OnWinPanelActive.AddListener(OnWinPanelActive);
        UISignals.OnFailPanelActive.AddListener(OnFailPanelActive);
        UISignals.OnGameplayPanelActive.AddListener(OnGameplayPanelActive);
        NextLevelButton.onClick.AddListener(LoadNextLevel);
        RetryLevelButton.onClick.AddListener(LoadCurrentLevel);
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
    
    private void LoadCurrentLevel()
    {
    }

    private void LoadNextLevel()
    {
    }
}