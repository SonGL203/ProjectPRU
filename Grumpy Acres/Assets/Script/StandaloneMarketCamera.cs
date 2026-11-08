using UnityEngine;

public class StandaloneMarketCamera : MonoBehaviour
{
    void Awake()
    {
        // Khi vao Market tu Farm (SampleScene) qua MarketTravel (Load Additive),
        // Farm da co Main Camera va AudioListener rieng.
        // Camera nay chi hoat dong khi ban mo truc tiep scene Market va bam Play de test view!
        var cams = Camera.allCameras;
        for (int i = 0; i < cams.Length; i++)
        {
            var c = cams[i];
            if (c != null && c.gameObject != gameObject && c.isActiveAndEnabled)
            {
                gameObject.SetActive(false);
                return;
            }
        }
    }
}
