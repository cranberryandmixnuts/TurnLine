using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    public GameObject[] regions;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnPanel(int id)
    {

    }
}