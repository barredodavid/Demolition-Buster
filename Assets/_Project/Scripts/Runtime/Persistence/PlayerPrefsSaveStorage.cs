using System;
using DemolitionBuster.Core.Progression;
using UnityEngine;

namespace DemolitionBuster.Persistence
{
    /// <summary>Stores <see cref="SaveData"/> as JSON in PlayerPrefs. Small, synchronous and good enough for a hyper-casual save.</summary>
    public sealed class PlayerPrefsSaveStorage : ISaveStorage
    {
        private readonly string _key;

        public PlayerPrefsSaveStorage(string key = "DemolitionBuster.Save")
        {
            _key = key;
        }

        public SaveData Load()
        {
            if (!PlayerPrefs.HasKey(_key)) return null;

            try
            {
                return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(_key));
            }
            catch (ArgumentException exception)
            {
                // A corrupted save must never block the game: start fresh and report it.
                Debug.LogWarning($"Discarding unreadable save data: {exception.Message}");
                return null;
            }
        }

        public void Save(SaveData data)
        {
            PlayerPrefs.SetString(_key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
