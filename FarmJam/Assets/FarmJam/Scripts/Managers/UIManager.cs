using Signals;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject MainPanel;
    public GameObject WinPanel;
    public GameObject FailPanel;
    public GameObject GameplayPanel;
    public Button PlayLevelButton;
    public Button NextLevelButton;
    public Button RetryLevelButton;
    
    public void Init()
    {
        UISignals.OnWinPanelActive.AddListener(OnWinPanelActive);
        UISignals.OnFailPanelActive.AddListener(OnFailPanelActive);
        UISignals.OnGameplayPanelActive.AddListener(OnGameplayPanelActive);
        PlayLevelButton.onClick.AddListener(OnPlayLevelButtonClicked);
        NextLevelButton.onClick.AddListener(OnClickedNextButtonClicked);
        RetryLevelButton.onClick.AddListener(OnClickedRetryButtonClicked);
        
        MainPanel.SetActive(true);
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
    
    private void OnPlayLevelButtonClicked()
    {
        MainPanel.SetActive(false);
        LevelManagerSignals.OnLoadCurrentLevel?.Dispatch();
    }
    
    private void OnClickedRetryButtonClicked()
    {
    }

    private void OnClickedNextButtonClicked()
    {
    }
}