using UnityEngine;
using TMPro;

public class Region : MonoBehaviour
{
    public int id;
    public bool isdefense;
    public int owner; // 0 = neutral, 1 = player, 2 = enemy
    public int count;
    [SerializeField] private TextMeshProUGUI Count;
}