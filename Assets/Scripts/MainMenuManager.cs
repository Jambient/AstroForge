using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Linq;

public class MainMenuManager : MonoBehaviour
{
    #region Variables
    [Header("Script References")]
    [SerializeField] private KeybindsManager keybindsManager;

    [Header("UI References")]
    [SerializeField] private List<GameObject> screens = new List<GameObject>();
    [SerializeField] private Image fadeFrame;
    [SerializeField] private RawImage mainMenuBackground;
    [SerializeField] private GameObject continueGameButton;

    [SerializeField] private List<GameObject> settingsButtons = new List<GameObject>();
    [SerializeField] private List<Sprite> settingsButtonSprites = new List<Sprite>();
    [SerializeField] private Transform settingsPages;
    [SerializeField] private GameObject bindingModal;
    [SerializeField] private Transform controlsSettingsContent;

    [SerializeField] private RectTransform progressBar;

    private string currentScreen = "TitleScreen";
    private int currentSettingsButtonIndex = 0;
    #endregion

    #region Public Methods
    public void ContinueGame()
    {
        if (currentScreen != "MainMenuScreen") { return; }

        SaveManager.instance.LoadGameData(out GlobalsManager.gameData);
        GlobalsManager.currentGameMode = GameMode.Restricted;
        SceneLoadManager.instance.LoadSceneAsync("ShipBuilding");
    }

    public void NewGame()
    {
        if (currentScreen != "MainMenuScreen") { return; }

        GameData gameData = new GameData();
        gameData.currentRound = 1;
        gameData.credits = 300;
        gameData.researchPoints = 0;

        GlobalsManager.gameData = gameData;
        GlobalsManager.currentGameMode = GameMode.Restricted;

        SaveManager.instance.SaveGameData(gameData);
        foreach (int shipId in SaveManager.instance.GetAllShipIDs())
        {
            SaveManager.instance.DeleteShipData(shipId);
        }

        SceneLoadManager.instance.LoadSceneAsync("ShipBuilding");
    }

    public void SandboxBuilder()
    {
        if (currentScreen != "MainMenuScreen") { return; }
        GlobalsManager.currentGameMode = GameMode.Sandbox;
        SceneLoadManager.instance.LoadSceneAsync("ShipBuilding");
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

    private void UpdateKeybinds()
    {
        //keybindsManager.playerInput.actions.FindActionMap("Game").actions.Select();

        //foreach (InputAction actionReference in keybindsManager.playerInput.actions.FindActionMap("Game").actions)
        //{
        //    int bindingIndex = actionReference.GetBindingIndex();
        //    string keyName = InputControlPath.ToHumanReadableString(actionReference.bindings[bindingIndex].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);



        //    //Sprite keybindIcon = Resources.Load<Sprite>($"InputPrompts/Keyboard/White/{keyName}");

        //    Debug.Log($"{actionReference.name} : {keyName}");
        //}

        var actions = keybindsManager.playerInput.actions.FindActionMap("Game").actions;
        foreach (Transform control in controlsSettingsContent)
        {
            if (control.name != "TitleText")
            {
                int actionIndex = keybindsManager.playerInput.actions.FindActionMap("Game").actions.IndexOf((data) => data.name == control.name);
                InputAction actionReference = actions[actionIndex];
                int bindingIndex = actionReference.GetBindingIndex();
                string keyName = InputControlPath.ToHumanReadableString(actionReference.bindings[bindingIndex].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);

                control.GetComponentsInChildren<TextMeshProUGUI>()[1].text = keyName;
            }
        }
    }

    public void RebindKey(string actionName)
    {
        bindingModal.SetActive(true);
        StartCoroutine(keybindsManager.StartRebindingCoroutine(actionName, () => {
            bindingModal.SetActive(false);
            UpdateKeybinds();
        }));
    }
    #endregion

    #region Private Methods
    private void OpenScreen(string screenName)
    {
        currentScreen = "";

        foreach (GameObject screen in screens)
        {
            screen.SetActive(screen.name == screenName);
        }

        currentScreen = screenName;
    }

    private IEnumerator FadeToScreen(string screenName)
    {
        currentScreen = "";

        fadeFrame.gameObject.SetActive(true);
        yield return fadeFrame.DOFade(1, 0.3f).WaitForCompletion();
        OpenScreen(screenName);
        yield return fadeFrame.DOFade(0, 0.3f).WaitForCompletion();
        fadeFrame.gameObject.SetActive(false);
    }

    private void OpenSettingsTab(string buttonName)
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
    #endregion

    #region MonoBehaviour Messages
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

        if (SaveManager.instance.LoadGameData(out GameData gameData))
        {
            continueGameButton.GetComponent<Button>().interactable = true;
            continueGameButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(113f / 255f, 113f / 255f, 113f / 255f);
        }

        UpdateKeybinds();
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
    #endregion
}