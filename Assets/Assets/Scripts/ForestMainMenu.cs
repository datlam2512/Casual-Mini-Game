using UnityEngine;
using UnityEngine.SceneManagement;
using static TrailInterface;

public sealed class ForestMainMenu : MonoBehaviour
{
    private enum Page { Home, Map, Graphics, Exit }
    private static readonly Color Olive = new(.33f, .42f, .19f);
    private static readonly string[] QualityChoices = { "Low", "Medium", "High" };
    private static readonly string[] FpsChoices = { "30", "60", "120", "Unlimited" };
    private static readonly string[] ScreenChoices = { "Windowed", "Fullscreen" };
    private static readonly Vector2[] Pins =
    {
        new(127, 125), new(420, 112), new(711, 128),
        new(718, 216), new(430, 218), new(145, 222),
        new(132, 318), new(418, 307), new(705, 315),
        new(716, 409), new(433, 401), new(155, 414)
    };

    private Page page;
    private bool loading;
    private Texture2D valley, map;
    private GUIStyle title, subtitle, heading, button, body, small;
    private static readonly string[] Numbers = CreateNumbers();

    private static string[] CreateNumbers()
    {
        var numbers = new string[TrailCatalog.Count];
        for (int i = 0; i < numbers.Length; i++) numbers[i] = (i + 1).ToString("00");
        return numbers;
    }

    private void Awake()
    {
        valley = Resources.Load<Texture2D>("TrailMenu/Valley");
        map = Resources.Load<Texture2D>("TrailMenu/Map");
        if (valley != null && Camera.main != null)
        {
            Camera.main.cullingMask = 0;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            page = page == Page.Home ? Page.Exit : Page.Home;
        if (page == Page.Home && Input.GetKeyDown(KeyCode.Return)) Load(1);
    }

    private void OnGUI()
    {
        if (title == null)
        {
            title = Style(82);
            subtitle = Style(43);
            heading = Style(32);
            button = Style(25);
            body = Style(19);
            small = Style(16);
        }

        Matrix4x4 previous = GUI.matrix;
        Vector2 viewport = TrailInterface.Begin();
        var screen = new Rect(Vector2.zero, viewport);
        if (valley != null) GUI.DrawTexture(screen, valley, ScaleMode.ScaleAndCrop);
        Vector2 canvas = viewport * .5f - new Vector2(480, 270);
        switch (page)
        {
            case Page.Map:
                DrawMap(screen, canvas);
                break;
            case Page.Graphics:
                DrawSettings(screen, canvas);
                break;
            case Page.Exit:
                DrawHome(canvas, false);
                DrawExit(screen, canvas);
                break;
            default:
                DrawHome(canvas, true);
                break;
        }
        GUI.matrix = previous;
    }

    private void DrawHome(Vector2 origin, bool interactive)
    {
        var titleRect = At(origin, 30, 39, 545, 98);
        Stamp(titleRect, "TINY", title, Cream, 5);
        Stamp(At(origin, 30, 124, 545, 54), "A D V E N T U R E", subtitle, new Color(1f, .81f, .37f), 4);

        var post = At(origin, 750, 139, 28, 386);
        TrailInterface.Panel(post, Ink, 6);
        TrailInterface.Panel(new Rect(post.x + 5, post.y + 4, post.width - 10, post.height - 7),
            new Color(.53f, .34f, .20f), 4);
        GUI.enabled = interactive && !loading;
        if (Sign(At(origin, 612, 152, 301, 80), "Play", false, true)) Load(1);
        if (Sign(At(origin, 603, 235, 301, 74), "Choose a trail", true)) page = Page.Map;
        if (Sign(At(origin, 617, 311, 301, 74), "Settings", false)) page = Page.Graphics;
        if (Sign(At(origin, 602, 389, 301, 74), "Exit", true)) page = Page.Exit;
        GUI.enabled = true;
    }

    private void DrawMap(Rect screen, Vector2 origin)
    {
        TrailInterface.Panel(screen, new Color(.18f, .22f, .10f, .46f), 0);
        var frame = At(origin, 54, 26, 852, 488);
        DrawPaper(frame);
        var paper = new Rect(frame.x + 9, frame.y + 9, frame.width - 18, frame.height - 18);
        if (map != null)
        {
            GUI.BeginGroup(paper);
            GUI.DrawTexture(new Rect(0, 0, paper.width, paper.height), map, ScaleMode.ScaleAndCrop);
            GUI.EndGroup();
        }
        SignFace(At(frame.position, 296, 8, 261, 66), "Trail map", button, primary: true);
        if (Sign(At(frame.position, 18, 15, 133, 53), "Back", true)) page = Page.Home;

        for (int i = 0; i < Pins.Length - 1; i++)
            DrawRoute(frame.position + Pins[i], frame.position + Pins[i + 1]);
        for (int i = 0; i < Pins.Length; i++)
            if (MapPin(frame.position + Pins[i], i + 1)) Load(i + 1);
    }

    private void DrawSettings(Rect screen, Vector2 origin)
    {
        TrailInterface.Panel(screen, new Color(.17f, .20f, .11f, .52f), 0);
        var frame = At(origin, 158, 55, 644, 430);
        DrawPaper(frame);
        Stamp(At(origin, 315, 76, 330, 52), "Graphics", heading, Cream, 3);
        DrawRow(At(origin, 188, 154, 584, 60), "Quality", QualityChoices, TrailGraphicsSettings.Quality,
            TrailGraphicsSettings.SetQuality);
        DrawRow(At(origin, 188, 231, 584, 60), "FPS limit", FpsChoices, TrailGraphicsSettings.FrameLimit,
            TrailGraphicsSettings.SetFrameLimit);
        DrawRow(At(origin, 188, 308, 584, 60), "Display", ScreenChoices, TrailGraphicsSettings.Fullscreen ? 1 : 0,
            index => TrailGraphicsSettings.SetFullscreen(index == 1));
        if (Sign(At(origin, 377, 398, 206, 61), "Done", false, true)) page = Page.Home;
    }

    private void DrawExit(Rect screen, Vector2 origin)
    {
        TrailInterface.Panel(screen, new Color(.17f, .20f, .11f, .60f), 0);
        DrawPaper(At(origin, 252, 170, 456, 204));
        Text(At(origin, 278, 199, 404, 56), "Exit Tiny Adventure?", heading, Ink);
        if (Sign(At(origin, 277, 274, 187, 63), "Stay", true)) page = Page.Home;
        if (Sign(At(origin, 496, 274, 187, 63), "Exit", false, true))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void DrawRow(Rect area, string label, string[] options, int selected, System.Action<int> apply)
    {
        TrailInterface.Panel(area, new Color(.86f, .72f, .49f), 12);
        Text(new Rect(area.x + 3, area.y, 128, area.height), label, body, Ink);
        const float gap = 5;
        float width = (area.width - 145 - (options.Length - 1) * gap) / options.Length;
        for (int i = 0; i < options.Length; i++)
        {
            var rect = new Rect(area.x + 135 + i * (width + gap), area.y + 8, width, area.height - 16);
            Color color = i == selected ? Olive : Paper;
            if (rect.Contains(Event.current.mousePosition)) color = Color.Lerp(color, Color.white, .17f);
            TrailInterface.Panel(rect, color, 8);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none) && i != selected) apply(i);
            Text(rect, options[i], small, i == selected ? Cream : Ink);
        }
    }

    private bool MapPin(Vector2 point, int number)
    {
        var hit = new Rect(point.x - 104, point.y - 26, 208, 80);
        bool hover = hit.Contains(Event.current.mousePosition);
        bool clicked = GUI.Button(hit, GUIContent.none, GUIStyle.none);
        Color accent = TrailCatalog.Accent(number);
        TrailInterface.Panel(new Rect(point.x - 23, point.y - 20, 46, 46), Ink, 23);
        TrailInterface.Panel(new Rect(point.x - 19, point.y - 17, 38, 38),
            hover ? Color.Lerp(accent, Color.white, .55f) : accent, 19);
        Text(new Rect(point.x - 19, point.y - 17, 38, 38), Numbers[number - 1], body, Ink);
        var name = new Rect(point.x - 93, point.y + 26, 186, 27);
        TrailInterface.Panel(name, hover ? Cream : new Color(.99f, .92f, .73f, .96f), 9);
        Text(name, TrailCatalog.Name(number), small, Ink);
        return clicked;
    }

    private static void DrawRoute(Vector2 from, Vector2 to)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(from, to) / 15);
        for (int i = 1; i < steps; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, (float)i / steps);
            TrailInterface.Panel(new Rect(point.x - 2.5f, point.y - 2.5f, 5, 5),
                new Color(.45f, .31f, .15f, .82f), 2.5f);
        }
    }

    private bool Sign(Rect rect, string caption, bool left, bool primary = false) =>
        TrailInterface.Sign(rect, caption, rect.width < 160 ? body : button, left, primary);

    private static void Text(Rect rect, string caption, GUIStyle style, Color color) =>
        TrailInterface.Label(rect, caption, style, color);

    private void Load(int number)
    {
        if (loading || !Application.CanStreamedLevelBeLoaded(TrailCatalog.Scene(number))) return;
        loading = true;
        SceneManager.LoadSceneAsync(TrailCatalog.Scene(number));
    }
}
