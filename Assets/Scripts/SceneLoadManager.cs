using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SceneLoadManager : MonoBehaviour
{
    #region Variables
    public static SceneLoadManager instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private RectTransform loadingBar;
    [SerializeField] private TextMeshProUGUI helpText;
    [SerializeField] private Image fadeFrame;

    private bool finishedLoading;
    private string[] helpMessages = { 
        "The <color=#01C8B1>mass</color> of your ship is very important for movement.",
        "Every ship requries a <color=#01C8B1>core</color> to work.",
        "Experiment with different <color=#01C8B1>weapon</color> combinations to find your preferred combat style.",
        "Install powerful <color=#01C8B1>engines</color> to boost your ship's speed and agility.",
        "Consider the <color=#01C8B1>energy</color> consumption of your ship's pieces to avoid power crashes.",
    };
    #endregion

    #region Public Methods
    /// <summary>
    /// Shows a loading screen while asynchronously loading the given scene
    /// </summary>
    /// <param name="sceneName">The name of the scene to load</param>
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(_LoadSceneAsync(sceneName));
    }
    #endregion

    #region Private Methods
    private IEnumerator _LoadSceneAsync(string sceneName)
    {
        // fade in the loading screen
        fadeFrame.gameObject.SetActive(true);
        yield return fadeFrame.DOFade(1, 0.3f).WaitForCompletion();

        helpText.text = helpMessages[Random.Range(0, helpMessages.Length)];
        loadingScreen.SetActive(true);

        yield return fadeFrame.DOFade(0, 0.3f).WaitForCompletion();
        fadeFrame.gameObject.SetActive(false);

        // start the loading of the scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Wait until the asynchronous scene fully loads
        while (!finishedLoading)
        {
            // animate the loading bar to the current progress
            yield return loadingBar.DOAnchorMax(new Vector2(asyncLoad.progress / 0.9f, 1), 0.3f).WaitForCompletion();

            if (asyncLoad.progress >= 0.9f)
            {
                fadeFrame.gameObject.SetActive(true);

                // animate the loading bar to finish and fade the screen to black
                yield return loadingBar.DOAnchorMax(new Vector2(1, 1), 0.3f).WaitForCompletion();
                yield return fadeFrame.DOFade(1, 0.3f).WaitForCompletion();

                // finish the scene loading
                asyncLoad.allowSceneActivation = true;
                finishedLoading = true;
            }
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
