using System.Windows;

namespace DeskFrame
{
    class Particle
    {
        private double _dirX;
        private double _dirY;

        private double _speed;
        public double Opacity = 1;

        public double X;
        public double Y;
        public bool ToRemove = true;

        private static readonly Random rnd = new Random();

        public Particle(double width, double height)
        {
            _speed = (rnd.NextDouble() * 1.5 + 0.8) / 2;

            switch (rnd.Next(4))
            {
                case 0: // top
                    X = rnd.NextDouble() * width;
                    Y = -10;
                    break;

                case 1:  // bottom
                    X = rnd.NextDouble() * width
                        ; Y = height + 10;
                    break;

                case 2: // left
                    X = -10;
                    Y = rnd.NextDouble() * height;
                    break;

                case 3: // right
                    X = width + 10;
                    Y = rnd.NextDouble() * height;
                    break;
            }

            Vector d = new Vector(width / 2 - X, height / 2 - Y);
            d.Normalize();
            _dirX = d.X;
            _dirY = d.Y;
        }

        public void Update(double cx, double cy, double centerRadius)
        {
            if (!ToRemove) return;

            double dx = cx - X;
            double dy = cy - Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance <= 40)
            {
                ToRemove = false;
                Opacity = 0;
                return;
            }

            X += _dirX * _speed;
            Y += _dirY * _speed;

            if (distance < centerRadius) // fade out when approaching center
            {
                Opacity = distance / centerRadius;
            }
        }
    }
}