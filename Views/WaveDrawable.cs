using Microsoft.Maui.Graphics;

namespace WaveTuneNew.Views
{
    public class WaveDrawable : IDrawable
    {
        private float _phase = 0f;

        public void Advance(float delta)
        {
            _phase += delta;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Color.FromArgb("#602191");
            canvas.StrokeSize = 2;
            canvas.Alpha = 0.6f;

            float width = dirtyRect.Width;
            float height = dirtyRect.Height;
            float midY = height / 2;
            float amplitude = height * 0.3f;
            float frequency = 2.5f;

            var path = new PathF();
            path.MoveTo(0, midY);

            for (float x = 0; x <= width; x += 2)
            {
                float normalX = x / width;
                float y = midY + amplitude *
                    (float)(Math.Sin(normalX * Math.PI * 2 * frequency + _phase) * 0.6 +
                             Math.Sin(normalX * Math.PI * 2 * frequency * 2.1 + _phase * 1.3) * 0.3 +
                             Math.Sin(normalX * Math.PI * 2 * frequency * 0.5 + _phase * 0.7) * 0.1);
                path.LineTo(x, y);
            }

            canvas.DrawPath(path);
        }
    }
}