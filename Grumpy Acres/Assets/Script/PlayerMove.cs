using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerMove : MonoBehaviour
{
    public float speed = 4f;
    public float hoeTime = 0.4f;

    [Header("Cuoc dat")]
    public Tilemap ground;
    public Tilemap soil;
    public TileBase soilTile;
    public Tilemap[] blockers;
    public Color tilledColor = new(0.8f, 0.6f, 0.45f);

    Rigidbody2D rb;
    Animator anim;
    Vector2 input;
    Vector2 facing = Vector2.down;
    float hoeTimer;
    string current;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (hoeTimer > 0f)
        {
            hoeTimer -= Time.deltaTime;
            if (hoeTimer <= 0f) Till();
            return;
        }

        float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
        float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1 : 0) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0);
        input = new Vector2(x, y).normalized;
        if (input != Vector2.zero)
            facing = Mathf.Abs(x) > Mathf.Abs(y) ? new Vector2(x, 0) : new Vector2(0, y);

        if (kb.spaceKey.wasPressedThisFrame)
        {
            input = Vector2.zero;
            hoeTimer = hoeTime;
            Play("Hoe", true);
            return;
        }
        Play(input == Vector2.zero ? "Idle" : "Run");
    }

    void FixedUpdate() => rb.linearVelocity = input * speed;

    void Play(string action, bool restart = false)
    {
        string dir = facing.y < 0 ? "Down" : facing.y > 0 ? "Up" : facing.x < 0 ? "Left" : "Right";
        string state = $"{action}_{dir}";
        if (state == current && !restart) return;
        current = state;
        anim.Play(state, 0, 0f);
    }

    // o dat ngay truoc chan nhan vat
    void Till()
    {
        if (!soil || !soilTile) return;
        var feet = rb.position + new Vector2(0f, -0.4f);
        var cell = soil.WorldToCell(feet + facing * 0.8f);
        if (ground && !ground.HasTile(cell)) return;
        foreach (var b in blockers)
            if (b && b.HasTile(cell)) return;

        soil.SetTile(cell, soilTile);
        soil.SetTileFlags(cell, TileFlags.None);
        soil.SetColor(cell, tilledColor);
    }
}
