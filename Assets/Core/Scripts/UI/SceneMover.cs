using UnityEngine;

public class SceneMover : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void MoveScene() => UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
}