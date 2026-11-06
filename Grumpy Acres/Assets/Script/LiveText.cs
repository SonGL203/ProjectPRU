using UnityEngine;
using UnityEngine.UI;

// Gan len TextMesh hoac UI Text da thiet ke san. Code chi dien con so vao cac cho {0}..{4} trong `format`.
//   {0} = vang cua player        {1} = so cowboy da thue      {2} = so chong canh dang dat
//   {3} = so chong go trong tui  {4} = so chong sat trong tui
public class LiveText : MonoBehaviour
{
    [TextArea(1, 3)]
    public string format = "{0}";

    TextMesh mesh;
    Text ui;

    void Awake()
    {
        mesh = GetComponent<TextMesh>();
        ui = GetComponent<Text>();
    }

    void OnEnable() => Refresh();
    void Update() => Refresh();

    void Refresh()
    {
        var wallet = PlayerWallet.Instance;
        string s;
        try
        {
            s = string.Format(format,
                wallet ? wallet.gold : 0,
                CowboyRoster.Count,
                GuardFrame.All.Count,
                GuardFrameStock.Count(FrameKind.Wood),
                GuardFrameStock.Count(FrameKind.Iron));
        }
        catch (System.FormatException) { return; }

        if (mesh && mesh.text != s) mesh.text = s;
        if (ui && ui.text != s) ui.text = s;
    }
}
