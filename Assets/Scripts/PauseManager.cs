using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance { get; private set; }
    public bool isGamePaused = false;

    [SerializeField] GameObject pauseMenu;

    public void Resume()
    {
        isGamePaused = false;
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
    }
    
    public void ReturnToMenu()
    {
        isGamePaused = false;
        if (GlobalsManager.currentGameMode == GameMode.Restricted)
        {
            SaveManager.instance.SaveGameData(GlobalsManager.gameData);
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    public void Quit()
    {
        if (GlobalsManager.currentGameMode == GameMode.Restricted)
        {
            SaveManager.instance.SaveGameData(GlobalsManager.gameData);
        }
        Application.Quit();
    }
    public void OnPause()
    {
        if (EventSystem.current.currentSelectedGameObject?.GetComponent<TMP_InputField>()) { return; }

        isGamePaused = true;
        EventSystem.current.SetSelectedGameObject(null);
        Time.timeScale = 0f;
        pauseMenu.SetActive(true);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }
}
