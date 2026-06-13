using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

// Build toan bo game Mario-like: sprites -> tiles -> animation -> prefab -> scene
public static class MarioGameBuilder
{
    const string SpriteFolder = "Assets/Resources/Simple Platformer 16 Assets- By JuhoSprite";
    const string Gen = "Assets/Generated";
    const int ChunkWidth = 32;

    [MenuItem("Tools/Mario/Build Game")]
    public static void BuildGame()
    {
        EnsureFolders();
        EnsureLayer("Ground");
        ConfigureSprites();
        var tiles = BuildTiles();
        var playerCtrl = BuildPlayerAnimator();
        var enemyCtrl = BuildEnemyAnimator();
        var playerPrefab = BuildPlayerPrefab(playerCtrl);
        var enemyPrefab = BuildEnemyPrefab(enemyCtrl);
        var chunkPrefabs = BuildChunkPrefabs(tiles);
        SetupScene(playerPrefab, enemyPrefab, chunkPrefabs);
        AssetDatabase.SaveAssets();
        Debug.Log("[MarioGameBuilder] Build hoan tat!");
    }

    static void EnsureFolders()
    {
        foreach (var f in new[] { "Assets/Generated", "Assets/Generated/Tiles", "Assets/Generated/Anim", "Assets/Prefabs" })
        {
            if (!AssetDatabase.IsValidFolder(f))
            {
                var parent = Path.GetDirectoryName(f).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(f));
            }
        }
    }

    static void EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;
        for (int i = 8; i < layers.arraySize; i++)
        {
            var sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
        Debug.LogError("Khong con slot layer trong!");
    }

    // ---------- SPRITES ----------

    static void ConfigureSprites()
    {
        foreach (var file in Directory.GetFiles(SpriteFolder, "*.png", SearchOption.AllDirectories))
        {
            var path = file.Replace('\\', '/');
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 16;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }

        SliceSheet(SpriteFolder + "/Player_Spritesheet.png", new (string, Rect)[]
        {
            ("player_idle_0", new Rect(0, 64, 16, 16)),
            ("player_idle_1", new Rect(16, 64, 16, 16)),
            ("player_run_0", new Rect(0, 48, 16, 16)),
            ("player_run_1", new Rect(16, 48, 16, 16)),
            ("player_run_2", new Rect(32, 48, 16, 16)),
            ("player_run_3", new Rect(0, 32, 16, 16)),
            ("player_crouch", new Rect(0, 16, 16, 16)),
            ("player_dead", new Rect(0, 0, 16, 16)),
        });

        SliceSheet(SpriteFolder + "/Enemy1.png", new (string, Rect)[]
        {
            ("enemy_walk_0", new Rect(0, 0, 16, 16)),
            ("enemy_walk_1", new Rect(16, 0, 16, 16)),
            ("enemy_walk_2", new Rect(32, 0, 16, 16)),
            ("enemy_walk_3", new Rect(48, 0, 16, 16)),
            ("enemy_squash", new Rect(80, 0, 16, 16)),
        });
    }

    static void SliceSheet(string path, (string name, Rect rect)[] frames)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        imp.spriteImportMode = SpriteImportMode.Multiple;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(imp);
        provider.InitSpriteEditorDataProvider();

        var rects = frames.Select(f => new SpriteRect
        {
            name = f.name,
            rect = f.rect,
            alignment = SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f),
            spriteID = GUID.Generate()
        }).ToArray();

        provider.SetSpriteRects(rects);
        var nameIdProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameIdProvider != null)
            nameIdProvider.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToArray());
        provider.Apply();
        imp.SaveAndReimport();
    }

    static Sprite LoadSprite(string texName, string spriteName = null)
    {
        var path = SpriteFolder + "/" + texName + ".png";
        var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        if (spriteName == null) return all.FirstOrDefault();
        return all.FirstOrDefault(s => s.name == spriteName);
    }

    // ---------- TILES ----------

    static Dictionary<string, TileBase> BuildTiles()
    {
        var result = new Dictionary<string, TileBase>();
        var defs = new (string key, string tex)[]
        {
            ("grass", "Grass_Block"),
            ("dirt", "Dirt_Block"),
            ("platform", "Platform"),
            ("question", "Question_Block"),
            ("brown", "Empty_Brown_Block"),
        };
        foreach (var d in defs)
        {
            var tilePath = Gen + "/Tiles/" + d.key + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = LoadSprite(d.tex);
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            result[d.key] = tile;
        }
        return result;
    }

    // ---------- ANIMATION ----------

    static AnimationClip MakeClip(string path, Sprite[] sprites, float fps, bool loop)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);

        var clip = new AnimationClip { frameRate = fps };
        var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        if (loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static AnimatorController BuildPlayerAnimator()
    {
        var idle = MakeClip(Gen + "/Anim/Player_Idle.anim",
            new[] { LoadSprite("Player_Spritesheet", "player_idle_0"), LoadSprite("Player_Spritesheet", "player_idle_1") }, 3f, true);
        var run = MakeClip(Gen + "/Anim/Player_Run.anim",
            new[] { LoadSprite("Player_Spritesheet", "player_run_0"), LoadSprite("Player_Spritesheet", "player_run_1"),
                    LoadSprite("Player_Spritesheet", "player_run_2"), LoadSprite("Player_Spritesheet", "player_run_3") }, 10f, true);
        var jump = MakeClip(Gen + "/Anim/Player_Jump.anim",
            new[] { LoadSprite("Player_Spritesheet", "player_run_3") }, 1f, false);
        var dead = MakeClip(Gen + "/Anim/Player_Dead.anim",
            new[] { LoadSprite("Player_Spritesheet", "player_dead") }, 1f, false);

        var ctrlPath = Gen + "/Anim/PlayerAnimator.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sRun = sm.AddState("Run"); sRun.motion = run;
        var sJump = sm.AddState("Jump"); sJump.motion = jump;
        var sDead = sm.AddState("Dead"); sDead.motion = dead;
        sm.defaultState = sIdle;

        AnimatorStateTransition T(AnimatorState from, AnimatorState to)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            return t;
        }

        T(sIdle, sRun).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        T(sRun, sIdle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        T(sIdle, sJump).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
        T(sRun, sJump).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
        T(sJump, sIdle).AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");

        var anyDead = sm.AddAnyStateTransition(sDead);
        anyDead.hasExitTime = false;
        anyDead.duration = 0f;
        anyDead.canTransitionToSelf = false;
        anyDead.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

        T(sDead, sIdle).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
        return ctrl;
    }

    static AnimatorController BuildEnemyAnimator()
    {
        var walk = MakeClip(Gen + "/Anim/Enemy_Walk.anim",
            new[] { LoadSprite("Enemy1", "enemy_walk_0"), LoadSprite("Enemy1", "enemy_walk_1"),
                    LoadSprite("Enemy1", "enemy_walk_2"), LoadSprite("Enemy1", "enemy_walk_3") }, 8f, true);
        var squash = MakeClip(Gen + "/Anim/Enemy_Squash.anim",
            new[] { LoadSprite("Enemy1", "enemy_squash") }, 1f, false);

        var ctrlPath = Gen + "/Anim/EnemyAnimator.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
        var sSquash = sm.AddState("Squash"); sSquash.motion = squash;
        sm.defaultState = sWalk;

        var t = sWalk.AddTransition(sSquash);
        t.hasExitTime = false;
        t.duration = 0f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "Die");
        return ctrl;
    }

    // ---------- PREFABS ----------

    static PhysicsMaterial2D GetSlipperyMaterial()
    {
        var path = Gen + "/Slippery.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("Slippery") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static GameObject BuildPlayerPrefab(AnimatorController ctrl)
    {
        var go = new GameObject("Player");
        go.tag = "Player";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Player_Spritesheet", "player_idle_0");
        sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.6f, 0.94f);
        box.offset = new Vector2(0f, -0.03f);
        box.sharedMaterial = GetSlipperyMaterial();

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;

        var pc = go.AddComponent<PlayerController>();
        pc.moveSpeed = 6f;
        pc.jumpForce = 15f;
        pc.groundMask = LayerMask.GetMask("Ground");

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Player.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject BuildEnemyPrefab(AnimatorController ctrl)
    {
        var go = new GameObject("Enemy");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Enemy1", "enemy_walk_0");
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.85f);
        box.offset = new Vector2(0f, -0.07f);
        box.sharedMaterial = GetSlipperyMaterial();

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;

        var ec = go.AddComponent<EnemyController>();
        ec.moveSpeed = 2.5f;
        ec.direction = -1;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Enemy.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------- MAP CHUNKS ----------

    static GameObject[] BuildChunkPrefabs(Dictionary<string, TileBase> tiles)
    {
        var a = BuildChunk("Chunk_Flat", tiles, tm =>
        {
            PaintGround(tm, tiles);
            for (int x = 10; x <= 12; x++) tm.SetTile(new Vector3Int(x, 3, 0), tiles["question"]);
            tm.SetTile(new Vector3Int(18, 3, 0), tiles["brown"]);
            tm.SetTile(new Vector3Int(19, 3, 0), tiles["brown"]);
        }, new (string tex, Vector3 pos)[]
        {
            ("Big_Bush", new Vector3(6f, 0.5f, 0f)),
            ("Hill_0", new Vector3(24f, 1f, 0f)),
            ("Clouds", new Vector3(16f, 6.5f, 0f)),
        });

        var b = BuildChunk("Chunk_Platforms", tiles, tm =>
        {
            PaintGround(tm, tiles);
            for (int x = 6; x <= 9; x++) tm.SetTile(new Vector3Int(x, 2, 0), tiles["platform"]);
            for (int x = 14; x <= 17; x++) tm.SetTile(new Vector3Int(x, 4, 0), tiles["platform"]);
            for (int x = 22; x <= 25; x++) tm.SetTile(new Vector3Int(x, 2, 0), tiles["platform"]);
        }, new (string tex, Vector3 pos)[]
        {
            ("Small_Bush", new Vector3(28f, 0.5f, 0f)),
            ("Clouds", new Vector3(8f, 7f, 0f)),
        });

        var c = BuildChunk("Chunk_Hill", tiles, tm =>
        {
            PaintGround(tm, tiles);
            for (int x = 12; x <= 19; x++) tm.SetTile(new Vector3Int(x, 0, 0), tiles["grass"]);
            tm.SetTile(new Vector3Int(15, 4, 0), tiles["question"]);
            tm.SetTile(new Vector3Int(16, 4, 0), tiles["question"]);
        }, new (string tex, Vector3 pos)[]
        {
            ("Hill_1", new Vector3(4f, 1.5f, 0f)),
            ("Big_Bush", new Vector3(26f, 0.5f, 0f)),
            ("Clouds", new Vector3(26f, 7f, 0f)),
        });

        return new[] { a, b, c };
    }

    static void PaintGround(Tilemap tm, Dictionary<string, TileBase> tiles)
    {
        for (int x = 0; x < ChunkWidth; x++)
        {
            tm.SetTile(new Vector3Int(x, -1, 0), tiles["grass"]);
            for (int y = -4; y <= -2; y++)
                tm.SetTile(new Vector3Int(x, y, 0), tiles["dirt"]);
        }
    }

    static GameObject BuildChunk(string name, Dictionary<string, TileBase> tiles,
        System.Action<Tilemap> paint, (string tex, Vector3 pos)[] decors)
    {
        var root = new GameObject(name);

        var gridGo = new GameObject("Grid");
        gridGo.transform.SetParent(root.transform, false);
        gridGo.AddComponent<Grid>();

        var groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(gridGo.transform, false);
        groundGo.layer = LayerMask.NameToLayer("Ground");
        var tm = groundGo.AddComponent<Tilemap>();
        groundGo.AddComponent<TilemapRenderer>();

        paint(tm);

        var rb = groundGo.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var tmc = groundGo.AddComponent<TilemapCollider2D>();
        tmc.compositeOperation = Collider2D.CompositeOperation.Merge;
        groundGo.AddComponent<CompositeCollider2D>();

        foreach (var d in decors)
        {
            var decorGo = new GameObject(d.tex);
            decorGo.transform.SetParent(root.transform, false);
            decorGo.transform.localPosition = d.pos;
            var dsr = decorGo.AddComponent<SpriteRenderer>();
            dsr.sprite = LoadSprite(d.tex);
            dsr.sortingOrder = -10;
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/" + name + ".prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------- SCENE ----------

    static void SetupScene(GameObject playerPrefab, GameObject enemyPrefab, GameObject[] chunkPrefabs)
    {
        // Don dep object cu
        foreach (var n in new[] { "Global Volume", "Directional Light", "Player", "GameManager", "MapManager", "EnemySpawner" })
        {
            var old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }
        // Xoa chunk instance cu trong scene
        foreach (var mm in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                     .Where(t => t != null && t.parent == null && t.name.StartsWith("Chunk_")).ToArray())
            Object.DestroyImmediate(mm.gameObject);

        // Camera
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.43f, 0.69f, 1f);
        cam.transform.position = new Vector3(2f, 3f, -10f);

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();

        // Background gan vao camera
        var oldBg = cam.transform.Find("Background");
        if (oldBg != null) Object.DestroyImmediate(oldBg.gameObject);
        var bg = new GameObject("Background");
        bg.transform.SetParent(cam.transform, false);
        bg.transform.localPosition = new Vector3(0f, 0f, 15f);
        bg.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("Background_0");
        bgSr.sortingOrder = -100;

        // Player
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(0f, 2f, 0f);
        follow.target = player.transform;

        // GameManager
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        gm.player = player.GetComponent<PlayerController>();
        gm.spawnPoint = new Vector3(0f, 2f, 0f);
        gm.respawnDelay = 2f;

        // MapManager
        var mmGo = new GameObject("MapManager");
        var mapMgr = mmGo.AddComponent<MapManager>();
        mapMgr.chunkPrefabs = chunkPrefabs;
        mapMgr.player = player.transform;
        mapMgr.chunkWidth = ChunkWidth;

        // EnemySpawner
        var esGo = new GameObject("EnemySpawner");
        var es = esGo.AddComponent<EnemySpawner>();
        es.enemyPrefab = enemyPrefab;
        es.spawnInterval = 3f;
        es.spawnY = 1.5f;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }
}
