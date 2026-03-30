using UnityEngine;

public class Parallax : MonoBehaviour
{
    public GameObject poi; // The player ship
    public GameObject[] panels; // The scrolling foregrounds
    public float scrollSpeed = -30f;
    // motionMult controls how much panels react to player movement
    public float motionMult = 0.25f;

    private float panelHt; // Height of each panel
    private float depth; // Depth of panels (that is, pos.z)

    void Start()
    {
        if (panels == null || panels.Length < 2 || panels[0] == null || panels[1] == null)
        {
            Debug.LogError("Parallax: assign two panel GameObjects in the Inspector.");
            enabled = false;
            return;
        }

        panelHt = panels[0].transform.localScale.y;
        depth = panels[0].transform.position.z;

        panels[0].transform.position = new Vector3(0, 0, depth);
        panels[1].transform.position = new Vector3(0, panelHt, depth);
    }

    void Update()
    {
        if (panels == null || panels.Length < 2 || panels[0] == null || panels[1] == null)
        {
            return;
        }

        float tY, tX = 0;
        tY = Time.time * scrollSpeed % panelHt + (panelHt * 0.5f);

        if (poi != null)
        {
            tX = -poi.transform.position.x * motionMult;
        }

        panels[0].transform.position = new Vector3(tX, tY, depth);
        if (tY >= 0)
        {
            panels[1].transform.position = new Vector3(tX, tY - panelHt, depth);
        }
        else
        {
            panels[1].transform.position = new Vector3(tX, tY + panelHt, depth);
        }
    }
}
