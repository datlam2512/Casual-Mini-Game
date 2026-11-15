using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

internal static class TrailExpansion
{
    private const string Art = "Assets/TinyAdventure/Level01/";
    private const string Output = "Assets/TinyAdventure/TrailExpansion/";
    private static Mesh shardMesh;
    private static Texture2D surfaceTexture, cliffTexture;
    private const string Template = "Assets/Scenes/Level02.unity";
    private static readonly float[][] Turns =
    {
        new[] { 0f, 0, 1, 1, 0, -1, 0 },
        new[] { 0f, -1, -2, -1, 0, 1, 2, 1 },
        new[] { 0f, 1, 2, 1, 0, -1, -2, -1, 0 },
        new[] { 0f, 0, -1, 0, 1, 0, -1, 0, 1, 1 },
        new[] { 0f, -1, -2, -2, -1, 0, 1, 2, 1, 0 },
        new[] { 0f, 1, 2, 2, 1, 0, 0, -1, -2, -1, 0 },
        new[] { 0f, -1, 0, 1, 2, 1, 0, -1, -2, -1, 0, 1 },
        new[] { 0f, 0, 1, 2, 1, 0, -1, -2, -1, 0, 1, 0 },
        new[] { 0f, -1, -2, -1, 0, 1, 0, -1, 0, 1, 2, 1, 0 },
        new[] { 0f, 1, 2, 1, 0, -1, -2, -1, 0, 1, 2, 1, 0, -1 }
    };

    private static readonly Color32[] GroundColors =
    {
        new(117, 166, 69, 255), new(148, 101, 61, 255), new(85, 147, 82, 255), new(166, 105, 56, 255),
        new(59, 132, 104, 255), new(88, 144, 177, 255), new(165, 104, 105, 255), new(137, 188, 211, 255),
        new(80, 76, 138, 255), new(98, 57, 58, 255), new(61, 73, 144, 255), new(115, 92, 62, 255)
    };
    private static readonly Color32[] SkyColors =
    {
        new(167, 212, 219, 255), new(227, 173, 132, 255), new(163, 211, 194, 255), new(235, 182, 119, 255),
        new(142, 206, 193, 255), new(152, 195, 231, 255), new(217, 158, 166, 255), new(185, 216, 235, 255),
        new(112, 103, 175, 255), new(150, 86, 99, 255), new(88, 105, 180, 255), new(188, 153, 107, 255)
    };

    private sealed class Theme
    {
        public Material surface, stone, trim, foliage, glow, sky, cloud, hazard, path;
        public Color fog;
    }

    [MenuItem("Tiny Adventure/Build new trails")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previous = SceneManager.GetActiveScene().path;
        if (!AssetDatabase.IsValidFolder(Output.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/TinyAdventure", "TrailExpansion");
        Mesh terrace = CreatePrism("Terrace", .50f, .50f, .15f);
        Mesh cliff = CreatePrism("Cliff", .50f, .32f, .22f);
        AddPrismUV(terrace);
        AddPrismUV(cliff);
        shardMesh = CreateShard();
        surfaceTexture = SurfaceTexture("StoneGrain", false);
        cliffTexture = SurfaceTexture("CliffStrata", true);
        var levels = EditorBuildSettings.scenes.ToList();
        for (int number = 3; number <= TrailCatalog.Count; number++)
        {
            string path = "Assets/Scenes/" + TrailCatalog.Scene(number) + ".unity";
            BuildTrail(number, path, terrace, cliff);
            if (!levels.Any(entry => entry.path == path))
                levels.Insert(levels.FindLastIndex(entry => entry.path.StartsWith("Assets/Scenes/Level", StringComparison.Ordinal)) + 1,
                    new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = levels.ToArray();
        PolishOriginalScenes();
        ExtendOriginal(1, terrace, cliff);
        ExtendOriginal(2, terrace, cliff);
        AssetDatabase.SaveAssets();
        if (!Application.isBatchMode && !string.IsNullOrEmpty(previous))
            EditorSceneManager.OpenScene(previous);
    }

    private static void PolishOriginalScenes()
    {
        foreach (string name in new[] { "MainMenu", "Level01_GreenHills", "Level02" })
        {
            var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{name}.unity");
            var camera = Camera.main;
            camera.farClipPlane = 155;
            if (name == "MainMenu")
            {
                camera.transform.localPosition = new Vector3(3.4f, 9.6f, -12.8f);
                camera.transform.localRotation = Quaternion.Euler(15, -5, 0);
            }
            var depth = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .FirstOrDefault(item => item.name == "Forest Depth");
            if (depth != null)
                foreach (var renderer in depth.GetComponentsInChildren<Renderer>())
                    if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                    {
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException($"Could not update {name}");
        }
    }

    private static void BuildTrail(int number, string path, Mesh terrace, Mesh cliff)
    {
        var scene = EditorSceneManager.OpenScene(Template);
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        var game = Object.FindFirstObjectByType<GreenHillsGame>();
        var follow = Object.FindFirstObjectByType<ThirdPersonCameraFollow>();
        var key = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(light => light.type == LightType.Directional);
        foreach (Transform keep in new[] { player.transform, game.transform, follow.transform, key.transform })
        {
            if (PrefabUtility.IsPartOfPrefabInstance(keep) && PrefabUtility.GetOutermostPrefabInstanceRoot(keep) != keep.gameObject)
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(keep),
                    PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            keep.SetParent(null, true);
        }
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root != player.gameObject && root != game.gameObject && root != follow.gameObject && root != key.gameObject)
                Object.DestroyImmediate(root);

        int rank = number - 3;
        Theme theme = CreateTheme(number);
        var course = new GameObject(TrailCatalog.Name(number)).transform;
        var start = Group("Player spawn", course);
        start.position = new Vector3(0, .04f, -3.7f);
        player.transform.SetPositionAndRotation(start.position, Quaternion.identity);
        follow.transform.SetPositionAndRotation(start.position, Quaternion.identity);
        var camera = follow.GetComponentInChildren<Camera>();
        camera.transform.localPosition = new Vector3(0, 5.25f, -7.4f);
        camera.transform.localRotation = Quaternion.Euler(24f, 0, 0);
        camera.fieldOfView = 56;
        camera.farClipPlane = 145;
        camera.clearFlags = CameraClearFlags.Skybox;

        Fields(game, fields =>
        {
            fields.FindProperty("spawn").objectReferenceValue = start;
            fields.FindProperty("fallHeight").floatValue = -5.5f;
        });
        RenderSettings.skybox = theme.sky;
        RenderSettings.sun = key;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = theme.fog;
        RenderSettings.fogStartDistance = 32f;
        RenderSettings.fogEndDistance = 116f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(new Color(.68f, .75f, .83f), theme.fog, .25f);
        RenderSettings.ambientEquatorColor = new Color(.38f, .46f, .54f);
        RenderSettings.ambientGroundColor = new Color(.22f, .23f, .30f);
        key.transform.rotation = Quaternion.Euler(44, -29 + rank * 7, 0);
        key.color = Color.Lerp(new Color(1f, .91f, .77f), TrailCatalog.Accent(number), number >= 9 ? .37f : .18f);
        key.intensity = number >= 9 ? 1.18f : 1.42f;
        key.shadowStrength = .87f;
        key.shadows = LightShadows.Soft;

        BuildCourse(course, number, theme, terrace, cliff, game, player);
        if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException($"Could not save {path}");
    }

    private static void BuildCourse(Transform course, int number, Theme theme, Mesh terrace, Mesh cliff,
        GreenHillsGame game, PlayerMovement player, Vector3? entrance = null)
    {
        var scenery = Group("Scenery", course);
        var platforms = Group("Route", course);
        var gameplay = Group("Gameplay", course);
        int rank = Mathf.Max(0, number - 3);
        int count = entrance.HasValue ? 9 + number * 2 : 19 + rank;
        var turns = Turns[rank];
        var random = new System.Random(number * 879);
        Vector3 previous = entrance ?? Vector3.zero;
        float previousDepth = 0;
        for (int index = 0; index < count; index++)
        {
            int act = Mathf.Min(2, index * 3 / count);
            bool end = index == count - 1;
            bool checkpoint = index == count / 3 || index == count * 2 / 3;
            bool encounter = index > 1 && !end && !checkpoint;
            bool slime = encounter && index % (rank >= 5 ? 4 : 5) == 3;
            bool sweeper = encounter && !slime && number >= 4 && (index + 2) % (rank >= 6 ? 5 : 7) == 0;
            bool landmark = index == 0 || end || checkpoint || slime || sweeper || index % 6 == 0;
            bool bridge = entrance.HasValue && index == 0 || index > 0 &&
                (index % 6 == 1 || slime || index == count / 3 + 1);
            float width = landmark ? (slime ? 12.6f : 10.3f) : 6.5f - rank * .28f + (index % 2) * .35f;
            float depth = landmark ? (slime ? 11.8f : 9.8f) : 4.8f - rank * .14f;
            float gap = bridge ? 2.8f + (index % 3) * .5f : .82f + rank * .115f + (index % 3) * .13f;
            int leg = index / (turns.Length - 1);
            int bend = index % (turns.Length - 1);
            if (leg % 2 == 1) bend = turns.Length - 1 - bend;
            float x = (entrance?.x ?? 0f) + turns[bend] * (1.55f + .08f * rank);
            float height = (entrance?.y ?? 0f) + (index == 0 ? 0 : index * .105f +
                (index % 4) * (.10f + rank * .033f));
            float z = index == 0 && !entrance.HasValue ? 0 : previous.z + (depth + previousDepth) * .5f + gap;
            Vector3 center = new(x, height, z);

            if (index > 0 && !bridge)
            {
                float rise = height - previous.y;
                float reach = new Vector2(gap + .48f, x - previous.x).magnitude;
                if (reach > 3.6f || rise > .64f)
                    throw new InvalidOperationException($"Unsafe jump: trail {number}, step {index}, {reach:F2} m, rise {rise:F2} m");
            }

            var island = Group($"{index + 1:00}  {(checkpoint ? "Checkpoint" : end ? "Finish" : slime ? "Slime clearing" : "Terrace")}", platforms);
            island.position = center;
            var cap = Piece("Walkable cap", island, terrace, theme.surface, Vector3.zero, new Vector3(width, .30f, depth));
            Ground(cap, terrace);
            Piece("Stone ledge", island, terrace, theme.path, Vector3.down * .25f, new Vector3(width * 1.02f, .18f, depth * 1.02f));
            Piece("Floating cliff", island, cliff, theme.stone, Vector3.down * .42f, new Vector3(width, 3.1f + (index % 4) * .6f, depth));
            if (bridge)
                Bridge(platforms, previous + Vector3.forward * (previousDepth * .5f - .30f),
                    center - Vector3.forward * (depth * .5f - .30f), terrace, theme, act);
            DressTerrace(island, width, depth, number, index, act, terrace, cliff, theme);
            if (end || checkpoint)
                Portal(gameplay, center, game, theme, terrace, end);
            else if (slime)
            {
                Slime(gameplay, center, game, player, rank);
                for (int side = -1; side <= 1; side += 2)
                    for (int c = -1; c <= 1; c++)
                        Coin(gameplay, center + new Vector3(side * 3.9f, .64f, c * 1.15f), game);
            }
            else if (sweeper)
                Sweeper(gameplay, center, game, rank, terrace, theme);
            else
                for (int coin = -1; coin <= 1; coin++)
                    Coin(gameplay, center + new Vector3(coin * .40f * (act - 1), .65f, coin * .96f), game);

            Decorate(scenery, center, width, index, number, act, landmark, random, theme, terrace, cliff);
            previous = center;
            previousDepth = depth;
        }
        Debug.Log($"Trail {number:00}: {count} terraces, route end at {previous.z:F1} m");
    }

    private static void Ground(Transform surface, Mesh mesh)
    {
        surface.gameObject.layer = LayerMask.NameToLayer("Ground");
        surface.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private static void Bridge(Transform parent, Vector3 a, Vector3 b, Mesh mesh, Theme theme, int act)
    {
        var bridge = Group("Bridge", parent);
        bridge.SetPositionAndRotation((a + b) * .5f - Vector3.up * .025f, Quaternion.LookRotation(b - a));
        float length = Vector3.Distance(a, b);
        const float width = 3.05f;
        Ground(Piece("Deck", bridge, mesh, theme.stone, Vector3.zero, new Vector3(width, .34f, length)), mesh);
        int planks = Mathf.CeilToInt(length / .47f);
        float spacing = length / planks;
        for (int i = 0; i < planks; i++)
            Piece("Cross plank", bridge, mesh, i % 4 == 0 ? theme.foliage : theme.path,
                new Vector3(0, .012f, -length * .5f + spacing * (i + .5f)),
                new Vector3(width * .94f, .055f, spacing * .89f), false);
        for (int side = -1; side <= 1; side += 2)
            Piece("Deck rim", bridge, mesh, act == 2 ? theme.trim : theme.stone,
                new Vector3(side * 1.35f, .022f, 0), new Vector3(.12f, .16f, length * .97f), false);
    }

    private static void ExtendOriginal(int number, Mesh terrace, Mesh cliff)
    {
        var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{TrailCatalog.Scene(number)}.unity");
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        var game = Object.FindFirstObjectByType<GreenHillsGame>();
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Beyond the old gate") Object.DestroyImmediate(root);
        var oldGate = GameObject.Find("Finish Gate Activation").GetComponent<LevelProgressTrigger>();
        var meadow = GameObject.Find("07_Portal_Meadow").transform;
        var bounds = meadow.GetComponentsInChildren<Collider>()
            .First(collider => collider.gameObject.layer == LayerMask.NameToLayer("Ground")).bounds;
        Physics.SyncTransforms();
        Vector3 edge = new(meadow.position.x, meadow.position.y, bounds.max.z);
        int ground = LayerMask.GetMask("Ground");
        while (edge.z > meadow.position.z &&
            !Physics.Raycast(edge + Vector3.up * 3, Vector3.down, 5, ground, QueryTriggerInteraction.Ignore))
            edge.z -= .2f;
        if (!Physics.Raycast(edge + Vector3.up * 3, Vector3.down, out var edgeHit, 5, ground, QueryTriggerInteraction.Ignore))
            throw new InvalidOperationException("No walkable edge found at the original gate");
        edge.y = edgeHit.point.y;
        var course = new GameObject("Beyond the old gate").transform;
        var respawn = Group("Gate checkpoint spawn", course);
        respawn.position = new Vector3(edge.x, edge.y + .04f, Mathf.Min(edge.z - .9f, oldGate.transform.position.z - 1.5f));
        oldGate.finish = false;
        oldGate.checkpoint = respawn;
        Theme theme = CreateTheme(number);
        BuildCourse(course, number, theme, terrace, cliff, game, player, edge);

        foreach (var slime in Object.FindObjectsByType<SlimePatrolAI>(FindObjectsSortMode.None))
        {
            if (slime.transform.IsChildOf(course)) continue;
            Fields(slime, fields =>
            {
                fields.FindProperty("detectRadius").floatValue = 5.5f + number * .5f;
                fields.FindProperty("chaseSpeed").floatValue = 2.95f + number * .23f;
            });
        }
        var light = RenderSettings.sun;
        if (light != null)
        {
            light.color = number == 1 ? new Color(1f, .92f, .78f) : new Color(1f, .77f, .53f);
            light.shadowStrength = .86f;
        }
        RenderSettings.fogEndDistance = 143;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException($"Could not extend trail {number}");
    }

    private static Theme CreateTheme(int number)
    {
        Color accent = TrailCatalog.Accent(number);
        bool dusk = number >= 9;
        var theme = new Theme
        {
            surface = MaterialFor(number, "Surface", GroundColors[number - 1]),
            path = MaterialFor(number, "TrailStone", Color.Lerp(GroundColors[number - 1], accent, .46f)),
            stone = MaterialFor(number, "Stone", Color.Lerp(new Color(.12f, .15f, .22f), GroundColors[number - 1], .32f)),
            trim = MaterialFor(number, "Inlay", accent, .14f),
            foliage = MaterialFor(number, "Foliage", Color.Lerp(new Color(.16f, .25f, .31f), accent, .48f)),
            glow = MaterialFor(number, "Beacon", Color.Lerp(accent, Color.white, .28f), .65f),
            cloud = MaterialFor(number, "Mist", Color.Lerp(accent, new Color(.82f, .89f, .98f), .72f)),
            hazard = MaterialFor(number, "Hazard", new Color(1f, .27f, .13f), .53f),
            fog = SkyColors[number - 1]
        };
        theme.surface.SetTexture("_BaseMap", surfaceTexture);
        theme.path.SetTexture("_BaseMap", surfaceTexture);
        theme.stone.SetTexture("_BaseMap", cliffTexture);
        string skyPath = Output + $"{number:00}_Sky.mat";
        theme.sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (!theme.sky)
        {
            theme.sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(theme.sky, skyPath);
        }
        theme.sky.SetColor("_SkyTint", Color.Lerp(theme.fog, new Color(.50f, .62f, .82f), .32f));
        theme.sky.SetColor("_GroundColor", Color.Lerp(theme.fog, Color.white, dusk ? .15f : .43f));
        theme.sky.SetFloat("_AtmosphereThickness", dusk ? .66f : 1.03f);
        theme.sky.SetFloat("_Exposure", dusk ? .78f : 1.05f);
        theme.sky.SetFloat("_SunSize", .032f);
        EditorUtility.SetDirty(theme.sky);
        return theme;
    }

    private static Material MaterialFor(int number, string name, Color color, float emission = 0)
    {
        string path = Output + $"{number:00}_{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", .25f);
        if (emission > 0)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void Decorate(Transform scenery, Vector3 center, float width, int index, int number, int act,
        bool landmark, System.Random random, Theme theme, Mesh terrace, Mesh cliff)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            float Offset(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
            Vector3 position = center + new Vector3(side * (width / 2 + Offset(2.6f, 5f)),
                Offset(-4.2f, -1.3f), Offset(-2.6f, 2.6f));
            var outcrop = Group("Scenic outcrop", scenery);
            outcrop.position = position;
            outcrop.localRotation = Quaternion.Euler(0, Offset(-28, 28), 0);
            var rock = Piece("Weathered rock", outcrop, cliff, theme.stone, Vector3.zero,
                new Vector3(Offset(2.3f, 4.3f), Offset(4.8f, 8f), Offset(2.8f, 4.2f)), false);
            Piece("Layered rim", outcrop, terrace, theme.foliage, Vector3.up * .035f,
                new Vector3(rock.localScale.x * .91f, .27f, rock.localScale.z * .9f), false);
            if (index % 2 == 0)
                Piece("Fractured boulder", outcrop, shardMesh, theme.stone,
                    new Vector3(side * 1.45f, -.4f, .56f), new Vector3(2.3f, 5.4f, 2.3f), false)
                    .localRotation = Quaternion.Euler(9, Offset(0, 90), side * 17);

            if (number <= 5 && (landmark || index % 2 == 0))
            {
                Plant(side == 1 ? "Stylized_Adventure_Tree" : "Stylized_Tall_Tree", outcrop, Vector3.zero,
                    Offset(2.5f, 4.15f), Offset(0, 360));
                Plant("Stylized_Flowering_Bush", outcrop, new Vector3(-.75f, .08f, -.55f), Offset(.75f, 1.5f), 75);
            }
            else if (number == 7 && landmark)
            {
                for (int column = -1; column <= 1; column += 2)
                {
                    Piece("Ruined pillar", outcrop, terrace, theme.path, new Vector3(column * .85f, 3.2f, 0),
                        new Vector3(.56f, 3.2f, .62f), false);
                    Piece("Carved crown", outcrop, terrace, theme.trim, new Vector3(column * .85f, 3.33f, 0),
                        new Vector3(.72f, .23f, .76f), false);
                }
                Piece("Ruined lintel", outcrop, terrace, theme.stone, Vector3.up * 3.85f,
                    new Vector3(2.4f, .45f, .62f), false);
            }
            else
            {
                for (int shard = -1; shard <= 1; shard++)
                {
                    float height = Offset(1.2f, landmark ? 4.7f : 3.0f);
                    bool illuminated = shard == 0 || act == 2;
                    var crystal = Piece("Crystal shard", outcrop, shardMesh, illuminated ? theme.trim : theme.foliage,
                        new Vector3(shard * .72f, height * .94f, Offset(-.7f, .7f)),
                        new Vector3(Offset(.4f, .95f), height, Offset(.42f, .8f)), false);
                    crystal.localRotation = Quaternion.Euler(9 * shard, Offset(0, 90), -15 * shard);
                }
            }

            if (index % 4 == 0)
            {
                Vector3 far = center + new Vector3(side * Offset(14, 25), Offset(-6, -1), Offset(2, 14));
                var ridge = Piece("Distant ridge", scenery, cliff, theme.stone, far,
                    new Vector3(Offset(5, 11), Offset(7, 16), Offset(5, 11)), false);
                ridge.rotation = Quaternion.Euler(0, Offset(-45, 45), Offset(-12, 12));
                var spire = Piece("Horizon peak", scenery, shardMesh, theme.stone,
                    far + new Vector3(side * Offset(11, 18), Offset(4, 11), Offset(11, 20)),
                    new Vector3(Offset(11, 19), Offset(18, 29), Offset(12, 17)), false);
                spire.rotation = Quaternion.Euler(0, Offset(0, 360), side * 9);
                Piece("Mist bank", scenery, cliff, theme.cloud, far + new Vector3(side * 7, -3, 5),
                    new Vector3(Offset(9, 17), Offset(.5f, 1.1f), 7), false);
            }
        }
    }

    private static void DressTerrace(Transform island, float width, float depth, int number, int index, int act,
        Mesh terrace, Mesh cliff, Theme theme)
    {
        int tiles = width >= 10f ? 5 : 3;
        for (int tile = 0; tile < tiles; tile++)
        {
            float z = ((tile + .5f) / tiles - .5f) * depth * .91f;
            float x = Mathf.Sin(tile * 1.6f + index * .62f) * (act * .21f);
            Piece("Inlaid trail slab", island, terrace, theme.path, new Vector3(x, .012f, z),
                new Vector3(2.05f + tile % 2 * .3f, .033f, depth * .88f / tiles), false)
                .localRotation = Quaternion.Euler(0, Mathf.Sin(index + tile) * (3 + act * 2), 0);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float edge = side * (width * .5f - .38f);
            for (int tick = -1; tick <= 1; tick += 2)
                Piece("Landing inlay", island, terrace, theme.trim, new Vector3(edge, .02f, tick * depth * .27f),
                    new Vector3(.22f, .035f, .63f), false);

            if (width < 7) continue;
            var feature = Group("Trail landmark", island);
            feature.localPosition = new Vector3(side * (width / 2 - 1.02f), 0, -depth * .23f);
            Piece("Plinth", feature, terrace, theme.stone, Vector3.up * .19f, new Vector3(1.15f, .21f, 1.0f));
            if (number <= 5)
            {
                Plant(index % 2 == 0 ? "Stylized_Flowering_Bush" : "Stylized_Forest_Mushroom",
                    feature, Vector3.up * .17f, 1.8f, index * 19f);
                Plant("Stylized_Grass_Tuft", island, new Vector3(side * (width / 2 - .8f), .02f, depth * .24f),
                    1.5f, index * 28f);
            }
            else
            {
                float height = 1f + act * .24f;
                Piece("Lantern frame", feature, cliff, theme.foliage, Vector3.up * height,
                    new Vector3(.46f, height - .19f, .46f))
                    .localRotation = Quaternion.Euler(0, 45, 0);
                Piece("Lantern crystal", feature, shardMesh, theme.glow, Vector3.up * (height + .38f),
                    new Vector3(.35f, .40f, .35f), false)
                    .localRotation = Quaternion.Euler(0, 45, 0);
                Piece("Cliff fragments", island, cliff, theme.foliage,
                    new Vector3(side * (width / 2 - .63f), .38f, depth * .28f),
                    new Vector3(.54f, .42f, .72f), false);
            }
        }
    }

    private static void Plant(string name, Transform parent, Vector3 position, float scale, float rotation)
    {
        var plant = Prefab(name, parent, parent.TransformPoint(position));
        plant.transform.localScale *= scale;
        plant.transform.localRotation = Quaternion.Euler(0, rotation, 0);
        foreach (var collider in plant.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (var renderer in plant.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }

    private static void Portal(Transform parent, Vector3 center, GreenHillsGame game, Theme theme, Mesh mesh, bool finish)
    {
        var marker = Group(finish ? "Finish gate" : "Checkpoint gate", parent);
        marker.position = center + Vector3.forward * (finish ? 2 : .3f);
        var respawn = Group("Safe respawn", marker);
        respawn.position = center + new Vector3(0, .04f, -2.1f);
        float span = finish ? 2.5f : 2f;
        var light = Group("Warm beacon light", marker).gameObject.AddComponent<Light>();
        light.transform.localPosition = new Vector3(0, 2.1f, -.65f);
        light.type = LightType.Point;
        light.color = theme.glow.color;
        light.intensity = finish ? 5f : 3.3f;
        light.range = 7f;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Piece("Gate pillar", marker, mesh, theme.stone, new Vector3(sign * span, 2.35f, 0), new Vector3(.56f, 2.35f, .65f));
            Piece("Pillar light", marker, mesh, theme.glow, new Vector3(sign * span, 2.28f, -.31f), new Vector3(.14f, 1.93f, .14f), false);
        }
        Piece("Lintel", marker, mesh, theme.stone, Vector3.up * 2.8f, new Vector3(span * 2 + .72f, .45f, .66f));
        Piece("Beacon strip", marker, mesh, theme.glow, Vector3.up * 2.65f + Vector3.back * .34f,
            new Vector3(span * 1.9f, .13f, .11f), false);
        Piece("Threshold", marker, mesh, theme.glow, Vector3.up * .025f, new Vector3(span * 2, .03f, .8f), false);
        var trigger = marker.gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0, 1.05f, 0);
        trigger.size = new Vector3(finish ? span * 2 : 6.9f, 2.6f, 1.2f);
        AddKinematicBody(marker.gameObject);
        var progress = marker.gameObject.AddComponent<LevelProgressTrigger>();
        progress.game = game;
        progress.checkpoint = respawn;
        progress.finish = finish;
    }

    private static void Slime(Transform parent, Vector3 center, GreenHillsGame game, PlayerMovement player, int rank)
    {
        var a = Group("Slime waypoint A", parent);
        var b = Group("Slime waypoint B", parent);
        a.position = center + new Vector3(-2.2f, .03f, 1.05f);
        b.position = center + new Vector3(2.2f, .03f, 1.05f);
        var slime = Prefab("SlimeEnemy", parent, a.position);
        var patrol = slime.GetComponent<SlimePatrolAI>();
        if (!patrol) patrol = slime.AddComponent<SlimePatrolAI>();
        Fields(patrol, fields =>
        {
            fields.FindProperty("game").objectReferenceValue = game;
            fields.FindProperty("player").objectReferenceValue = player.transform;
            fields.FindProperty("pointA").objectReferenceValue = a;
            fields.FindProperty("pointB").objectReferenceValue = b;
            fields.FindProperty("groundMask").intValue = 1 << LayerMask.NameToLayer("Ground");
            fields.FindProperty("patrolSpeed").floatValue = 1.10f + rank * .11f;
            fields.FindProperty("chaseSpeed").floatValue = 3.15f + rank * .16f;
            fields.FindProperty("detectRadius").floatValue = 5.7f + rank * .25f;
        });
    }

    private static void Sweeper(Transform parent, Vector3 center, GreenHillsGame game, int rank, Mesh mesh, Theme theme)
    {
        var rail = Group("Sweeper rail", parent);
        rail.position = center + Vector3.forward * .65f;
        Piece("Caution stripe", rail, mesh, theme.hazard, Vector3.up * .02f, new Vector3(6.75f, .035f, .24f), false);
        var sweeper = Group("Moving hazard", rail);
        sweeper.localPosition = new Vector3(0, .76f, 0);
        var visual = Piece("Hazard capsule", sweeper, mesh, theme.hazard, Vector3.zero, new Vector3(1.28f, .72f, .65f), false);
        GameObjectUtility.SetStaticEditorFlags(visual.gameObject, 0);
        var hit = sweeper.gameObject.AddComponent<BoxCollider>();
        hit.isTrigger = true;
        hit.center = new Vector3(0, -.36f, 0);
        hit.size = new Vector3(1.26f, .72f, .64f);
        var body = AddKinematicBody(sweeper.gameObject);
        body.interpolation = RigidbodyInterpolation.Interpolate;
        Fields(sweeper.gameObject.AddComponent<TrailSweeper>(), fields =>
        {
            fields.FindProperty("game").objectReferenceValue = game;
            fields.FindProperty("travel").floatValue = 2.45f;
            fields.FindProperty("period").floatValue = 4.8f - rank * .24f;
        });
    }

    private static void Coin(Transform parent, Vector3 position, GreenHillsGame game)
    {
        var coin = Prefab("Golden_Collectible_Coin", parent, position);
        var hit = coin.GetComponent<Collider>();
        if (hit == null) hit = coin.AddComponent<SphereCollider>();
        hit.isTrigger = true;
        if (hit is SphereCollider sphere) sphere.radius = .56f;
        if (!coin.TryGetComponent<Rigidbody>(out _)) AddKinematicBody(coin);
        var pickup = coin.GetComponent<CoinPickup>();
        if (!pickup) pickup = coin.AddComponent<CoinPickup>();
        Fields(pickup, fields => fields.FindProperty("game").objectReferenceValue = game);
    }

    private static Rigidbody AddKinematicBody(GameObject target)
    {
        var body = target.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        return body;
    }

    private static void Fields(Object target, Action<SerializedObject> assign)
    {
        var fields = new SerializedObject(target);
        assign(fields);
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform Group(string name, Transform parent)
    {
        var transform = new GameObject(name).transform;
        transform.SetParent(parent, false);
        return transform;
    }

    private static GameObject Prefab(string name, Transform parent, Vector3 position)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Prefabs/" + name + ".prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        instance.transform.position = position;
        return instance;
    }

    private static Transform Piece(string name, Transform parent, Mesh mesh, Material material, Vector3 local,
        Vector3 scale, bool shadow = true)
    {
        var piece = Group(name, parent);
        piece.localPosition = local;
        piece.localScale = scale;
        piece.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = piece.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
        GameObjectUtility.SetStaticEditorFlags(piece.gameObject, StaticEditorFlags.BatchingStatic);
        return piece;
    }

    private static Texture2D SurfaceTexture(string name, bool cliff)
    {
        string path = Output + name + ".png";
        var saved = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (saved != null) return saved;
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x / (float)size;
            float v = y / (float)size;
            float broad = Mathf.PerlinNoise(u * 5.2f + 4.3f, v * 5.2f + 9.7f);
            float flecks = Mathf.PerlinNoise(u * 41.0f + 16.7f, v * 41.0f + 12.3f);
            float grain = Mathf.PerlinNoise(u * 83 + 3.1f, v * 83 + 30.4f);
            float value = .83f + broad * .13f + (flecks - .5f) * .11f + (grain - .5f) * .07f;
            if (cliff)
            {
                float strata = Mathf.Sin((v * 11f + broad * .6f + u * .23f) * Mathf.PI * 2f);
                value -= .13f * Mathf.SmoothStep(0f, 1f, (strata - .32f) * 1.3f);
            }
            else if (flecks > .68f)
            {
                value += .1f * Mathf.SmoothStep(0f, 1f, (flecks - .68f) * 7f);
            }
            byte tint = (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
            pixels[y * size + x] = new Color32(tint, tint, tint, 255);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void AddPrismUV(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            Vector3 normal = normals[i];
            if (Mathf.Abs(normal.y) > .72f)
                uv[i] = new Vector2(vertex.x + .5f, vertex.z + .5f);
            else
                uv[i] = new Vector2((Mathf.Abs(normal.x) > Mathf.Abs(normal.z) ? vertex.z : vertex.x) + .5f, vertex.y + 1f);
        }
        mesh.uv = uv;
        EditorUtility.SetDirty(mesh);
    }

    private static Mesh CreateShard()
    {
        const string path = Output + "Shard.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing) return existing;
        const int sides = 6;
        var vertices = new List<Vector3>();
        void Triangle(Vector3 a, Vector3 b, Vector3 c) => vertices.AddRange(new[] { a, b, c });
        Vector3 At(int index, float radius, float y)
        {
            float angle = index * Mathf.PI * 2f / sides;
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            Vector3 a = At(i, .5f, -.22f);
            Vector3 b = At(next, .5f, -.22f);
            Vector3 c = At(next, .28f, -1f);
            Vector3 d = At(i, .28f, -1f);
            Triangle(new Vector3(.04f, 0f, -.04f), b, a);
            Triangle(a, b, c);
            Triangle(a, c, d);
            Triangle(Vector3.down, d, c);
        }
        var mesh = new Mesh { name = "Shard" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(Enumerable.Range(0, vertices.Count).ToArray(), 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Mesh CreatePrism(string name, float top, float bottom, float bevel)
    {
        string path = Output + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh) return mesh;
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        Vector2[] outline =
        {
            new(-1 + bevel, -1), new(1 - bevel, -1), new(1, -1 + bevel), new(1, 1 - bevel),
            new(1 - bevel, 1), new(-1 + bevel, 1), new(-1, 1 - bevel), new(-1, -1 + bevel)
        };
        Vector3 At(int i, float radius, float y) => new(outline[i].x * radius, y, outline[i].y * radius);
        void Face(params Vector3[] corners)
        {
            int start = vertices.Count;
            vertices.AddRange(corners);
            for (int i = 1; i < corners.Length - 1; i++)
                indices.AddRange(new[] { start, start + i, start + i + 1 });
        }
        Face(Enumerable.Range(0, 8).Reverse().Select(i => At(i, top, 0)).ToArray());
        Face(Enumerable.Range(0, 8).Select(i => At(i, bottom, -1)).ToArray());
        for (int i = 0; i < 8; i++)
        {
            int next = (i + 1) % 8;
            Face(At(i, top, 0), At(next, top, 0), At(next, bottom, -1), At(i, bottom, -1));
        }
        mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}
