using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionNode : MonoBehaviour
{
    public enum OwnerKind
    {
        Neutral,
        Player,
        Enemy
    }

    public static int MaxLevelCache = 10;
    public static Color PlayerColorCache = new Color(0.20f, 0.60f, 1.00f, 1f);
    public static Color PlayerLightColorCache = new Color(0.72f, 0.86f, 1.00f, 1f);
    public static Color EnemyColorCache = new Color(1.00f, 0.25f, 0.25f, 1f);
    public static Color EnemyLightColorCache = new Color(1.00f, 0.80f, 0.80f, 1f);
    public static Color NeutralColorCache = new Color(0.74f, 0.74f, 0.74f, 1f);
    public static int TroopsForMaxIntensityCache = 50;

    [SerializeField] private int id;
    [SerializeField] private Button button;
    [SerializeField] private bool isDefending;
    [SerializeField] private int troopCount = 0;
    [SerializeField] private int level = 1;
    [SerializeField] private TMP_Text troopLabel;
    [SerializeField] private OwnerKind owner = OwnerKind.Neutral;
    [SerializeField] private Image regionFill;

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
            RefreshOwnerColor();
        }
    }

    public int Level
    {
        get { return level; }
        set
        {
            int max = MaxLevelCache;
            level = Mathf.Clamp(value, 1, max);
            RefreshLabel();
        }
    }

    public OwnerKind Owner
    {
        get { return owner; }
        set
        {
            owner = value;
            RefreshOwnerColor();
        }
    }

    public RectTransform Rect
    {
        get { return transform as RectTransform; }
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
        if (level < 1) level = 1;
        RefreshLabel();
        RefreshOwnerColor();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnClicked);
    }

    public void RefreshLabel()
    {
        int max = MaxLevelCache;
        string lvStr = level >= max ? "MAX" : level.ToString();
        troopLabel.text = $"Lv{lvStr}\n{troopCount}";
    }

    private void RefreshOwnerColor()
    {
        if (owner == OwnerKind.Neutral)
        {
            regionFill.color = NeutralColorCache;
            return;
        }

        int cap = TroopsForMaxIntensityCache;
        float t = cap <= 0 ? 1f : Mathf.Clamp01((float)troopCount / cap);
        if (owner == OwnerKind.Player) regionFill.color = Color.Lerp(PlayerLightColorCache, PlayerColorCache, t);
        else regionFill.color = Color.Lerp(EnemyLightColorCache, EnemyColorCache, t);
    }

    private void OnClicked()
    {
        GameController.Instance.OnRegionClicked(this);
    }
}