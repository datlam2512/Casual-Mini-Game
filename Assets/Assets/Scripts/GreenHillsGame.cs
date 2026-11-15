using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static TrailInterface;

public sealed class GreenHillsGame : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Transform spawn;
    [SerializeField] private ThirdPersonCameraFollow followCamera;
    [SerializeField] private float fallHeight = -5f;

    public int Score { get; private set; }
    public int Lives { get; private set; } = 3;
    public bool IsPlaying => !paused && !recovering && !completed && !loading && Lives > 0;
    private readonly HashSet<Transform> activatedCheckpoints = new();
    private bool completed;
    private bool recovering;
    private bool loading;
    private bool paused;
    private bool playerWasEnabled;
    private float previousTimeScale;
    private string lossMessage;
    private float noticeUntil;
    private int level;
    private string levelLabel;
    private string scoreLabel = "Coins  00";
    private string lifeLabel = "Lives  3";
    private string resultLabel;
    private Color accent;
    private GUIStyle heading, body, small;

    private void Awake()
    {
        level = TrailCatalog.Number(gameObject.scene.name);
        levelLabel = $"{level:00}   {TrailCatalog.Name(level)}";
        accent = TrailCatalog.Accent(level);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (paused) Resume();
            else if (CanPause) Pause();
        }
        if (IsPlaying && player.transform.position.y < fallHeight) LoseLife("You fell off the trail");
    }

    private bool CanPause => !paused && !loading && !completed && Lives > 0;

    private void Pause()
    {
        paused = true;
        previousTimeScale = Time.timeScale;
        playerWasEnabled = player.enabled;
        player.enabled = false;
        Time.timeScale = 0;
    }

    private void Resume()
    {
        RestoreClock();
        player.enabled = playerWasEnabled;
    }

    private void RestoreClock()
    {
        if (!paused) return;
        paused = false;
        Time.timeScale = previousTimeScale;
    }

    private void OnDisable() => RestoreClock();

    public void ActivateCheckpoint(Transform marker)
    {
        if (!IsPlaying || marker == null || !activatedCheckpoints.Add(marker)) return;
        spawn = marker;
        noticeUntil = Time.time + 2f;
    }

    public void CompleteLevel()
    {
        if (!IsPlaying) return;
        completed = true;
        player.enabled = false;
        resultLabel = $"{Score} coins collected";
    }

    public bool TryCollect()
    {
        if (!IsPlaying) return false;
        Score++;
        scoreLabel = $"Coins  {Score:00}";
        return true;
    }

    public void LoseLife(string message)
    {
        if (!IsPlaying) return;
        lossMessage = message;
        Lives--;
        lifeLabel = $"Lives  {Lives}";
        resultLabel = $"{Score} coins collected";
        recovering = true;
        player.enabled = false;
        if (Lives > 0) StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(1.25f);
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        controller.enabled = true;
        player.ResetMotion();
        if (followCamera != null) followCamera.SnapToTarget();
        player.enabled = true;
        recovering = false;
    }

    private void OnGUI()
    {
        if (heading == null)
        {
            heading = Style(32);
            body = Style(20);
            small = Style(16);
        }

        Matrix4x4 previous = GUI.matrix;
        Vector2 viewport = Begin();
        DrawHud(viewport);

        if (paused)
            DrawPause(viewport);
        else if (completed || Lives == 0)
            DrawResult(viewport);
        else if (recovering)
            Toast(viewport, lossMessage);
        else if (Time.time < noticeUntil)
            Toast(viewport, "Checkpoint saved");

        GUI.matrix = previous;
    }

    private void DrawHud(Vector2 viewport)
    {
        var plate = new Rect(16, 12, 250, 54);
        SignFace(plate, levelLabel, body);
        Panel(new Rect(43, 51, 25, 3), accent, 1.5f);

        float right = viewport.x - 16;
        var stats = new Rect(right - 325, 17, 205, 44);
        DrawPaper(stats);
        Panel(new Rect(stats.x + 103, stats.y + 12, 2, 20), new Color(.63f, .45f, .26f), 1);
        Label(new Rect(stats.x + 5, stats.y, 98, stats.height), scoreLabel, small, Ink);
        Label(new Rect(stats.x + 106, stats.y, 92, stats.height), lifeLabel, small, Ink);

        var menu = new Rect(right - 112, 11, 112, 56);
        if (CanPause)
        {
            if (Sign(menu, "Menu", small)) Pause();
        }
        else SignFace(menu, "Menu", small);
    }

    private void Toast(Vector2 viewport, string caption)
    {
        var rect = new Rect(viewport.x * .5f - 115, 78, 230, 43);
        DrawPaper(rect);
        Label(rect, caption, small, Ink);
    }

    private void DrawPause(Vector2 viewport)
    {
        Panel(new Rect(Vector2.zero, viewport), Scrim, 0);
        Vector2 origin = viewport * .5f - new Vector2(215, 137);
        DrawPaper(At(origin, 0, 0, 430, 274));
        SignFace(At(origin, 84, 18, 262, 65), "Paused", heading, primary: true);
        Label(At(origin, 18, 88, 394, 35), levelLabel, small, Ink);
        if (Sign(At(origin, 85, 128, 260, 67), "Resume", body, primary: true)) Resume();
        if (Sign(At(origin, 114, 199, 202, 52), "Main menu", small, left: true)) Load("MainMenu");
    }

    private void DrawResult(Vector2 viewport)
    {
        Panel(new Rect(Vector2.zero, viewport), Scrim, 0);
        Vector2 origin = viewport * .5f - new Vector2(215, 108);
        DrawPaper(At(origin, 0, 0, 430, 216));
        Label(At(origin, 14, 21, 402, 51),
            Lives == 0 ? "Out of lives" : level == TrailCatalog.Count ? "Adventure complete" : "Trail cleared",
            heading, Ink);
        Label(At(origin, 15, 75, 400, 32), resultLabel, body, Ink);

        var primary = At(origin, 24, 130, 192, 63);
        if (Lives == 0 && Sign(primary, "Retry", body, primary: true)) Load(gameObject.scene.name);
        if (completed && level < TrailCatalog.Count && Sign(primary, "Next trail", body, primary: true))
            Load(TrailCatalog.Scene(level + 1));
        float menuX = completed && level == TrailCatalog.Count ? 123 : 228;
        if (Sign(At(origin, menuX, 130, 178, 63), "Main menu", small, left: true)) Load("MainMenu");
    }

    private void Load(string scene)
    {
        if (loading || !Application.CanStreamedLevelBeLoaded(scene)) return;
        loading = true;
        RestoreClock();
        player.enabled = false;
        SceneManager.LoadSceneAsync(scene);
    }
}
