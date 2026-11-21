using UnityEditor;

namespace OrbitFlock.Editor
{
    public static class WindowsBuild
    {
        [MenuItem("Orbit Flock/Build Windows x64")]
        public static void Build()
        {
            StandaloneBuild.BuildPlayer(BuildTarget.StandaloneWindows64, "Build/OrbitFlock-Windows", "OrbitFlock.exe");
        }
    }
}
