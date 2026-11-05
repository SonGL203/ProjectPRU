#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CowboyAnimationBuilder
{
    const string SpriteDir = "Assets/sprites/Cowbow";
    const string AnimDir = "Assets/Animation";
    const string PrefabDir = "Assets/Prefabs";
    const int Cell = 128;
    const float FrameRate = 12f;
    const float PrefabScale = 1.4f;

    struct Tier
    {
        public string name, suffix;
        public float interval, range, damage, health;
    }

    static readonly Tier[] Tiers =
    {
        new Tier { name = "Cowboy1", suffix = "",   interval = 1.2f, range = 5f, damage = 12f, health = 50f },
        new Tier { name = "Cowboy2", suffix = " 1", interval = 0.9f, range = 6f, damage = 15f, health = 70f },
        new Tier { name = "Cowboy3", suffix = " 2", interval = 0.6f, range = 7f, damage = 20f, health = 90f },
    };

    [MenuItem("Tools/Farm/4. Build Cowboys")]
    public static void BuildAll()
    {
        foreach (var t in Tiers) Build(t);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Da tao animation + controller + prefab Cowboy1, Cowboy2, Cowboy3 trong Assets/Prefabs.");
    }

    static void Build(Tier t)
    {
        string dir = $"{AnimDir}/{t.name}";
        EnsureFolder(dir);
        EnsureFolder(PrefabDir);

        var idle = LoadFrames($"Idle{t.suffix}");
        var walk = LoadFrames($"Walk{t.suffix}");
        var shot = LoadFrames($"Shot{t.suffix}");

        var clips = new Dictionary<string, AnimationClip>
        {
            ["idle"] = MakeClip($"{dir}/{t.name}_idle.anim", idle, true),
            ["walk"] = MakeClip($"{dir}/{t.name}_walk.anim", walk, true),
            ["shot"] = MakeClip($"{dir}/{t.name}_shot.anim", shot, false),
        };

        var controller = MakeController($"{dir}/{t.name}Controller.controller", clips);
        MakePrefab($"{PrefabDir}/{t.name}.prefab", controller, idle, t);
    }

    static List<Sprite> LoadFrames(string file)
    {
        string path = $"{SpriteDir}/{file}.png";
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int count = tex.width / Cell;

        var meta = new SpriteMetaData[count];
        for (int i = 0; i < count; i++)
            meta[i] = new SpriteMetaData
            {
                name = $"{file}_{i}",
                rect = new Rect(i * Cell, 0, Cell, tex.height),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
            };

        imp.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618
        imp.spritesheet = meta;
#pragma warning restore CS0618
        imp.filterMode = FilterMode.Point;
        imp.SaveAndReimport();

        var list = new List<Sprite>();
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is Sprite s) list.Add(s);
        list.Sort((a, b) => IndexOf(a.name).CompareTo(IndexOf(b.name)));
        return list;
    }

    static int IndexOf(string spriteName) => int.Parse(spriteName.Substring(spriteName.LastIndexOf('_') + 1));

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
        ctrl.AddParameter("Walk", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);

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
        toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Walk");

        var toIdle = states["walk"].AddTransition(states["idle"]);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Walk");

        var any = sm.AddAnyStateTransition(states["shot"]);
        any.hasExitTime = false;
        any.duration = 0f;
        any.canTransitionToSelf = false;
        any.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");

        var back = states["shot"].AddTransition(states["idle"]);
        back.hasExitTime = true;
        back.exitTime = 1f;
        back.duration = 0f;

        return ctrl;
    }

    static void MakePrefab(string path, AnimatorController ctrl, List<Sprite> idle, Tier t)
    {
        var go = new GameObject(Path.GetFileNameWithoutExtension(path));
        var sr = go.AddComponent<SpriteRenderer>();
        if (idle.Count > 0) sr.sprite = idle[0];
        sr.sortingOrder = 5;
        go.AddComponent<Animator>().runtimeAnimatorController = ctrl;

        var cb = go.AddComponent<Cowboy>();
        cb.fireInterval = t.interval;
        cb.shootRange = t.range;
        cb.bulletDamage = t.damage;
        cb.maxHealth = t.health;

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
