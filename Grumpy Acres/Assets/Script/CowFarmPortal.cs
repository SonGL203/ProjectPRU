using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CowFarm
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class CowFarmPortal : MonoBehaviour
    {
        [SerializeField] bool returnToMain;
        [SerializeField] Transform returnPoint;

        PlayerMove farmPlayer;
        Vector3 farmCenter;

        static CowFarmPortal entrance;
        static Vector3 playerReturn, cameraReturn;
        static float cameraSize;
        static bool busy;
        const string FarmScene = "CowFarm";

        void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerMove>();
            if (!player || busy || MarketTravel.Busy || MarketTravel.InMarket) return;

            if (returnToMain)
            {
                if (entrance) entrance.StartCoroutine(entrance.Exit(player, gameObject.scene));
                return;
            }

            if (Enemy.Alive.Count == 0 && !SceneManager.GetSceneByName(FarmScene).isLoaded &&
                Application.CanStreamedLevelBeLoaded(FarmScene) && Camera.main)
                StartCoroutine(Enter(player));
        }

        IEnumerator Enter(PlayerMove player)
        {
            busy = true;
            var camera = Camera.main;
            cameraReturn = camera.transform.position;
            cameraSize = camera.orthographicSize;
            playerReturn = returnPoint ? returnPoint.position : transform.position + Vector3.left * 2f;

            yield return SceneManager.LoadSceneAsync(FarmScene, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName(FarmScene);
            var farm = FindFirstObjectByType<CowFarmDairy>();
            if (!farm || farm.gameObject.scene != scene || !camera || !player)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
                busy = false;
                yield break;
            }

            entrance = this;
            Move(player, farm.Spawn.position);
            farmPlayer = player;
            farmCenter = farm.transform.position;
            camera.orthographicSize = 5.5f;
            FollowFarmPlayer(camera);
            busy = false;
        }

        void LateUpdate()
        {
            if (entrance != this || busy || !farmPlayer) return;
            var camera = Camera.main;
            if (camera) FollowFarmPlayer(camera);
        }

        void FollowFarmPlayer(Camera camera)
        {
            const float farmHalfWidth = 11.88f;
            const float farmHalfHeight = 7.92f;
            float vertical = camera.orthographicSize;
            float horizontal = vertical * camera.aspect;
            float xRange = Mathf.Max(0f, farmHalfWidth - horizontal);
            float yRange = Mathf.Max(0f, farmHalfHeight - vertical);
            var offset = farmPlayer.transform.position - farmCenter;
            camera.transform.position = new Vector3(
                farmCenter.x + Mathf.Clamp(offset.x, -xRange, xRange),
                farmCenter.y + Mathf.Clamp(offset.y, -yRange, yRange),
                cameraReturn.z);
        }

        IEnumerator Exit(PlayerMove player, Scene scene)
        {
            busy = true;
            Move(player, playerReturn);
            var camera = Camera.main;
            if (camera)
            {
                camera.transform.position = cameraReturn;
                camera.orthographicSize = cameraSize;
            }
            yield return SceneManager.UnloadSceneAsync(scene);
            farmPlayer = null;
            entrance = null;
            busy = false;
        }

        static void Move(PlayerMove player, Vector3 position)
        {
            player.transform.position = position;
            var body = player.GetComponent<Rigidbody2D>();
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetTravel()
        {
            entrance = null;
            busy = false;
        }
    }
}
