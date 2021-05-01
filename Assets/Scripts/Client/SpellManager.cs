using UnityEngine;

public class SpellManager : MonoBehaviour
{
    public int selectedBlock = 0;

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && selectedBlock != 1)
        {
            SetEmitterActive(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && selectedBlock != 2)
        {
            SetEmitterActive(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && selectedBlock != 3)
        {
            SetEmitterActive(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) && selectedBlock != 4)
        {
            SetEmitterActive(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) && selectedBlock != 13)
        {
            SetEmitterActive(13);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6) && selectedBlock != 14)
        {
            SetEmitterActive(14);
        }
    }

    private void SetEmitterActive(int i)
    {
        selectedBlock = i;
    }
}
