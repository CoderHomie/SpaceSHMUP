using UnityEngine;
using UnityEngine.Serialization;

public class Hero : MonoBehaviour
{
    static public Hero S { get; private set; }

    [Header("Inscribed")]
    public float speed = 30;
    public float rollMult = -45;
    public float pitchMult = 30;
    public Weapon[] weapons;

    [Header("Dynamic")]
    [Range(0, 4)]
    [SerializeField]
    private float _shieldLevel = 1;

    public Bounds bounds;

    [FormerlySerializedAs("lastTriggeringGO")]
    public GameObject lastTriggerGo = null;

    void Awake()
    {
        if (S == null)
        {
            S = this;
        }
        else
        {
            Debug.LogError("Hero.Awake() - Attempted to assign second Hero.S!");
        }

        bounds = Utils.CombineBoundsOfChildren(gameObject);
    }

    void Start()
    {
        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogError("Hero: Assign the weapons array with Weapon child objects (each needs a Collar child). See Chapter 32.");
            return;
        }

        if (!ValidateWeaponsArray())
        {
            return;
        }

        ClearWeapons();
        if (weapons[0] != null)
        {
            weapons[0].SetType(WeaponType.blaster);
        }
    }

    /// <summary>Returns false if any slot is null or missing a Weapon component.</summary>
    bool ValidateWeaponsArray()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null)
            {
                Debug.LogError($"Hero: weapons[{i}] is null. Re-assign Weapon components on Weapon_{i} (script GUID may be broken — select the slot and re-add the Weapon script if needed).");
                return false;
            }
        }

        return true;
    }

    void Update()
    {
        float xAxis = Input.GetAxis("Horizontal");
        float yAxis = Input.GetAxis("Vertical");

        Vector3 pos = transform.position;
        pos.x += xAxis * speed * Time.deltaTime;
        pos.y += yAxis * speed * Time.deltaTime;
        transform.position = pos;

        bounds.center = transform.position;

        Vector3 off = Utils.ScreenBoundsCheck(bounds, BoundsTest.onScreen);
        if (off != Vector3.zero)
        {
            pos -= off;
            transform.position = pos;
        }

        transform.rotation = Quaternion.Euler(yAxis * pitchMult, xAxis * rollMult, 0);

        // Jump: Input Manager button (GetButton) + Space fallback for odd setups.
        bool fireHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.Space);
        if (fireHeld && weapons != null)
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                Weapon w = weapons[i];
                if (w != null && w.gameObject.activeInHierarchy)
                {
                    w.Fire();
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject go = Utils.FindTaggedParent(other.gameObject);
        if (go != null)
        {
            if (go.tag == "Enemy")
            {
                if (go == lastTriggerGo)
                {
                    return;
                }

                lastTriggerGo = go;
                shieldLevel--;
                Destroy(go);
            }
            else if (go.tag == "PowerUp")
            {
                AbsorbPowerUp(go);
            }
            else
            {
                Debug.Log("Triggered: " + go.name);
            }
        }
        else
        {
            Debug.Log("Triggered: " + other.gameObject.name);
        }
    }

    /// <summary>Picks up the power-up. Returns false if nothing applied (duplicate weapon with no empty slot).</summary>
    public bool AbsorbPowerUp(GameObject go)
    {
        if (go == null || !go.activeInHierarchy)
        {
            return false;
        }

        PowerUp pu = go.GetComponent<PowerUp>();
        if (pu == null || weapons == null || weapons.Length == 0 || weapons[0] == null)
        {
            Debug.LogError("Hero.AbsorbPowerUp: missing PowerUp or weapons[0].");
            return false;
        }

        switch (pu.type)
        {
            case WeaponType.shield:
                if (shieldLevel >= 4f)
                {
                    return false;
                }

                shieldLevel++;
                break;

            default:
                if (pu.type == weapons[0].type)
                {
                    Weapon w = GetEmptyWeaponSlot();
                    if (w == null)
                    {
                        return false;
                    }

                    w.SetType(pu.type);
                }
                else
                {
                    ClearWeapons();
                    weapons[0].SetType(pu.type);
                }

                break;
        }

        pu.AbsorbedBy(gameObject);
        return true;
    }

    Weapon GetEmptyWeaponSlot()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            Weapon w = weapons[i];
            if (w != null && w.type == WeaponType.none)
            {
                return w;
            }
        }
        return null;
    }

    void ClearWeapons()
    {
        if (weapons == null)
        {
            return;
        }

        foreach (Weapon w in weapons)
        {
            if (w != null)
            {
                w.SetType(WeaponType.none);
            }
        }
    }

    public float shieldLevel
    {
        get { return _shieldLevel; }
        private set
        {
            _shieldLevel = Mathf.Min(value, 4);
            if (value < 0)
            {
                Destroy(gameObject);
                Main.HERO_DIED();
            }
        }
    }
}
