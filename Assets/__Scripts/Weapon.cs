using UnityEngine;

// This is an enum of the various possible weapon types
// It also includes a "shield" type to allow a shield power-up
// Items marked [NI] below are Not Implemented in this book
public enum WeaponType
{
    none,    // The default / no weapon
    blaster, // A simple blaster
    spread,  // Two shots simultaneously
    phaser,  // Shots that move in waves [NI]
    missile, // Homing missiles [NI]
    laser,   // Damage over time [NI]
    shield,  // Raise shieldLevel
}

// The WeaponDefinition class allows you to set the properties
// of a specific weapon in the Inspector. Main has an array
// of WeaponDefinitions that makes this possible
[System.Serializable]
public class WeaponDefinition
{
    public WeaponType type = WeaponType.none;
    public string letter; // The letter to show on the power-up
    public Color color = Color.white; // Color of collar and power up
    public GameObject projectilePrefab; // Prefab for projectile
    public Color projectileColor = Color.white;
    public float damageOnHit = 0; // Amount of damage caused
    public float continuousDamage = 0; // Damage per second (laser)
    public float delayBetweenShots = 0;
    public float velocity = 20; // Speed of projectiles
}

// Note: weapon prefabs, colors, and so on are set in the class Main
public class Weapon : MonoBehaviour
{
    static public Transform PROJECTILE_ANCHOR;

    [Header("Dynamic")]
    [SerializeField]
    private WeaponType _type = WeaponType.blaster;
    public WeaponDefinition def;
    public GameObject collar;
    public float lastShot; // Time last shot was fired

    bool _warnedMissingPrefab;

    void Awake()
    {
        collar = transform.Find("Collar").gameObject;
    }

    void Start()
    {
        // Do not call SetType(_type) here — Hero.Start configures slots. Calling SetType from Weapon.Start
        // runs in arbitrary order vs Hero.Start and can re-activate Weapon_1 (spread) after ClearWeapons.

        if (PROJECTILE_ANCHOR == null)
        {
            GameObject go = new GameObject("_Projectile_Anchor");
            PROJECTILE_ANCHOR = go.transform;
        }
    }

    public WeaponType type
    {
        get { return _type; }
        set { SetType(value); }
    }

    public void SetType(WeaponType wt)
    {
        _type = wt;

        if (type == WeaponType.none)
        {
            gameObject.SetActive(false);
            return;
        }

        // Resolve definition before SetActive(true); Hero calls Fire() on active children only.
        def = Main.GetWeaponDefinition(_type);
        if (def.projectilePrefab != null)
        {
            _warnedMissingPrefab = false;
        }

        collar.GetComponent<Renderer>().material.color = def.color;
        lastShot = 0;

        gameObject.SetActive(true);
    }

    public void Fire()
    {
        if (!gameObject.activeInHierarchy) return;
        if (def == null || def.projectilePrefab == null)
        {
            if (!_warnedMissingPrefab)
            {
                Debug.LogWarning("Weapon: Cannot fire — assign a projectile prefab in Main > weaponDefinitions for " + type + ".");
                _warnedMissingPrefab = true;
            }

            return;
        }

        if (Time.time - lastShot < def.delayBetweenShots)
        {
            return;
        }

        ProjectileHero p;
        switch (type)
        {
            case WeaponType.blaster:
                p = MakeProjectile();
                if (p != null)
                {
                    p.GetComponent<Rigidbody>().velocity = Vector3.up * def.velocity;
                }

                break;

            case WeaponType.spread:
                p = MakeProjectile();
                if (p != null)
                {
                    p.GetComponent<Rigidbody>().velocity = Vector3.up * def.velocity;
                }

                p = MakeProjectile();
                if (p != null)
                {
                    p.GetComponent<Rigidbody>().velocity = new Vector3(0.2f, 0.9f, 0) * def.velocity;
                }

                break;
        }
    }

    public ProjectileHero MakeProjectile()
    {
        if (def.projectilePrefab == null)
        {
            Debug.LogError("Weapon: projectilePrefab missing in Main weaponDefinitions for type " + type);
            return null;
        }

        if (PROJECTILE_ANCHOR == null)
        {
            GameObject anchorGo = new GameObject("_Projectile_Anchor");
            PROJECTILE_ANCHOR = anchorGo.transform;
        }

        GameObject go = Instantiate(def.projectilePrefab);
        if (transform.parent.gameObject.tag == "Hero")
        {
            go.tag = "ProjectileHero";
            int pl = LayerMask.NameToLayer("ProjectileHero");
            if (pl < 0)
            {
                Debug.LogError("Weapon: Layer 'ProjectileHero' is missing. Add it in Edit > Project Settings > Tags and Layers.");
            }
            else
            {
                go.layer = pl;
            }
        }
        else
        {
            go.tag = "ProjectileEnemy";
            int pl = LayerMask.NameToLayer("ProjectileEnemy");
            if (pl >= 0)
            {
                go.layer = pl;
            }
        }

        go.transform.position = collar.transform.position;
        go.transform.SetParent(PROJECTILE_ANCHOR, true);
        ProjectileHero p = go.GetComponent<ProjectileHero>();
        if (p == null)
        {
            Debug.LogError("Weapon: projectile prefab must have a ProjectileHero component.");
            Destroy(go);
            return null;
        }

        p.type = type;

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        lastShot = Time.time;
        return p;
    }
}
