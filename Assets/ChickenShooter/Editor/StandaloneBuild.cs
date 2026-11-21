using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace OrbitFlock.Editor
{
    public static class StandaloneBuild
    {
        [MenuItem("Orbit Flock/Build Linux x64")]
        public static void BuildLinux()
        {
            BuildPlayer(BuildTarget.StandaloneLinux64, "Build/OrbitFlock-Linux-x64", "OrbitFlock.x86_64");
        }

        internal static void BuildPlayer(BuildTarget target, string defaultOutput, string executableName)
        {
            string output = Environment.GetEnvironmentVariable("ORBIT_FLOCK_BUILD");
            if (string.IsNullOrWhiteSpace(output))
                output = defaultOutput;

            Directory.CreateDirectory(output);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = Path.Combine(output, executableName),
                target = target,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"{target} build {report.summary.result} ({report.summary.totalErrors} errors).");
        }
    }
}
