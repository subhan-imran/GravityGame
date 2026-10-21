using UnityEngine;

namespace GravityGame
{
    public sealed class StarCollection : MonoBehaviour
    {
        public SphericalPlayer player;
        public CollectibleStar[] stars = new CollectibleStar[0];
        public event System.Action<CollectibleStar> StarCollected;
        public int CollectedCount { get; private set; }
        public int TotalCount => stars.Length;
        public bool AllCollected => TotalCount > 0 && CollectedCount == TotalCount;

        public bool TryCollect(CollectibleStar star, SphericalPlayer collector)
        {
            if (!isActiveAndEnabled || collector == null || collector != player || collector.stars != this ||
                collector.HasLost || collector.HasWon || collector.IsPaused || star == null ||
                star.collection != this || star.Collected || !star.isActiveAndEnabled ||
                System.Array.IndexOf(stars, star) < 0) return false;
            star.SetCollected(true);
            CollectedCount++;
            StarCollected?.Invoke(star);
            if (AllCollected) player.CompleteStarRun();
            return true;
        }

        // Checkpoint retries preserve progress. A fresh run restores every pickup.
        public void ResetProgress()
        {
            CollectedCount = 0;
            foreach (var star in stars) if (star != null) star.SetCollected(false);
        }
    }
}
