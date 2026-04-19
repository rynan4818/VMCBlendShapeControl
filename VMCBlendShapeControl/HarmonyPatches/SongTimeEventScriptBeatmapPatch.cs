//すのー(Snow1226)さんのCameraPlusの HarmonyPatches/SongScriptBeatmapPatch.cs をコピーさせていただきました。
//https://github.com/Snow1226/CameraPlus/blob/master/CameraPlus/HarmonyPatches/SongScriptBeatmapPatch.cs
//CameraPlusライセンス:MIT License (https://github.com/Snow1226/CameraPlus/blob/master/LICENSE)

using System.IO;
using HarmonyLib;

namespace VMCBlendShapeControl.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelSelectionNavigationController), nameof(LevelSelectionNavigationController.HandleLevelCollectionNavigationControllerDidChangeLevelDetailContent))]
    internal class SongTimeEventScriptBeatmapPatch
    {
        private static string _latestSelectedLevelPath = string.Empty;
        public const string SongScriptFileName = "SongVMCBlendShape.json";
        public const string NalulunaAvatarsEventsFileName = "NalulunaAvatarsEvents.json";
        public static string CustomLevelScriptPath = string.Empty;

        static void Postfix(LevelSelectionNavigationController __instance)
        {
            if (CustomLevelLoaderPatch.Instance == null || CustomLevelLoaderPatch.Instance._loadedBeatmapSaveData == null || __instance.beatmapLevel == null)
            {
                CustomLevelScriptPath = string.Empty;
                return;
            }

            if (CustomLevelLoaderPatch.Instance._loadedBeatmapSaveData.ContainsKey(__instance.beatmapLevel.levelID))
            {
                string currentLevelPath = CustomLevelLoaderPatch.Instance._loadedBeatmapSaveData[__instance.beatmapLevel.levelID].customLevelFolderInfo.folderPath;
                if (currentLevelPath != _latestSelectedLevelPath)
                {
                    _latestSelectedLevelPath = currentLevelPath;
#if DEBUG
                    Plugin.Log.Notice($"Selected CustomLevel Path :\n {currentLevelPath}");
#endif

                    var songScriptPath = Path.Combine(currentLevelPath, SongScriptFileName);
                    if (File.Exists(songScriptPath))
                    {
                        CustomLevelScriptPath = songScriptPath;
                        Plugin.Log.Notice($"Found SongScript path : \n{songScriptPath}");
                        return;
                    }

                    var nalulunaScriptPath = Path.Combine(currentLevelPath, NalulunaAvatarsEventsFileName);
                    CustomLevelScriptPath = File.Exists(nalulunaScriptPath) ? nalulunaScriptPath : string.Empty;
                }
            }
            else
            {
                CustomLevelScriptPath = string.Empty;
            }
        }
    }
}