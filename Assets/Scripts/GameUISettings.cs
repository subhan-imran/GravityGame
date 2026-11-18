using UnityEngine;

namespace GravityGame
{
    [CreateAssetMenu(menuName = "GravityGame/UI Settings", fileName = "UISettings")]
    public sealed class GameUISettings : ScriptableObject
    {
        private static GameUISettings defaults;
        public static GameUISettings Defaults
        {
            get
            {
                if (defaults == null)
                {
                    defaults = CreateInstance<GameUISettings>();
                    defaults.hideFlags = HideFlags.HideAndDontSave;
                }
                return defaults;
            }
        }
        [Header("Dynamic HUD colors")]
        public Color jumpNormal = new Color(0.12f, 0.58f, 0.84f, 1f);
        public Color jumpReady = new Color(1f, 0.78f, 0.36f, 1f);
        public Color jumpText = Color.white;
        public Color jumpReadyText = new Color(0.035f, 0.075f, 0.14f, 0.96f);
        public Color oxygenFull = new Color(0.3f, 0.9f, 0.65f);
        public Color oxygenInSpace = new Color(0.36f, 0.85f, 0.95f);
        public Color oxygenLow = new Color(1f, 0.78f, 0.36f);
        public Color oxygenCritical = new Color(0.98f, 0.35f, 0.32f);
        [Range(0f, 1f)] public float lowThreshold = 0.3f;
        [Range(0f, 1f)] public float criticalThreshold = 0.15f;
        [Header("Dynamic HUD text")]
        [Tooltip("Supported tokens: {planet}, {next}, {stage}, {remaining}, {count}, {total}.")]
        public string gravityStatus = "{planet}  /  GRAVITY ACTIVE";
        public string spaceStatus = "SPACE  /  NO GRAVITY";
        public string starCounter = "STARS  {count} / {total}";
        public string oxygenEmptyLabel = "OXYGEN EMPTY";
        public string oxygenFullLabel = "OXYGEN FULL";
        public string oxygenLowLabel = "OXYGEN LOW";
        public string oxygenLabel = "OXYGEN";
        public string groundedHint = "Chain 3 jumps to escape gravity - running is optional";
        public string chainHint = "Jump again in {remaining}s  /  next {next}/3";
        public string airborneHint = "Jump {stage}/3 - tap again just after landing";
        public string exploreHint = "Explore freely. Find all the stars to win.";
        [TextArea] public string jumpLabel = "JUMP\n{next} / 3";
        [TextArea] public string escapeLabel = "ESCAPE\n3 / 3";
        [Header("Tutorial text")]
        public TutorialCopy tutorial = new TutorialCopy();

        private void OnValidate()
        {
            lowThreshold = Mathf.Clamp01(lowThreshold);
            criticalThreshold = Mathf.Clamp(criticalThreshold, 0f, lowThreshold);
        }
    }

    [System.Serializable]
    public sealed class TutorialCopy
    {
        public string moveHeading = "1 / 5  STRANDED";
        [TextArea] public string move = "Your flight is gone. Find a way home.\nUse the left stick. Push farther to run.";
        public string firstHeading = "2 / 5  JUMP 1";
        [TextArea] public string first = "Tap JUMP, then land.\nJumps 1 and 2 stay in gravity.";
        public string secondHeading = "3 / 5  JUMP 2";
        [TextArea] public string secondGrounded = "Tap jump 2 within {remaining}s.";
        [TextArea] public string secondAirborne = "Wait for the landing pulse.\nThen tap again within {window} seconds.";
        public string thirdHeading = "4 / 5  JUMP 3";
        [TextArea] public string thirdGrounded = "Aim at another planet.\nTap gold JUMP for 3!";
        [TextArea] public string thirdAirborne = "Land again, then jump 3.\nAim at another planet.";
        public string travelHeading = "5 / 5  TRAVEL";
        [TextArea] public string travel = "Reach gravity to refill oxygen.\nBlue cape: safe. Red cape: empty.";
        public string exploreHeading = "READY TO EXPLORE";
        [TextArea] public string explore = "Collect stars to chart your way home.\nLand anywhere. Retry keeps your stars.";
    }
}
