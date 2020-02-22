using UnityEngine;
using System.Collections;
using Leap.Unity;

public class HandCycler : MonoBehaviour
{
    public string[] handNames;
    public int currentHand = 0;

    private HandModelManager manager;
 
    public Material[] LightMaterials;
    public Material[] DarkMaterials;

    private bool darkSkin = true;

    void Start()
    {
        manager = GetComponent<HandModelManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            manager.DisableGroup(handNames[currentHand]);
            currentHand = (currentHand + 1) % 3;
            manager.EnableGroup(handNames[currentHand]);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            darkSkin = !darkSkin;
        }

        UpdateHands();
    }

    public void UpdateHands()
    {
        // ToDo: not too optimal
        bool leftVisible = false;
        bool rightVisible = false;
        SkinnedMeshRenderer[] meshes = manager.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer sm in meshes)
        {
            sm.material = (darkSkin ? DarkMaterials[currentHand] : LightMaterials[currentHand]);

            leftVisible = leftVisible || sm.transform.parent.name.Contains("Left");
            rightVisible = rightVisible || sm.transform.parent.name.Contains("Right");
        }
    }
}
