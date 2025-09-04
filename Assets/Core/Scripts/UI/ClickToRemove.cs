using UnityEngine;

public class ClickToRemove : MonoBehaviour
{
    public void Remove()
    {
        Destroy(gameObject);
    }
}