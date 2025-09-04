using UnityEngine;

public class ClickToRemove : MonoBehaviour
{
    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void Remove()
    {
        Destroy(gameObject);
    }
}