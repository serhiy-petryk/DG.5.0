namespace DGCore.Helpers
{
    public class ColorInfo
    {
        // This library is independent of Windows/WPF applications.
        // Therefore I can't use System.Drawing.Color and System.Windows.Media.Color.
        // I have created this class to support color in this project

        #region =========  Static section  ============
        public static readonly ColorInfo[] GroupColors =
        {
            new ColorInfo(0xDC, 0xDC, 0xDC), new ColorInfo(255, 153, 204), new ColorInfo(255, 204, 153), new ColorInfo(255, 255, 153),
            new ColorInfo(204, 255, 204), new ColorInfo(204, 255, 255), new ColorInfo(153, 204, 255), new ColorInfo(204, 153, 255)
        };

        public static ColorInfo GetGroupColor(int level) => GroupColors[level == 0 ? 0 : ((level - 1) % (GroupColors.Length - 1)) + 1];
        public static int GetExcelColor(ColorInfo color) => (color.B << 16) + (color.G << 8) + color.R;
        #endregion

        #region =========  Instance section  ============
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public ColorInfo(byte r, byte g, byte b)
        {
            R = r; G = g; B = b;
        }
        #endregion
    }
}
