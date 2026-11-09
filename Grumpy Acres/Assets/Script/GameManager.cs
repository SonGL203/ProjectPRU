using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum WeekDay { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager ManagerGame { get; private set; }
    public static GameManager Instance => ManagerGame;
    public static bool HasInstance
    {
        get { return ManagerGame != null; }
    }

    [Header("Ngay trong tuan")]
    public WeekDay today = WeekDay.Monday;
    public int week = 1;
    public WeekDay attackDay = WeekDay.Sunday;

    public event Action<WeekDay, int> OnNewDay;

    [Header("Game over (object thiet ke san trong scene)")]
    [Tooltip("Bang Game Over: bat len khi nha bi pha, an luc dau")]
    public GameObject gameOverPanel;

    public bool IsGameOver { get; private set; }

    public bool IsAttackDay
    {
        get { return today == attackDay; }
    }

    void Awake()
    {
        if (ManagerGame && ManagerGame != this) { Destroy(gameObject); return; }
        ManagerGame = this;
    }

    void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        OnNewDay?.Invoke(today, week);
    }

    void Update()
    {
        if (!IsGameOver) return;

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Restart();
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    public void PlayerDefeated()
    {
        foreach (var e in Enemy.Alive.ToArray())
            if (e) Destroy(e.gameObject);

        if (CropManager.Instance) CropManager.Instance.DestroyAllFarmland();
        Cowboy.RemoveAll();
        CowboyRoster.Clear();

        if (DayNightCycle.Instance) DayNightCycle.Instance.SkipToNextMorning();
        else NextDay();
    }

    void Restart()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        Cowboy.RemoveAll();
        CowboyRoster.Clear();

        // GameManager cu van con song luc GameManager moi chay Awake; bo ManagerGame cu de ban moi khong tu huy minh.
        ManagerGame = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (ManagerGame == this) ManagerGame = null;
    }

    public void NextDay()
    {
        if (today == WeekDay.Sunday)
        {
            today = WeekDay.Monday;
            week++;
        }
        else today++;

        OnNewDay?.Invoke(today, week);
    }
}
