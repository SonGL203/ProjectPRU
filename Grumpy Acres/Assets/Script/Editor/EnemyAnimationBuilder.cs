#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EnemyAnimationBuilder
{
    const string SpriteDir = "Assets/sprites/Enemy";
    const string AnimDir = "Assets/Animation";
    const string PrefabDir = "Assets/Prefabs";
    const float FrameRate = 20f;
    const float PrefabScale = 0.3f;

    static readonly string[] Actions = { "idle", "walk", "run", "attack", "hurt", "die", "jump" };
    static readonly HashSet<string> Looping = new() { "idle", "walk", "run" };

    [MenuItem("Tools/Farm/3. Build Enemy Animations")]
    public static void BuildAll()
    {
        Build("5_enemies_1", "Enemy1");
        Build("9_enemies_1", "Enemy2");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Da tao animation + controller + prefab cho Enemy1, Enemy2.");
    }

    static void Build(string prefix, string name)
    {
        string dir = $"{AnimDir}/{name}";
        EnsureFolder(dir);
        EnsureFolder(PrefabDir);

        var clips = new Dictionary<string, AnimationClip>();
        foreach (var action in Actions)
        {
            var sprites = LoadSprites(prefix, action);
            if (sprites.Count == 0) continue;
            clips[action] = MakeClip($"{dir}/{name}_{action}.anim", sprites, Looping.Contains(action));
        }

        var controller = MakeController($"{dir}/{name}Controller.controller", clips);
        MakePrefab($"{PrefabDir}/{name}.prefab", controller, LoadSprites(prefix, "idle"));
    }

    static List<Sprite> LoadSprites(string prefix, string action)
    {
        var list = new List<Sprite>();
        var files = Directory.GetFiles(SpriteDir, $"{prefix}_{action}_*.png");
        System.Array.Sort(files);
        foreach (var f in files)
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(f.Replace('\\', '/')))
                if (obj is Sprite s) { list.Add(s); break; }
        return list;
    }

    static AnimationClip MakeClip(string path, List<Sprite> sprites, bool loop)
    {
        AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { frameRate = FrameRate };

        var keys = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < keys.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / FrameRate, value = sprites[i] };

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static AnimatorController MakeController(string path, Dictionary<string, AnimationClip> clips)
    {
        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var states = new Dictionary<string, AnimatorState>();
        foreach (var kv in clips)
        {
            var st = sm.AddState(kv.Key);
            st.motion = kv.Value;
            states[kv.Key] = st;
        }

        sm.defaultState = states["idle"];

        var toWalk = states["idle"].AddTransition(states["walk"]);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.05f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var toIdle = states["walk"].AddTransition(states["idle"]);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        AnyTo(sm, states["attack"], "Attack", states["idle"]);
        AnyTo(sm, states["hurt"], "Hurt", states["idle"]);
        AnyTo(sm, states["die"], "Die", null);

        return ctrl;
    }

    static void AnyTo(AnimatorStateMachine sm, AnimatorState target, string trigger, AnimatorState back)
    {
        var t = sm.AddAnyStateTransition(target);
        t.hasExitTime = false;
        t.duration = 0f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);

        if (back == null) return;
        var r = target.AddTransition(back);
        r.hasExitTime = true;
        r.exitTime = 1f;
        r.duration = 0f;
    }

    static void MakePrefab(string path, AnimatorController ctrl, List<Sprite> idle)
    {
        var go = new GameObject(Path.GetFileNameWithoutExtension(path));
        var sr = go.AddComponent<SpriteRenderer>();
        if (idle.Count > 0) sr.sprite = idle[0];
        sr.sortingOrder = 5;
        go.AddComponent<Animator>().runtimeAnimatorController = ctrl;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(1.2f, 1.6f);

        go.transform.localScale = Vector3.one * PrefabScale;
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
