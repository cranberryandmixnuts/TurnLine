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
    [SerializeField] private Image regionImage;
    [SerializeField] private TMP_Text troopLabel;
    [SerializeField] private bool isDefending;
    [SerializeField] private int troopCount = 0;
    [SerializeField] private int level = 1;
    [SerializeField] private OwnerKind owner = OwnerKind.Neutral;

    private Color _neutralBaseColor;

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
            if (GameController.Instance != null) GameController.Instance.NotifyTroopChanged();
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

    public OwnerKind Owner
    {
        get { return owner; }
        set
        {
            owner = value;
            ApplyOwnerTint();
        }
    }

    public RectTransform Rect
    {
        get { return transform as RectTransform; }
    }

    public Color NeutralBaseColor
    {
        get { return _neutralBaseColor; }
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (regionImage == null) regionImage = GetComponent<Image>();
        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
            button.transition = Selectable.Transition.None;
        }
        _neutralBaseColor = regionImage != null ? regionImage.color : Color.white;
        RefreshLabel();
    }

    private void Start()
    {
        if (GameController.Instance != null) GameController.Instance.RegisterRegion(this);
        ApplyOwnerTint();
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClicked);
    }

    public void RefreshLabel()
    {
        if (troopLabel != null) troopLabel.text = $"Lv{level}\n{troopCount}";
    }

    public void ApplyOwnerTint()
    {
        if (regionImage == null) return;

        if (owner == OwnerKind.Neutral)
        {
            regionImage.color = _neutralBaseColor;
            return;
        }

        if (GameController.Instance == null)
        {
            regionImage.color = _neutralBaseColor;
            return;
        }

        Color tint = GameController.Instance.GetTintFor(this);
        regionImage.color = tint;
    }

    private void OnClicked()
    {
        if (GameController.Instance == null) return;
        GameController.Instance.OnRegionClicked(this);
    }
}