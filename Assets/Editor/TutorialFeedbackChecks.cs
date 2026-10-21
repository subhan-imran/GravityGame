using System;
using UnityEngine;

namespace GravityGame.Editor
{
    public static class TutorialFeedbackChecks
    {
        public static void Validate(SphericalPlayer player, MobileGameUI ui, Action<int> step)
        {
            var body = player.GetComponent<Rigidbody>();
            var tutorial = ui.Tutorial;
            var feedback = ui.Feedback;
            player.RestartRun();
            step(50);
            ui.ReplayTutorial();
            Require(tutorial.Active && tutorial.Step == TutorialStep.Move, "Tutorial begins with movement");
            player.SetTouchMovement(Vector2.up * 0.5f);
            step(12);
            player.SetTouchMovement(Vector2.zero);
            Require(tutorial.Step == TutorialStep.FirstJump, "Real movement advances the guidance");
            int jumps = feedback.JumpCueCount;
            int landings = feedback.LandingCueCount;
            player.RequestJump();
            step(1);
            Require(tutorial.Step == TutorialStep.SecondJump && feedback.JumpCueCount == jumps + 1 &&
                feedback.toast.text == "JUMP 1 / 3", "First real takeoff produces coaching and feedback");
            Land();
            Require(feedback.LandingCueCount == landings + 1 && feedback.PulseVisible &&
                !feedback.landingPulse.raycastTarget, "Landing pulses once without stealing jump touches");
            step(5);
            Require(feedback.LandingCueCount == landings + 1, "Standing still does not repeat landing cues");
            float remaining = player.JumpWindowRemaining;
            ui.PauseGame();
            step(100);
            Require(tutorial.Step == TutorialStep.SecondJump && feedback.PulseVisible &&
                Mathf.Approximately(remaining, player.JumpWindowRemaining), "Pause preserves coaching, pulse and jump timing");
            ui.ResumeGame();
            player.RequestJump();
            step(1);
            Require(tutorial.Step == TutorialStep.ThirdJump, "Jump two teaches the escape jump");
            Land();
            Require(player.EscapeJumpReady && feedback.toast.text == "THIRD JUMP READY!", "Second landing highlights the third jump");
            player.RequestJump();
            step(1);
            Require(tutorial.Step == TutorialStep.Travel && !player.LowerJumpConstrained &&
                feedback.JumpCueCount == jumps + 3, "Only third jump releases containment and teaches travel");
            for (int i = 0; i < 200 && player.IsInGravityField && !player.HasLost; i++) step(1);
            Require(!player.IsInGravityField, "Third jump can still leave gravity");
            body.position = Vector3.one * 500f;
            Physics.SyncTransforms();
            step(1);
            Require(player.HasLost, "Missed third jump still loses");
            ui.RetryFromCheckpoint();
            step(2);
            Require(tutorial.Step == TutorialStep.FirstJump && !feedback.PulseVisible && !feedback.BurstVisible,
                "Retry restarts chain guidance and clears old effects");
            ui.SkipTutorialButton.onClick.Invoke();
            Require(!tutorial.Active && !tutorial.card.activeSelf, "Skip immediately allows free exploration");
            ui.PauseGame();
            ui.ReplayTutorialButton.onClick.Invoke();
            Require(!player.IsPaused && tutorial.Active && tutorial.Step == TutorialStep.Move,
                "Pause menu replays the tutorial without resetting the run");
            ui.Initialize();
            ui.Initialize();
            player.RestartRun();
            step(50);
            int before = feedback.JumpCueCount;
            player.RequestJump();
            step(1);
            Require(feedback.JumpCueCount == before + 1, "Repeated initialization does not duplicate feedback listeners");
            int starsBefore = feedback.StarCueCount;
            Require(player.stars.TryCollect(player.stars.stars[0], player) && feedback.BurstVisible &&
                feedback.StarCueCount == starsBefore + 1, "Star pickup creates a sparkle burst and cue");
            feedback.Tick(1f);
            Require(!feedback.BurstVisible, "Pickup effect finishes and hides pooled sparkles");
            player.RestartRun();
            step(50);
            ui.ReplayTutorial();
            Debug.Log("TUTORIAL_FEEDBACK_VALIDATION_PASSED: actual movement/takeoffs/landings, pause timing, third-jump release, retry, skip/replay, idempotent listeners and pickup burst lifecycle.");

            void Land()
            {
                for (int i = 0; i < 400 && !player.IsGrounded && !player.HasLost; i++) step(1);
                Require(player.IsGrounded && !player.HasLost, "Tutorial lower jump returns to the surface");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Tutorial/feedback validation failed: " + message);
        }
    }
}
