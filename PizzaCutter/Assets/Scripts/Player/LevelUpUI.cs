using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance { get; private set; }

    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private Button[] choiceButtons; //amount of buttons determined in inspector
    [SerializeField] private TextMeshProUGUI[] choiceTexts;

    private PlayerStats playerStats;
    private List<StatUpgradeOption> currentChoices = new List<StatUpgradeOption>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerStats = player.GetComponent<PlayerStats>();
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
    }

    public void ShowLevelUpChoices()
    {
        currentChoices.Clear();

        while (currentChoices.Count < 3)
        {
            StatUpgradeOption option = playerStats.RollRandomUpgrade();

            //will keep looping and setting stats until no stat duplicates
            bool isDuplicate = false;
            foreach (var existing in currentChoices)
            {
                if (existing.statType == option.statType)
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate)
            {
                currentChoices.Add(option);
            }
        }

        //displays options on UI buttons
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            StatUpgradeOption option = currentChoices[i];
            choiceTexts[i].text = $"{FormatStatName(option.statType)}\n+{option.pointsGained} Points!";

            choiceButtons[i].onClick.RemoveAllListeners();
            //finds out which choice was actually pressed, calls selectchoice with the button number
            choiceButtons[i].onClick.AddListener(() => {
                Debug.Log($"Button {index} clicked via listener!");
                SelectChoice(index);
            });
        }

        levelUpPanel.SetActive(true);
    }




    //function called to call a function in playerstats to actually apply the points..ikr
    private void SelectChoice(int choiceIndex)
    {
        Debug.Log($"[LEVEL UP UI] Button clicked at index {choiceIndex}!");
        StatUpgradeOption selected = currentChoices[choiceIndex];
        playerStats.ApplyStatUpgrade(selected.statType, selected.pointsGained);
        levelUpPanel.SetActive(false);
    }

    // formats the stat variable into text for showlevelupchoices
    private string FormatStatName(StatType type)
    {
        switch (type)
        {
            case StatType.MoveSpeed: return "Move Speed";
            case StatType.AttackSpeed: return "Attack Speed";
            case StatType.Damage: return "Damage";
            case StatType.MaxHealth: return "Max Health";
            case StatType.IFrames: return "Invincibility Duration";
            case StatType.PickupRadius: return "Magnet Range";

            default: return type.ToString();
        }

    }


    

} 
