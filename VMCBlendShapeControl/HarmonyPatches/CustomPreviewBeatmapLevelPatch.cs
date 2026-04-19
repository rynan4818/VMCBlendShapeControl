using System.IO;
using HarmonyLib;

namespace VMCBlendShapeControl.HarmonyPatches
{
    [HarmonyPatch(typeof(CustomPreviewBeatmapLevel), nameof(CustomPreviewBeatmapLevel.GetCoverImageAsync))]
    internal class CustomPreviewBeatmapLevelPatch
    {
        public const string SongScriptFileName = "SongVMCBlendShape.json";
        public const string NalulunaAvatarsEventsFileName = "NalulunaAvatarsEvents.json";
        public static string CustomLevelScriptPath = string.Empty;

        static void Postfix(CustomPreviewBeatmapLevel __instance)
        {
            var songScriptPath = Path.Combine(__instance.customLevelPath, SongScriptFileName);
            if (File.Exists(songScriptPath))
            {
                CustomLevelScriptPath = songScriptPath;
                return;
            }

            var nalulunaScriptPath = Path.Combine(__instance.customLevelPath, NalulunaAvatarsEventsFileName);
            CustomLevelScriptPath = File.Exists(nalulunaScriptPath) ? nalulunaScriptPath : string.Empty;
        }
    }
}
