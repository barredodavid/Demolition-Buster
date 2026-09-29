using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DemolitionBuster.Editor
{
    /// <summary>
    /// Build WebGL en ligne de commande (pour le portfolio) :
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod DemolitionBuster.Editor.BuildScript.BuildWebGL
    /// </summary>
    public static class BuildScript
    {
        private const string OutputDir = "Builds/WebGL";

        public static void BuildWebGL()
        {
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            Debug.Log($"Build result: {report.summary.result}, errors: {report.summary.totalErrors}");
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
                return;
            }

            // Le template par défaut fige le canvas en 960x600 sur desktop : on le fait remplir son cadre (iframe portrait).
            string indexPath = Path.Combine(OutputDir, "index.html");
            File.WriteAllText(indexPath, File.ReadAllText(indexPath)
                .Replace("canvas.style.width = \"960px\";", "canvas.style.width = \"100%\";")
                .Replace("canvas.style.height = \"600px\";", "canvas.style.height = \"100%\";"));
            string cssPath = Path.Combine(OutputDir, "TemplateData/style.css");
            File.WriteAllText(cssPath, File.ReadAllText(cssPath)
                .Replace("#unity-container.unity-desktop { left: 50%; top: 50%; transform: translate(-50%, -50%) }",
                         "#unity-container.unity-desktop { position: fixed; width: 100%; height: 100% }")
                .Replace("#unity-canvas { background: #231F20 }",
                         "#unity-canvas { background: #231F20; width: 100%; height: 100% }"));
        }
    }
}
