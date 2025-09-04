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
            level = Mathf.Clamp(value, 1, GameController.Instance.MaxLevel);
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
        if (level < 1) level = GameController.Instance.StartLevel;
        RefreshLabel();
        RefreshOwnerColor();
    }

    private void OnEnable()
    {
        GameController.Instance.RegisterRegion(this);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnClicked);
    }

    public void RefreshLabel()
    {
        string lvStr = Level >= GameController.Instance.MaxLevel ? "MAX" : Level.ToString();
        troopLabel.text = $"Lv{lvStr}\n{troopCount}";
    }

    private void RefreshOwnerColor()
    {
        if (owner == OwnerKind.Neutral)
        {
            regionFill.color = GameController.Instance.NeutralColor;
            return;
        }

        int cap = GameController.Instance.TroopsForMaxIntensity;
        float t = cap <= 0 ? 1f : Mathf.Clamp01((float)troopCount / cap);
        if (owner == OwnerKind.Player) regionFill.color = Color.Lerp(GameController.Instance.PlayerLightColor, GameController.Instance.PlayerColor, t);
        else regionFill.color = Color.Lerp(GameController.Instance.EnemyLightColor, GameController.Instance.EnemyColor, t);
    }

    private void OnClicked()
    {
        GameController.Instance.OnRegionClicked(this);
    }
}