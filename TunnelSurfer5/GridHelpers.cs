using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TunnelVision
{
    public class GridHelpers
    {
        #region RowCount Property

        /// <summary>
        /// Adds the specified number of Rows to RowDefinitions. 
        /// Default Height is Auto
        /// </summary>
        public static readonly DependencyProperty RowCountProperty =
            DependencyProperty.RegisterAttached(
                "RowCount", typeof(int), typeof(GridHelpers),
                new PropertyMetadata(-1, RowCountChanged));

        // Get
        public static int GetRowCount(DependencyObject obj)
        {
            return (int)obj.GetValue(RowCountProperty);
        }

        // Set
        public static void SetRowCount(DependencyObject obj, int value)
        {
            obj.SetValue(RowCountProperty, value);
        }

        // Change Event - Adds the Rows
        public static void RowCountChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || (int)e.NewValue < 0)
                return;

            Grid grid = (Grid)obj;
            grid.RowDefinitions.Clear();

            for (int i = 0; i < (int)e.NewValue; i++)
                grid.RowDefinitions.Add(
                    new RowDefinition() { Height = GridLength.Auto });

          
            SetStarRows(grid);
        }

        #endregion

        #region ColumnCount Property

        /// <summary>
        /// Adds the specified number of Columns to ColumnDefinitions. 
        /// Default Width is Auto
        /// </summary>
        public static readonly DependencyProperty ColumnCountProperty =
            DependencyProperty.RegisterAttached(
                "ColumnCount", typeof(int), typeof(GridHelpers),
                new PropertyMetadata(-1, ColumnCountChanged));

        // Get
        public static int GetColumnCount(DependencyObject obj)
        {
            return (int)obj.GetValue(ColumnCountProperty);
        }

        // Set
        public static void SetColumnCount(DependencyObject obj, int value)
        {
            obj.SetValue(ColumnCountProperty, value);
        }

        // Change Event - Add the Columns
        public static void ColumnCountChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || (int)e.NewValue < 0)
                return;

            Grid grid = (Grid)obj;
            grid.ColumnDefinitions.Clear();

            for (int i = 0; i < (int)e.NewValue; i++)
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition() { Width = GridLength.Auto });

            SetStarColumns(grid);

            for (int i = 0; i < (int)e.NewValue; i++)
            {
                System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
                LinearGradientBrush fiveColorLGB = new LinearGradientBrush();
                fiveColorLGB.StartPoint = new Point(1, 0);
                fiveColorLGB.EndPoint = new Point(1, 1);
                // Create and add Gradient stops

                switch (i % 5)
                {
                    case 0:
                        {
                            GradientStop blueGS = new GradientStop();
                            blueGS.Color = Colors.Blue;
                            blueGS.Offset = 0.0;
                          
                            fiveColorLGB.GradientStops.Add(blueGS);

                            GradientStop blueGS2 = new GradientStop();
                            blueGS.Color = Colors.DarkBlue;
                            blueGS.Offset = 1;
                            fiveColorLGB.GradientStops.Add(blueGS2);

                            break;
                        }
                    // Set Fill property of rectangle

                    case 1:
                        {
                            GradientStop blueGS = new GradientStop();
                            blueGS.Color = Colors.Red;
                            blueGS.Offset = 0.0;
                            fiveColorLGB.GradientStops.Add(blueGS);

                            GradientStop blueGS2 = new GradientStop();
                            blueGS.Color = Colors.DarkRed;
                            blueGS.Offset = 1;
                            fiveColorLGB.GradientStops.Add(blueGS2);

                            break;
                        }


                    case 2:
                        {
                            GradientStop blueGS = new GradientStop();
                            blueGS.Color = Colors.Green;
                            blueGS.Offset = 0.0;
                            fiveColorLGB.GradientStops.Add(blueGS);

                            GradientStop blueGS2 = new GradientStop();
                            blueGS.Color = Colors.DarkGreen;
                            blueGS.Offset = 1;
                            fiveColorLGB.GradientStops.Add(blueGS2);

                            break;
                        }


                    case 3:
                        {
                            GradientStop blueGS = new GradientStop();
                            blueGS.Color = Colors.Gold;
                            blueGS.Offset = 0.0;
                            fiveColorLGB.GradientStops.Add(blueGS);

                            GradientStop blueGS2 = new GradientStop();
                            blueGS.Color = Colors.DarkOrange;
                            blueGS.Offset = 1;
                            fiveColorLGB.GradientStops.Add(blueGS2);

                            break;
                        }



                    case 4:
                        {
                            GradientStop blueGS = new GradientStop();
                            blueGS.Color = Colors.Gray;
                            blueGS.Offset = 0.0;
                            fiveColorLGB.GradientStops.Add(blueGS);

                            GradientStop blueGS2 = new GradientStop();
                            blueGS.Color = Colors.DarkSlateGray;
                            blueGS.Offset = 1;
                            fiveColorLGB.GradientStops.Add(blueGS2);

                            break;
                        }
                }

                rect.Fill = fiveColorLGB;
                Grid.SetColumn(rect, i);
                grid.Children.Add(rect);
            }
        }

        #endregion

        #region StarRows Property

        /// <summary>
        /// Makes the specified Row's Height equal to Star. 
        /// Can set on multiple Rows
        /// </summary>
        public static readonly DependencyProperty StarRowsProperty =
            DependencyProperty.RegisterAttached(
                "StarRows", typeof(string), typeof(GridHelpers),
                new PropertyMetadata(string.Empty, StarRowsChanged));

        // Get
        public static string GetStarRows(DependencyObject obj)
        {
            return (string)obj.GetValue(StarRowsProperty);
        }

        // Set
        public static void SetStarRows(DependencyObject obj, string value)
        {
            obj.SetValue(StarRowsProperty, value);
        }

        // Change Event - Makes specified Row's Height equal to Star
        public static void StarRowsChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || string.IsNullOrEmpty(e.NewValue.ToString()))
                return;

            SetStarRows((Grid)obj);
        }

        #endregion

        #region StarColumns Property

        /// <summary>
        /// Makes the specified Column's Width equal to Star. 
        /// Can set on multiple Columns
        /// </summary>
        public static readonly DependencyProperty StarColumnsProperty =
            DependencyProperty.RegisterAttached(
                "StarColumns", typeof(string), typeof(GridHelpers),
                new PropertyMetadata(string.Empty, StarColumnsChanged));

        // Get
        public static string GetStarColumns(DependencyObject obj)
        {
            return (string)obj.GetValue(StarColumnsProperty);
        }

        // Set
        public static void SetStarColumns(DependencyObject obj, string value)
        {
            obj.SetValue(StarColumnsProperty, value);
        }

        // Change Event - Makes specified Column's Width equal to Star
        public static void StarColumnsChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || string.IsNullOrEmpty(e.NewValue.ToString()))
                return;

            SetStarColumns((Grid)obj);
        }

        #endregion

        private static void SetStarColumns(Grid grid)
        {
            string[] Columns =
                GetStarColumns(grid).Split(',');

            string[] starColumns = new string[Columns.Length];
            double[] widthColumns = new double[Columns.Length];
            for (int i = 0; i < Columns.Length; i++)
            {
                if (Columns[i].Contains('*') == false)
                {
                    starColumns[i] = Columns[i];
                    widthColumns[i] = 1;
                }
                else
                {
                    string[] parts = Columns[i].Split('*');
                    starColumns[i] = parts[0];
                    double.TryParse(parts[1], out widthColumns[i]);
                }
            }

            for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                if (starColumns.Contains(i.ToString()))
                    for (int j = 0; j < starColumns.Length; j++)
                    {
                        if (starColumns[j] == i.ToString())
                        {
                            grid.ColumnDefinitions[i].Width =
                                new GridLength(widthColumns[j], GridUnitType.Star);
                        }
                    }
            }
        }

        private static void SetStarRows(Grid grid)
        {
            string[] starRows =
                GetStarRows(grid).Split(',');

            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                if (starRows.Contains(i.ToString()))
                    grid.RowDefinitions[i].Height =
                        new GridLength(1, GridUnitType.Star);
            }
        }
    }
}
