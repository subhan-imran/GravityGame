using System;
using UnityEngine;

namespace GravityGame.Editor
{
    public static class FirstExpeditionChecks
    {
        public static void Validate(SphericalPlayer player, MobileGameUI ui, Action<int> step)
        {
            FirstExpedition.ValidateLayout(player);
            var body = player.GetComponent<Rigidbody>();
            bool objectiveEnabled = player.stars.enabled;
            player.stars.enabled = false;
            try
            {
                // Test both directions on every edge of the triangle with real takeoffs,
                // zero steering and no launch helper. Players can choose any landing site.
                foreach (var source in player.gravitySources)
                foreach (var destination in player.gravitySources)
                {
                    if (source == destination) continue;
                    player.Respawn();
                    Vector3 normal = (destination.transform.position - source.transform.position).normalized;
                    body.position = source.transform.position + normal * (source.Radius + 1.05f);
                    body.rotation = Quaternion.FromToRotation(Vector3.up, normal);
                    body.linearVelocity = Vector3.zero;
                    player.planet = source;
                    player.transform.SetPositionAndRotation(body.position, body.rotation);
                    Physics.SyncTransforms();
                    step(60);
                    Require(player.IsGrounded && player.planet == source, "Start on " + source.name);
                    for (int stage = 1; stage <= 2; stage++)
                    {
                        player.RequestJump();
                        step(1);
                        Require(player.JumpChain == stage && player.LowerJumpConstrained, "Lower jump remains constrained");
                        for (int i = 0; i < 400 && !player.IsGrounded && !player.HasLost; i++) step(1);
                        Require(player.IsGrounded && player.planet == source && !player.HasLost, "Lower arc lands on source");
                    }
                    int retries = player.RespawnCount;
                    player.RequestJump();
                    step(1);
                    Require(player.JumpChain == 3 && !player.LowerJumpConstrained, "Third jump releases the source");
                    bool sawSpace = false;
                    for (int i = 0; i < 600 && !player.HasLost; i++)
                    {
                        step(1);
                        sawSpace |= !player.IsInGravityField;
                        if (player.IsGrounded && player.planet == destination) break;
                    }
                    Debug.Log("EXPEDITION_ROUTE: " + source.name + " -> " + destination.name +
                        " landed=" + player.IsGrounded + " planet=" + player.planet.name + " space=" + sawSpace);
                    Require(sawSpace && !player.HasLost && player.IsGrounded && player.planet == destination &&
                        player.RespawnCount == retries && !player.HasWon, "Free ballistic crossing " + source.name + " -> " + destination.name);
                    step(30);
                    ui.RetryFromCheckpoint();
                    Require(player.planet == destination && body.linearVelocity.sqrMagnitude < 0.001f,
                        "Safe landing updates the retry checkpoint on " + destination.name);
                }
            }
            finally
            {
                player.stars.enabled = objectiveEnabled;
                player.RestartRun();
                step(50);
                ui.ReplayTutorial();
            }
            Debug.Log("FIRST_EXPEDITION_VALIDATION_PASSED: six bidirectional routes, real three-jump takeoffs, space gaps, free landings and per-planet retry checkpoints.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("First Expedition flight: " + message);
        }
    }
}
