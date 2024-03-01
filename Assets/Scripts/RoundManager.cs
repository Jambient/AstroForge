using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct Enemy
{
    public GameObject enemyPrefab;
    public int cost;
}

public class RoundManager : MonoBehaviour
{
    #region Variables
    public static RoundManager instance { get; private set; }

    [Header("Public Variables")]
    public List<Enemy> spawnableEnemies = new List<Enemy>();
    public int creditsEarned;
    public int initialEnemyCount;
    public bool roundFinished;

    [Header("Script References")]
    [SerializeField] private HUDManager hudManager;

    [Header("Object References")]
    [SerializeField] private Transform enemies;
    [SerializeField] private Transform discoveries;
    [SerializeField] private Renderer arenaRing;
    [SerializeField] private GameObject creditDiscoveryPrefab;
    #endregion

    #region Private Methods
    /// <summary>
    /// Spawns enemies based on the current round
    /// </summary>
    private void SpawnCurrentRoundEnemies()
    {
        int round = GlobalsManager.gameData.currentRound;
        int roundPoints = round * 4;
        int availablePoints = roundPoints;

        // generate a valid list of enemies
        List<Enemy> generatedEnemies = new List<Enemy>();
        while (generatedEnemies.Sum((data) => data.cost) < roundPoints * 0.85)
        {
            availablePoints = roundPoints;
            generatedEnemies.Clear();

            while (availablePoints > 0 && generatedEnemies.Count < 30)
            {
                // get the enemies that are "affordable" given the available points
                List<Enemy> enemiesWithinPoints = spawnableEnemies.Where((data) => availablePoints - data.cost >= 0).ToList();

                if (enemiesWithinPoints.Count > 0)
                {
                    // get a random enemy from the available enemies and add it to the generated enemies list
                    int randEnemyIndex = Random.Range(0, enemiesWithinPoints.Count);
                    Enemy randEnemy = enemiesWithinPoints[randEnemyIndex];

                    generatedEnemies.Add(randEnemy);
                    availablePoints -= randEnemy.cost;
                } else
                {
                    break;
                }
            }
        }
        initialEnemyCount = generatedEnemies.Count;

        // spawn the enemies randomly in the arena
        foreach (Enemy enemy in generatedEnemies)
        {
            Renderer enemyRenderer = enemy.enemyPrefab.GetComponent<Renderer>();
            Vector2 position = GetValidPositionInsideArena(enemyRenderer.bounds.size);

            Instantiate(enemy.enemyPrefab, position, Quaternion.identity, enemies);
        }

        // spawn the discoveries randomly in the arena
        for (int i = 0; i < 3; i++)
        {
            Renderer discoveryRenderer = creditDiscoveryPrefab.GetComponent<Renderer>();
            Vector2 position = GetValidPositionInsideArena(discoveryRenderer.bounds.size);

            Instantiate(creditDiscoveryPrefab, position, Quaternion.identity, discoveries);
        }
    }

    /// <summary>
    /// Gets a valid spawning position that is inside the arena
    /// </summary>
    /// <param name="boundsSize"></param>
    /// <returns></returns>
    private Vector2 GetValidPositionInsideArena(Vector3 boundsSize)
    {
        bool foundValidPosition = false;
        Vector2 position = Vector2.zero;

        // find a valid position with the same method used for the enemy spawning
        while (!foundValidPosition)
        {
            position = Random.insideUnitCircle.normalized * Random.Range(10, arenaRing.bounds.extents.x);
            if (Physics2D.OverlapBoxAll(position, boundsSize, 0).Length == 0)
            {
                foundValidPosition = true;
            }
        }

        return position;
    }
    #endregion

    #region MonoBehaviour Messages
    private void Update()
    {
        if (roundFinished) { return; }

        // check if all enemies have been killed by the player
        if (enemies.childCount == 0)
        {
            roundFinished = true;
            GlobalsManager.gameData.credits += creditsEarned;
            hudManager.ShowRoundCompletedUI();

            GlobalsManager.gameData.currentRound += 1;
            SaveManager.instance.SaveGameData(GlobalsManager.gameData);
        }

        // check if the players ship core has been destroyed
        if (ShipController.core == null)
        {
            roundFinished = true;
            hudManager.ShowRoundFailedUI();
        }
    }

    private void Start()
    {
        SpawnCurrentRoundEnemies();
        hudManager.RefreshMinimap();
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
    #endregion
}

