using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace xOPS_Console
{
    public static class AsciiTimeSeries2
    {
        class YLabel
        {
            public double Value { get; set; }
            public string Text { get; set; }
        }

        private static Tuple<string[], char[,]> BuildGraph(IList<double> series, AsciiOptions options = null, double? knownMin = null, double? knownMax = null)
        {
            if (series.Count < 2) return null;

            options = options ?? new AsciiOptions();

            var min = knownMin.HasValue ? knownMin.Value : series.Min();
            var max = knownMax.HasValue ? knownMax.Value : series.Max();
            var count = series.Count;

            var range = Math.Abs(max - min);
            var bucket = range / (options.HeigthLines - 1);
            var rows = options.HeigthLines;

            int maxLabelLength = 0;

            var yAxisLabels = GetYAxisLabels(max, bucket, rows, options, out maxLabelLength);
            var yAxis = CreateYAxis(yAxisLabels, series[0], bucket);

            var labelsAndAxisLength = maxLabelLength + 1;

            var xAreaWidth = count;
            var i = 0;

            if (options.MaxWidthCharacters > 0 && count + labelsAndAxisLength > options.MaxWidthCharacters)
            {
                xAreaWidth = options.MaxWidthCharacters - labelsAndAxisLength;
                i = count - xAreaWidth;
            }

            var plot = CreatePlot(rows, xAreaWidth, options.EmptyChar); //y,x = row, column

            var col = 0;

            for (i = i; i < count - 2; i++)
            {
                var currVal = series[i];
                var nextVal = series[i + 1];

                var curRow = (int)Math.Round((max - currVal) / bucket);
                var nextRow = (int)Math.Round((max - nextVal) / bucket);
                var diff = nextRow - curRow;

                if (curRow == nextRow)
                {
                    plot[curRow, col] = '─';
                }
                else if (curRow < nextRow)
                {
                    plot[curRow, col] = '╮';
                    plot[nextRow, col] = '╰';
                }
                else if (curRow > nextRow)
                {
                    plot[curRow, col] = '╯';
                    plot[nextRow, col] = '╭';
                }

                if (diff > 1)
                {
                    for (var k = curRow + 1; k < nextRow; k++)
                        plot[k, col] = '│';
                }
                if (diff < -1)
                {
                    for (var k = curRow - 1; k > nextRow; k--)
                        plot[k, col] = '│';
                }

                col++;
            }

            return new Tuple<string[], char[,]>(yAxis, plot);
        }

        public static string SeriesToString(IList<double> series, AsciiOptions options = null, double? knownMin = null, double? knownMax = null )
        {
            Debug.Write("Building graph as string...");

            var sw = new Stopwatch();
            sw.Start();

            var axisAndPlot = BuildGraph(series, options, knownMin, knownMax);

            var s =  CombineToString(axisAndPlot.Item1, axisAndPlot.Item2);

            sw.Stop();
            Debug.WriteLine(" Done, time(ms): "+sw.ElapsedMilliseconds);

            return s;
        }

        public static string[] SeriesToLines(IList<double> series, AsciiOptions options = null, double? knownMin = null, double? knownMax = null)
        {
            Debug.Write("Building graph as lines...");

            var sw = new Stopwatch();
            sw.Start();

            var axisAndPlot = BuildGraph(series, options, knownMin, knownMax);

            var lines = CombineToLines(axisAndPlot.Item1, axisAndPlot.Item2);

            sw.Stop();
            Debug.WriteLine(" Done, time(ms): " + sw.ElapsedMilliseconds);

            return lines;
        }

        static double[] GetYAxisTicks(double max, double bucket, int rows)
        {
            var result = new double[rows];

            for (var i = 0; i < rows; i++)
            {
                result[i] = max - i * bucket;
            }

            return result;
        }

        static IList<YLabel> GetYAxisLabels(double max, double bucket, int rows, AsciiOptions options, out int labelMaxLength)
        {
            var yAxisTicks = GetYAxisTicks(max, bucket, rows);
            var labels = new List<YLabel>(yAxisTicks.Length);
            
            foreach (var tick in yAxisTicks)
            {
                var label = new YLabel();
                label.Value = tick;
                label.Text = tick.ToString(options.LabelFormat);
                labels.Add(label);
            }

            labelMaxLength = labels.Max(label => label.Text.Length) + options.LabelLeftMargin;

            var padRight = String.Empty.PadLeft(options.LabelRightMargin);

            foreach (var label in labels)
            {
                label.Text = label.Text.PadLeft(labelMaxLength) + padRight;
            }

            labelMaxLength += options.LabelRightMargin;

            return labels;
        }

        static string[] CreateYAxis(IList<YLabel> yAxisLabels, double firstValue, double bucket)
        {
            var axis = new string[yAxisLabels.Count];

            for (var i = 0; i < yAxisLabels.Count; i++)
            {
                axis[i] = yAxisLabels[i].Text
                    + ((Math.Abs(yAxisLabels[i].Value-firstValue) < bucket/2) ? "┼" : "┤");
            }

            return axis;
        }

        static char[,] CreatePlot(int rows, int cols, char emptyChar)
        {
            var array = new char[rows, cols];

            for (var i = 0; i < rows; i++)
                for (var k = 0; k < cols; k++)
                {
                    array[i,k] = emptyChar;
                }

            return array;
        }

        static string CombineToString(string[] yAxis, char[,] plot)
        {
            if (yAxis.Length != plot.GetUpperBound(0)+1)
                throw new InvalidOperationException("yAxis and plot must have same number of rows");

            var sb = new StringBuilder();

            for (int i = 0; i < yAxis.Length; i++)
            {
                sb.Append(yAxis[i]);
                for (var k = 0; k <= plot.GetUpperBound(1); k++)
                    sb.Append(plot[i, k]); //y,x
                if (i < yAxis.Length-1) sb.AppendLine();
            }

            return sb.ToString();
        }

        static string[] CombineToLines(string[] yAxis, char[,] plot)
        {
            if (yAxis.Length != plot.GetUpperBound(0) + 1)
                throw new InvalidOperationException("yAxis and plot must have same number of rows");

            var lines = new string[yAxis.Length];
            var sb = new StringBuilder();

            for (int i = 0; i < yAxis.Length; i++)  
            {
                sb.Append(yAxis[i]);
                for (var k = 0; k <= plot.GetUpperBound(1); k++)
                    sb.Append(plot[i, k]); //y,x

                lines[i] = sb.ToString();
                sb.Clear();
            }

            return lines;
        }

        public static string MergeTwoGraphs(string[] first, string[] second, string split, int padRight = 0)
        {
            if (first.Length != second.Length)
                throw new InvalidOperationException("frist and second must have same number of elements");

            var sb = new StringBuilder();

            for (int i = 0; i < first.Length; i++)
            {
                sb.Append(first[i]);
                if (first[i].Length < padRight) sb.Append(' ', padRight - first[i].Length);

                sb.Append(second[i]);
                if (second[i].Length < padRight) sb.Append(' ', padRight - second[i].Length);

                if (i < first.Length-1) sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public class AsciiOptions
    {
        public int HeigthLines { get; set; } = 10;

        public int MaxWidthCharacters { get; set; } = 0;

        public char EmptyChar { get; set; } = ' ';

        public string LabelFormat { get; set; } = "0.00";

        public int LabelLeftMargin { get; set; } = 1;

        public int LabelRightMargin { get; set; } = 1;
    }
}