using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }
    public static int savedGold = -1;

    public int gold = 0;
    public event Action<int> OnChanged;

    void Awake()
    {
        Instance = this;
        if (savedGold >= 0)
        {
            gold = savedGold;
        }
        else
        {
            if (gold <= 0) gold = 150;
            savedGold = gold;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f2Key.wasPressedThisFrame) Add(100);
    }

    public void Add(int amount)
    {
        gold += amount;
        savedGold = gold;
        OnChanged?.Invoke(gold);
    }

    public bool TrySpend(int amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        savedGold = gold;
        OnChanged?.Invoke(gold);
        return true;
    }
}
