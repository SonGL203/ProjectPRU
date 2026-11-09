using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SeasonClockUI : MonoBehaviour
{
    public static SeasonClockUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI seasonText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI weatherEffectText;

    void Awake()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Market" || scene == "CowboyMarket")
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Neu da keo san Text trong Inspector (hoac co san con trong GameObject) thi khong can code sinh UI nua
        if (seasonText == null || dayText == null)
        {
            // Tu tim neu da co san cac component con
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length >= 2)
            {
                if (seasonText == null) seasonText = texts[0];
                if (dayText == null) dayText = texts[1];
            }
            else
            {
                BuildUI();
            }
        }
    }

    void Start()
    {
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged += UpdateDisplay;
            SeasonManager.Instance.OnDayAdvanced += (day, season) => UpdateDisplay(season);
            UpdateDisplay(SeasonManager.Instance.currentSeason);
        }

        if (GameManager.ManagerGame != null)
        {
            GameManager.ManagerGame.OnNewDay += (today, week) =>
            {
                if (SeasonManager.Instance != null)
                    UpdateDisplay(SeasonManager.Instance.currentSeason);
            };
        }
    }

    void OnDestroy()
    {
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged -= UpdateDisplay;
        }
    }

    void BuildUI()
    {
        // Dam bao goc cha nam o goc tren ben phai
        var rootRT = GetComponent<RectTransform>();
        if (rootRT != null)
        {
            rootRT.anchorMin = new Vector2(1f, 1f);
            rootRT.anchorMax = new Vector2(1f, 1f);
            rootRT.pivot = new Vector2(1f, 1f);
            rootRT.anchoredPosition = new Vector2(-15f, -15f);
            rootRT.sizeDelta = new Vector2(160f, 48f);
        }

        // 1. Tao Panel o goc tren ben phai man hinh (Top-Right) nho gon
        var panelGO = new GameObject("SeasonClockPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(transform, false);

        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.pivot = new Vector2(1f, 1f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = Vector2.zero;

        var panelImg = panelGO.GetComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.82f);

        // Bo vien panel mong nhe
        var outline = panelGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.7f, 0.45f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // 2. Text Mua (Season)
        var seasonGO = new GameObject("SeasonText", typeof(RectTransform), typeof(TextMeshProUGUI));
        seasonGO.transform.SetParent(panelGO.transform, false);
        var seasonRT = seasonGO.GetComponent<RectTransform>();
        seasonRT.anchorMin = new Vector2(0f, 1f);
        seasonRT.anchorMax = new Vector2(1f, 1f);
        seasonRT.pivot = new Vector2(0f, 1f);
        seasonRT.anchoredPosition = new Vector2(8f, -4f);
        seasonRT.sizeDelta = new Vector2(-16f, 20f);

        seasonText = seasonGO.GetComponent<TextMeshProUGUI>();
        seasonText.fontSize = 13;
        seasonText.fontStyle = FontStyles.Bold;
        seasonText.alignment = TextAlignmentOptions.MidlineRight;
        seasonText.color = new Color(1f, 0.92f, 0.65f);
        seasonText.text = "Mùa Xuân";

        // 3. Text Ngay & Thu (Day & WeekDay)
        var dayGO = new GameObject("DayText", typeof(RectTransform), typeof(TextMeshProUGUI));
        dayGO.transform.SetParent(panelGO.transform, false);
        var dayRT = dayGO.GetComponent<RectTransform>();
        dayRT.anchorMin = new Vector2(0f, 1f);
        dayRT.anchorMax = new Vector2(1f, 1f);
        dayRT.pivot = new Vector2(0f, 1f);
        dayRT.anchoredPosition = new Vector2(8f, -24f);
        dayRT.sizeDelta = new Vector2(-16f, 18f);

        dayText = dayGO.GetComponent<TextMeshProUGUI>();
        dayText.fontSize = 10;
        dayText.alignment = TextAlignmentOptions.MidlineRight;
        dayText.color = new Color(0.9f, 0.9f, 0.95f);
        dayText.text = "Ngày 1 / 7";
    }

    void UpdateDisplay(Season season)
    {
        if (SeasonManager.Instance == null) return;

        string name = SeasonManager.GetSeasonNameVN(season);
        float mult = SeasonManager.Instance.GetCurrentGrowthMultiplier();
        string speedDesc = mult >= 1.2f ? " (+30% tốc độ)" : (mult <= 0.8f ? " (-40% lạnh)" : "");

        if (seasonText != null)
        {
            seasonText.text = $"{name}";
            // Doi mau chu theo mua
            seasonText.color = season switch
            {
                Season.Spring => new Color(0.55f, 0.95f, 0.55f),
                Season.Summer => new Color(1.0f, 0.85f, 0.35f),
                Season.Autumn => new Color(1.0f, 0.6f, 0.3f),
                Season.Winter => new Color(0.6f, 0.85f, 1.0f),
                _ => Color.white
            };
        }

        if (dayText != null)
        {
            string weekDayStr = GameManager.ManagerGame != null ? GetWeekDayVN(GameManager.ManagerGame.today) : "";
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
