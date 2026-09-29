using System;
using System.Collections.Generic;

namespace DemolitionBuster.Core.Progression
{
    /// <summary>Everything persisted between sessions. Plain fields so any serializer (JsonUtility included) can handle it.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int coins;
        public int bestScore;
        public List<string> ownedItemIds = new List<string>();
        public string selectedItemId;
    }

    public interface ISaveStorage
    {
        /// <summary>Returns the stored data, or null when nothing was saved yet.</summary>
        SaveData Load();

        void Save(SaveData data);
    }

    /// <summary>Non-persistent storage, used by tests and as a fallback.</summary>
    public sealed class InMemorySaveStorage : ISaveStorage
    {
        public SaveData Data { get; private set; }
        public int SaveCount { get; private set; }

        public InMemorySaveStorage(SaveData initial = null)
        {
            Data = initial;
        }

        public SaveData Load() => Data;

        public void Save(SaveData data)
        {
            Data = data;
            SaveCount++;
        }
    }
}
