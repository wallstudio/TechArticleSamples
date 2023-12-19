using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, new[] { "__png__" }, new [] { "png" })]
class NegaPosiPngImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var png = new Texture2D(2, 2);
        png.LoadImage(System.IO.File.ReadAllBytes(assetPath));
        png.Apply();
        ctx.AddObjectToAsset("main obj", png);
        ctx.SetMainObject(png);

        var nega = new Texture2D(png.width, png.height){ name = $"nega {png.name}"};
        nega.SetPixels(png.GetPixels().Select(c => new Color(1 - c.r, 1 - c.g, 1 - c.b, c.a)).ToArray());
        nega.Apply();
        ctx.AddObjectToAsset("nega obj", nega);
    }
}

class Processor : AssetPostprocessor
{
    void OnPreprocessAsset()
    {
        if(assetPath.EndsWith(".neg.png"))
        {
            var overrideImporter = AssetDatabase.GetImporterOverride(assetPath);
            if (overrideImporter == null)
            {
                AssetDatabase.SetImporterOverride<NegaPosiPngImporter>(assetPath);
            }
        }
    }
}