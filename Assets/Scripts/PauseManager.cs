using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;

    public void Resume()
    {
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
    }
    public void ReturnToMenu()
    {
        SaveManager.instance.SaveGameData(GlobalsManager.gameData);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    public void Quit()
    {
        SaveManager.instance.SaveGameData(GlobalsManager.gameData);
        Application.Quit();
    }
    public void OnPause()
    {
        EventSystem.current.SetSelectedGameObject(null);
        Time.timeScale = 0f;
        pauseMenu.SetActive(true);
    }
}
