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
            GameController.Instance.NotifyTroopChanged();
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
        button.onClick.AddListener(OnClicked);
        button.transition = Selectable.Transition.None;
        _neutralBaseColor = regionImage.color;
        RefreshLabel();
    }

    private void Start()
    {
        GameController.Instance.RegisterRegion(this);
        ApplyOwnerTint();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnClicked);
    }

    public void RefreshLabel()
    {
        troopLabel.text = $"Lv{level}\n{troopCount}";
    }

    public void ApplyOwnerTint()
    {
        if (owner == OwnerKind.Neutral)
        {
            regionImage.color = _neutralBaseColor;
            return;
        }

        Color tint = GameController.Instance.GetTintFor(this);
        regionImage.color = tint;
    }

    private void OnClicked()
    {
        GameController.Instance.OnRegionClicked(this);
    }
}
