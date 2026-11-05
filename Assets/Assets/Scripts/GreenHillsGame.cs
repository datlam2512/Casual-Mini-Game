using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GreenHillsGame : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Transform spawn;
    [SerializeField] private ThirdPersonCameraFollow followCamera;
    [SerializeField] private float fallHeight = -5f;

    public int Score { get; private set; }
    public int Lives { get; private set; } = 3;
    public bool IsPlaying => !recovering && !completed && Lives > 0;
    private bool completed;
    private bool checkpointActive;
    private string notice;
    private bool IsSecondLevel => SceneManager.GetActiveScene().name == "Level02";
    private float noticeUntil;

    private bool recovering;
    private bool restarting;
    private string lossMessage;
    private GUIStyle heading, body, button;

    private void Update()
    {
        if (IsPlaying && player.transform.position.y < fallHeight)
        {
            LoseLife("You fell off the trail");
        }
    }

    public void ActivateCheckpoint(Transform marker)
    {
        if (!IsPlaying || checkpointActive) return;
        spawn = marker;
        checkpointActive = true;
        notice = "Checkpoint saved";
        noticeUntil = Time.unscaledTime + 3f;
    }

    public void CompleteLevel()
    {
        if (!IsPlaying) return;
        completed = true;
        player.enabled = false;
    }

    public bool TryCollect()
    {
        if (!IsPlaying) return false;
        Score++;
        return true;
    }

    public void LoseLife(string message)
    {
        if (!IsPlaying) return;
        lossMessage = message;
        Lives--;
        recovering = true;
        player.enabled = false;
        if (Lives > 0) StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSecondsRealtime(1.25f);
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
            heading = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            heading.normal.textColor = new Color(.15f, .29f, .20f);
            body = new GUIStyle(heading) { fontSize = 20, fontStyle = FontStyle.Normal };
            button = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            button.normal.textColor = new Color(.18f, .29f, .12f);
            button.hover.textColor = button.normal.textColor;
            button.active.textColor = button.normal.textColor;
            var gold = new Texture2D(1, 1);
            gold.SetPixel(0, 0, new Color(1f, .77f, .32f));
            gold.Apply();
            button.normal.background = gold;
            button.hover.background = gold;
            button.active.background = gold;
        }

        var previousMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        float height = Screen.height / scale;
        var hud = new Rect(width * .5f - 154f, 16f, 308f, 46f);
        DrawPanel(hud, new Color(1f, .96f, .82f, .97f));
        GUI.Label(hud, $"COINS  {Score:00}     LIVES  {Lives} / 3", body);
        DrawPanel(new Rect(18, 18, 228, 40), new Color(1f, .96f, .82f, .94f));
        GUI.Label(new Rect(18, 18, 228, 40), IsSecondLevel ? "SUNSET HOLLOW  /  02" : "GREEN HILLS  /  01", body);
        if (Time.unscaledTime < noticeUntil)
            GUI.Label(new Rect(width / 2 - 200, 72, 400, 40), notice, body);
        if (completed)
        {
            DrawPanel(new Rect(0, 0, width, height), new Color(.04f,.12f,.12f,.7f));
            var panel = new Rect(width / 2 - 220, height / 2 - 120, 440, 240);
            DrawPanel(panel, new Color(1f,.96f,.84f,.99f));
            GUI.Label(new Rect(panel.x, panel.y + 20, 440, 48), "Adventure complete!", heading);
            GUI.Label(new Rect(panel.x, panel.y + 76, 440, 40), $"You collected {Score} coins", body);
            bool available = !IsSecondLevel && Application.CanStreamedLevelBeLoaded("Level02");
            GUI.enabled = available;
            if (GUI.Button(new Rect(panel.x + 90, panel.y + 130, 260, 50), IsSecondLevel ? "Adventure completed" : "Continue to Map 2", button))
                SceneManager.LoadScene("Level02");
            GUI.enabled = true;
            if (GUI.Button(new Rect(panel.x + 120, panel.y + 190, 200, 36), "Level selection", button)) SceneManager.LoadScene("MainMenu");
            GUI.matrix = previousMatrix;
            return;
        }

        if (IsPlaying && GUI.Button(new Rect(width - 105, 18, 88, 40), "Menu", button)) SceneManager.LoadScene("MainMenu");
        if (!IsPlaying)
        {
            DrawPanel(new Rect(0, 0, width, height), new Color(.05f, .13f, .10f, .62f));
            var card = new Rect(width * .5f - 205f, height * .5f - 104f, 410f, 208f);
            DrawPanel(card, new Color(1f, .96f, .84f, .99f));
            GUI.Label(new Rect(card.x, card.y + 19, card.width, 45), Lives == 0 ? "Game Over" : lossMessage, heading);
            GUI.Label(new Rect(card.x, card.y + 67, card.width, 40),
                Lives == 0 ? $"Coins collected: {Score}" : $"Lives left: {Lives}", body);
            if (Lives == 0)
            {
                if (GUI.Button(new Rect(card.center.x - 85, card.y + 123, 170, 49), "Restart", button))
                {
                    Restart();
                }
            }
            else
            {
                GUI.Label(new Rect(card.x, card.y + 124, card.width, 42), checkpointActive ? "Returning to your checkpoint…" : "Returning to the start…", body);
            }
        }
        GUI.matrix = previousMatrix;
    }

    private void Restart()
    {
        if (Lives != 0 || restarting) return;
        restarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static void DrawPanel(Rect rect, Color color)
    {
        var previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 5, rect.width, rect.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0,0,0,.22f), 0, 16);
        GUI.color = Color.white;
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, 16);
        GUI.DrawTexture(new Rect(rect.x + 14, rect.y + 1, rect.width - 28, 3), Texture2D.whiteTexture,
            ScaleMode.StretchToFill, true, 0, new Color(1f, .78f, .35f, .9f), 0, 2);
        GUI.color = previous;
    }
}
