using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct Enemy
{
    public GameObject enemyPrefab;
    public int cost;
}

public class RoundManager : MonoBehaviour
{
    public static RoundManager instance { get; private set; }

    public List<Enemy> spawnableEnemies = new List<Enemy>();
    public int creditsEarned;
    public int initialEnemyCount;
    public bool roundFinished;

    [SerializeField] private HUDManager hudManager;

    [SerializeField] private Transform enemies;
    [SerializeField] private Transform discoveries;
    [SerializeField] private Renderer arenaRing;
    [SerializeField] private GameObject creditDiscoveryPrefab;

    public void SpawnCurrentRoundEnemies()
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
                int randEnemyIndex = Random.Range(0, spawnableEnemies.Count);
                Enemy randEnemy = spawnableEnemies[randEnemyIndex];

                if (availablePoints - randEnemy.cost >= 0)
                {
                    generatedEnemies.Add(randEnemy);
                    availablePoints -= randEnemy.cost;
                }
            }
        }
        initialEnemyCount = generatedEnemies.Count;

        // spawn the enemies randomly in the arena
        foreach (Enemy enemy in generatedEnemies)
        {
            bool foundValidPosition = false;
            Vector2 position = Vector2.zero;
            Renderer enemyRenderer = enemy.enemyPrefab.GetComponent<Renderer>();

            while (!foundValidPosition)
            {
                position = Random.insideUnitCircle.normalized * Random.Range(10, arenaRing.bounds.extents.x);
                if (Physics2D.OverlapBoxAll(position, enemyRenderer.bounds.size, 0).Length == 0)
                {
                    foundValidPosition = true;
                }
            }

            Instantiate(enemy.enemyPrefab, position, Quaternion.identity, enemies);
        }

        // spawn the discoveries randomly in the area
        for (int i = 0; i < 5; i++)
        {
            bool foundValidPosition = false;
            Vector2 position = Vector2.zero;
            Renderer enemyRenderer = creditDiscoveryPrefab.GetComponent<Renderer>();

            while (!foundValidPosition)
            {
                position = Random.insideUnitCircle.normalized * Random.Range(10, arenaRing.bounds.extents.x);
                if (Physics2D.OverlapBoxAll(position, enemyRenderer.bounds.size, 0).Length == 0)
                {
                    foundValidPosition = true;
                }
            }

            Instantiate(creditDiscoveryPrefab, position, Quaternion.identity, discoveries);
        }
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Update()
    {
        if (roundFinished) { return; }

        if (enemies.childCount == 0)
        {
            roundFinished = true;
            GlobalsManager.gameData.credits += creditsEarned;
            hudManager.ShowRoundCompletedUI();
            GlobalsManager.gameData.currentRound += 1;
            SaveManager.instance.SaveGameData(GlobalsManager.gameData);
        }

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
}

