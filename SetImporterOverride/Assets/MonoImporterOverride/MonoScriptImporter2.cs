using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditorInternal;
using UnityEngine;

[ScriptedImporter(1, new[] { "__cs__" }, new [] { "cs" })]
public class MonoScriptImporter2 : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var cs = MonoScripts.CreateMonoScript(File.ReadAllText(assetPath), "", "", "", false);
        ctx.AddObjectToAsset("main obj", cs);
        ctx.SetMainObject(cs);

        ctx.AddObjectToAsset("txt obj", new TextAsset(File.ReadAllText(assetPath)));
    }
}

class Processor2 : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
        foreach(var assetPath in importedAssets)
        {
            if(assetPath.EndsWith(".x.cs"))
            {
                var overrideImporter = AssetDatabase.GetImporterOverride(assetPath);
                if (overrideImporter == null)
                {
                    AssetDatabase.SetImporterOverride<NegaPosiPngImporter>(assetPath);
                    overrideImporter = AssetDatabase.GetImporterOverride(assetPath);
                }
            }
        }
    }
    
    // void OnPreprocessAsset()
    // {
    //     if(assetPath.EndsWith(".x.cs"))
    //     {
    //         var overrideImporter = AssetDatabase.GetImporterOverride(assetPath);
    //         if (overrideImporter == null)
    //         {
    //             AssetDatabase.SetImporterOverride<NegaPosiPngImporter>(assetPath);
    //         }
    //     }
    // }
}