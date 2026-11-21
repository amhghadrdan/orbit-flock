using System;
using System.IO;
using UnityEngine;

namespace OrbitFlock
{
    internal static class CheckpointStore
    {
        internal static string DataDirectory { get; set; }
        internal static string FilePath => Path.Combine(DataDirectory ?? Application.persistentDataPath, "savegame.json");

        internal static FlightCheckpoint Read()
        {
            try
            {
                return File.Exists(FilePath) ? JsonUtility.FromJson<FlightCheckpoint>(File.ReadAllText(FilePath)) : null;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Debug.LogWarning($"Unable to load the flight checkpoint. {error.Message}");
                return null;
            }
        }

        internal static void Write(FlightCheckpoint checkpoint)
        {
            string path = FilePath;
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(checkpoint, true));
                Replace(temporaryPath, path);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Unable to write the flight checkpoint. {error.Message}");
            }
            finally
            {
                Delete(temporaryPath);
            }
        }

        internal static void Clear() => Delete(FilePath);

        private static void Delete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Unable to delete the flight checkpoint file. {error.Message}");
            }
        }

        private static void Replace(string temporaryPath, string path)
        {
            if (!File.Exists(path))
            {
                try
                {
                    File.Move(temporaryPath, path);
                    return;
                }
                catch (IOException) when (File.Exists(path))
                {
                    // Another player instance published its first save before this move.
                }
            }
            File.Replace(temporaryPath, path, null);
        }
    }
}
