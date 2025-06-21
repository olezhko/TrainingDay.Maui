namespace TrainingDay.Maui.Extensions
{
    public static class AnimationExtensions
    {
        /// <summary>
        /// Spins any <see cref="VisualElement"/> forever.
        /// </summary>
        /// <param name="element">Element to rotate.</param>
        /// <param name="duration">Milliseconds per 360°.</param>
        public static async Task StartInfiniteRotate(
            this VisualElement element,
            CancellationToken cancellationToken = default,
            uint duration = 1000)
        {
            // Keep going as long as the element is on the visual tree.
            while (element.Handler != null)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                await element.RotateTo(360, duration, Easing.Linear);
                element.Rotation = 0;          // avoid overflow
            }
        }
    }
}
