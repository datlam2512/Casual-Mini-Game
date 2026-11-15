using UnityEngine;

internal static class TrailInterface
{
    public static readonly Color Ink = new(.23f, .15f, .095f);
    public static readonly Color Cream = new(1f, .94f, .77f);
    public static readonly Color Paper = new(.98f, .88f, .66f);
    public static readonly Color Gold = new(.98f, .74f, .31f);
    public static readonly Color Scrim = new(.17f, .20f, .11f, .60f);
    private static Texture2D sign;
    private static Texture2D backSign;

    public static GUIStyle Style(int size)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset()
        };
        style.normal.textColor = Color.white;
        return style;
    }

    public static Vector2 Begin()
    {
        float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        return new Vector2(Screen.width / scale, Screen.height / scale);
    }

    public static void Panel(Rect rect, Color color, float radius = 14)
    {
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, radius);
    }

    public static void Label(Rect rect, string value, GUIStyle style, Color color)
    {
        Color previous = GUI.contentColor;
        GUI.contentColor = color;
        GUI.Label(rect, value, style);
        GUI.contentColor = previous;
    }

    public static bool Sign(Rect rect, string caption, GUIStyle style, bool left = false, bool primary = false)
    {
        bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        SignFace(rect, caption, style, left, hover, primary);
        return clicked;
    }

    public static void SignFace(Rect rect, string caption, GUIStyle style, bool left = false, bool hover = false,
        bool primary = false)
    {
        if (left && !backSign) backSign = Resources.Load<Texture2D>("TrailMenu/SignBack");
        if (!left && !sign) sign = Resources.Load<Texture2D>("TrailMenu/Sign");
        Texture2D texture = left ? backSign : sign;
        if (texture != null)
        {
            Color previous = GUI.color;
            GUI.color = hover ? Color.white : primary ? new Color(1f, .99f, .92f) : new Color(.93f, .91f, .82f);
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }
        else Panel(rect, Ink);
        rect.x += left ? 4 : -5;
        rect.y -= 1;
        Stamp(rect, caption, style, primary ? Gold : Cream, 2);
    }

    public static void DrawPaper(Rect rect)
    {
        Panel(new Rect(rect.x + 4, rect.y + 8, rect.width, rect.height), new Color(.15f, .11f, .06f, .4f), 22);
        Panel(rect, Ink, 20);
        Panel(new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10), Paper, 16);
    }

    public static Rect At(Vector2 origin, float x, float y, float width, float height) =>
        new(origin.x + x, origin.y + y, width, height);

    public static void Stamp(Rect area, string caption, GUIStyle style, Color fill, float stroke)
    {
        Label(At(area.position, 0, stroke + 1, area.width, area.height), caption, style, Ink);
        Label(At(area.position, -stroke, 0, area.width, area.height), caption, style, Ink);
        Label(At(area.position, stroke, 0, area.width, area.height), caption, style, Ink);
        Label(At(area.position, 0, -stroke, area.width, area.height), caption, style, Ink);
        Label(area, caption, style, fill);
    }
}
