using UnityEngine;

public class Enemy_2 : Enemy
{
    // Enemy_2 uses a Sin wave to modify a 2-point linear interpolation
    // Errata Ch.32: use points (not vList) for the control points array
    public Vector3[] points;
    public float birthTime;
    public float lifeTime = 10;
    // Determines how much the Sine wave will affect movement
    public float sinEccentricity = 0.6f;

    void Start()
    {
        points = new Vector3[2];

        Vector3 cbMin = Utils.camBounds.min;
        Vector3 cbMax = Utils.camBounds.max;

        Vector3 v = Vector3.zero;
        v.x = cbMin.x - Main.S.enemySpawnPadding;
        v.y = Random.Range(cbMin.y, cbMax.y);
        points[0] = v;

        v = Vector3.zero;
        v.x = cbMax.x + Main.S.enemySpawnPadding;
        v.y = Random.Range(cbMin.y, cbMax.y);
        points[1] = v;

        if (Random.value < 0.5f)
        {
            Vector3 p0 = points[0];
            p0.x *= -1;
            points[0] = p0;
            Vector3 p1 = points[1];
            p1.x *= -1;
            points[1] = p1;
        }

        birthTime = Time.time;
    }

    public override void Move()
    {
        float u = (Time.time - birthTime) / lifeTime;

        if (u > 1)
        {
            Destroy(gameObject);
            return;
        }

        u = u + sinEccentricity * (Mathf.Sin(u * Mathf.PI * 2));

        pos = (1 - u) * points[0] + u * points[1];
        base.Move();
    }
}
