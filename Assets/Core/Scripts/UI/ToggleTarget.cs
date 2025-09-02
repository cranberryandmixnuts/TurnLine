using UnityEngine;

public class ToggleTarget : MonoBehaviour
{
    public void TurnOn()
    {
        this.gameObject.SetActive(true);
    }

    public void TurnOff()
    {
        this.gameObject.SetActive(false);
    }

    public void Toggle()
    {
        this.gameObject.SetActive(!this.gameObject.activeSelf);
    }
}