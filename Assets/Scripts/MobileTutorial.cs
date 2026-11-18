using UnityEngine;
using UnityEngine.UI;

namespace GravityGame
{
    public enum TutorialStep { Move, FirstJump, SecondJump, ThirdJump, Travel, Explore, Complete }

    public sealed class MobileTutorial : MonoBehaviour
    {
        public GameUISettings settings;
        [Tooltip("Used when no shared UI Settings asset is assigned.")]
        public TutorialCopy copy = new TutorialCopy();
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
            var text = settings != null && settings.tutorial != null ? settings.tutorial : copy;
            switch (Step)
            {
                case TutorialStep.Move:
                    heading.text = text.moveHeading;
                    instructions.text = text.move;
                    break;
                case TutorialStep.FirstJump:
                    heading.text = text.firstHeading;
                    instructions.text = text.first;
                    break;
                case TutorialStep.SecondJump:
                    heading.text = text.secondHeading;
                    instructions.text = player.IsGrounded
                        ? Format(text.secondGrounded) : Format(text.secondAirborne);
                    break;
                case TutorialStep.ThirdJump:
                    heading.text = text.thirdHeading;
                    instructions.text = player.IsGrounded
                        ? text.thirdGrounded : text.thirdAirborne;
                    break;
                case TutorialStep.Travel:
                    heading.text = text.travelHeading;
                    instructions.text = text.travel;
                    break;
                case TutorialStep.Explore:
                    heading.text = text.exploreHeading;
                    instructions.text = text.explore;
                    break;
            }
        }

        private string Format(string text) => (text ?? "")
            .Replace("{remaining}", player.JumpWindowRemaining.ToString("0.0"))
            .Replace("{window}", player.consecutiveJumpWindow.ToString("0.0"));

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
