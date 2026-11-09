using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeasonClockUI : MonoBehaviour
{
    public static SeasonClockUI Instance { get; private set; }

    [Header("UI References (Canvas UI)")]
    public TextMeshProUGUI seasonText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI weatherEffectText;

    void Awake()
    {
        // Khong hien thi tren bat ky canh cho Market nao de giu man hinh luon thoang dang
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Market" || sceneName == "CowboyMarket")
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Tim cac component con neu co san trong prefab/scene
        if (seasonText == null || dayText == null)
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length >= 2)
            {
                if (seasonText == null) seasonText = texts[0];
                if (dayText == null) dayText = texts[1];
            }
        }
    }

    void Start()
    {
        EnsureSeasonManager();

        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged += UpdateDisplay;
            SeasonManager.Instance.OnDayAdvanced += (day, season) => UpdateDisplay(season);
            UpdateDisplay(SeasonManager.Instance.currentSeason);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewDay += (today, week) =>
            {
                if (SeasonManager.Instance != null)
                    UpdateDisplay(SeasonManager.Instance.currentSeason);
            };
        }
    }

    void EnsureSeasonManager()
    {
        if (SeasonManager.Instance == null)
        {
            var sm = Object.FindFirstObjectByType<SeasonManager>();
            if (sm == null)
            {
                var smGO = new GameObject("SeasonManager", typeof(SeasonManager));
                sm = smGO.GetComponent<SeasonManager>();
                sm.currentSeason = Season.Spring;
                sm.daysPerSeason = 7;
                sm.currentDayInSeason = 1;
            }
        }
    }

    void OnDestroy()
    {
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged -= UpdateDisplay;
        }
    }

    void Update()
    {
        if (SeasonManager.Instance == null)
        {
            EnsureSeasonManager();
        }
    }

    public void UpdateDisplay(Season season)
    {
        if (SeasonManager.Instance == null) return;

        string name = SeasonManager.GetSeasonNameVN(season);
        float mult = SeasonManager.Instance.GetCurrentGrowthMultiplier();
        string speedDesc = mult >= 1.2f ? " (+30% tốc độ)" : (mult <= 0.8f ? " (-40% lạnh)" : "");

        Color seasonColor = season switch
        {
            Season.Spring => new Color(0.4f, 1.0f, 0.5f),
            Season.Summer => new Color(1.0f, 0.85f, 0.35f),
            Season.Autumn => new Color(1.0f, 0.6f, 0.3f),
            Season.Winter => new Color(0.6f, 0.85f, 1.0f),
            _ => Color.white
        };

        if (seasonText != null)
        {
            // Dung ten mua thuan khong emoji de tranh loi ky tu o vuong tren LiberationSans SDF
            seasonText.text = $"{name}";
            seasonText.color = seasonColor;
        }

        if (dayText != null)
        {
            string weekDayStr = GameManager.Instance != null ? GetWeekDayVN(GameManager.Instance.today) : "Thứ 2";
            dayText.text = $"Ngày {SeasonManager.Instance.currentDayInSeason}/{SeasonManager.Instance.daysPerSeason} ({weekDayStr}){speedDesc}";
        }
    }

    string GetWeekDayVN(WeekDay day)
    {
        return day switch
        {
            WeekDay.Monday => "Thứ 2",
            WeekDay.Tuesday => "Thứ 3",
            WeekDay.Wednesday => "Thứ 4",
            WeekDay.Thursday => "Thứ 5",
            WeekDay.Friday => "Thứ 6",
            WeekDay.Saturday => "Thứ 7",
            WeekDay.Sunday => "Chủ Nhật",
            _ => day.ToString()
        };
    }
}
