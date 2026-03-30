using UnityEngine;

// Part is another serializable data storage class just like WeaponDefinition
[System.Serializable]
public class Part
{
    public string name; // The name of this part
    public float health; // The amount of health this part has
    public string[] protectedBy; // The other parts that protect this

    public GameObject go; // The GameObject of this part
    public Material mat; // The Material to show damage
}

public class Enemy_4 : Enemy
{
    // Enemy_4 will start offscreen and then pick a random point on screen to
    // move to. Once it has arrived, it will pick another random point and continue
    // until the player has shot it down
    public Vector3[] points;
    public float timeStart;
    public Part[] parts;
    public float duration = 4;

    protected override void Awake()
    {
        base.Awake();
        BindAllParts();
    }

    void Start()
    {
        points = new Vector3[2];
        points[0] = pos;
        points[1] = pos;

        InitMovement();
        BindAllParts();
    }

    void BindAllParts()
    {
        if (parts == null)
        {
            return;
        }

        foreach (Part prt in parts)
        {
            BindPartToHierarchy(prt);
        }

        BindPartsUsingMeshColliders();
        AssignUnboundPartsToUnusedMeshColliders();
    }

    void BindPartToHierarchy(Part prt)
    {
        if (prt == null) return;

        Transform t = !string.IsNullOrEmpty(prt.name) ? transform.Find(prt.name) : null;
        if (t == null && !string.IsNullOrEmpty(prt.name))
        {
            int slash = prt.name.LastIndexOf('/');
            string leaf = slash >= 0 ? prt.name.Substring(slash + 1) : prt.name;
            t = FindDeepChildByName(transform, leaf);
        }

        if (t != null)
        {
            prt.go = t.gameObject;
            Renderer r = prt.go.GetComponent<Renderer>();
            if (r != null)
            {
                prt.mat = r.material;
            }
        }
        else
        {
            Debug.LogWarning("Enemy_4: Part not found (inspector path or leaf name): " + prt.name);
        }
    }

    static Transform FindDeepChildByName(Transform root, string leafName)
    {
        foreach (Transform c in root.GetComponentsInChildren<Transform>(true))
        {
            if (c.name.Equals(leafName, System.StringComparison.OrdinalIgnoreCase))
            {
                return c;
            }
        }

        return null;
    }

    /// <summary>Assigns any still-unbound Part to a MeshCollider whose GameObject name matches the Part path leaf (case-insensitive).</summary>
    void BindPartsUsingMeshColliders()
    {
        if (parts == null)
        {
            return;
        }

        MeshCollider[] cols = GetComponentsInChildren<MeshCollider>(true);
        foreach (Part prt in parts)
        {
            if (prt == null || prt.go != null || string.IsNullOrEmpty(prt.name))
            {
                continue;
            }

            int slash = prt.name.LastIndexOf('/');
            string leaf = slash >= 0 ? prt.name.Substring(slash + 1) : prt.name;

            foreach (MeshCollider col in cols)
            {
                if (col == null || col.gameObject == null) continue;
                if (!col.gameObject.name.Equals(leaf, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                prt.go = col.gameObject;
                Renderer r = prt.go.GetComponent<Renderer>();
                if (r != null)
                {
                    prt.mat = r.material;
                }

                break;
            }
        }
    }

    /// <summary>If names/paths still do not match the FBX, map each unbound Part to an unused MeshCollider in hierarchy order.</summary>
    void AssignUnboundPartsToUnusedMeshColliders()
    {
        MeshCollider[] cols = GetComponentsInChildren<MeshCollider>(true);
        System.Array.Sort(cols, (a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));

        System.Collections.Generic.HashSet<GameObject> used = new System.Collections.Generic.HashSet<GameObject>();

        foreach (Part prt in parts)
        {
            if (prt != null && prt.go != null)
            {
                used.Add(prt.go);
            }
        }

        int ci = 0;
        foreach (Part prt in parts)
        {
            if (prt == null || prt.go != null)
            {
                continue;
            }

            while (ci < cols.Length && used.Contains(cols[ci].gameObject))
            {
                ci++;
            }

            if (ci >= cols.Length)
            {
                break;
            }

            prt.go = cols[ci].gameObject;
            used.Add(prt.go);
            Renderer r = prt.go.GetComponent<Renderer>();
            if (r != null)
            {
                prt.mat = r.material;
            }

            ci++;
        }
    }

    void InitMovement()
    {
        Vector3 p1 = Vector3.zero;
        float esp = Main.S.enemySpawnPadding;
        Bounds cBounds = Utils.camBounds;
        p1.x = Random.Range(cBounds.min.x + esp, cBounds.max.x - esp);
        p1.y = Random.Range(cBounds.min.y + esp, cBounds.max.y - esp);

        points[0] = points[1];
        points[1] = p1;

        timeStart = Time.time;
    }

    public override void Move()
    {
        float u = (Time.time - timeStart) / duration;
        if (u >= 1)
        {
            InitMovement();
            u = 0;
        }

        u = 1 - Mathf.Pow(1 - u, 2);

        pos = (1 - u) * points[0] + u * points[1];
    }

    /// <summary>Called only from <see cref="Enemy.OnCollisionEnter"/> so boss logic is never duplicated or skipped.</summary>
    public void HandleProjectileCollision(Collision coll)
    {
        ProjectileHero p = Enemy.TryGetProjectileHeroFromCollision(coll, transform);
        if (p == null)
        {
            return;
        }

        GameObject other = p.gameObject;

        if (bounds.size == Vector3.zero)
        {
            bounds = Utils.CombineBoundsOfChildren(gameObject);
            boundsCenterOffset = bounds.center - transform.position;
        }

        bounds.center = transform.position + boundsCenterOffset;
        if (Utils.ScreenBoundsCheck(bounds, BoundsTest.offScreen) != Vector3.zero)
        {
            Destroy(other);
            return;
        }

        GameObject goHit = Enemy.InferEnemyColliderGameObject(coll, transform);
        Part prtHit = goHit != null ? ResolvePartFromCollider(goHit) : null;

        if (prtHit == null && parts != null && parts.Length == 1)
        {
            prtHit = parts[0];
        }

        if (prtHit == null)
        {
            Destroy(other);
            return;
        }

        if (prtHit.protectedBy != null)
        {
            foreach (string s in prtHit.protectedBy)
            {
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }

                if (!Destroyed(s))
                {
                    Destroy(other);
                    return;
                }
            }
        }

        float damage = DamageFromWeapon(p.type);
        prtHit.health -= damage;
        ShowLocalizedDamage(prtHit.mat);
        if (prtHit.health <= 0 && prtHit.go != null)
        {
            prtHit.go.SetActive(false);
        }

        if (AreAllPartsDestroyed())
        {
            Main.S.ShipDestroyed(this);
            Destroy(gameObject);
        }

        Destroy(other);
    }

    Part FindPart(string n)
    {
        foreach (Part prt in parts)
        {
            if (prt.name == n)
            {
                return prt;
            }
        }
        return null;
    }

    Part FindPart(GameObject go)
    {
        foreach (Part prt in parts)
        {
            if (prt.go == go)
            {
                return prt;
            }
        }
        return null;
    }

    /// <summary>Walk from the collider's GameObject up toward this enemy — hits must resolve to a Part (root-only colliders break multipart damage).</summary>
    Part ResolvePartFromCollider(GameObject hitGo)
    {
        if (hitGo == null)
        {
            return null;
        }

        Transform t = hitGo.transform;
        while (t != null && t != transform)
        {
            Part p = FindPart(t.gameObject);
            if (p != null)
            {
                return p;
            }

            t = t.parent;
        }

        return null;
    }

    bool Destroyed(GameObject go)
    {
        return Destroyed(FindPart(go));
    }

    bool Destroyed(string n)
    {
        return Destroyed(FindPart(n));
    }

    bool Destroyed(Part prt)
    {
        if (prt == null)
        {
            return true;
        }
        return prt.health <= 0;
    }

    bool AreAllPartsDestroyed()
    {
        if (parts == null || parts.Length == 0)
        {
            return false;
        }

        foreach (Part prt in parts)
        {
            if (prt != null && prt.health > 0f)
            {
                return false;
            }
        }

        return true;
    }

    void ShowLocalizedDamage(Material m)
    {
        if (m == null)
        {
            return;
        }

        m.color = Color.red;
        remainingDamageFrames = showDamageForFrame;
    }
}
