/*  Copyright © 2021-2024, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of flowOSD.
 *
 *  flowOSD is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  flowOSD is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with flowOSD. If not, see <https://www.gnu.org/licenses/>.   
 *
 */

namespace flowOSD.UI.Controls;

using System;
using flowOSD.Core.Hardware;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

[TemplatePart(Name = "CANVAS_PART", Type = typeof(Canvas))]
public class FanCurveTuner : Control
{
    #region CoordinateGridBrush

    public static readonly DependencyProperty CoordinateGridBrushProperty = DependencyProperty.Register(
        nameof(CoordinateGridBrush),
        typeof(Brush),
        typeof(FanCurveTuner),
        new PropertyMetadata(null, OnCoordinateGridBrushPropertyChanged));

    private static void OnCoordinateGridBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FanCurveTuner tuner && e.NewValue is Brush brush)
        {
            foreach (var line in tuner.temperatureLines)
            {
                line.Stroke = brush;
            }

            foreach (var line in tuner.valueLines)
            {
                line.Stroke = brush;
            }
        }
    }

    public Brush CoordinateGridBrush
    {
        get => (Brush)GetValue(CoordinateGridBrushProperty);
        set => SetValue(CoordinateGridBrushProperty, value);
    }

    #endregion

    #region CoordinateGridThickness

    public static readonly DependencyProperty CoordinateGridThicknessProperty = DependencyProperty.Register(
        nameof(CoordinateGridThickness),
        typeof(double),
        typeof(FanCurveTuner),
        new PropertyMetadata(new Thickness(1), OnCoordinateGridThicknessPropertyChanged));

    private static void OnCoordinateGridThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FanCurveTuner tuner && e.NewValue is double value)
        {
            foreach (var line in tuner.temperatureLines)
            {
                line.StrokeThickness = value;
            }

            foreach (var line in tuner.valueLines)
            {
                line.StrokeThickness = value;
            }
        }
    }

    public double CoordinateGridThickness
    {
        get => (double)GetValue(CoordinateGridThicknessProperty);
        set => SetValue(CoordinateGridThicknessProperty, value);
    }

    #endregion

    #region CoordinateGridLabelBrush

    public static readonly DependencyProperty CoordinateGridLabelBrushProperty = DependencyProperty.Register(
        nameof(CoordinateGridLabelBrush),
        typeof(Brush),
        typeof(FanCurveTuner),
        new PropertyMetadata(null, OnCoordinateGridLabelBrushPropertyChanged));

    private static void OnCoordinateGridLabelBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FanCurveTuner tuner && e.NewValue is Brush brush)
        {
            foreach (var line in tuner.temperatureLabels)
            {
                line.Foreground = brush;
            }

            foreach (var line in tuner.valueLabels)
            {
                line.Foreground = brush;
            }
        }
    }

    public Brush CoordinateGridLabelBrush
    {
        get => (Brush)GetValue(CoordinateGridLabelBrushProperty);
        set => SetValue(CoordinateGridLabelBrushProperty, value);
    }

    #endregion

    #region LineBrush

    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
        nameof(LineBrush),
        typeof(Brush),
        typeof(FanCurveTuner),
        new PropertyMetadata(null, OnLineBrushPropertyChanged));

    private static void OnLineBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FanCurveTuner tuner && e.NewValue is Brush brush)
        {
            tuner.dataLine.Stroke = brush;

            foreach (var t in tuner.thumbs)
            {
                t.Background = brush;
                t.Foreground = brush;
            }
        }
    }

    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    #endregion

    #region DisabledLineBrush

    public static readonly DependencyProperty DisabledLineBrushProperty = DependencyProperty.Register(
        nameof(DisabledLineBrush),
        typeof(Brush),
        typeof(FanCurveTuner),
        new PropertyMetadata(null, OnDisabledLineBrushPropertyChanged));

    private static void OnDisabledLineBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FanCurveTuner tuner && !tuner.IsEnabled && e.NewValue is Brush brush)
        {
            tuner.UpdateState();
        }
    }

    public Brush DisabledLineBrush
    {
        get => (Brush)GetValue(DisabledLineBrushProperty);
        set => SetValue(DisabledLineBrushProperty, value);
    }

    #endregion

    #region LineThickness

    public static readonly DependencyProperty LineThicknessProperty = DependencyProperty.Register(
        nameof(LineThickness),
        typeof(double),
        typeof(FanCurveTuner),
        new PropertyMetadata(new Thickness(2), OnLineThicknessPropertyChanged));

    private static void OnLineThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FanCurveTuner tuner && e.NewValue is double value)
        {
            tuner.dataLine.StrokeThickness = value;
        }
    }

    public double LineThickness
    {
        get => (double)GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    #endregion

    #region Title

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(FanCurveTuner),
        new PropertyMetadata(""));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    #endregion

    private List<Thumb> thumbs = new List<Thumb>();
    private Polyline dataLine = new Polyline();

    private List<Line> temperatureLines = new List<Line>(), valueLines = new List<Line>();
    private List<TextBlock> temperatureLabels = new List<TextBlock>(), valueLabels = new List<TextBlock>();

    private CurvePoint? current;

    public FanCurveTuner()
    {
        DataContextChanged += OnDataContextChanged;
        IsEnabledChanged += OnIsEnabledChanged;
    }

    private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateState();
    }

    private void UpdateState()
    {
        dataLine.Stroke = IsEnabled ? LineBrush : DisabledLineBrush;

        foreach (var t in thumbs)
        {
            t.IsEnabled = IsEnabled;

            t.Background = IsEnabled ? LineBrush : DisabledLineBrush;
            t.Foreground = IsEnabled ? LineBrush : DisabledLineBrush;
        }
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataSource != null)
        {
            DataSource.Changed -= DataSource_Changed;
        }

        DataSource = args.NewValue as FanCurveDataSource;

        if (DataSource != null)
        {
            DataSource.Changed += DataSource_Changed;
        }

        Init();
    }

    private Canvas? Canvas { get; set; }

    private FanCurveDataSource? DataSource { get; set; }

    private double ThumbRadius { get; } = 10;

    private int MinTemperature { get; } = 20;

    private int MaxTemperature { get; } = 100;

    protected override void OnApplyTemplate()
    {
        if (Canvas != null)
        {
            Canvas.SizeChanged -= Canvas_SizeChanged;
        }

        Canvas = GetTemplateChild("CANVAS_PART") as Canvas;

        if (Canvas != null)
        {
            Canvas.SizeChanged += Canvas_SizeChanged;
        }

        base.OnApplyTemplate();

        InitAxes();
        Init();
    }

    private void DataSource_Changed(object? sender, EventArgs e)
    {
        UpdatePositions();
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateAxesPositions();

        UpdatePositions();
    }

    public void Init()
    {
        if (Canvas == null)
        {
            return;
        }

        dataLine.Points.Clear();
        if (!Canvas.Children.Contains(dataLine))
        {
            Canvas.Children.Add(dataLine);
        }

        foreach (var t in thumbs)
        {
            Canvas.Children.Remove(t);
        }

        thumbs.Clear();

        if (DataSource == null)
        {
            return;
        }

        // Curve

        dataLine.IsHitTestVisible = false;
        dataLine.Stroke = LineBrush;
        dataLine.StrokeThickness = LineThickness;

        // Thumbs

        while (thumbs.Count < (DataSource?.Count ?? 0))
        {
            var thumb = new Thumb();
            thumb.DragStarted += OnDragStarted;
            thumb.DragDelta += OnDragDelta;
            thumb.DragCompleted += OnDragCompleted;

            thumb.Width = ThumbRadius * 2;
            thumb.Height = ThumbRadius * 2;

            thumb.Visibility = Visibility.Visible;
            thumb.CornerRadius = new CornerRadius(ThumbRadius);
            thumb.Background = LineBrush;
            thumb.Foreground = LineBrush;

            thumbs.Add(thumb);
            Canvas.Children.Add(thumb);

            dataLine.Points.Add(new Windows.Foundation.Point(0, 0));
        }

        UpdatePositions();
        UpdateState();
    }

    private void UpdatePositions()
    {
        if (thumbs.Count == 0 || DataSource == null)
        {
            return;
        }

        for (var i = 0; i < DataSource.Count; i++)
        {
            if (thumbs[i].IsDragging)
            {
                continue;
            }

            var point = ToCurvePoint(DataSource[i]);
            thumbs[i].Tag = DataSource[i];

            Canvas.SetLeft(thumbs[i], point.X - ThumbRadius);
            Canvas.SetTop(thumbs[i], point.Y - ThumbRadius);

            dataLine.Points[i] = new Windows.Foundation.Point(point.X, point.Y);
        }
    }

    private void InitAxes()
    {
        if (Canvas == null)
        {
            return;
        }

        Canvas.Children.Clear();

        var temperatureLinesCount = (MaxTemperature - MinTemperature) / 10;

        for (var i = 0; i < temperatureLinesCount * 2 + 1; i++)
        {
            var line = new Line();
            line.Stroke = CoordinateGridBrush;
            line.StrokeThickness = CoordinateGridThickness;
            line.IsHitTestVisible = false;

            temperatureLines.Add(line);
            Canvas.Children.Add(line);

            if (i % 2 == 0)
            {
                var label = new TextBlock();
                label.Text = $"{MinTemperature + (i / 2f) * 10}°";
                label.Foreground = CoordinateGridLabelBrush;
                label.FontSize = 10;
                label.Padding = new Thickness(0, 10, 0, 0);
                temperatureLabels.Add(label);
                Canvas.Children.Add(label);
            }
        }

        for (var i = 0; i <= 10; i++)
        {
            var line = new Line();
            line.Stroke = CoordinateGridBrush;
            line.StrokeThickness = CoordinateGridThickness;

            valueLines.Add(line);
            Canvas.Children.Add(line);

            //   if (i > 0)
            {
                var label = new TextBlock();
                label.Text = $"{i * 10}%";
                label.Foreground = CoordinateGridLabelBrush;
                label.FontSize = 10;
                label.Padding = new Thickness(0, 0, 10, 0);
                valueLabels.Add(label);
                Canvas.Children.Add(label);
            }
        }
    }

    private void UpdateAxesPositions()
    {
        if (Canvas == null)
        {
            return;
        }

        var size = new Windows.Foundation.Size(Canvas.ActualWidth, Canvas.ActualHeight);

        // Temperature

        float dTemperature = (MaxTemperature - MinTemperature);

        for (var i = 0; i < temperatureLines.Count; i++)
        {
            var line = temperatureLines[i];

            var x = GetTemperatureX(MinTemperature + Convert.ToInt32(Math.Round(i / 2f * 10)));

            line.X1 = x;
            line.Y1 = 0;

            line.X2 = x;
            line.Y2 = Canvas.ActualHeight;
        }

        for (var i = 0; i < temperatureLabels.Count; i++)
        {
            var label = temperatureLabels[i];
            label.Measure(size);

            var x = GetTemperatureX(MinTemperature + i * 10) - label.ActualWidth / 2;
            var y = Canvas.ActualHeight;

            Canvas.SetLeft(label, x);
            Canvas.SetTop(label, y);
        }

        // Fan %

        for (var i = 0; i < valueLines.Count; i++)
        {
            var line = valueLines[i];

            line.X1 = 0;
            line.Y1 = Canvas.ActualHeight * (i / 10f); // 10% step;

            line.X2 = Canvas.ActualWidth;
            line.Y2 = Canvas.ActualHeight * (i / 10f); // 10% step;
        }

        valueLabels[0].Measure(size);
        var isTiny = Canvas.ActualHeight / 10 < 1.5 * valueLabels[0].ActualHeight;

        for (var i = 0; i < valueLabels.Count; i++)
        {
            var label = valueLabels[i];
            label.Measure(size);
            label.Visibility = isTiny && i % 2 == 0 ? Visibility.Collapsed : Visibility.Visible;

            var x = -label.ActualWidth;
            var y = Canvas.ActualHeight * ((10 - i) / 10f) - label.ActualHeight / 2;

            Canvas.SetLeft(label, x);
            Canvas.SetTop(label, y);
        }
    }

    private void OnDragStarted(object sender, DragStartedEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            current = new CurvePoint()
            {
                X = Canvas.GetLeft(thumb) + ThumbRadius,
                Y = Canvas.GetTop(thumb) + ThumbRadius
            };
        }
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is Thumb thumb && current != null)
        {
            current.X += e.HorizontalChange;
            current.Y += e.VerticalChange;

            SetLocation(thumb, current.X, current.Y);
        }
    }

    private void SetLocation(Thumb thumb, double x, double y)
    {
        var index = thumbs.IndexOf(thumb);
        if (index == -1 || Canvas == null || DataSource == null)
        {
            return;
        }

        var minX = index == 0 
            ? 0 
            : GetTemperatureX(DataSource[index - 1].Temperature + DataSource.GridSize);
        var maxX = index == DataSource.Count - 1
            ? Canvas.ActualWidth
            : GetTemperatureX(DataSource[index + 1].Temperature - DataSource.GridSize);

        var minY = 0;
        var maxY = Canvas.ActualHeight;

        var left = Math.Min(maxX, Math.Max(minX, x)) - ThumbRadius;
        var top = Math.Min(maxY, Math.Max(minY, y)) - ThumbRadius;

        Canvas.SetLeft(thumb, left);
        Canvas.SetTop(thumb, top);

        dataLine.Points[index] = new Windows.Foundation.Point(left + ThumbRadius, top + ThumbRadius);

        var current = ToDataPoint(
                    Canvas.GetLeft(thumb) + ThumbRadius,
                    Canvas.GetTop(thumb) + ThumbRadius);

        for (var i = 0; i < index; i++)
        {
            if (DataSource[i].Value > current.Value)
            {
                DataSource[i] = new FanDataPoint(DataSource[i].Temperature, current.Value);
            }
        }

        for (var i = index + 1; i < DataSource.Count; i++)
        {
            if (DataSource[i].Value < current.Value)
            {
                DataSource[i] = new FanDataPoint(DataSource[i].Temperature, current.Value);
            }
        }
    }

    private void OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (sender is Thumb thumb && DataSource != null)
        {
            var index = thumbs.IndexOf(thumb);
            if (index > -1)
            {
                DataSource[index] = ToDataPoint(
                    Canvas.GetLeft(thumb) + ThumbRadius,
                    Canvas.GetTop(thumb) + ThumbRadius);
            }

            current = null;
        }
    }

    private FanDataPoint ToDataPoint(double x, double y)
    {
        float d = MaxTemperature - MinTemperature;

        return new FanDataPoint(
            (byte)Math.Round(MinTemperature + (x / Canvas!.ActualWidth) * d),
            (byte)Math.Round((1 - y / Canvas!.ActualHeight) * 100));
    }

    private CurvePoint ToCurvePoint(FanDataPoint dataPoint)
    {
        float d = MaxTemperature - MinTemperature;

        return new CurvePoint(
            Canvas!.ActualWidth * (dataPoint.Temperature - MinTemperature) / d,
            Canvas!.ActualHeight * (1.0 - dataPoint.Value / 100f));
    }

    private float GetTemperatureX(int temperature)
    {
        float d = MaxTemperature - MinTemperature;

        return Convert.ToSingle(Canvas!.ActualWidth * (temperature - MinTemperature) / d);
    }

    private float GetValueY(int value)
    {
        return Convert.ToSingle(Canvas!.ActualHeight * (1.0 - value / 100f));
    }

    private sealed class CurvePoint
    {
        public CurvePoint()
            : this(0, 0)
        { }

        public CurvePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }

        public double Y { get; set; }
    }
}
