using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace xOPS_Console
{

    public class AsciiTimeSeries
    {
        /// <summary>
        /// Total height (outter) of the chart in lines
        /// </summary>
        public int HeigthLines { get; set; } = 10;

        /// <summary>
        /// Total width (outter) of the chart in characters/columns (PlotAreaWidth + YAxisAndLabelsWidth), unused columns are filled with EmptyChar
        /// 0 - width is determined by the number of data point in the chart, the chart can grow infinitely.
        /// If the width is > 0 (it's constrained) and number of data points is gtreater than plt can fit, only tail of the collection will be displayed.
        /// </summary>
        public int WidthCharacters { get; set; } = 0;

        /// <summary>
        /// The witdh of vertical Y axis determined by values/sizes of ticks, their format (LabelFormat) and margins (YLabelLeftPadding, YLabelRightPadding).
        /// Essentialy it is the maximum with of padded tick label + 1 for the axis line
        /// </summary>
        public int YAxisAndLabelsWidth { get; private set; }

        /// <summary>
        /// The number of columns in plot area. It is determined as the difference of WidthCharacters-YAxisAndLabelsWidth (if the width is limited)
        /// or grows with Series/SeriesModifiable number of items
        /// </summary>
        public int PlotAreaWidth { get; private set; }

        /// <summary>
        /// PlotAreaWidth+1. The number of data points that are visible/can fit in the plot area is greater by 1 than the number of columns.
        /// If WidthCharacters is set this shows how many points can fit, otherwise how many data points there're now
        /// </summary>
        public int DataPoints => PlotAreaWidth + 1;

        public char EmptyChar { get; set; } = ' ';

        public char AbovePointChar { get; set; } = ' ';

        public char BelowPointChar { get; set; } = ' ';

        public string LabelFormat { get; set; } = "0.00";

        public int YLabelLeftPadding { get; set; } = 1;

        public int YLabelRightPadding { get; set; } = 1;

        public double? Min { get; set; }

        public double? Max { get; set; }

        double? prevMin, prevMax;
        int? prevPlotWidth, prevHeight, prevCount;

        IEnumerable<double> series;

        /// <summary>
        /// Simplest way to build chart, though unlike SeriesModifiable not good for collections which can be modified in another thread, also doesn't support diff rerendering
        /// </summary>
        public IEnumerable<double> Series
        {
            get => series;
            set
            {
                series = value;
                seriesModifiable = null;
            }
        }

        IList<double> seriesModifiable;

        /// <summary>
        /// Use in multi-threaded environment if the series can be changed in other threads while the graph is being built (and avoid collection chamged exception in foreach)
        /// </summary>
        public IList<double> SeriesModifiable
        {
            get => seriesModifiable;
            set
            {
                seriesModifiable = value;
                series = null;
            }
        }

        string[] yAxis;

        char[,] plot;

        int labelsAndAxisLength = 0;

        void BuildGraph()
        {
            if (Series == null && SeriesModifiable == null) return;

            IEnumerable<double> series = Series != null ? Series : SeriesModifiable;
            var count = Series != null ? Series.Count() : SeriesModifiable.Count;

            if (count < 2) return;

            var min = Min.HasValue ? Min.Value : series.Min();
            var max = Max.HasValue ? Max.Value : series.Max();

            var range = Math.Abs(max - min);
            var bucket = range / (HeigthLines - 1);
            var yAxisChanged = false;

            if (yAxis == null || prevMin != min || prevMax != max ||  prevHeight != HeigthLines)
            {
                BuildYAxis(min, max, bucket, HeigthLines, out labelsAndAxisLength);
                prevMin = min;
                prevMax = max;
                prevHeight = HeigthLines;
                yAxisChanged = true;
            }

            PlotAreaWidth = count;
            var startAt = 0;
            var i = 0;
            var col = 0;

            if (WidthCharacters > 0)
            {
                PlotAreaWidth = WidthCharacters - labelsAndAxisLength;

                if (count > PlotAreaWidth+2)
                {
                    startAt = count - PlotAreaWidth-2;
                }
            }

            if (plot == null || prevPlotWidth != PlotAreaWidth || Series != null || yAxisChanged) // build the plot from scratch
            {
                BuildPlot(HeigthLines, PlotAreaWidth); //y,x = row, column
                prevPlotWidth = PlotAreaWidth;
            }
            else if (prevCount.HasValue && startAt > 0) // try to shift existing plot and render only the diff, works only with SeriesModifiable
            {
                var diff = count - prevCount.Value;
                ShiftColumnsLeft(plot, diff);
                startAt = count - 2 - diff;
                col = PlotAreaWidth  - diff;
            }

            prevCount = count;

            if (Series != null)
            {
                var enumarator = series.GetEnumerator();

                while (i < count - 2)
                {
                    if (i >= startAt)
                    {
                        var currVal = enumarator.Current;
                        enumarator.MoveNext();
                        var nextVal = enumarator.Current;

                        ConnectDots(max, bucket, col, currVal, nextVal);

                        col++;
                        i++;
                    }
                    else
                    {
                        enumarator.MoveNext();
                        i++;
                    }
                }
            }
            else
            {
                for (i = startAt; i < count - 2; i++)
                {
                    var currVal = SeriesModifiable[i];
                    var nextVal = SeriesModifiable[i+1];

                    ConnectDots(max, bucket, col, currVal, nextVal);

                    col++;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConnectDots(double max, double bucket, int col, double currVal, double nextVal)
        {
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


            for (var k = Math.Max(curRow, nextRow) + 1; k < HeigthLines; k++)
                plot[k, col] = BelowPointChar;

            for (var k = Math.Min(curRow, nextRow) - 1; k >= 0; k--)
                plot[k, col] = AbovePointChar;
        }

        double[] GetYAxisTicks(double max, double bucket, int rows)
        {
            var result = new double[rows];

            for (var i = 0; i < rows; i++)
            {
                result[i] = max - i * bucket;
            }

            return result;
        }

        void BuildYAxis(double min, double max, double bucket, int rows, out int labelMaxLength)
        {
            var yAxisTicks = GetYAxisTicks(max, bucket, rows);

            if (yAxis == null || prevHeight != HeigthLines)
                yAxis = new string[HeigthLines];

            labelMaxLength = max.ToString(LabelFormat).Length;

            if (min.ToString(LabelFormat).Length > labelMaxLength) labelMaxLength = min.ToString(LabelFormat).Length;

            labelMaxLength += YLabelLeftPadding;

            var padRight = String.Empty.PadLeft(YLabelRightPadding);

            for (int i = 0; i < yAxis.Length; i++)
            {
                yAxis[i] = yAxisTicks[i].ToString(LabelFormat).PadLeft(labelMaxLength)+ padRight+ "┤";
            }

            labelMaxLength += YLabelRightPadding + 1;
        }

        void BuildPlot(int rows, int cols)
        {
            if (plot == null || plot.GetUpperBound(0)+1 != rows || plot.GetUpperBound(1)+1 != cols)     
                plot  = new char[rows, cols];

            for (var i = 0; i < rows; i++)
                for (var k = 0; k < cols; k++)
                {
                    plot[i, k] = EmptyChar;
                }
        }

        void ShiftColumnsLeft(char[,] plot, int n)
        {
            for (var i = 0; i < plot.GetUpperBound(0) + 1; i++)
            {
                for (var k = 0; k < plot.GetUpperBound(1) - n + 1; k++)
                {
                    plot[i, k] = plot[i, k + n];
                }
                for (var k = plot.GetUpperBound(1) - n + 1; k < plot.GetUpperBound(1) + 1; k++)
                {
                    plot[i, k] = EmptyChar;
                }
            }
        }

        string CombineToString(string[] yAxis, char[,] plot)
        {
            if (yAxis.Length != plot.GetUpperBound(0) + 1)
                throw new InvalidOperationException("yAxis and plot must have same number of rows");

            var sb = new StringBuilder();

            for (int i = 0; i < yAxis.Length; i++)
            {
                sb.Append(yAxis[i]);
                for (var k = 0; k <= plot.GetUpperBound(1); k++)
                    sb.Append(plot[i, k]); //y,x
                if (i < yAxis.Length - 1) sb.AppendLine();
            }

            return sb.ToString();
        }

        string[] CombineToLines(string[] yAxis, char[,] plot)
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

        public string RenderToString()
        {
            Debug.Write("Building graph as string...");

            var sw = new Stopwatch();
            sw.Start();

            BuildGraph();

            var s = CombineToString(yAxis, plot);

            sw.Stop();
            Debug.WriteLine(" Done, time(ms): " + sw.ElapsedMilliseconds);

            return s;
        }

        public string[] RenderToLines()
        {
            Debug.Write("Building graph as lines...");

            var sw = new Stopwatch();
            sw.Start();

            BuildGraph();

            var lines = CombineToLines(yAxis, plot);

            sw.Stop();
            Debug.WriteLine(" Done, time(ms): " + sw.ElapsedMilliseconds);

            return lines;
        }

        public static string MergeTwoGraphs(AsciiTimeSeries first, AsciiTimeSeries second, string split, int padRight = 0)
        {
            var lines1 = first.RenderToLines();
            var lines2 = second.RenderToLines();

            if (lines1.Length != lines2.Length)
                throw new InvalidOperationException("frist and second must have same number of elements");

            var sb = new StringBuilder();

            for (int i = 0; i < first.yAxis.Length; i++)
            {
                sb.Append(lines1[i]);
                if (lines1[i].Length < padRight) sb.Append(' ', padRight - lines1[i].Length);

                sb.Append(split);

                sb.Append(lines2[i]);
                if (lines2[i].Length < padRight) sb.Append(' ', padRight - lines2[i].Length);

                if (i < lines1.Length - 1) sb.AppendLine();
            }

            return sb.ToString();
        }

    }
}