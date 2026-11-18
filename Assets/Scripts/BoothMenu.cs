using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GravityGame
{
    [DisallowMultipleComponent]
    public sealed class BoothMenu : MonoBehaviour
    {
        public WorldProgress world;
        public MobileGameUI gameUI;
        [Header("Saved UI references")]
        public Button openButton;
        public GameObject panel;
        public GameObject inventoryPage;
        public GameObject settingsPage;
        public Button inventoryTab;
        public Button settingsTab;
        public Button saveButton;
        public Button closeButton;
        public Button volumeDown;
        public Button volumeUp;
        public Button lookDown;
        public Button lookUp;
        public Text stationHeading;
        public Text inventoryText;
        public Text statusText;
        public Text volumeText;
        public Text lookText;
        private PhoneBooth activeBooth;
        public bool IsOpen => activeBooth != null;

        private void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            saveButton.onClick.AddListener(Save);
            inventoryTab.onClick.AddListener(() => ShowPage(false));
            settingsTab.onClick.AddListener(() => ShowPage(true));
            volumeDown.onClick.AddListener(() => ChangeVolume(-0.1f));
            volumeUp.onClick.AddListener(() => ChangeVolume(0.1f));
            lookDown.onClick.AddListener(() => ChangeLook(-0.25f));
            lookUp.onClick.AddListener(() => ChangeLook(0.25f));
            panel.SetActive(false);
        }

        private void Update()
        {
            bool available = !IsOpen && world.FindNearbyBooth() != null;
            openButton.gameObject.SetActive(available);
            if (available && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            PhoneBooth nearby = world.FindNearbyBooth();
            if (nearby == null) return;
            activeBooth = nearby;
            stationHeading.text = nearby.stationName;
            statusText.text = world.LoadNotice ?? "";
            RefreshInventory();
            RefreshSettings();
            ShowPage(false);
            openButton.gameObject.SetActive(false);
            panel.SetActive(true);
            gameUI.PauseGame();
        }

        public void Close()
        {
            if (!IsOpen) return;
            activeBooth = null;
            panel.SetActive(false);
            gameUI.ResumeGame();
        }

        public void Save()
        {
            if (!IsOpen) return;
            world.TrySave(activeBooth, out string message);
            statusText.text = message;
        }

        private void ShowPage(bool settings)
        {
            inventoryPage.SetActive(!settings);
            settingsPage.SetActive(settings);
            inventoryTab.interactable = settings;
            settingsTab.interactable = !settings;
        }

        private void RefreshInventory()
        {
            var objective = world.Level.objective;
            var text = new StringBuilder().Append("Star fragments   ").Append(objective.CollectedCount)
                .Append(" / ").Append(objective.TotalCount);
            foreach (var planet in world.Level.player.gravitySources)
            {
                int owned = 0;
                int total = 0;
                foreach (var star in objective.stars)
                {
                    if (star.planet != planet) continue;
                    total++;
                    if (star.Collected) owned++;
                }
                if (total > 0) text.Append("\n").Append(planet.name).Append("   ").Append(owned).Append(" / ").Append(total);
            }
            inventoryText.text = text.ToString();
        }

        private void ChangeVolume(float amount)
        {
            world.SetVolume(Mathf.Round((world.Volume + amount) * 10f) / 10f);
            SettingsChanged();
        }

        private void ChangeLook(float amount)
        {
            world.SetLookMultiplier(world.LookMultiplier + amount);
            SettingsChanged();
        }

        private void SettingsChanged()
        {
            RefreshSettings();
            statusText.text = "Settings applied. Select Save to keep them.";
        }

        private void RefreshSettings()
        {
            volumeText.text = "Volume   " + Mathf.RoundToInt(world.Volume * 100f) + "%";
            lookText.text = "Look sensitivity   " + Mathf.RoundToInt(world.LookMultiplier * 100f) + "%";
            volumeDown.interactable = world.Volume > 0f;
            volumeUp.interactable = world.Volume < 1f;
            lookDown.interactable = world.LookMultiplier > 0.5f;
            lookUp.interactable = world.LookMultiplier < 2f;
        }
    }
}
