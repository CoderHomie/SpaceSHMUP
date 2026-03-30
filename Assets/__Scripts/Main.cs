using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-100)]
public class Main : MonoBehaviour
{
    static public Main S;

    static public Dictionary<WeaponType, WeaponDefinition> W_DEFS;

    [Header("Inscribed")]
    public GameObject[] prefabEnemies;
    public float enemySpawnPerSecond = 0.5f;
    [FormerlySerializedAs("enemyInsetDefault")]
    public float enemySpawnPadding = 1.5f;
    public WeaponDefinition[] weaponDefinitions;
    public GameObject prefabPowerUp;
    public WeaponType[] powerUpFrequency = new WeaponType[]
    {
        WeaponType.blaster, WeaponType.blaster, WeaponType.spread, WeaponType.shield
    };
    public float gameRestartDelay = 2f;

    [Header("Dynamic")]
    public WeaponType[] activeWeaponTypes;
    public float enemySpawnRate;

    public void ShipDestroyed(Enemy e)
    {
        if (Random.value <= e.powerUpDropChance)
        {
            if (prefabPowerUp == null)
            {
                Debug.LogWarning("Main: prefabPowerUp is not assigned; skipping power-up spawn.");
                return;
            }

            int ndx = Random.Range(0, powerUpFrequency.Length);
            WeaponType puType = powerUpFrequency[ndx];

            GameObject go = Instantiate(prefabPowerUp);
            PowerUp pu = go.GetComponent<PowerUp>();
            if (pu == null)
            {
                Debug.LogError("Main: prefabPowerUp must have a PowerUp component.");
                Destroy(go);
                return;
            }

            pu.SetType(puType);
            pu.transform.position = e.transform.position;
        }
    }

    void Awake()
    {
        S = this;

        Utils.SetCameraBounds();

        enemySpawnRate = 1f / enemySpawnPerSecond;
        Invoke(nameof(SpawnEnemy), enemySpawnRate);

        W_DEFS = new Dictionary<WeaponType, WeaponDefinition>();
        if (weaponDefinitions == null || weaponDefinitions.Length == 0)
        {
            Debug.LogError("Main: Assign the weaponDefinitions array (Chapter 32). Blaster/spread/shield need entries with projectile prefabs and colors.");
        }
        else
        {
            foreach (WeaponDefinition def in weaponDefinitions)
            {
                if (def == null) continue;
                W_DEFS[def.type] = def;
            }
        }

        EnsureGameplayLayerCollisions();
    }

    /// <summary>
    /// Makes sure Physics layer matrix allows hero shots vs enemies and shield vs enemies.
    /// Project settings matrix can leave pairs unchecked; this forces collision where needed.
    /// </summary>
    static void EnsureGameplayLayerCollisions()
    {
        void AllowPair(string a, string b)
        {
            int la = LayerMask.NameToLayer(a);
            int lb = LayerMask.NameToLayer(b);
            if (la < 0 || lb < 0)
            {
                return;
            }

            Physics.IgnoreLayerCollision(la, lb, false);
        }

        AllowPair("ProjectileHero", "Enemy");
        AllowPair("ProjectileEnemy", "Hero");
        AllowPair("Hero", "Enemy");
        AllowPair("PowerUp", "Hero");
    }

    static public WeaponDefinition GetWeaponDefinition(WeaponType wt)
    {
        if (W_DEFS.ContainsKey(wt))
        {
            return W_DEFS[wt];
        }
        return new WeaponDefinition();
    }

    void Start()
    {
        if (weaponDefinitions == null || weaponDefinitions.Length == 0)
        {
            activeWeaponTypes = new WeaponType[0];
            return;
        }

        activeWeaponTypes = new WeaponType[weaponDefinitions.Length];
        for (int i = 0; i < weaponDefinitions.Length; i++)
        {
            activeWeaponTypes[i] = weaponDefinitions[i].type;
        }
    }

    public void SpawnEnemy()
    {
        int ndx = Random.Range(0, prefabEnemies.Length);
        GameObject go = Instantiate(prefabEnemies[ndx]);

        Vector3 pos = Vector3.zero;
        float xMin = Utils.camBounds.min.x + enemySpawnPadding;
        float xMax = Utils.camBounds.max.x - enemySpawnPadding;
        pos.x = Random.Range(xMin, xMax);
        pos.y = Utils.camBounds.max.y + enemySpawnPadding;
        go.transform.position = pos;

        Invoke(nameof(SpawnEnemy), enemySpawnRate);
    }

    void DelayedRestart()
    {
        Invoke(nameof(Restart), gameRestartDelay);
    }

    void Restart()
    {
        SceneManager.LoadScene("_Scene_0");
    }

    static public void HERO_DIED()
    {
        S.DelayedRestart();
    }
}
