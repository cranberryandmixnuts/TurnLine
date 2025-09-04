using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionNode : MonoBehaviour
{
    [SerializeField] private int id;
    [SerializeField] private Button button;
    [SerializeField] private bool isDefending;
    [SerializeField] private int troopCount = 0;
    [SerializeField] private int level = 1;
    [SerializeField] private TMP_Text troopLabel;

    public int Id
    {
        get { return id; }
    }

    public bool IsDefending
    {
        get { return isDefending; }
        set { isDefending = value; }
    }

    public int TroopCount
    {
        get { return troopCount; }
        set
        {
            troopCount = Mathf.Max(0, value);
            RefreshLabel();
        }
    }

    public int Level
    {
        get { return level; }
        set
        {
            level = Mathf.Max(1, value);
            RefreshLabel();
        }
    }

    public RectTransform Rect
    {
        get { return transform as RectTransform; }
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClicked);
        RefreshLabel();
    }

    private void OnEnable()
    {
        if (GameController.Instance != null) GameController.Instance.RegisterRegion(this);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClicked);
    }

    public void RefreshLabel()
    {
        if (troopLabel != null) troopLabel.text = $"Lv{level}\n{troopCount}";
    }

    private void OnClicked()
    {
        if (GameController.Instance == null) return;
        GameController.Instance.OnRegionClicked(this);
    }
}