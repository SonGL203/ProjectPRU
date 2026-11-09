using UnityEngine;

// So bo nguoi choi dang so huu. Chuong bo (CowFarm) chi hien so bo nay, cho Market mua / ban.
public static class CowHerd
{
    public const int Capacity = 3;
    public const int StartCows = 1;

    public static int Owned = StartCows;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Owned = StartCows;
    }
}
