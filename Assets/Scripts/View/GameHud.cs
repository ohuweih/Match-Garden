using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Shared sizing keeps the three HUD columns apart when the Game view is resized.
public static class GameHud
{
    public const float Margin = 16f;
    private const float Gap = 12f;
    public static float ColumnWidth => Mathf.Min(360f, Mathf.Max(1f, (Screen.width - Margin * 2f - Gap * 2f) / 3f));
    public static float LeftColumn => Margin;
    public static float CenterColumn => (Screen.width - ColumnWidth) * 0.5f;
    public static float RightColumn => Screen.width - Margin - ColumnWidth;
    public static float TopRowHeight
    {
        get
        {
            EnsureStyles();
            return Mathf.Max(76f, info.CalcHeight(new GUIContent("HOT STREAK!\nMove 5/5"), ColumnWidth));
        }
    }
    public static float SecondRowY => Margin + TopRowHeight + Gap;

    private static GUISkin cachedSkin;
    private static GUIStyle info;
    private static GUIStyle bodyText;
    private static GUIStyle button;

    // GUI.skin is only accessed from OnGUI through these drawing methods.
    private static void EnsureStyles()
    {
        if (info != null && cachedSkin == GUI.skin) return;
        cachedSkin = GUI.skin;
        info = new GUIStyle(GUI.skin.box)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            padding = new RectOffset(16, 16, 14, 14),
            border = new RectOffset(0, 0, 0, 0)
        };
        info.normal.background = Texture2D.whiteTexture;
        info.normal.textColor = Color.white;
        bodyText = new GUIStyle(info)
        {
            fontSize = 20,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperLeft
        };
        button = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(16, 16, 12, 12)
        };
        button.normal.textColor = Color.white;
        button.hover.textColor = Color.white;
        button.active.textColor = Color.white;
        button.focused.textColor = Color.white;
    }

    public static void Panel(float x, float y, string text, float minimumHeight, bool body = false)
    {
        EnsureStyles();
        var style = body ? bodyText : info;
        float height = Mathf.Max(minimumHeight, style.CalcHeight(new GUIContent(text), ColumnWidth));
        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.08f, 0.10f, 0.14f, 0.97f);
        GUI.Box(new Rect(x, y, ColumnWidth, height), text, style);
        GUI.backgroundColor = previousBackground;
    }

    public static bool Result(string title, string message, string buttonLabel = "Retry")
    {
        EnsureStyles();
        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;
        GUI.depth = -100;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
        string content = title + "\n\n" + message;
        float height = Mathf.Max(150f, info.CalcHeight(new GUIContent(content), ColumnWidth));
        float top = Mathf.Max(Margin, (Screen.height - height - 76f) * 0.5f);
        Panel(CenterColumn, top, content, height);
        bool clicked = GUI.Button(new Rect(CenterColumn, top + height + Gap, ColumnWidth, 64f), buttonLabel, button);
        GUI.depth = previousDepth;
        return clicked;
    }

    public static float PanelHeight(string text, float minimumHeight, bool body = false)
    {
        EnsureStyles();
        return Mathf.Max(minimumHeight, (body ? bodyText : info).CalcHeight(new GUIContent(text), ColumnWidth));
    }

    public static float ButtonHeight(string text)
    {
        EnsureStyles();
        return Mathf.Max(56f, button.CalcHeight(new GUIContent(text), ColumnWidth));
    }

    public static bool Button(float x, float y, string text)
    {
        EnsureStyles();
        return GUI.Button(new Rect(x, y, ColumnWidth, ButtonHeight(text)), text, button);
    }

    public static bool PointerOver(float x, float y, float width, float height)
    {
#if ENABLE_INPUT_SYSTEM
        var pointer = Pointer.current;
        if (pointer == null) return false;
        var position = pointer.position.ReadValue();
#else
        var mouse = Input.mousePosition;
        var position = new Vector2(mouse.x, mouse.y);
#endif
        return new Rect(x, y, width, height).Contains(new Vector2(position.x, Screen.height - position.y));
    }

    public static bool HintButton()
    {
        EnsureStyles();
        float width = Mathf.Min(200f, ColumnWidth);
        return GUI.Button(new Rect(Screen.width - Margin - width, Margin, width, TopRowHeight), "Hint", button);
    }
}
