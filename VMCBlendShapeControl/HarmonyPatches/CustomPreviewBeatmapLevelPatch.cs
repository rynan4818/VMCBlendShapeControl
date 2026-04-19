using System.IO;
using HarmonyLib;

namespace VMCBlendShapeControl.HarmonyPatches
{
    [HarmonyPatch(typeof(CustomPreviewBeatmapLevel), nameof(CustomPreviewBeatmapLevel.GetCoverImageAsync))]
    internal class CustomPreviewBeatmapLevelPatch
    {
        public const string SongScriptFileName = "SongVMCBlendShape.json";
        public static string CustomLevelScriptPath = string.Empty;

        static void Postfix(CustomPreviewBeatmapLevel __instance)
        {
            var scriptPath = Path.Combine(__instance.customLevelPath, SongScriptFileName);
            CustomLevelScriptPath = File.Exists(scriptPath) ? scriptPath : string.Empty;
        }
    }
}
