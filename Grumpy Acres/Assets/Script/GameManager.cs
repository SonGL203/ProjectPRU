using System;
using UnityEngine;

public enum WeekDay { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Header("Ngay trong tuan")]
    public WeekDay today = WeekDay.Monday;
    public int week = 1;
    public WeekDay attackDay = WeekDay.Sunday;

    public event Action<WeekDay, int> OnNewDay;

    public bool IsAttackDay => today == attackDay;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => OnNewDay?.Invoke(today, week);

    public void PlayerDefeated()
    {
        foreach (var e in Enemy.Alive.ToArray())
            if (e) Destroy(e.gameObject);

        if (CropManager.Instance) CropManager.Instance.DestroyAllFarmland();
        Cowboy.RemoveAll();
        CowboyRoster.Clear();
        Structure.RestoreAll();

        if (DayNightCycle.Instance) DayNightCycle.Instance.SkipToNextMorning();
        else NextDay();
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
