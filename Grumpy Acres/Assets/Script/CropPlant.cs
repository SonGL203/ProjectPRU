using UnityEngine;

public class CropPlant : MonoBehaviour
{
    [Header("Thanh phan")]
    public SpriteRenderer spriteRenderer;

    [Header("Hieu ung Visual")]
    [Tooltip("Hieu ung nhay/nay nhe khi cay lon sang giai doan tiep theo")]
    public bool bounceOnGrow = true;

    [Tooltip("Bieu tuong/hieu ung khi cay da chin")]
    public GameObject matureIndicator;

    Vector3 initialScale;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        initialScale = transform.localScale;
    }

    /// <summary>
    /// Cap nhat Sprite theo giai doan cua cay
    /// </summary>
    public void SetStage(Sprite stageSprite, bool isMature = false)
    {
        if (spriteRenderer != null && stageSprite != null)
        {
            spriteRenderer.sprite = stageSprite;
        }

        if (matureIndicator != null)
        {
            matureIndicator.SetActive(isMature);
        }

        if (bounceOnGrow && gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateGrowBounce());
        }
    }

    System.Collections.IEnumerator AnimateGrowBounce()
    {
        Vector3 baseScale = initialScale != Vector3.zero ? initialScale : transform.localScale;
        Vector3 peakScale = baseScale * 1.15f;

        float elapsed = 0f;
        float duration = 0.12f;

        // Phong to nhe
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(baseScale, peakScale, elapsed / duration);
            yield return null;
        }

        // Thu ve kich thuoc chuan
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(peakScale, baseScale, elapsed / duration);
            yield return null;
        }

        transform.localScale = baseScale;
    }

    /// <summary>
    /// Hieu ung chớp đỏ khi bị quái cắn
    /// </summary>
    public void FlashDamaged()
    {
        if (spriteRenderer != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(DamageFlashRoutine());
        }
    }

    System.Collections.IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 1f);
        yield return new WaitForSeconds(0.12f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = original;
        }
    }
}
