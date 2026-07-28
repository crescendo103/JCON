using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>레벨업 시 3택 1 강화 카드 패널. 카드 3개 슬롯은 MCP로 미리 배치되며, 스크립트는 내용/클릭만 채운다.</summary>
public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance { get; private set; }

    [System.Serializable]
    public class CardSlot
    {
        public GameObject root;
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text levelText;
        public TMP_Text descriptionText;
        public Button button;
    }

    [SerializeField] GameObject panelRoot;
    [SerializeField] UpgradePool upgradePool;
    [SerializeField] CardSlot[] cardSlots = new CardSlot[3];

    List<UpgradeChoice> currentChoices = new List<UpgradeChoice>();

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void ShowLevelUp()
    {
        if (GameManager.Instance.State != GameState.LevelUp)
        {
            GameManager.Instance.EnterLevelUp();
        }

        currentChoices = upgradePool.GetChoices(cardSlots.Length);
        PopulateCards();

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    void PopulateCards()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            var slot = cardSlots[i];
            if (slot == null || slot.root == null) continue;

            if (i < currentChoices.Count)
            {
                var choice = currentChoices[i];
                slot.root.SetActive(true);
                if (slot.icon != null) { slot.icon.sprite = choice.Icon; slot.icon.color = choice.IconColor; }
                if (slot.nameText != null) slot.nameText.text = choice.Title;
                if (slot.levelText != null) slot.levelText.text = choice.LevelText;
                if (slot.descriptionText != null) slot.descriptionText.text = choice.Description;

                if (slot.button != null)
                {
                    slot.button.onClick.RemoveAllListeners();
                    int captured = i;
                    slot.button.onClick.AddListener(() => OnCardChosen(captured));
                }
            }
            else
            {
                slot.root.SetActive(false);
            }
        }
    }

    void OnCardChosen(int index)
    {
        if (index < 0 || index >= currentChoices.Count) return;

        ApplyUpgrade(currentChoices[index]);

        if (panelRoot != null) panelRoot.SetActive(false);

        if (LevelSystem.Instance.CurrentXP >= LevelSystem.Instance.XPToNext)
        {
            LevelSystem.Instance.TryLevelUp();
        }
        else
        {
            GameManager.Instance.ExitLevelUp();
        }
    }

    void ApplyUpgrade(UpgradeChoice choice)
    {
        var inv = GameManager.Instance.Weapons;
        switch (choice.kind)
        {
            case UpgradeKind.NewWeapon: inv.AddWeapon(choice.weapon); break;
            case UpgradeKind.WeaponLevelUp: inv.LevelUpWeapon(choice.weapon.type); break;
            case UpgradeKind.NewPassive: inv.AddOrLevelUpPassive(choice.passive); break;
            case UpgradeKind.PassiveLevelUp: inv.AddOrLevelUpPassive(choice.passive); break;
        }
    }
}
