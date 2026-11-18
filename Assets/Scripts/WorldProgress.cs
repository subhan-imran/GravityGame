using System;
using System.Collections.Generic;
using UnityEngine;

namespace GravityGame
{
    // LevelAuthoring wires scene references first. Restore before the player records its starting checkpoint.
    [DefaultExecutionOrder(-900)]
    [RequireComponent(typeof(LevelAuthoring))]
    public sealed class WorldProgress : MonoBehaviour
    {
        private const int SaveVersion = 1;
        [Tooltip("Stable save slot for this world. Use a different ID for worlds that have separate progress.")]
        public string worldId = "open-world";
        private LevelAuthoring level;
        public LevelAuthoring Level => level != null ? level : (level = GetComponent<LevelAuthoring>());
        public PhoneBooth[] Booths { get; private set; } = Array.Empty<PhoneBooth>();
        public float Volume { get; private set; } = 1f;
        public float LookMultiplier { get; private set; } = 1f;
        public string LoadNotice { get; private set; }
        public string SaveKey => "GravityGame.World." + worldId;
        private float defaultSensitivity;
        private float previousVolume;
        private bool initialized;

        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public string boothId;
            public string[] collected;
            public float volume;
            public float lookMultiplier;
        }

        private void Awake() => Initialize();

        internal void Initialize()
        {
            if (initialized) return;
            initialized = true;
            Booths = GetComponentsInChildren<PhoneBooth>();
            defaultSensitivity = Level.cameraRig.sensitivity;
            previousVolume = AudioListener.volume;
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            if (!HasValidIds())
            {
                LoadNotice = "This world has missing or duplicate IDs. Save data was left intact.";
                return;
            }
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            }
            catch (ArgumentException)
            {
                data = null;
            }
            if (data == null || data.version != SaveVersion || data.collected == null)
            {
                LoadNotice = "Saved data could not be read. Exploring from the starting point.";
                return;
            }

            Level.objective.RestoreProgress(new HashSet<string>(data.collected, StringComparer.Ordinal));
            SetVolume(data.volume);
            SetLookMultiplier(data.lookMultiplier);
            var booth = Array.Find(Booths, item => item.persistentId == data.boothId && item.planet != null &&
                item.arrival != null && item.isActiveAndEnabled);
            if (booth == null)
            {
                LoadNotice = "Saved booth is unavailable. Inventory restored at the starting point.";
                return;
            }
            booth.GetArrivalPose(out Vector3 position, out Quaternion rotation);
            Level.player.planet = booth.planet;
            Level.player.transform.SetPositionAndRotation(position, rotation);
            var body = Level.player.GetComponent<Rigidbody>();
            body.position = position;
            body.rotation = rotation;
            LoadNotice = "Resumed at " + booth.stationName + ".";
        }

        public PhoneBooth FindNearbyBooth()
        {
            var player = Level.player;
            if (player == null || player.IsPaused) return null;
            PhoneBooth nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var booth in Booths)
            {
                if (booth == null || !booth.IsAvailableTo(player)) continue;
                float candidate = (booth.transform.position - player.transform.position).sqrMagnitude;
                if (candidate >= distance) continue;
                nearest = booth;
                distance = candidate;
            }
            return nearest;
        }

        public bool TrySave(PhoneBooth booth, out string message)
        {
            if (booth == null || Array.IndexOf(Booths, booth) < 0 ||
                !booth.IsAvailableTo(Level.player) || !Level.player.IsPaused)
            {
                message = "Use a nearby booth to save.";
                return false;
            }
            if (!HasValidIds())
            {
                message = "Save unavailable. A booth or fragment has a missing or duplicate ID.";
                return false;
            }
            var collected = new List<string>();
            foreach (var star in Level.objective.stars)
                if (star.Collected) collected.Add(star.persistentId);
            var data = new SaveData
            {
                version = SaveVersion,
                boothId = booth.persistentId,
                collected = collected.ToArray(),
                volume = Volume,
                lookMultiplier = LookMultiplier
            };
            try
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException error)
            {
                Debug.LogWarning("Could not save world progress: " + error.Message, this);
                message = "Save failed. Try again.";
                return false;
            }
            message = "Saved at " + booth.stationName + ".";
            return true;
        }

        public bool HasValidIds()
        {
            var boothIds = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(worldId) || Booths.Length == 0) return false;
            foreach (var booth in Booths)
                if (booth == null || booth.planet == null || booth.arrival == null ||
                    string.IsNullOrWhiteSpace(booth.persistentId) || !boothIds.Add(booth.persistentId)) return false;
            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var star in Level.objective.stars)
                if (star == null || string.IsNullOrWhiteSpace(star.persistentId) || !itemIds.Add(star.persistentId)) return false;
            return true;
        }

        public void SetVolume(float value)
        {
            Volume = float.IsNaN(value) ? 1f : Mathf.Clamp01(value);
            AudioListener.volume = Volume;
        }

        public void SetLookMultiplier(float value)
        {
            LookMultiplier = float.IsNaN(value) ? 1f : Mathf.Clamp(value, 0.5f, 2f);
            Level.cameraRig.sensitivity = defaultSensitivity * LookMultiplier;
        }

        private void OnDestroy()
        {
            if (initialized) AudioListener.volume = previousVolume;
        }
    }
}
