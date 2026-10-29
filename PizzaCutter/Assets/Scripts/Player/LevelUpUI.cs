using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance { get; private set; }

    [Header("UI Stuff")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private Button[] choiceButtons; //amount of buttons determined in inspector
    [SerializeField] private TextMeshProUGUI[] choiceTexts;
    [SerializeField] private TextMeshProUGUI titleText; //changes for normal leveling vs weapon upgrades

    [Header("All Base Weapons")]
    [SerializeField] private WeaponData baseMeleeData;
    [SerializeField] private WeaponData baseMeatballData;
    [SerializeField] private WeaponData baseGarlicData;
    [SerializeField] private WeaponData baseMopedData;

    [Header("Audio")]
    [SerializeField] private AudioClip sound1;
    [Range(0f, 1f)][SerializeField] private float volume1 = 1.0f;
    [SerializeField] private AudioClip sound2;
    [Range(0f, 1f)][SerializeField] private float volume2 = 1.0f;




    private PlayerStats playerStats;
    private PlayerWeaponManager weaponManager;
    private List<StatUpgradeOption> currentChoices = new List<StatUpgradeOption>();
    private List<WeaponData> currentWeaponChoices = new List<WeaponData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LevelUpUI] Duplicate LevelUpUI found. Removing the duplicate component.");
            Destroy(this);
            return;
        }

        Instance = this;
        RefreshPlayerReferences();
    }

    private void Start()
    {
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        RefreshPlayerReferences();
    }

    private void OnDestroy()
    {
        // Do not leave a static reference pointing to a destroyed scene object.
        if (Instance == this)
            Instance = null;
    }

    private void RefreshPlayerReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            playerStats = null;
            weaponManager = null;
            return;
        }

        playerStats = player.GetComponent<PlayerStats>();
        weaponManager = player.GetComponent<PlayerWeaponManager>();

        if (playerStats == null)
            Debug.LogError("[LevelUpUI] PlayerStats could not be found on the Player.");

        if (weaponManager == null)
            Debug.LogError("[LevelUpUI] PlayerWeaponManager could not be found on the Player.");
    }

    public void ResetForNewRun()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        currentChoices.Clear();
        currentWeaponChoices.Clear();
        RefreshPlayerReferences();
        Time.timeScale = 1f;

        Debug.Log("[LevelUpUI] Upgrade UI reset for a new run.");
    }

    // public void ShowLevelUpChoices()
    // {
    // currentChoices.Clear();

    // while (currentChoices.Count < 3)
    // {
    // StatUpgradeOption option = playerStats.RollRandomUpgrade();

    //will keep looping and setting stats until no stat duplicates
    // bool isDuplicate = false;
    // foreach (var existing in currentChoices)
    // {
    // if (existing.statType == option.statType)
    // {
    // isDuplicate = true;
    //break;
    //    }
    // }
    //if (!isDuplicate)
    //{
    //  currentChoices.Add(option);
    // }
    // }

    //displays options on UI buttons
    // for (int i = 0; i < choiceButtons.Length; i++)
    //  {
    //    int index = i;
    //  StatUpgradeOption option = currentChoices[i];
    //choiceTexts[i].text = $"{FormatStatName(option.statType)}\n+{option.pointsGained} Points!";

    //choiceButtons[i].onClick.RemoveAllListeners();
    //finds out which choice was actually pressed, calls selectchoice with the button number
    // choiceButtons[i].onClick.AddListener(() => {
    //  Debug.Log($"Button {index} clicked via listener!");
    //  SelectChoice(index);
    // });
    // }

    //    levelUpPanel.SetActive(true);
    // }


    public bool TryShowLevelUpChoices()
    {
        RefreshPlayerReferences();

        if (titleText != null) titleText.text = "SELECT STAT UPGRADE";

        // make sure of UI references before attempting to pause.
        if (levelUpPanel == null ||
            choiceButtons == null ||
            choiceTexts == null ||
            choiceButtons.Length < 3 ||
            choiceTexts.Length < 3)
        {
            Debug.LogError(
                "[LEVEL UP UI ERROR] Missing panel, buttons, or texts. " +
                "Check the LevelUpUI Inspector references."
            );

            return false;
        }

        // find PlayerStats if it hasn't been assigned yet.
        if (playerStats == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerStats = player.GetComponent<PlayerStats>();
            }
        }

        if (playerStats == null)
        {
            Debug.LogError(
                "[LEVEL UP UI ERROR] PlayerStats could not be found."
            );

            return false;
        }

        // to prepare three upgrade choices
        currentChoices.Clear();

        if (System.Enum.GetValues(typeof(StatType)).Length < 3)
        {
            Debug.LogError("[LevelUpUI] At least three StatType values are required for three distinct choices.");
            return false;
        }

        while (currentChoices.Count < 3)
        {
            StatUpgradeOption option =
                playerStats.RollRandomUpgrade();

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

        // making sure all 3 buttons are good to go.
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            StatUpgradeOption option = currentChoices[i];
            choiceButtons[i].interactable = true;

            choiceTexts[i].text =
                $"{FormatStatName(option.statType)}\n" +
                $"+{option.pointsGained} Points!";

            choiceButtons[i].onClick.RemoveAllListeners();

            choiceButtons[i].onClick.AddListener(() =>
            {
                SelectChoice(index);
            });
        }

        // show UI before pausing the game pleeeeeeeeeeease.
        levelUpPanel.SetActive(true);
        if (sound1 != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(sound1, volume1);
        }

        Debug.Log("[LEVEL UP UI] Upgrade panel opened.");

        Time.timeScale = 0f;

        return true;
    }



    //function called to call a function in playerstats to actually apply the points..ikr
    private void SelectChoice(int choiceIndex)
    {
        if (sound2 != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(sound2, volume2);
        }

        Debug.Log($"[LEVEL UP UI] Button clicked at index {choiceIndex}!");
        StatUpgradeOption selected = currentChoices[choiceIndex];
        playerStats.ApplyStatUpgrade(selected.statType, selected.pointsGained);
        levelUpPanel.SetActive(false);
    }


    //              WEAPON UPGRADE FUNCTIONS 



    public void ShowWeaponUpgradeChoices()
    {
        RefreshPlayerReferences();

        if (levelUpPanel == null || choiceButtons == null || choiceTexts == null ||
            choiceButtons.Length == 0 || choiceTexts.Length < choiceButtons.Length)
        {
            Debug.LogError("[LevelUpUI] Major upgrade UI references are missing or incomplete.");
            return;
        }


        



        currentWeaponChoices.Clear();
        List<WeaponData> availableUpgrades = BuildWeaponUpgradePool();

        if (availableUpgrades == null || availableUpgrades.Count == 0)
        {
            TryShowLevelUpChoices();
            return;
        }
        else if (titleText != null) 
        { 
        titleText.text = "CHOOSE WEAPON: ";
        }

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i;

                if (i < availableUpgrades.Count)
                {
                    WeaponData wData = availableUpgrades[i];
                    currentWeaponChoices.Add(wData);

                    choiceButtons[i].interactable = true;

                    //checking if its unlocking the weapon or upgrading, checks if its tier 1, otherwise etc..
                    string actionType = wData.tier == 1 ? "UNLOCK WEAPON" : $"UPGRADE TIER {wData.tier}";
                    choiceTexts[i].text = $"{actionType}\n{wData.weaponName}";

                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => SelectWeaponChoice(index));
                }
                else
                {
                    //Blank slot if fewer than 3 actual options exist
                    choiceButtons[i].interactable = false;
                    choiceTexts[i].text = "    ";
                    choiceButtons[i].onClick.RemoveAllListeners();



                }
            }

        

        if (sound1 != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(sound1, volume1);
        }

        levelUpPanel.SetActive(true);
    }

    private List<WeaponData> BuildWeaponUpgradePool()
    {
        List<WeaponData> fullPool = new List<WeaponData>();

        //checking for weapon manager stuff
        if (weaponManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                weaponManager = player.GetComponent<PlayerWeaponManager>();
            }
        }

        if (weaponManager == null)
        {
            Debug.LogError("[LevelUpUI] PlayerWeaponManager could not be found on the Player!");
            return fullPool;
        }

        int playerLevel = (playerStats != null) ? playerStats.currentLevel : 1;

        //offers next tier or unlocking for melee
        if (!weaponManager.IsWeaponUnlocked(WeaponType.Melee))
        {
            if (baseMeleeData != null) fullPool.Add(baseMeleeData);
        }
        else if (weaponManager.currentMeleeData != null && weaponManager.currentMeleeData.nextTierUpgrade != null)
        {
            fullPool.Add(weaponManager.currentMeleeData.nextTierUpgrade);
        }

        //then gun
        if (!weaponManager.IsWeaponUnlocked(WeaponType.MeatballGun))
        {
            if (baseMeatballData != null) fullPool.Add(baseMeatballData);
        }
        else if (weaponManager.currentMeatballData != null && weaponManager.currentMeatballData.nextTierUpgrade != null)
        {
            fullPool.Add(weaponManager.currentMeatballData.nextTierUpgrade);
        }

        //then garlic
        if (!weaponManager.IsWeaponUnlocked(WeaponType.Garlic))
        {
            if (baseGarlicData != null) fullPool.Add(baseGarlicData);
        }
        else if (weaponManager.currentGarlicData != null && weaponManager.currentGarlicData.nextTierUpgrade != null)
        {
            fullPool.Add(weaponManager.currentGarlicData.nextTierUpgrade);
        }

        if (!weaponManager.IsWeaponUnlocked(WeaponType.Moped))
        {
            if (baseMopedData != null) fullPool.Add(baseMopedData);
        }
        else if (weaponManager.currentMopedData != null && weaponManager.currentMopedData.nextTierUpgrade != null)
        {
            fullPool.Add(weaponManager.currentMopedData.nextTierUpgrade);
        }

        //ADD MORE WEAPONS HERE

        //shuffles them, giving out 3 choices
        List<WeaponData> randomizedPool = new List<WeaponData>();
        while (fullPool.Count > 0 && randomizedPool.Count < 3)
        {
            int randomIndex = Random.Range(0, fullPool.Count);
            randomizedPool.Add(fullPool[randomIndex]);
            fullPool.RemoveAt(randomIndex);
        }

        return randomizedPool;

    }




    private void SelectWeaponChoice(int index)
    {
        if (sound2 != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(sound2, volume2);
        }
        WeaponData selectedWeapon = currentWeaponChoices[index];
        weaponManager.ApplyWeaponUpgrade(selectedWeapon);

        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
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
