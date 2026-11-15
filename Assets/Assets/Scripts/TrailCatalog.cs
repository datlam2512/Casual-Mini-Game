using UnityEngine;

public static class TrailCatalog
{
    private static readonly string[] Names =
    {
        "Green Hills", "Sunset Hollow", "Fern Terrace", "Amber Switchback",
        "Jade Crossing", "Cloudstep Ridge", "Coral Ruins", "Frostline Basin",
        "Twilight Spires", "Obsidian Run", "Astral Causeway", "Crown Summit"
    };

    private static readonly Color32[] Accents =
    {
        new(161, 225, 150, 255), new(255, 191, 124, 255), new(135, 221, 174, 255),
        new(246, 183, 101, 255), new(93, 220, 190, 255), new(148, 207, 255, 255),
        new(249, 154, 141, 255), new(156, 228, 239, 255), new(185, 159, 255, 255),
        new(255, 135, 112, 255), new(155, 162, 255, 255), new(255, 217, 137, 255)
    };

    public static int Count => Names.Length;
    public static string Name(int number) => Names[number - 1];
    public static Color Accent(int number) => Accents[number - 1];
    public static string Scene(int number) => number == 1 ? "Level01_GreenHills" : $"Level{number:00}";

    public static int Number(string scene)
    {
        if (scene == "Level01_GreenHills") return 1;
        return scene.StartsWith("Level", System.StringComparison.Ordinal) &&
            int.TryParse(scene.Substring(5), out int number) && number >= 2 && number <= Count
            ? number : 1;
    }
}
