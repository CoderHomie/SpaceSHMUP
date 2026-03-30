using UnityEngine;

public class ProjectileHero : MonoBehaviour
{
    [SerializeField]
    private WeaponType _type;

    public WeaponType type
    {
        get { return _type; }
        set { SetType(value); }
    }

    void Awake()
    {
        InvokeRepeating(nameof(CheckOffscreen), 2f, 2f);
    }

    public void SetType(WeaponType eType)
    {
        _type = eType;
        WeaponDefinition def = Main.GetWeaponDefinition(_type);
        // Mat_Projectile uses ProtoTools/UnlitAlpha — alpha 0 makes the mesh fully invisible.
        Color c = def.projectileColor;
        if (c.a <= 0f)
        {
            c.a = 1f;
        }

        GetComponent<Renderer>().material.color = c;
    }

    void CheckOffscreen()
    {
        if (Utils.ScreenBoundsCheck(GetComponent<Renderer>().bounds, BoundsTest.offScreen) != Vector3.zero)
        {
            Destroy(gameObject);
        }
    }
}
