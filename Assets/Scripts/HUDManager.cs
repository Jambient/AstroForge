using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
struct Indicator
{
    public Transform trackedObject;
    public RectTransform indicator;
}

public enum UpdateMode
{
    Smooth,
    Immediate
}

public class HUDManager : MonoBehaviour
{
    #region Variables
    [Header("Object References")]
    [SerializeField] private Transform enemies;
    [SerializeField] private Transform discoveries;
    [SerializeField] private GameObject enemyIndicator;
    [SerializeField] private GameObject discoveryIndicator;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI researchPointsText;

    [SerializeField] private RectTransform powerUsageBar;
    [SerializeField] private RectTransform coreHealthBar;

    [SerializeField] private Transform roundCompletedContainer;
    [SerializeField] private TextMeshProUGUI roundCompletedTitleText;
    [SerializeField] private TextMeshProUGUI creditsEarnedStatText;
    [SerializeField] private TextMeshProUGUI timeTakenStatText;

    [SerializeField] private Transform roundFailedContainer;
    [SerializeField] private TextMeshProUGUI roundFailedTitleText;
    [SerializeField] private TextMeshProUGUI enemiesKilledStatText;

    [SerializeField] private RectTransform playerIndicator;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private RectTransform ringBoundary;
    [SerializeField] private Transform mapIndicators;

    private const float minimapWorldSize = 15;
    private List<Indicator> activeIndicators = new List<Indicator>();
    #endregion

    #region Public Methods
    /// <summary>
    /// Refreshes the minimaps indicators
    /// </summary>
    public void RefreshMinimap()
    {
        foreach (Transform child in mapIndicators)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform enemy in enemies)
        {
            AddNewIndicator(enemy, enemyIndicator);
        }
        foreach (Transform discovery in discoveries)
        {
            AddNewIndicator(discovery, discoveryIndicator);
        }
    }

    /// <summary>
    /// Updates the power usage stat on the UI
    /// </summary>
    /// <param name="newPowerUsagePercent">New percentage</param>
    /// <param name="updateMode">The mode for updating the stat bar</param>
    public void UpdatePowerUsageStat(float newPowerUsagePercent, UpdateMode updateMode)
    {
        Vector2 newAnchorMax = new Vector2(1, newPowerUsagePercent);
        switch (updateMode)
        {
            case UpdateMode.Smooth:
                powerUsageBar.anchorMax = Vector2.Lerp(powerUsageBar.anchorMax, newAnchorMax, 4 * Time.deltaTime);
                break;
            case UpdateMode.Immediate:
                powerUsageBar.anchorMax = newAnchorMax;
                break;
        }
    }

    /// <summary>
    /// Updates the core health stat on the UI
    /// </summary>
    /// <param name="newHealthPercent">New percentage</param>
    /// <param name="updateMode">The mode for updating the stat bar</param>
    public void UpdateCoreHealthStat(float newHealthPercent, UpdateMode updateMode)
    {
        Vector2 newAnchorMax = new Vector2(1, newHealthPercent);
        switch (updateMode)
        {
            case UpdateMode.Smooth:
                coreHealthBar.anchorMax = Vector2.Lerp(coreHealthBar.anchorMax, newAnchorMax, 4 * Time.deltaTime);
                break;
            case UpdateMode.Immediate:
                coreHealthBar.anchorMax = newAnchorMax;
                break;
        }
    }

    /// <summary>
    /// Updates and shows the round completed UI
    /// </summary>
    public void ShowRoundCompletedUI()
    {
        roundCompletedTitleText.text = $"ROUND {GlobalsManager.gameData.currentRound} COMPLETE";
        creditsEarnedStatText.text = $"+{RoundManager.instance.creditsEarned} GC";
        timeTakenStatText.text = $"{Mathf.FloorToInt(Time.timeSinceLevelLoad / 60f).ToString("00")}:{(Time.timeSinceLevelLoad % 60).ToString("00")}";
        roundCompletedContainer.gameObject.SetActive(true);
    }

    /// <summary>
    /// Updates and shows the round failed UI
    /// </summary>
    public void ShowRoundFailedUI()
    {
        roundFailedTitleText.text = $"ROUND {GlobalsManager.gameData.currentRound} FAILED";
        enemiesKilledStatText.text = $"{RoundManager.instance.initialEnemyCount - enemies.childCount}/{RoundManager.instance.initialEnemyCount}";
        roundFailedContainer.gameObject.SetActive(true);
    }

    /// <summary>
    /// Returns the user to the hanger
    /// </summary>
    public void ReturnToHanger()
    {
        SceneManager.LoadScene("ShipBuilding");
    }

    /// <summary>
    /// Reloads the current scene
    /// </summary>
    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Adds a new indicator 
    /// </summary>
    /// <param name="trackedObject">The object the indicator is tracking</param>
    /// <param name="indicatorPrefab">The prefab for the indicator</param>
    private void AddNewIndicator(Transform trackedObject, GameObject indicatorPrefab)
    {
        GameObject newIndicator = Instantiate(indicatorPrefab, mapIndicators);
        Indicator indicatorData = new Indicator();
        indicatorData.trackedObject = trackedObject;
        indicatorData.indicator = newIndicator.GetComponent<RectTransform>();

        activeIndicators.Add(indicatorData);
    }

    /// <summary>
    /// Checks if the tracked object of an indicator is still valid, if not then the indicator is destroyed.
    /// </summary>
    /// <param name="data">Data about the indicator</param>
    /// <returns>True if the indicator is valid, False otherwise</returns>
    private bool IsIndicatorValid(Indicator data)
    {
        if (data.trackedObject == null && !ReferenceEquals(data.trackedObject, null))
        {
            Destroy(data.indicator.gameObject);
            return false;
        }
        else
        {
            return true;
        }
    }
    #endregion

    #region MonoBehaviour Messages
    private void Start()
    {
        //Transform minimap = canvas.Find("Minimap");
        //Transform shipStats = canvas.Find("ShipStats");
        //Transform roundInfo = canvas.Find("RoundInfo");

        //Transform minimapContent = minimap.Find("Content");
        //playerIndicator = (RectTransform)minimapContent.Find("PlayerIndicator");

        //Transform minimapTextContainer = minimap.Find("TextContainer");
        //enemyCountText = minimapTextContainer.GetComponentInChildren<TextMeshProUGUI>();

        //mapIndicators = minimapContent.Find("MapIndicators");
        //ringBoundary = (RectTransform)minimapContent.Find("RingBoundary");

        //Transform powerUsageContainer = shipStats.Find("PowerUsage");
        //Transform coreHealthContainer = shipStats.Find("CoreHealth");
        //powerUsageBar = (RectTransform)powerUsageContainer.Find("InnerBar");
        //coreHealthBar = (RectTransform)coreHealthContainer.Find("InnerBar");

        //roundCompletedContainer = canvas.Find("RoundCompleted");
        //roundFailedContainer = canvas.Find("RoundFailed");

        // the round info, round completed, and round failed ui is only in the in-game scene not the testing scene
        if (roundText)
        {
            //Transform topSection = roundInfo.GetChild(0);
            //Transform bottomSection = roundInfo.GetChild(1);
            //TextMeshProUGUI[] textComponents = bottomSection.GetComponentsInChildren<TextMeshProUGUI>();
            roundText.text = $"ROUND {GlobalsManager.gameData.currentRound}";

            //coinText = textComponents[0];
            //researchPointsText = textComponents[1];

            //roundCompletedTitleText = roundCompletedContainer.GetComponentInChildren<TextMeshProUGUI>();
            //roundFailedTitleText = roundFailedContainer.GetComponentInChildren<TextMeshProUGUI>();

            //Transform creditsEarnedStatContainer = roundCompletedContainer.Find("CreditsEarnedStat");
            //Transform timeTakenStatContainer = roundCompletedContainer.Find("TimeTakenStat");
            //Transform enemiesKilledStatContainer = roundFailedContainer.Find("EnemiesKilledStat");

            //creditsEarnedStatText = creditsEarnedStatContainer.GetComponentsInChildren<TextMeshProUGUI>()[1];
            //timeTakenStatText = timeTakenStatContainer.GetComponentsInChildren<TextMeshProUGUI>()[1];
            //enemiesKilledStatText = enemiesKilledStatContainer.GetComponentsInChildren<TextMeshProUGUI>()[1];
        }

        RefreshMinimap();
    }

    private void Update()
    {
        playerIndicator.rotation = Quaternion.Euler(0, 0, ShipController.ship.eulerAngles.z);

        int amountOfEnemies = enemies.childCount;
        enemyCountText.text = $"{amountOfEnemies} ENEM{(amountOfEnemies != 1 ? "IES" : "Y")}";

        // update indicators
        activeIndicators.RemoveAll(data => !IsIndicatorValid(data));
        foreach (Indicator indicatorData in activeIndicators)
        {
            Vector3 direction = indicatorData.trackedObject.position - ShipController.ship.position;
            float alpha = direction.magnitude / minimapWorldSize;
            Vector3 indicatorPosition = direction.normalized * 125 * Mathf.Min(alpha, 1);

            indicatorData.indicator.sizeDelta = alpha <= 1 ? new Vector2(30, 30) : new Vector2(20, 20);
            indicatorData.indicator.localPosition = indicatorPosition;
        }

        float playerOffsetAlpha = ShipController.ship.position.magnitude / minimapWorldSize;
        ringBoundary.localPosition = 125 * playerOffsetAlpha * -ShipController.ship.position.normalized;

        if (coinText)
        {
            coinText.text = $"{GlobalsManager.gameData.credits} (+{RoundManager.instance.creditsEarned})";
            researchPointsText.text = GlobalsManager.gameData.researchPoints.ToString();
        }
    }
    #endregion
}
