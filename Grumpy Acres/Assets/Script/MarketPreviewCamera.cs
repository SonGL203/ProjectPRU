using UnityEngine;

// Camera chi de xem scene CowboyMarket khi mo rieng trong editor.
// Khi scene duoc nap them vao SampleScene (luc choi that) da co Main Camera -> tu bien mat.
public class MarketPreviewCamera : MonoBehaviour
{
    void Awake()
    {
        foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (c.gameObject != gameObject && c.CompareTag("MainCamera"))
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
        }
    }
}
