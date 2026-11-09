using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeasonClockUI : MonoBehaviour
{
    public static SeasonClockUI Instance { get; private set; }

    [Header("UI References (Tuy chon neu co Canvas)")]
    public TextMeshProUGUI seasonText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI weatherEffectText;

    static Texture2D _pixelTex;

    void Awake()
    {
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
        string icon = SeasonManager.GetSeasonIcon(season);
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
            seasonText.text = $"{icon} {name}";
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

    static void DrawSolidRect(Rect r, Color c)
    {
        if (_pixelTex == null)
        {
            _pixelTex = new Texture2D(1, 1);
            _pixelTex.SetPixel(0, 0, Color.white);
            _pixelTex.Apply();
        }
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, _pixelTex);
        GUI.color = old;
    }

    static void DrawPanel(Rect r, Color bg, Color border, float borderWidth = 2.5f)
    {
        // Shadow
        DrawSolidRect(new Rect(r.x + 3f, r.y + 3f, r.width, r.height), new Color(0f, 0f, 0f, 0.5f));
        // Border
        DrawSolidRect(r, border);
        // Body
        DrawSolidRect(new Rect(r.x + borderWidth, r.y + borderWidth, r.width - borderWidth * 2f, r.height - borderWidth * 2f), bg);
    }

    static void DrawShadowLabel(Rect r, string text, GUIStyle style, Color textColor, Color shadowColor, float offset = 1.5f)
    {
        Color old = style.normal.textColor;
        style.normal.textColor = shadowColor;
        GUI.Label(new Rect(r.x + offset, r.y + offset, r.width, r.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(r, text, style);
        style.normal.textColor = old;
    }

    void OnGUI()
    {
        // Ty le phong to tu dong theo do phan giai man hinh de chu luon ro net
        float scale = Mathf.Clamp(Screen.height / 900f, 1.25f, 2.5f);

        float w = Mathf.Round(230f * scale);
        float h = Mathf.Round(76f * scale);
        float x = Screen.width - w - 16f * scale;
        float y = 16f * scale;

        // Khung nen toi sang trong voi vien vang
        Color bgCard = new Color(0.08f, 0.11f, 0.16f, 0.96f);
        Color borderGold = new Color(1.0f, 0.82f, 0.22f, 0.95f);
        float bw = Mathf.Round(2.5f * scale);
        DrawPanel(new Rect(x, y, w, h), bgCard, borderGold, bw);

        // Header bar nhe cho Mua
        float headerH = Mathf.Round(30f * scale);
        DrawSolidRect(new Rect(x + bw, y + bw, w - bw * 2f, headerH), new Color(0.14f, 0.19f, 0.26f, 0.98f));
        DrawSolidRect(new Rect(x + bw, y + bw + headerH, w - bw * 2f, Mathf.Round(1.5f * scale)), new Color(1.0f, 0.82f, 0.22f, 0.7f));

        var sm = SeasonManager.Instance;
        Season season = sm != null ? sm.currentSeason : Season.Spring;
        string seasonName = SeasonManager.GetSeasonNameVN(season).ToUpper();
        string icon = SeasonManager.GetSeasonIcon(season);
        int day = sm != null ? sm.currentDayInSeason : 1;
        int maxDays = sm != null ? sm.daysPerSeason : 7;
        float mult = sm != null ? sm.GetCurrentGrowthMultiplier() : 1.3f;

        Color seasonColor = season switch
        {
            Season.Spring => new Color(0.45f, 1.0f, 0.55f),
            Season.Summer => new Color(1.0f, 0.88f, 0.35f),
            Season.Autumn => new Color(1.0f, 0.65f, 0.35f),
            Season.Winter => new Color(0.6f, 0.88f, 1.0f),
            _ => Color.white
        };

        // Dong 1: MUA (To, dam, ruc ro)
        var seasonStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(17 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawShadowLabel(new Rect(x, y + 2f * scale, w, headerH), $"{icon} {seasonName}", seasonStyle, seasonColor, Color.black, 2f);

        // Dong 2: NGAY & THU
        string weekDay = GameManager.Instance != null ? GetWeekDayVN(GameManager.Instance.today) : "Thứ 2";
        string timeStr = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight ? "Tối" : "Sáng 06:00";
        var dayStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(13 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        float row2Y = y + headerH + 5f * scale;
        DrawShadowLabel(new Rect(x, row2Y, w, 20f * scale), $"📅 Ngày {day}/{maxDays} ({weekDay}) • {timeStr}", dayStyle, Color.white, Color.black, 1.5f);

        // Dong 3: HIEU UNG BUFF MUA
        string buffDesc = mult >= 1.2f ? "⚡ Cây trồng: +30% Tốc độ lớn" : (mult <= 0.8f ? "❄ Lạnh giá: -40% Tốc độ lớn" : "🌱 Tốc độ bình thường");
        var buffStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(12 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        float row3Y = row2Y + 18f * scale;
        Color buffColor = mult >= 1.2f ? new Color(0.4f, 0.95f, 1f) : (mult <= 0.8f ? new Color(1f, 0.6f, 0.6f) : new Color(0.85f, 0.85f, 0.85f));
        DrawShadowLabel(new Rect(x, row3Y, w, 18f * scale), buffDesc, buffStyle, buffColor, Color.black, 1.5f);
    }
}
