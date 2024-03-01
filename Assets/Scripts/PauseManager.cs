using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    #region Variables
    public static PauseManager instance { get; private set; }

    [Header("Public Variables")]
    public bool isGamePaused = false;

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenu;

    [Header("Script References")]
    [SerializeField] private BuildingSystem buildingSystem;
    #endregion

    #region Public Methods
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
        GlobalsManager.currentShipID = -1;
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
    #endregion

    #region Private Methods
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
    #endregion

    #region MonoBehaviour Messages
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
    #endregion
}
