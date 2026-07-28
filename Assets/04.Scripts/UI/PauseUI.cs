using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>일시정지 패널. 보유 무기/패시브 아이콘 목록 표시 + 계속하기/메인으로 버튼.</summary>
public class PauseUI : MonoBehaviour
{
    [System.Serializable]
    public class IconSlot
    {
        public GameObject root;
        public Image icon;
        public TMP_Text levelText;
    }

    [SerializeField] GameObject panelRoot;
    [SerializeField] Button pauseButton;
    [SerializeField] Button resumeButton;
    [SerializeField] Button mainMenuButton;
    [SerializeField] IconSlot[] weaponSlots = new IconSlot[6];
    [SerializeField] IconSlot[] passiveSlots = new IconSlot[6];

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (pauseButton != null) pauseButton.onClick.AddListener(Show);
        if (resumeButton != null) resumeButton.onClick.AddListener(Hide);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(() => GameFlowManager.LoadMainMenu());
    }

    public void Show()
    {
        GameManager.Instance.RequestPause();
        PopulateInventory();
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Hide()
    {
        GameManager.Instance.RequestResume();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void PopulateInventory()
    {
        var inv = GameManager.Instance.Weapons;
        if (inv == null) return;

        int wi = 0;
        foreach (var kv in inv.ActiveWeapons)
        {
            if (wi >= weaponSlots.Length) break;
            var slot = weaponSlots[wi];
            slot.root.SetActive(true);
            if (slot.icon != null) { slot.icon.sprite = kv.Value.Data.icon; slot.icon.color = kv.Value.Data.placeholderColor; }
            if (slot.levelText != null) slot.levelText.text = $"Lv.{kv.Value.Level}";
            wi++;
        }
        for (; wi < weaponSlots.Length; wi++) weaponSlots[wi].root.SetActive(false);

        int pi = 0;
        foreach (var kv in inv.ActivePassives)
        {
            if (pi >= passiveSlots.Length) break;
            var slot = passiveSlots[pi];
            slot.root.SetActive(true);
            if (slot.icon != null) { slot.icon.sprite = kv.Value.data.icon; slot.icon.color = kv.Value.data.placeholderColor; }
            if (slot.levelText != null) slot.levelText.text = $"Lv.{kv.Value.level}";
            pi++;
        }
        for (; pi < passiveSlots.Length; pi++) passiveSlots[pi].root.SetActive(false);
    }
}
