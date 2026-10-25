using UnityEngine;
using UnityEngine.UI;

namespace GravityGame
{
    public enum TutorialStep { Move, FirstJump, SecondJump, ThirdJump, Travel, Explore, Complete }

    public sealed class MobileTutorial : MonoBehaviour
    {
        public SphericalPlayer player;
        public GameObject card;
        public Text heading;
        public Text instructions;
        private const string CompletedKey = "GravityGame.Tutorial.Completed.v1";
        public TutorialStep Step { get; private set; }
        public bool Active => Step != TutorialStep.Complete;
        private SphericalPlayer subscribedPlayer;
        private StarCollection subscribedStars;
        private PlanetGravity startingPlanet;
        private int lastRespawn;
        private float exploreTime;
        private bool initialized;

        public void Initialize()
        {
            Unsubscribe();
            subscribedPlayer = player;
            subscribedStars = player.stars;
            player.Jumped += OnJump;
            player.Landed += OnLanding;
            if (subscribedStars != null) subscribedStars.StarCollected += OnStar;
            if (!initialized)
            {
                initialized = true;
                startingPlanet = player.planet;
                lastRespawn = player.RespawnCount;
                Step = !Application.isEditor && PlayerPrefs.GetInt(CompletedKey, 0) == 1
                    ? TutorialStep.Complete : TutorialStep.Move;
            }
            Refresh();
        }

        public void StartTutorial()
        {
            startingPlanet = player.planet;
            lastRespawn = player.RespawnCount;
            exploreTime = 0f;
            Step = TutorialStep.Move;
            Refresh();
        }

        public void Skip()
        {
            Step = TutorialStep.Complete;
            if (Application.isPlaying && !Application.isEditor)
            {
                PlayerPrefs.SetInt(CompletedKey, 1);
                PlayerPrefs.Save();
            }
            Refresh();
        }

        public void Tick(float dt)
        {
            if (player == null || player.IsPaused || player.HasLost || player.HasWon) return;
            if (lastRespawn != player.RespawnCount)
            {
                lastRespawn = player.RespawnCount;
                if (Active && Step != TutorialStep.Move) Step = TutorialStep.FirstJump;
            }
            if (Step == TutorialStep.Move && player.IsGrounded && player.MovementInput.sqrMagnitude > 0.01f &&
                Vector3.ProjectOnPlane(player.Velocity, player.GravityUp).magnitude > 0.5f)
                Step = TutorialStep.FirstJump;
            if ((Step == TutorialStep.SecondJump || Step == TutorialStep.ThirdJump) &&
                player.IsGrounded && player.JumpChain == 0) Step = TutorialStep.FirstJump;
            if (Step == TutorialStep.Explore)
            {
                exploreTime += dt;
                if (exploreTime >= 4f) Skip();
            }
            Refresh();
        }

        private void OnJump(int stage)
        {
            if (!Active || Step == TutorialStep.Explore) return;
            lastRespawn = player.RespawnCount;
            Step = stage == 1 ? TutorialStep.SecondJump : stage == 2 ? TutorialStep.ThirdJump : TutorialStep.Travel;
            Refresh();
        }

        private void OnLanding(int stage)
        {
            if (Step != TutorialStep.Travel) return;
            if (player.planet != startingPlanet) { Step = TutorialStep.Explore; exploreTime = 0f; }
            else Step = TutorialStep.FirstJump;
            Refresh();
        }

        private void OnStar(CollectibleStar star)
        {
            if (Step == TutorialStep.Explore || player.stars.AllCollected) Skip();
        }

        private void Refresh()
        {
            if (card == null) return;
            card.SetActive(Active);
            if (!Active) return;
            switch (Step)
            {
                case TutorialStep.Move:
                    heading.text = "1 / 5  STRANDED";
                    instructions.text = "Your flight is gone. Find a way home.\nUse the left stick. Push farther to run.";
                    break;
                case TutorialStep.FirstJump:
                    heading.text = "2 / 5  JUMP 1";
                    instructions.text = "Tap JUMP, then land.\nJumps 1 and 2 stay in gravity.";
                    break;
                case TutorialStep.SecondJump:
                    heading.text = "3 / 5  JUMP 2";
                    instructions.text = player.IsGrounded
                        ? "Tap jump 2 within " + player.JumpWindowRemaining.ToString("0.0") + "s."
                        : "Wait for the landing pulse.\nThen tap again within " + player.consecutiveJumpWindow.ToString("0.0") + " seconds.";
                    break;
                case TutorialStep.ThirdJump:
                    heading.text = "4 / 5  JUMP 3";
                    instructions.text = player.IsGrounded
                        ? "Aim at another planet.\nTap gold JUMP for 3!"
                        : "Land again, then jump 3.\nAim at another planet.";
                    break;
                case TutorialStep.Travel:
                    heading.text = "5 / 5  TRAVEL";
                    instructions.text = "Reach gravity to refill oxygen.\nBlue cape: safe. Red cape: empty.";
                    break;
                case TutorialStep.Explore:
                    heading.text = "READY TO EXPLORE";
                    instructions.text = "Collect stars to chart your way home.\nLand anywhere. Retry keeps your stars.";
                    break;
            }
        }

        private void OnDisable() => Unsubscribe();
        private void Unsubscribe()
        {
            if (subscribedPlayer != null)
            {
                subscribedPlayer.Jumped -= OnJump;
                subscribedPlayer.Landed -= OnLanding;
            }
            if (subscribedStars != null) subscribedStars.StarCollected -= OnStar;
        }
    }
}
