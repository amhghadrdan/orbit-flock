using UnityEngine;

namespace OrbitFlock
{
    [RequireComponent(typeof(Camera))]
    public sealed class PlayfieldCamera : MonoBehaviour
    {
        public const float HalfWidth = 8f;
        public const float HalfHeight = 4.5f;
        [SerializeField] private bool framePlayfield;
        private Camera view;
        private int lastWidth, lastHeight;

        private void OnEnable()
        {
            view = GetComponent<Camera>();
            lastWidth = lastHeight = 0;
            Resize();
        }

        private void LateUpdate() => Resize();

        private void Resize()
        {
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            if (lastWidth == width && lastHeight == height)
                return;

            lastWidth = width;
            lastHeight = height;
            float targetAspect = HalfWidth / HalfHeight;
            float scale = Mathf.Min((float)width / FlightUi.Width, (float)height / FlightUi.Height);
            float referenceWidth = framePlayfield ? FlightUi.FlightWidth : FlightUi.Width;
            float referenceHeight = framePlayfield ? FlightUi.FlightHeight : FlightUi.Height;
            float viewportWidth = referenceWidth * scale / width;
            float viewportHeight = referenceHeight * scale / height;
            float offsetY = framePlayfield ? (FlightUi.FlightBottom - FlightUi.FlightTop) * 0.5f * scale / height : 0f;
            view.rect = new Rect((1f - viewportWidth) / 2f, (1f - viewportHeight) / 2f + offsetY,
                viewportWidth, viewportHeight);
            view.orthographicSize = HalfHeight;
            view.aspect = targetAspect;
        }

        public static bool IsOutside(Vector2 point, float margin = 0.6f)
        {
            return Mathf.Abs(point.x) > HalfWidth + margin || Mathf.Abs(point.y) > HalfHeight + margin;
        }
    }
}
