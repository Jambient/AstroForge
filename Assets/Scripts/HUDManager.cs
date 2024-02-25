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
    public static HUDManager instance { get; private set; }

    [SerializeField] private Transform canvas;
    [SerializeField] private Transform enemies;
    [SerializeField] private Transform discoveries;
    [SerializeField] private GameObject enemyIndicator;
    [SerializeField] private GameObject discoveryIndicator;

    private const float minimapWorldSize = 15;

    // round info ui references
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI researchPointsText;

    // ship info ui references
    private RectTransform powerUsageBar;
    private RectTransform coreHealthBar;

    // round completed ui references
    private Transform roundCompletedContainer;
    private TextMeshProUGUI roundCompletedTitleText;
    private TextMeshProUGUI creditsEarnedStatText;
    private TextMeshProUGUI timeTakenStatText;

    // round failed ui references
    private Transform roundFailedContainer;
    private TextMeshProUGUI roundFailedTitleText;
    private TextMeshProUGUI enemiesKilledStatText;

    // minimap ui references
    private RectTransform playerIndicator;
    private TextMeshProUGUI enemyCountText;
    private RectTransform ringBoundary;
    private Transform mapIndicators;
    private List<Indicator> activeIndicators = new List<Indicator>();

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

    public void ShowRoundCompletedUI()
    {
        roundCompletedTitleText.text = $"ROUND {GlobalsManager.gameData.currentRound} COMPLETE";
        creditsEarnedStatText.text = $"+{RoundManager.instance.creditsEarned} GC";
        timeTakenStatText.text = $"{Mathf.FloorToInt(Time.timeSinceLevelLoad / 60f).ToString("00")}:{(Time.timeSinceLevelLoad % 60).ToString("00")}";
        roundCompletedContainer.gameObject.SetActive(true);
    }

    public void ShowRoundFailedUI()
    {
        roundFailedTitleText.text = $"ROUND {GlobalsManager.gameData.currentRound} FAILED";
        enemiesKilledStatText.text = $"{RoundManager.instance.initialEnemyCount - enemies.childCount}/{RoundManager.instance.initialEnemyCount}";
        roundFailedContainer.gameObject.SetActive(true);
    }

    public void ReturnToHanger()
    {
        SceneManager.LoadScene("ShipBuilding");
    }

    private void AddNewIndicator(Transform enemy, GameObject indicatorPrefab)
    {
        GameObject newIndicator = Instantiate(indicatorPrefab, mapIndicators);
        Indicator indicatorData = new Indicator();
        indicatorData.trackedObject = enemy;
        indicatorData.indicator = newIndicator.GetComponent<RectTransform>();

        activeIndicators.Add(indicatorData);
    }

    private bool CheckIndicator(Indicator data)
    {
        if (data.trackedObject == null && !ReferenceEquals(data.trackedObject, null))
        {
            Destroy(data.indicator.gameObject);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Start()
    {
        Transform minimap = canvas.Find("Minimap");
        Transform shipStats = canvas.Find("ShipStats");
        Transform roundInfo = canvas.Find("RoundInfo");

        Transform minimapContent = minimap.Find("Content");
        playerIndicator = (RectTransform)minimapContent.Find("PlayerIndicator");

        Transform minimapTextContainer = minimap.Find("TextContainer");
        enemyCountText = minimapTextContainer.GetComponentInChildren<TextMeshProUGUI>();

        mapIndicators = minimapContent.Find("MapIndicators");
        ringBoundary = (RectTransform)minimapContent.Find("RingBoundary");

        Transform powerUsageContainer = shipStats.Find("PowerUsage");
        Transform coreHealthContainer = shipStats.Find("CoreHealth");
        powerUsageBar = (RectTransform)powerUsageContainer.Find("InnerBar");
        coreHealthBar = (RectTransform)coreHealthContainer.Find("InnerBar");

        roundCompletedContainer = canvas.Find("RoundCompleted");
        roundFailedContainer = canvas.Find("RoundFailed");

        // the round info, round completed, and round failed ui is only in the in-game scene not the testing scene
        if (roundInfo)
        {
            Transform topSection = roundInfo.GetChild(0);
            Transform bottomSection = roundInfo.GetChild(1);
            TextMeshProUGUI[] textComponents = bottomSection.GetComponentsInChildren<TextMeshProUGUI>();

            TextMeshProUGUI roundText = topSection.GetComponentInChildren<TextMeshProUGUI>();
            roundText.text = $"ROUND {GlobalsManager.gameData.currentRound}";

            coinText = textComponents[0];
            researchPointsText = textComponents[1];

            roundCompletedTitleText = roundCompletedContainer.GetComponentInChildren<TextMeshProUGUI>();
            roundFailedTitleText = roundFailedContainer.GetComponentInChildren<TextMeshProUGUI>();

            Transform creditsEarnedStatContainer = roundCompletedContainer.Find("CreditsEarnedStat");
            Transform timeTakenStatContainer = roundCompletedContainer.Find("TimeTakenStat");
            Transform enemiesKilledStatContainer = roundFailedContainer.Find("EnemiesKilledStat");

            creditsEarnedStatText = creditsEarnedStatContainer.GetComponentsInChildren<TextMeshProUGUI>()[1];
            timeTakenStatText = timeTakenStatContainer.GetComponentsInChildren<TextMeshProUGUI>()[1];
            enemiesKilledStatText = enemiesKilledStatContainer.GetComponentsInChildren<TextMeshProUGUI>()[1];
        }

        RefreshMinimap();
    }

    private void Update()
    {
        playerIndicator.rotation = Quaternion.Euler(0, 0, ShipController.ship.eulerAngles.z);

        int amountOfEnemies = enemies.childCount;
        enemyCountText.text = $"{amountOfEnemies} ENEM{(amountOfEnemies != 1 ? "IES" : "Y")}";

        // update indicators
        activeIndicators.RemoveAll(CheckIndicator);
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
