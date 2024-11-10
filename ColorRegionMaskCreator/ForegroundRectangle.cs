namespace ColorRegionMaskCreator
{
    internal class ForegroundRectangle
    {
        public int Left = int.MaxValue;
        public int Top = int.MaxValue;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;

        /// <summary>
        /// Makes sure the coordinates are in the rectangle.
        /// </summary>
        public void Include(int x, int y)
        {
            if (Top > y) Top = y;
            if (Left > x) Left = x;
            if (Right < x) Right = x;
            if (Bottom < y) Bottom = y;
        }
    }
}
