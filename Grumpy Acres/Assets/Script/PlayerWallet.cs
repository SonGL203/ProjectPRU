using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    public int gold = 0;
    public event Action<int> OnChanged;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f2Key.wasPressedThisFrame) Add(100);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(12, 8, 300, 24), $"Vang: {gold}   (F2: +100 de test)");
    }

    public void Add(int amount)
    {
        gold += amount;
        OnChanged?.Invoke(gold);
    }

    public bool TrySpend(int amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        OnChanged?.Invoke(gold);
        return true;
    }
}
