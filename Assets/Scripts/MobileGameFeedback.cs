using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GravityGame
{
    public sealed class MobileGameFeedback : MonoBehaviour
    {
        public SphericalPlayer player;
        public PlanetCamera cameraRig;
        public Text toast;
        public Image landingPulse;
        public bool soundEnabled = true;
        [Header("Messages and cue colors")]
        public string jumpMessage = "JUMP {stage} / 3";
        public string escapeMessage = "ESCAPE JUMP!";
        public string readyMessage = "THIRD JUMP READY!";
        public string secondMessage = "LANDED - TAP FOR JUMP 2";
        public string landingMessage = "LANDED";
        public string starMessage = "STAR {count} / {total}";
        public int JumpCueCount { get; private set; }
        public int LandingCueCount { get; private set; }
        public int StarCueCount { get; private set; }
        public bool PulseVisible => pulseTime > 0f;
        public bool BurstVisible => burstTime > 0f;
        private SphericalPlayer subscribedPlayer;
        private StarCollection subscribedStars;
        private AudioSource source;
        private AudioClip[] clips;
        private Transform[] sparks;
        private Vector3[] sparkDirections;
        private Vector3 burstPosition, burstUp;
        private float toastTime, pulseTime, burstTime;
        private int lastRespawn;
        public Color Gold = new Color(1f, 0.78f, 0.36f);
        public Color Cyan = new Color(0.35f, 0.9f, 1f);

        public void Initialize()
        {
            Unsubscribe();
            subscribedPlayer = player;
            subscribedStars = player.stars;
            player.Jumped += OnJump;
            player.Landed += OnLanding;
            if (subscribedStars != null) subscribedStars.StarCollected += OnStar;
            lastRespawn = player.RespawnCount;
            if (Application.isPlaying && source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0.25f;
                clips = new[] { Tone(370f, 0.1f), Tone(520f, 0.12f), Tone(760f, 0.2f),
                    Tone(150f, 0.07f), Tone(1040f, 0.16f), Tone(660f, 0.35f) };
            }
        }

        public void Tick(float dt)
        {
            if (player == null) return;
            if (player.RespawnCount != lastRespawn)
            {
                lastRespawn = player.RespawnCount;
                Clear();
            }
            if (player.IsPaused) { if (source != null) source.Stop(); return; }
            toastTime = Mathf.Max(0f, toastTime - dt);
            pulseTime = Mathf.Max(0f, pulseTime - dt);
            burstTime = Mathf.Max(0f, burstTime - dt);
            if (toast != null)
            {
                var color = toast.color;
                color.a = Mathf.Clamp01(toastTime * 3f);
                toast.color = color;
            }
            if (landingPulse != null)
            {
                float t = pulseTime / 0.45f;
                landingPulse.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, t);
                var color = landingPulse.color;
                color.a = Mathf.Sin(t * Mathf.PI) * 0.35f;
                landingPulse.color = color;
            }
            if (sparks == null) return;
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i].gameObject.SetActive(burstTime > 0f);
                if (burstTime <= 0f) continue;
                float t = 1f - burstTime / 0.6f;
                sparks[i].position = burstPosition + sparkDirections[i] * (t * 2.5f) + burstUp * Mathf.Sin(t * Mathf.PI) * 0.4f;
                sparks[i].localScale = Vector3.one * ((1f - t) * 0.24f);
                if (cameraRig != null) sparks[i].LookAt(cameraRig.transform, burstUp);
            }
        }

        private void OnJump(int stage)
        {
            SynchronizeRespawn();
            JumpCueCount++;
            Show(stage == 3 ? escapeMessage : jumpMessage.Replace("{stage}", stage.ToString()), stage == 3 ? Gold : Cyan);
            Play(stage - 1);
        }

        private void OnLanding(int stage)
        {
            SynchronizeRespawn();
            LandingCueCount++;
            pulseTime = 0.45f;
            bool ready = player.EscapeJumpReady;
            if (landingPulse != null) landingPulse.color = ready ? Gold : Cyan;
            Show(ready ? readyMessage : stage == 1 ? secondMessage : landingMessage, ready ? Gold : Cyan);
            Play(ready ? 1 : 3);
        }

        private void OnStar(CollectibleStar star)
        {
            SynchronizeRespawn();
            StarCueCount++;
            Show(starMessage.Replace("{count}", player.stars.CollectedCount.ToString())
                .Replace("{total}", player.stars.TotalCount.ToString()), Gold);
            Play(player.stars.AllCollected ? 5 : 4);
            PrepareSparks(star);
            burstPosition = star.transform.position;
            burstUp = star.transform.up;
            burstTime = 0.6f;
            for (int i = 0; sparks != null && i < sparks.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / sparks.Length;
                sparkDirections[i] = star.transform.right * Mathf.Cos(angle) + star.transform.forward * Mathf.Sin(angle);
            }
            Tick(0f);
        }

        private void PrepareSparks(CollectibleStar star)
        {
            if (sparks != null || star.visual == null) return;
            var mesh = star.visual.GetComponent<MeshFilter>();
            var renderer = star.visual.GetComponent<MeshRenderer>();
            if (mesh == null || renderer == null) return;
            sparks = new Transform[8];
            sparkDirections = new Vector3[8];
            for (int i = 0; i < sparks.Length; i++)
            {
                var node = new GameObject("Pickup sparkle", typeof(MeshFilter), typeof(MeshRenderer));
                node.hideFlags = HideFlags.DontSave;
                node.transform.SetParent(transform, false);
                node.GetComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                var sparkRenderer = node.GetComponent<MeshRenderer>();
                sparkRenderer.sharedMaterial = renderer.sharedMaterial;
                sparkRenderer.shadowCastingMode = ShadowCastingMode.Off;
                sparkRenderer.receiveShadows = false;
                node.SetActive(false);
                sparks[i] = node.transform;
            }
        }

        private void SynchronizeRespawn()
        {
            if (lastRespawn == player.RespawnCount) return;
            lastRespawn = player.RespawnCount;
            Clear();
        }

        private void Show(string message, Color color)
        {
            toastTime = 1.2f;
            if (toast == null) return;
            toast.text = message;
            toast.color = color;
        }

        private void Play(int index)
        {
            if (source != null && soundEnabled && !player.IsPaused) source.PlayOneShot(clips[index]);
        }

        private static AudioClip Tone(float frequency, float duration)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Min(1f, t / 0.008f) * Mathf.Pow(1f - i / (float)samples.Length, 2f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * (t + t * t)) * envelope * 0.6f;
            }
            var clip = AudioClip.Create("Gameplay chime", samples.Length, 1, rate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        public void Clear()
        {
            toastTime = pulseTime = burstTime = 0f;
            if (source != null) source.Stop();
            if (toast != null) { var color = toast.color; color.a = 0f; toast.color = color; }
            if (landingPulse != null) { var color = landingPulse.color; color.a = 0f; landingPulse.color = color; }
            if (sparks != null) foreach (var spark in sparks) if (spark != null) spark.gameObject.SetActive(false);
        }

        private void OnDisable() { Clear(); Unsubscribe(); }
        private void OnDestroy()
        {
            if (clips != null) foreach (var clip in clips) if (clip != null) Destroy(clip);
        }
        private void Unsubscribe()
        {
            if (subscribedPlayer != null) { subscribedPlayer.Jumped -= OnJump; subscribedPlayer.Landed -= OnLanding; }
            if (subscribedStars != null) subscribedStars.StarCollected -= OnStar;
        }
    }
}
