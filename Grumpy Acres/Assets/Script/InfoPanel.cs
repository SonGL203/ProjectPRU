using UnityEngine;

// Bang thong tin thiet ke san trong scene. Code chi bat / tat, khong dung vao chu hay bo cuc.
public class InfoPanel : MonoBehaviour
{
    public void Show()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
}
