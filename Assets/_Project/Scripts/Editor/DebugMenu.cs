using DemolitionBuster.Config;
using DemolitionBuster.Core.Progression;
using DemolitionBuster.Gameplay;
using DemolitionBuster.Persistence;
using UnityEditor;
using UnityEngine;

namespace DemolitionBuster.Editor
{
    /// <summary>
    /// Test helpers under "Demolition Buster > Debug". In Play mode they go through the running
    /// <see cref="GameController"/> (so the UI updates and the in-memory progress is not overwritten);
    /// outside Play mode they edit the save directly.
    /// </summary>
    public static class DebugMenu
    {
        private const string Menu = "Demolition Buster/Debug/";
        private const string CatalogPath = "Assets/_Project/Data/DestroyerCatalog.asset";
        private const int CoinGift = 5000;

        [MenuItem(Menu + "Débloquer tous les personnages", priority = 100)]
        private static void UnlockAllDestroyers()
        {
            if (TryGetRunningGame(out GameController game))
            {
                game.DebugUnlockAllDestroyers();
            }
            else
            {
                var progress = new PlayerProgress(new PlayerPrefsSaveStorage());
                foreach (DestroyerDefinition destroyer in LoadCatalog().Destroyers) progress.TryBuy(destroyer.Id, 0);
            }
            Debug.Log("[Demolition Buster] Tous les personnages sont débloqués : choisis-les dans la boutique.");
        }

        [MenuItem(Menu + "Ajouter 5000 pièces", priority = 101)]
        private static void AddCoins()
        {
            if (TryGetRunningGame(out GameController game)) game.DebugAddCoins(CoinGift);
            else new PlayerProgress(new PlayerPrefsSaveStorage()).AddCoins(CoinGift);
            Debug.Log($"[Demolition Buster] +{CoinGift} pièces.");
        }

        [MenuItem(Menu + "Réinitialiser la sauvegarde", priority = 120)]
        private static void ResetSave()
        {
            if (!EditorUtility.DisplayDialog("Réinitialiser la sauvegarde",
                    "Pièces, record et personnages achetés seront effacés.", "Effacer", "Annuler")) return;
            PlayerPrefs.DeleteKey("DemolitionBuster.Save");
            PlayerPrefs.Save();
            Debug.Log("[Demolition Buster] Sauvegarde effacée.");
        }

        // The running game keeps its progress in memory and would overwrite a reset save on its next commit.
        [MenuItem(Menu + "Réinitialiser la sauvegarde", validate = true)]
        private static bool CanResetSave() => !EditorApplication.isPlaying;

        private static bool TryGetRunningGame(out GameController game)
        {
            game = EditorApplication.isPlaying ? Object.FindAnyObjectByType<GameController>() : null;
            return game != null;
        }

        private static DestroyerCatalog LoadCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<DestroyerCatalog>(CatalogPath)
                ?? throw new System.IO.FileNotFoundException("Destroyer catalog missing: run Build Project Content first.", CatalogPath);
        }
    }
}
