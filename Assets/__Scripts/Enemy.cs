using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Inscribed")]
    public float speed = 10f;
    public float fireRate = 0.3f;
    public float health = 10;
    public int score = 100;

    public int showDamageForFrame = 2;
    public float powerUpDropChance = 1f;

    [Header("Dynamic")]
    public Bounds bounds;
    public Vector3 boundsCenterOffset;
    public Color[] originalColors;
    public Material[] materials;
    public int remainingDamageFrames = 0;

    protected virtual void Awake()
    {
        materials = Utils.GetAllMaterials(gameObject);
        originalColors = new Color[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            originalColors[i] = materials[i].color;
        }

        InvokeRepeating(nameof(CheckOffscreen), 0f, 2f);
    }

    void Update()
    {
        Move();

        if (remainingDamageFrames > 0)
        {
            remainingDamageFrames--;
            if (remainingDamageFrames == 0)
            {
                UnShowDamage();
            }
        }
    }

    public virtual void Move()
    {
        Vector3 tempPos = pos;
        tempPos.y -= speed * Time.deltaTime;
        pos = tempPos;
    }

    public Vector3 pos
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    void CheckOffscreen()
    {
        if (bounds.size == Vector3.zero)
        {
            bounds = Utils.CombineBoundsOfChildren(gameObject);
            boundsCenterOffset = bounds.center - transform.position;
        }

        bounds.center = transform.position + boundsCenterOffset;
        Vector3 off = Utils.ScreenBoundsCheck(bounds, BoundsTest.offScreen);
        if (off != Vector3.zero)
        {
            if (off.y < 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public virtual void OnCollisionEnter(Collision coll)
    {
        Enemy_4 boss = GetComponent<Enemy_4>();
        if (boss != null)
        {
            boss.HandleProjectileCollision(coll);
            return;
        }

        ProjectileHero p = TryGetProjectileHeroFromCollision(coll, transform);
        if (p == null)
        {
            return;
        }

        GameObject other = p.gameObject;
        bounds.center = transform.position + boundsCenterOffset;
        if (bounds.extents == Vector3.zero || Utils.ScreenBoundsCheck(bounds, BoundsTest.offScreen) != Vector3.zero)
        {
            Destroy(other);
            return;
        }

        ShowDamage();
        health -= DamageFromWeapon(p.type);
        if (health <= 0)
        {
            Main.S.ShipDestroyed(this);
            Destroy(gameObject);
        }

        Destroy(other);
    }

    internal static ProjectileHero TryGetProjectileHeroFromCollision(Collision coll, Transform enemyRoot)
    {
        if (coll.contactCount <= 0 || enemyRoot == null)
        {
            return null;
        }

        for (int i = 0; i < coll.contactCount; i++)
        {
            ContactPoint cp = coll.GetContact(i);
            Collider tc = cp.thisCollider;
            Collider oc = cp.otherCollider;
            Collider projectileSide = null;

            if (tc != null && ColliderIsUnderEnemy(tc, enemyRoot) && oc != null && !ColliderIsUnderEnemy(oc, enemyRoot))
            {
                projectileSide = oc;
            }
            else if (oc != null && ColliderIsUnderEnemy(oc, enemyRoot) && tc != null && !ColliderIsUnderEnemy(tc, enemyRoot))
            {
                projectileSide = tc;
            }

            if (projectileSide == null)
            {
                continue;
            }

            ProjectileHero ph = projectileSide.GetComponent<ProjectileHero>();
            if (ph == null)
            {
                ph = projectileSide.GetComponentInParent<ProjectileHero>();
            }

            if (ph != null)
            {
                return ph;
            }
        }

        return null;
    }

    internal static bool ColliderIsUnderEnemy(Collider c, Transform enemyRoot)
    {
        return c != null && ColliderIsUnderEnemy(c.transform, enemyRoot);
    }

    static bool ColliderIsUnderEnemy(Transform t, Transform enemyRoot)
    {
        while (t != null)
        {
            if (t == enemyRoot)
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }

    internal static GameObject InferEnemyColliderGameObject(Collision coll, Transform enemyRoot)
    {
        if (coll.contactCount <= 0 || enemyRoot == null)
        {
            return null;
        }

        for (int i = 0; i < coll.contactCount; i++)
        {
            ContactPoint cp = coll.GetContact(i);
            if (cp.thisCollider != null && ColliderIsUnderEnemy(cp.thisCollider, enemyRoot))
            {
                return cp.thisCollider.gameObject;
            }

            if (cp.otherCollider != null && ColliderIsUnderEnemy(cp.otherCollider, enemyRoot))
            {
                return cp.otherCollider.gameObject;
            }
        }

        return null;
    }

    protected static float DamageFromWeapon(WeaponType wt)
    {
        WeaponDefinition def = Main.GetWeaponDefinition(wt);
        if (def.damageOnHit > 0f)
        {
            return def.damageOnHit;
        }

        return Main.GetWeaponDefinition(WeaponType.blaster).damageOnHit;
    }

    void ShowDamage()
    {
        foreach (Material m in materials)
        {
            m.color = Color.red;
        }
        remainingDamageFrames = showDamageForFrame;
    }

    void UnShowDamage()
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].color = originalColors[i];
        }
    }
}
