using System;
using UnityEngine;

public enum Season
{
    Spring, // Mua Xuan
    Summer, // Mua Ha
    Autumn, // Mua Thu
    Winter  // Mua Dong
}

[DefaultExecutionOrder(-90)]
public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }

    [Header("Cau hinh Mua")]
    [Tooltip("Mua hien tai trong game")]
    public Season currentSeason = Season.Spring;

    [Tooltip("So ngay trong moi mua (Mac dinh la 7 ngay/mua de tien test, khong hardcode de tuy y chinh trong Inspector)")]
    public int daysPerSeason = 7;

    [Tooltip("Ngay thu may trong mua hien tai (1..daysPerSeason)")]
    public int currentDayInSeason = 1;

    [Tooltip("Tong so nam da troi qua")]
    public int currentYear = 1;

    [Header("He so toc do lon cua cay theo tung Mua (Growth Speed Multiplier)")]
    [Tooltip("He so mua Xuan: 1.3f = lon nhanh hon 30%")]
    public float springGrowthMultiplier = 1.3f;

    [Tooltip("He so mua Ha: 1.0f = toc do binh thuong")]
    public float summerGrowthMultiplier = 1.0f;

    [Tooltip("He so mua Thu: 1.0f = toc do binh thuong")]
    public float autumnGrowthMultiplier = 1.0f;

    [Tooltip("He so mua Dong: 0.6f = lanh gia nen lon cham hon 40%")]
    public float winterGrowthMultiplier = 0.6f;

    [Header("Phim tat test chuyen mua nhanh")]
    public bool enableTestKeys = true;

    // Events
    public event Action<Season> OnSeasonChanged;
    public event Action<int, Season> OnDayAdvanced; // (dayInSeason, season)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Dang ky lang nghe su kien sang ngay moi tu GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewDay += HandleNewDay;
        }

        OnSeasonChanged?.Invoke(currentSeason);
        OnDayAdvanced?.Invoke(currentDayInSeason, currentSeason);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewDay -= HandleNewDay;
        }
    }

    void Update()
    {
        if (!enableTestKeys) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        // Phim F4: Nhay sang ngay tiep theo de test
        if (kb.f4Key.wasPressedThisFrame)
        {
            if (GameManager.Instance != null) GameManager.Instance.NextDay();
            else AdvanceDay();
        }

        // Phim F5: Chuyen ngay sang Mua ke tiep
        if (kb.f5Key.wasPressedThisFrame)
        {
            NextSeason();
        }
    }

    void HandleNewDay(WeekDay today, int week)
    {
        AdvanceDay();
    }

    public void AdvanceDay()
    {
        currentDayInSeason++;
        if (currentDayInSeason > daysPerSeason)
        {
            currentDayInSeason = 1;
            NextSeason();
        }
        else
        {
            OnDayAdvanced?.Invoke(currentDayInSeason, currentSeason);
        }
    }

    public void NextSeason()
    {
        switch (currentSeason)
        {
            case Season.Spring: currentSeason = Season.Summer; break;
            case Season.Summer: currentSeason = Season.Autumn; break;
            case Season.Autumn: currentSeason = Season.Winter; break;
            case Season.Winter:
                currentSeason = Season.Spring;
                currentYear++;
                break;
        }

        currentDayInSeason = 1;
        Debug.Log($"[SeasonManager] Chuyen mua -> {GetSeasonNameVN(currentSeason)} (Nam {currentYear})");
        OnSeasonChanged?.Invoke(currentSeason);
        OnDayAdvanced?.Invoke(currentDayInSeason, currentSeason);
    }

    /// <summary>
    /// Lay he so toc do lon cua cay dua theo mua hien tai
    /// </summary>
    public float GetCurrentGrowthMultiplier()
    {
        return currentSeason switch
        {
            Season.Spring => springGrowthMultiplier,
            Season.Summer => summerGrowthMultiplier,
            Season.Autumn => autumnGrowthMultiplier,
            Season.Winter => winterGrowthMultiplier,
            _ => 1.0f
        };
    }

    public static string GetSeasonNameVN(Season s)
    {
        return s switch
        {
            Season.Spring => "Mùa Xuân",
            Season.Summer => "Mùa Hạ",
            Season.Autumn => "Mùa Thu",
            Season.Winter => "Mùa Đông",
            _ => s.ToString()
        };
    }

    public static string GetSeasonIcon(Season s)
    {
        return s switch
        {
            Season.Spring => "🌸",
            Season.Summer => "☀️",
            Season.Autumn => "🍂",
            Season.Winter => "❄️",
            _ => "🌱"
        };
    }
}

