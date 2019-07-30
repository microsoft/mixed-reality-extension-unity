using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
class HttpClientSupport : AssetPostprocessor
{
    private static void OnGeneratedCSProjectFiles()
    {
        Debug.Log("OnGeneratedCSProjectFiles");
        var dir = Directory.GetCurrentDirectory();
        var files = Directory.GetFiles(dir, "*.csproj");
        foreach (var file in files)
        {
            AddHttpClientSupport(file);
        }
    }

    static bool AddHttpClientSupport(string file)
    {
        var text = File.ReadAllText(file);
        var find = "<Reference Include=\"System\" />";
        var replace = "<Reference Include=\"System\" /> <Reference Include=\"System.Net.Http\" />";
        if (text.IndexOf(find) != -1)
        {
            text = Regex.Replace(text, find, replace);
            File.WriteAllText(file, text);
            return true;
        }
        else
        {
            return false;
        }
    }
}
