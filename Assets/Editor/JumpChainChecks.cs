using System;
using UnityEngine;

namespace GravityGame.Editor
{
    public static class JumpChainChecks
    {
        public static void Validate(SphericalPlayer player, MobileGameUI ui, Action<int> step)
        {
            var body = player.GetComponent<Rigidbody>();
            player.RestartRun();
            step(50);
            player.RequestJump();
            step(1);
            Require(player.JumpChain == 1, "First grounded tap starts the chain");
            for (int i = 0; i < 20; i++) { player.RequestJump(); step(1); }
            Require(player.JumpChain == 1, "Airborne taps cannot skip jump stages");
            Land();
            float window = player.JumpWindowRemaining;
            ui.PauseGame();
            step(100);
            Require(player.JumpChain == 1 && Mathf.Approximately(window, player.JumpWindowRemaining), "Pause freezes the chain window");
            ui.ResumeGame();
            step(Mathf.CeilToInt(player.consecutiveJumpWindow / Time.fixedDeltaTime) + 2);
            Require(player.JumpChain == 0 && player.NextJump == 1, "Waiting after landing resets to jump one");
            float first = JumpAndLand(1);
            float second = JumpAndLand(2);
            Require(second > first && player.EscapeJumpReady, "Each consecutive jump is higher");
            player.RequestJump();
            step(1);
            Require(player.JumpChain == 3 && Vector3.Dot(body.linearVelocity, player.GravityUp) > second,
                "Third tap launches the escape jump while stationary");
            for (int i = 0; i < 350 && !player.HasLost; i++) step(1);
            Require(player.HasLost && !player.IsInGravityField, "Unaimed third jump can escape and lose in space");
            ui.RetryFromCheckpoint();
            Require(player.JumpChain == 0 && player.NextJump == 1, "Retry resets the chain");

            foreach (var planet in player.gravitySources)
            {
                player.Respawn();
                body.position = planet.transform.position + Vector3.up * (planet.Radius + 1.05f);
                body.rotation = Quaternion.identity;
                body.linearVelocity = Vector3.zero;
                player.planet = planet;
                player.transform.SetPositionAndRotation(body.position, body.rotation);
                Physics.SyncTransforms();
                step(50);
                player.SetTouchMovement(Vector2.up);
                step(40);
                Require(player.IsRunning, "Running movement is retained");
                // Keep the movement finger held through both arcs, as on a phone.
                player.RequestJump();
                step(1);
                Require(player.JumpChain == 1, "Running cannot skip jump one on " + planet.name);
                Land();
                player.RequestJump();
                step(1);
                Require(player.JumpChain == 2, "Running cannot skip jump two on " + planet.name);
                Land();
                player.SetTouchMovement(Vector2.zero);
                Require(player.planet == planet && !player.HasLost, "First two jumps stay inside " + planet.name + " gravity at running speed");
            }
            player.RestartRun();
            step(50);
            Debug.Log("JUMP_CHAIN_VALIDATION_PASSED: airborne spam, grounded timing, paused timing, increasing heights, stationary escape, missed-jump loss, retry reset, first two running jumps on both planets.");

            float JumpAndLand(int expected)
            {
                player.RequestJump();
                step(1);
                float velocity = Vector3.Dot(body.linearVelocity, player.GravityUp);
                Require(player.JumpChain == expected, "Grounded jump stage " + expected);
                Land();
                return velocity;
            }
            void Land()
            {
                var origin = player.planet;
                for (int i = 0; i < 400 && !player.IsGrounded && !player.HasLost; i++) step(1);
                Require(player.IsGrounded && !player.HasLost && player.planet == origin, "Lower jump returns to its starting planet");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Jump-chain validation failed: " + message);
        }
    }
}
