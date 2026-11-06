using UnityEngine;

// Object thong bao tu thiet ke trong scene: Flash() bat len roi tu tat sau vai giay.
public class AutoHide : MonoBehaviour
{
    public float seconds = 2.5f;

    public static void Flash(AutoHide a)
    {
        if (!a) return;
        a.gameObject.SetActive(true);
        a.CancelInvoke();
        a.Invoke(nameof(Off), a.seconds);
    }

    void Off() => gameObject.SetActive(false);
}
