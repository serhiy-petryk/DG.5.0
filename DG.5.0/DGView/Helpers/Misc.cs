using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Helpers
{
    public static class Misc
    {
        // Filter helper for datagrid
        public static bool SetFilter(string text, string filterText)
        {
            if ((filterText ?? "") == "") return true;
            var words = filterText.ToLowerInvariant().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length; i++)
                words[i] = words[i].Trim();

            text = (text ?? "").ToLowerInvariant();
            return words.All(word => text.IndexOf(word, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        public static ImageSource GetImageSourceFromGeometry(Geometry geometry, Brush brush, Pen pen)
        {
            var geometryDrawing = new GeometryDrawing(brush, pen, geometry);
            return new DrawingImage { Drawing = geometryDrawing };
        }

        public static void OpenDGDialog(DataGrid dataGrid, FrameworkElement dialogView, string titleResourceName, Geometry icon)
        {
            var owner = dataGrid.GetVisualParents<MwiChild>().FirstOrDefault();
            var host = owner.GetDialogHost();
            var transforms = host.GetActualLayoutTransforms();
            var width = Math.Max(200, Window.GetWindow(host).ActualWidth * 2 / 3 / transforms.Value.M11);
            var height = Math.Max(200, Window.GetWindow(host).ActualHeight * 2 / 3 / transforms.Value.M22);
            OpenMwiDialog(host, dialogView, icon, (child, adorner) =>
            {
                child.Height = height;
                child.Width = width;
                child.Theme = owner.ActualTheme;
                child.ThemeColor = owner.ActualThemeColor;
                var titleResource = Application.Current.TryFindResource(titleResourceName);
                if (titleResource == null)
                    child.Title = titleResourceName;
                else
                    child.SetResourceReference(MwiChild.TitleProperty, titleResourceName);
            });
        }

        public static void OpenMwiDialog(FrameworkElement host, FrameworkElement dialogContent, Geometry icon, Action<MwiChild, DialogAdorner> beforeShowDialogAction)
        {
            var width = dialogContent.Width;
            var height = dialogContent.Height;
            if (!double.IsNaN(dialogContent.Width)) dialogContent.Width = double.NaN;
            if (!double.IsNaN(dialogContent.Height)) dialogContent.Height = double.NaN;

            var content = new MwiChild
            {
                Content = dialogContent,
                LimitPositionToPanelBounds = true,
                VisibleButtons = MwiChild.Buttons.Close | MwiChild.Buttons.Maximize,
                IsActive = true
            };

            // Migrate valid width/height values from dialogContent to host
           if (!double.IsNaN(width)) content.Width = width;
           if (!double.IsNaN(height)) content.Height = height;

            // var adorner = new DialogAdorner(_owner.DialogHost) { CloseOnClickBackground = true };
            var adorner = new DialogAdorner(host) { CloseOnClickBackground = true };
            beforeShowDialogAction?.Invoke(content, adorner);
            if (icon != null)
            {
                var brush = (Brush)ColorHslBrush.Instance.Convert(content.ActualThemeColor, typeof(Brush), "+50%", null);
                content.Icon = GetImageSourceFromGeometry(icon, brush, null);
            }
            adorner.ShowContentDialog(content);
        }

        #region =======  difference between two strings  ========
        // see Ben Gripka comment in https://stackoverflow.com/questions/2344320/comparing-strings-with-tolerance
        //      https://en.wikipedia.org/wiki/Levenshtein_distance

        public static double MyCalculateSimilarity(string source, string target)
        {
            source = source?.Replace(".", "").Replace(",", "").Replace("  ", " ").ToUpper();
            target = target?.Replace(".", "").Replace(",", "").Replace("  ", " ").ToUpper();

            // Substring
            if (source != null && target != null && source.Length > 5 && target.Length > source.Length &&
                target.Substring(0, source.Length) == source)
                return 1.0;
            if (source != null && target != null && source.Length > target.Length && target.Length > 5 &&
                source.Substring(0, target.Length) == target)
                return 1.0;

            return CalculateSimilarity(source, target);
        }

        /// <summary>
        /// Calculate percentage similarity of two strings
        /// <param name="source">Source String to Compare with</param>
        /// <param name="target">Targeted String to Compare</param>
        /// <returns>Return Similarity between two strings from 0 to 1.0</returns>
        /// </summary>
        public static double CalculateSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target)) return 1.0;
            if ((source == null) || (target == null)) return 0.0;
            if ((source.Length == 0) || (target.Length == 0)) return 0.0;
            if (source == target) return 1.0;


            int stepsToSame = LevenshteinDistance(source, target);
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }

        public static int LevenshteinDistance(string source, string target)
        {
            // degenerate cases
            if (source == target) return 0;
            if (source.Length == 0) return target.Length;
            if (target.Length == 0) return source.Length;

            // create two work vectors of integer distances
            int[] v0 = new int[target.Length + 1];
            int[] v1 = new int[target.Length + 1];

            // initialize v0 (the previous row of distances)
            // this row is A[0][i]: edit distance for an empty s
            // the distance is just the number of characters to delete from t
            for (int i = 0; i < v0.Length; i++)
                v0[i] = i;

            for (int i = 0; i < source.Length; i++)
            {
                // calculate v1 (current row distances) from the previous row v0

                // first element of v1 is A[i+1][0]
                //   edit distance is delete (i+1) chars from s to match empty t
                v1[0] = i + 1;

                // use formula to fill in the rest of the row
                for (int j = 0; j < target.Length; j++)
                {
                    var cost = (source[i] == target[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(v1[j] + 1, Math.Min(v0[j + 1] + 1, v0[j] + cost));
                }

                // copy v1 (current row) to v0 (previous row) for next iteration
                for (int j = 0; j < v0.Length; j++)
                    v0[j] = v1[j];
            }

            return v1[target.Length];
        }
        #endregion

    }
}
