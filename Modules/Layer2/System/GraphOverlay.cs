// Developer: heaplyn
// Date: 2026-08-17
// Summary: High-performance function plotter for the Calculus Studio.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Linq;

namespace HeaplitLauncher
{
    public class GraphOverlay : BaseOverlay
    {
        private readonly Canvas _canvas;
        private string _equation = "";

        public GraphOverlay(string equation) : base($"GRAPH: {equation}", 600, 600)
        {
            _equation = equation;
            _canvas = new Canvas { Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), Margin = new Thickness(10) };
            this.UserContent = _canvas;
            this.Loaded += (s, e) => Plot();
        }

        private void Plot()
        {
            _canvas.Children.Clear();
            double w = _canvas.ActualWidth;
            double h = _canvas.ActualHeight;
            if (w == 0 || h == 0) return;

            // Draw Axes
            var xAxis = new Line { X1 = 0, Y1 = h / 2, X2 = w, Y2 = h / 2, Stroke = Brushes.Gray, StrokeThickness = 1 };
            var yAxis = new Line { X1 = w / 2, Y1 = 0, X2 = w / 2, Y2 = h, Stroke = Brushes.Gray, StrokeThickness = 1 };
            _canvas.Children.Add(xAxis); _canvas.Children.Add(yAxis);

            // Simple Plotting (x from -10 to 10)
            var points = new List<Point>();
            double scale = 30; // pixels per unit
            for (double x = -10; x <= 10; x += 0.1)
            {
                string expr = _equation.Replace("x", $"({x})");
                string res = CoreRegistry.Intelligence.Math.Evaluate(expr);
                if (double.TryParse(res, out double y))
                {
                    double screenX = (w / 2) + (x * scale);
                    double screenY = (h / 2) - (y * scale);
                    if (screenY >= 0 && screenY <= h) points.Add(new Point(screenX, screenY));
                }
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                var line = new Line { X1 = points[i].X, Y1 = points[i].Y, X2 = points[i+1].X, Y2 = points[i+1].Y, Stroke = Brushes.Cyan, StrokeThickness = 2 };
                _canvas.Children.Add(line);
            }
        }
    }
}
