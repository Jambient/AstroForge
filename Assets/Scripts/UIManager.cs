using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> screens = new List<GameObject>();
    [SerializeField] private Image fadeFrame;
    [SerializeField] private RawImage mainMenuBackground;

    [SerializeField] private List<GameObject> settingsButtons = new List<GameObject>();
    [SerializeField] private List<Sprite> settingsButtonSprites = new List<Sprite>();
    [SerializeField] private Transform settingsPages;

    [SerializeField] private RectTransform progressBar;

    private string currentScreen = "TitleScreen";
    private int currentSettingsButtonIndex = 0;

    void OpenScreen(string screenName)
    {
        currentScreen = "";

        foreach (GameObject screen in screens)
        {
            screen.SetActive(screen.name == screenName);
        }

        currentScreen = screenName;
    }

    IEnumerator FadeToScreen(string screenName)
    {
        currentScreen = "";

        fadeFrame.gameObject.SetActive(true);
        yield return fadeFrame.DOFade(1, 0.3f).WaitForCompletion();
        OpenScreen(screenName);
        yield return fadeFrame.DOFade(0, 0.3f).WaitForCompletion();
        fadeFrame.gameObject.SetActive(false);
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        yield return FadeToScreen("LoadingScreen");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            //yield return progressBar.Find("InnerBar").GetComponent<RectTransform>().DOSizeDelta(new Vector2(asyncLoad.progress * progressBar.rect.width, 10), 1).WaitForCompletion();
            yield return null;
            if (asyncLoad.progress >= 0.9f)
            {
                fadeFrame.gameObject.SetActive(true);
                yield return fadeFrame.DOFade(1, 0.3f).WaitForCompletion();

                asyncLoad.allowSceneActivation = true;
            }
        }
    }

    void OpenSettingsTab(string buttonName)
    {
        foreach (GameObject button in settingsButtons)
        {
            Image buttonImage = button.GetComponent<Image>();
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            Button buttonProperties = button.GetComponent<Button>();
            if (button.name == buttonName)
            {
                buttonImage.sprite = settingsButtonSprites[3];
                buttonText.color = new Color(0, 0, 0, 1);
                buttonProperties.interactable = false;
            } else
            {
                int tabIndex = settingsButtons.FindIndex(x => x.name == button.name);
                if (tabIndex == 0)
                {
                    buttonImage.sprite = settingsButtonSprites[0];
                } else if (tabIndex - 1 == settingsButtons.Count)
                {
                    buttonImage.sprite = settingsButtonSprites[2];
                } else 
                {
                    buttonImage.sprite = settingsButtonSprites[1];
                }

                buttonText.color = new Color(1, 1, 1, 0.5f);
                buttonProperties.interactable = true;
            }
        }

        foreach (Transform page in settingsPages)
        {
            page.gameObject.SetActive(page.name == buttonName);
        }
    }

    private void Start()
    {
        foreach (GameObject button in settingsButtons)
        {
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                currentSettingsButtonIndex = settingsButtons.FindIndex(x => x.name == button.name);
                OpenSettingsTab(button.name);
            });
        }
    }

    private void Update()
    {
        if (currentScreen == "TitleScreen" && Input.anyKeyDown)
        {
            StartCoroutine(FadeToScreen("MainMenuScreen"));
        }

        if (currentScreen == "MainMenuScreen")
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StartCoroutine(FadeToScreen("TitleScreen"));
            }

            if (Input.mousePresent)
            {
                Vector2 mousePosition = Input.mousePosition;
                mousePosition /= new Vector2(Screen.width, Screen.height);

                Vector2 offsetVector = mousePosition - new Vector2(0.5f, 0.5f);
                mainMenuBackground.uvRect = new Rect(offsetVector.x * 0.015f, offsetVector.y * 0.015f, 1, 1);
            }
        }

        if (currentScreen == "SettingsScreen")
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StartCoroutine(FadeToScreen("MainMenuScreen"));
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                currentSettingsButtonIndex--;
                if (currentSettingsButtonIndex < 0)
                {
                    currentSettingsButtonIndex = settingsButtons.Count - 1;
                }
                OpenSettingsTab(settingsButtons[currentSettingsButtonIndex].name);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                currentSettingsButtonIndex = ++currentSettingsButtonIndex % settingsButtons.Count;
                OpenSettingsTab(settingsButtons[currentSettingsButtonIndex].name);
            }
        }
    }

    // Main Menu Button Functions
    public void ContinueGame()
    {
        Debug.Log("Not implemented");
    }

    public void NewGame()
    {
        Debug.Log("Not implemented");
    }
    public void SandboxBuilder()
    {
        if (currentScreen != "MainMenuScreen") { return; }
        StartCoroutine(LoadSceneAsync("SandboxBuilder"));
    }
    public void Settings()
    {
        if (currentScreen != "MainMenuScreen") { return; }
        StartCoroutine(FadeToScreen("SettingsScreen"));

        currentSettingsButtonIndex = 0;
        OpenSettingsTab(settingsButtons[currentSettingsButtonIndex].name);

    }
    public void Exit()
    {
        Application.Quit();
    }
}
