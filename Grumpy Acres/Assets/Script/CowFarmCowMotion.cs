using UnityEngine;

namespace CowFarm
{
    [DefaultExecutionOrder(-5)]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CowFarmCowMotion : MonoBehaviour
    {
        [SerializeField] Sprite[] idleFrames;
        [SerializeField, Min(0.1f)] float framesPerSecond = 5f;
        [SerializeField] Vector2 wanderRange = new(0.55f, 0.45f);
        [SerializeField, Min(0.1f)] float walkSpeed = 0.55f;

        SpriteRenderer cow;
        PlayerMove player;
        Vector3 home, destination;
        float frameOffset, waitUntil;

        void Awake()
        {
            cow = GetComponent<SpriteRenderer>();
            player = FindFirstObjectByType<PlayerMove>();
            home = transform.localPosition;
            destination = home;
            frameOffset = Mathf.Abs(transform.position.x * 7.3f);
            waitUntil = Time.time + Random.Range(0.5f, 2.5f);
        }

        void Update()
        {
            if (Time.time >= waitUntil) Wander();

            if (idleFrames.Length == 0) return;
            int frame = Mathf.FloorToInt(Time.time * framesPerSecond + frameOffset) % idleFrames.Length;
            cow.sprite = idleFrames[frame];
        }

        void Wander()
        {
            var position = transform.localPosition;
            if ((destination - position).sqrMagnitude < 0.0025f)
            {
                destination = home + new Vector3(Random.Range(-wanderRange.x, wanderRange.x),
                    Random.Range(-wanderRange.y, wanderRange.y), 0f);
                waitUntil = Time.time + Random.Range(1.25f, 3f);
                return;
            }

            var next = Vector3.MoveTowards(position, destination, walkSpeed * Time.deltaTime);
            var worldNext = transform.parent ? transform.parent.TransformPoint(next) : next;
            if (player)
            {
                var playerPosition = (Vector2)player.transform.position;
                float currentDistance = (playerPosition - (Vector2)transform.position).sqrMagnitude;
                float nextDistance = (playerPosition - (Vector2)worldNext).sqrMagnitude;
                if (nextDistance < 0.64f && nextDistance < currentDistance) return;
            }

            if (Mathf.Abs(destination.x - position.x) > 0.05f)
                cow.flipX = destination.x < position.x;
            transform.localPosition = next;
        }
    }
}
