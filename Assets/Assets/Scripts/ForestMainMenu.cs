using UnityEngine;
using UnityEngine.SceneManagement;
public sealed class ForestMainMenu : MonoBehaviour
{
    private bool levelSelect;
    private bool loading;
    private GUIStyle title, subtitle, heading, text, button;
    private Texture2D gold;
    private void OnGUI()
    {
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 54, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            title.normal.textColor = new Color(1f,.96f,.80f);
            subtitle = new GUIStyle(title) { fontSize = 18, fontStyle = FontStyle.Normal };
            heading = new GUIStyle(title) { fontSize = 27 };
            text = new GUIStyle(subtitle) { wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            gold = new Texture2D(1,1);
            gold.SetPixel(0,0,new Color(1,.79f,.35f));gold.Apply();
            button.normal.background=gold;button.hover.background=gold;button.active.background=gold;
            button.normal.textColor=new Color(.11f,.25f,.17f);
            button.hover.textColor=button.normal.textColor;button.active.textColor=button.normal.textColor;
        }
        float scale=Mathf.Min(Screen.width/960f,Screen.height/540f);
        var previous=GUI.matrix;
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
        float width=Screen.width/scale, height=Screen.height/scale;
        GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.025f,.10f,.08f,.48f),0,0);
        float left=width*.5f-420;
        GUI.Label(new Rect(left,35,820,42),"A LITTLE EXPLORER. A BIG ADVENTURE.",subtitle);
        GUI.Label(new Rect(left,75,850,76),"Tiny Adventure",title);
        GUI.Label(new Rect(left,150,700,40),"Follow the trail. Find the magic.",subtitle);
        GUI.enabled=!loading;
        if (!levelSelect)
        {
            Panel(new Rect(left,224,375,230));
            GUI.Label(new Rect(left+25,241,330,40),"The forest is calling",heading);
            GUI.Label(new Rect(left+25,288,325,62),"Collect golden coins, dodge playful slimes and discover two magical forest trails.",text);
            if(GUI.Button(new Rect(left+25,356,325,47),"Start adventure",button))Load("Level01_GreenHills");
            if(GUI.Button(new Rect(left+25,412,325,30),"Choose a trail",button))levelSelect=true;
        }
        else
        {
            Card(new Rect(left,230,395,220),"01  GREEN HILLS","A gentle beginning","Sunny meadows, wooden bridges and your first forest adventure.","Level01_GreenHills");
            Card(new Rect(left+420,230,395,220),"02  SUNSET HOLLOW","A winding woodland trail","Warm golden light, woodland stepping stones and a hidden wishing gate.","Level02");
            if(GUI.Button(new Rect(left,468,130,35),"Back",button))levelSelect=false;
        }
        GUI.Label(new Rect(width-355,height-36,340,30),"WASD / arrows   ·   Space to jump   ·   Shift to run",new GUIStyle(subtitle){fontSize=14});
        GUI.enabled=true;GUI.matrix=previous;
    }
    private void Card(Rect area,string name,string difficulty,string description,string scene)
    {
        Panel(area);
        GUI.Label(new Rect(area.x+22,area.y+15,350,40),name,heading);
        GUI.Label(new Rect(area.x+22,area.y+57,350,28),difficulty,subtitle);
        GUI.Label(new Rect(area.x+22,area.y+94,350,55),description,text);
        if(GUI.Button(new Rect(area.x+22,area.y+163,350,40),"Explore",button))Load(scene);
    }
    private static void Panel(Rect rect)
    {
        GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.06f,.22f,.16f,.94f),0,18);
    }
    private void Load(string scene)
    {
        loading=true;SceneManager.LoadScene(scene);
    }
    private void OnDestroy() { if(gold!=null)Destroy(gold); }
}
