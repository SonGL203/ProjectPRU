using UnityEngine;
using UnityEngine.UI;

// HUD thiet ke san trong Canvas. Code chi: cap nhat con so vang, bat / tat cac bang.
public class GameHud : MonoBehaviour
{
    public static GameHud Instance { get; private set; }

    [Tooltip("Text hien vang. Dinh dang chinh o goldFormat ({0} = so vang)")]
    public Text goldText;
    public string goldFormat = "Vàng: {0}";
    [Tooltip("Bang hien khi dang cam chong tren hotbar")]
    public GameObject hintPanel;
    [Tooltip("Thong bao dat khong duoc (tuy chon)")]
    public AutoHide failPanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (hintPanel) hintPanel.SetActive(false);
        if (failPanel) failPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        var w = PlayerWallet.Instance;
        if (goldText && w) goldText.text = string.Format(goldFormat, w.gold);
    }

    public static void ShowPlacing(bool on)
    {
        if (!Instance || !Instance.hintPanel) return;
        if (Instance.hintPanel.activeSelf != on) Instance.hintPanel.SetActive(on);
    }

    public static void Fail()
    {
        if (Instance) AutoHide.Flash(Instance.failPanel);
    }
}
