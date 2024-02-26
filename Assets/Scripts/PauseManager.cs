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

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private BuildingSystem buildingSystem;

    public void Resume()
    {
        isGamePaused = false;
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
    }
    
    public void ReturnToMenu()
    {
        isGamePaused = false;
        SaveDataIfNeeded();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    public void Quit()
    {
        SaveDataIfNeeded();
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

    private void SaveDataIfNeeded()
    {
        if (GlobalsManager.currentGameMode == GameMode.Restricted)
        {
            SaveManager.instance.SaveGameData(GlobalsManager.gameData);
        }
        if (GlobalsManager.currentShipID >= 0 && GlobalsManager.inBuildMode)
        {
            buildingSystem.SaveCurrentShipData();
        }
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
