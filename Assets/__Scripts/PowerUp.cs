using UnityEngine;

public class PowerUp : MonoBehaviour
{
    // This is an unusual but handy use of Vector2's: x holds a min value and y a max value
    // for a Random.Range() that will be called later
    public Vector2 rotMinMax = new Vector2(15, 90);
    public Vector2 driftMinMax = new Vector2(0.25f, 2);
    public float lifeTime = 6f; // Seconds the powerup exists
    public float fadeTime = 4f; // Seconds it will then fade

    [Header("Dynamic")]
    [SerializeField]
    private WeaponType _type; // Errata Ch.32: use _type (not type) for the serialized field
    public GameObject cube; // Reference to the Cube child
    public TextMesh letter; // Reference to the TextMesh
    public Vector3 rotPerSecond; // Euler rotation speed
    public float birthTime;

    public WeaponType type
    {
        get { return _type; }
    }

    void Awake()
    {
        cube = transform.Find("Cube").gameObject;
        letter = GetComponent<TextMesh>();

        Vector3 vel = Random.onUnitSphere;
        vel.z = 0;
        vel.Normalize();
        vel *= Random.Range(driftMinMax.x, driftMinMax.y);
        GetComponent<Rigidbody>().velocity = vel;

        transform.rotation = Quaternion.identity;

        rotPerSecond = new Vector3(
            Random.Range(rotMinMax.x, rotMinMax.y),
            Random.Range(rotMinMax.x, rotMinMax.y),
            Random.Range(rotMinMax.x, rotMinMax.y));

        InvokeRepeating(nameof(CheckOffscreen), 2f, 2f);

        birthTime = Time.time;
    }

    void OnTriggerEnter(Collider other)
    {
        Hero hero = other.GetComponentInParent<Hero>();
        if (hero == null && Hero.S != null)
        {
            hero = Hero.S;
        }

        if (hero == null)
        {
            return;
        }

        hero.AbsorbPowerUp(gameObject);
    }

    void Update()
    {
        if (cube == null || letter == null)
        {
            return;
        }

        cube.transform.rotation = Quaternion.Euler(rotPerSecond * Time.time);

        float elapsed = Time.time - birthTime;
        if (elapsed > lifeTime + fadeTime)
        {
            Destroy(gameObject);
            return;
        }

        float u = (Time.time - (birthTime + lifeTime)) / fadeTime;
        if (u >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        if (u > 0)
        {
            float fade = 1f - u;

            Renderer cubeRend = cube.GetComponent<Renderer>();
            if (cubeRend != null && cubeRend.material != null)
            {
                Color c = cubeRend.material.color;
                c.a = fade;
                cubeRend.material.color = c;
            }

            Color lc = letter.color;
            lc.a = fade;
            letter.color = lc;

            MeshRenderer rootMr = GetComponent<MeshRenderer>();
            if (rootMr != null && rootMr.material != null)
            {
                Color rm = rootMr.material.color;
                rm.a = lc.a;
                rm.r = lc.r;
                rm.g = lc.g;
                rm.b = lc.b;
                rootMr.material.color = rm;
            }

            if (fade <= 0.02f)
            {
                letter.text = string.Empty;
            }
        }
    }

    // This SetType() differs from those on Weapon and ProjectileHero
    public void SetType(WeaponType wt)
    {
        WeaponDefinition def = Main.GetWeaponDefinition(wt);
        if (cube != null)
        {
            Renderer cr = cube.GetComponent<Renderer>();
            if (cr != null && cr.material != null)
            {
                Color col = def.color;
                if (col.a <= 0f)
                {
                    col.a = 1f;
                }

                cr.material.color = col;
            }
        }

        if (letter != null)
        {
            letter.text = string.IsNullOrEmpty(def.letter) ? "?" : def.letter;
            Color lc = letter.color;
            if (lc.a <= 0f)
            {
                lc.a = 1f;
            }

            letter.color = lc;
        }

        _type = wt; // Errata Ch.32: assign _type, not type
    }

    public void AbsorbedBy(GameObject target)
    {
        Destroy(gameObject);
    }

    void CheckOffscreen()
    {
        if (Utils.ScreenBoundsCheck(cube.GetComponent<Renderer>().bounds, BoundsTest.offScreen) != Vector3.zero)
        {
            Destroy(gameObject);
        }
    }
}
