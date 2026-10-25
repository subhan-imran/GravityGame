using System;
using UnityEngine;

namespace GravityGame.Editor
{
    public static class OxygenChecks
    {
        public static void Validate(SphericalPlayer player, MobileGameUI ui, Action<int> step)
        {
            float capacity = player.oxygenCapacity;
            var body = player.GetComponent<Rigidbody>();
            try
            {
                player.RestartRun();
                step(40);
                Require(Mathf.Approximately(player.OxygenFraction, 1f) && Mathf.Approximately(ui.OxygenBarFraction, 1f) &&
                    ui.OxygenStatusText == "OXYGEN FULL", "Full tank and HUD in gravity");
                Require(!ui.OxygenFill.raycastTarget && !ui.OxygenFill.transform.parent.GetComponent<UnityEngine.UI.Image>().raycastTarget,
                    "The oxygen bar does not intercept touches");
                GoToSpace();
                step(1);
                Require(!player.HasLost && !player.IsInGravityField && player.OxygenRemaining > 0f,
                    "Distance cannot kill a player with oxygen left");
                float before = player.OxygenRemaining;
                int drainSteps = Mathf.Max(1, Mathf.FloorToInt(capacity * 0.2f / Time.fixedDeltaTime));
                step(drainSteps);
                Require(Mathf.Abs(before - player.OxygenRemaining - drainSteps * Time.fixedDeltaTime) < 0.001f &&
                    Mathf.Approximately(ui.OxygenBarFraction, player.OxygenFraction), "Physics-time drain is reflected by bar width");
                ui.PauseGame();
                before = player.OxygenRemaining;
                step(100);
                Require(Mathf.Approximately(before, player.OxygenRemaining), "Pause freezes oxygen use");
                ui.ResumeGame();
                var home = player.gravitySources[0];
                body.position = home.transform.position + Vector3.up * (home.Radius + 1.05f);
                body.rotation = Quaternion.identity;
                body.linearVelocity = Vector3.zero;
                player.planet = home;
                Physics.SyncTransforms();
                step(1);
                Require(Mathf.Approximately(player.OxygenFraction, 1f) && ui.OxygenStatusText == "OXYGEN FULL",
                    "Entering gravity immediately refills the tank");
                Require(player.stars.TryCollect(player.stars.stars[0], player), "Collect a star before an oxygen defeat");
                GoToSpace();
                step(Mathf.CeilToInt(capacity * 0.8f / Time.fixedDeltaTime));
                Require(!player.HasLost && ui.OxygenStatusText == "OXYGEN LOW" && player.OxygenFraction <= 0.3f,
                    "Low oxygen has a text warning and a visible remaining fill");
                int retries = player.RespawnCount;
                step(Mathf.CeilToInt(capacity / Time.fixedDeltaTime) + 2);
                Require(player.HasLost && body.isKinematic && player.OxygenFraction == 0f && ui.LossVisible &&
                    player.RespawnCount == retries, "Empty tank defeats once without automatic rescue");
                ui.RetryFromCheckpoint();
                Require(Mathf.Approximately(player.OxygenFraction, 1f) && player.stars.CollectedCount == 1,
                    "Retry refills oxygen and preserves collected stars");
                // A future tank upgrade only needs to increase this stat. No second hidden
                // timeout or distance cap may terminate the longer flight.
                player.oxygenCapacity = capacity * 2f;
                GoToSpace();
                step(Mathf.CeilToInt(capacity / Time.fixedDeltaTime) + 3);
                Require(!player.HasLost && player.OxygenRemaining > 0f && player.OxygenFraction < 0.5f,
                    "Larger capacity survives beyond the original oxygen duration");
                step(Mathf.CeilToInt(capacity * 2f / Time.fixedDeltaTime) + 2);
                Require(player.HasLost && player.OxygenRemaining == 0f, "Upgraded tank still depletes normally");
                ui.PlayAgain();
                Require(Mathf.Approximately(player.OxygenFraction, 1f) && player.stars.CollectedCount == 0,
                    "A fresh run starts with a full tank and resets stars");
            }
            finally
            {
                player.oxygenCapacity = capacity;
                player.RestartRun();
                step(50);
                ui.ReplayTutorial();
            }
            Debug.Log("OXYGEN_VALIDATION_PASSED: drain, HUD fill, low warning, refill on gravity entry, pause, depletion, retry/star persistence, replay and doubled capacity beyond the old time/distance limits.");

            void GoToSpace()
            {
                player.Respawn();
                body.position = Vector3.one * 500f;
                body.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Oxygen validation failed: " + message);
        }
    }
}
