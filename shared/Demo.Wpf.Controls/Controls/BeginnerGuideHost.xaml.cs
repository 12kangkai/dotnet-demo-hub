using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;

namespace Demo.Wpf.Controls.Controls;

public partial class BeginnerGuideHost : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty AnchorProperty =
        DependencyProperty.Register(
            nameof(Anchor),
            typeof(FrameworkElement),
            typeof(BeginnerGuideHost),
            new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(BeginnerGuideHost),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    public static readonly DependencyProperty StepsProperty =
        DependencyProperty.Register(
            nameof(Steps),
            typeof(IEnumerable),
            typeof(BeginnerGuideHost),
            new FrameworkPropertyMetadata(null, OnStepsChanged));

    public static readonly DependencyProperty CurrentIndexProperty =
        DependencyProperty.Register(
            nameof(CurrentIndex),
            typeof(int),
            typeof(BeginnerGuideHost),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCurrentIndexChanged));

    private readonly RelayCommand _previousCommand;
    private readonly RelayCommand _nextCommand;
    private readonly RelayCommand _skipCommand;
    private List<BeginnerGuideStep> _steps = [];
    private bool _isPlacementUpdateQueued;
    private double _highlightLeft;
    private double _highlightTop;
    private double _highlightWidth;
    private double _highlightHeight;
    private double _cardLeft;
    private double _cardTop;
    private bool _hasTarget;

    public BeginnerGuideHost()
    {
        _previousCommand = new RelayCommand(GoPrevious, () => CanGoPrevious);
        _nextCommand = new RelayCommand(GoNext, () => HasSteps);
        _skipCommand = new RelayCommand(Close);

        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        RefreshSteps();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? Completed;

    public FrameworkElement? Anchor
    {
        get => (FrameworkElement?)GetValue(AnchorProperty);
        set => SetValue(AnchorProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public IEnumerable? Steps
    {
        get => (IEnumerable?)GetValue(StepsProperty);
        set => SetValue(StepsProperty, value);
    }

    public int CurrentIndex
    {
        get => (int)GetValue(CurrentIndexProperty);
        set => SetValue(CurrentIndexProperty, value);
    }

    public BeginnerGuideStep? CurrentStep => HasSteps ? _steps[CurrentIndex] : null;

    public ICommand PreviousCommand => _previousCommand;

    public ICommand NextCommand => _nextCommand;

    public ICommand SkipCommand => _skipCommand;

    public bool HasSteps => _steps.Count > 0;

    public bool CanGoPrevious => CurrentIndex > 0;

    public bool HasAccentText => !string.IsNullOrWhiteSpace(CurrentStep?.AccentText);

    public string ProgressText => HasSteps ? $"{CurrentIndex + 1} / {_steps.Count}" : "0 / 0";

    public string PrimaryButtonText => CurrentIndex >= _steps.Count - 1 ? "完成" : "下一步";

    public double HighlightLeft
    {
        get => _highlightLeft;
        private set => SetField(ref _highlightLeft, value);
    }

    public double HighlightTop
    {
        get => _highlightTop;
        private set => SetField(ref _highlightTop, value);
    }

    public double HighlightWidth
    {
        get => _highlightWidth;
        private set => SetField(ref _highlightWidth, value);
    }

    public double HighlightHeight
    {
        get => _highlightHeight;
        private set => SetField(ref _highlightHeight, value);
    }

    public double CardLeft
    {
        get => _cardLeft;
        private set => SetField(ref _cardLeft, value);
    }

    public double CardTop
    {
        get => _cardTop;
        private set => SetField(ref _cardTop, value);
    }

    public bool HasTarget
    {
        get => _hasTarget;
        private set => SetField(ref _hasTarget, value);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BeginnerGuideHost host && (bool)e.NewValue)
        {
            host.ClampCurrentIndex();
            host.QueuePlacementUpdate();
        }
    }

    private static void OnStepsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BeginnerGuideHost host)
        {
            host.RefreshSteps();
        }
    }

    private static void OnCurrentIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BeginnerGuideHost host)
        {
            host.ClampCurrentIndex();
            host.RefreshState();
            host.QueuePlacementUpdate();
        }
    }

    private void RefreshSteps()
    {
        _steps = Steps?.OfType<BeginnerGuideStep>().ToList() ?? [];
        ClampCurrentIndex();
        RefreshState();
    }

    private void ClampCurrentIndex()
    {
        int maxIndex = Math.Max(0, _steps.Count - 1);
        int clampedIndex = Math.Clamp(CurrentIndex, 0, maxIndex);

        if (clampedIndex != CurrentIndex)
        {
            SetCurrentValue(CurrentIndexProperty, clampedIndex);
        }
    }

    private void RefreshState()
    {
        RaisePropertyChanged(nameof(CurrentStep));
        RaisePropertyChanged(nameof(HasSteps));
        RaisePropertyChanged(nameof(CanGoPrevious));
        RaisePropertyChanged(nameof(HasAccentText));
        RaisePropertyChanged(nameof(ProgressText));
        RaisePropertyChanged(nameof(PrimaryButtonText));

        _previousCommand.NotifyCanExecuteChanged();
        _nextCommand.NotifyCanExecuteChanged();
    }

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        RaisePropertyChanged(propertyName);
        return true;
    }

    private void GoPrevious()
    {
        if (CurrentIndex > 0)
        {
            CurrentIndex--;
        }
    }

    private void GoNext()
    {
        if (!HasSteps)
        {
            Close();
            return;
        }

        if (CurrentIndex >= _steps.Count - 1)
        {
            Close();
            Completed?.Invoke(this, EventArgs.Empty);
            return;
        }

        CurrentIndex++;
    }

    private void Close()
    {
        IsOpen = false;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LayoutUpdated += OnLayoutUpdated;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        LayoutUpdated -= OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (IsOpen)
        {
            QueuePlacementUpdate();
        }
    }

    private void OnOverlayCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        QueuePlacementUpdate();
    }

    private void OnGuideCardSizeChanged(object sender, SizeChangedEventArgs e)
    {
        QueuePlacementUpdate();
    }

    private void QueuePlacementUpdate()
    {
        if (!IsOpen || _isPlacementUpdateQueued)
        {
            return;
        }

        _isPlacementUpdateQueued = true;

        Dispatcher.BeginInvoke(
            () =>
            {
                _isPlacementUpdateQueued = false;
                UpdatePlacement();
            },
            DispatcherPriority.Loaded);
    }

    private void UpdatePlacement()
    {
        if (!IsOpen || PART_OverlayCanvas.ActualWidth <= 0 || PART_OverlayCanvas.ActualHeight <= 0)
        {
            return;
        }

        FrameworkElement? target = ResolveTargetElement();
        Rect targetRect = GetTargetRect(target);
        const double highlightPadding = 8;

        HasTarget = target is not null;
        HighlightLeft = Math.Max(12, targetRect.Left - highlightPadding);
        HighlightTop = Math.Max(12, targetRect.Top - highlightPadding);
        HighlightWidth = Math.Max(48, targetRect.Width + highlightPadding * 2);
        HighlightHeight = Math.Max(40, targetRect.Height + highlightPadding * 2);

        Size cardSize = GetCardSize();
        Point cardPoint = ChooseCardPoint(targetRect, cardSize);

        CardLeft = cardPoint.X;
        CardTop = cardPoint.Y;
    }

    private FrameworkElement? ResolveTargetElement()
    {
        string? targetName = CurrentStep?.TargetName;

        if (string.IsNullOrWhiteSpace(targetName))
        {
            return Anchor;
        }

        object? target = Anchor?.FindName(targetName);

        if (target is not FrameworkElement)
        {
            target = Window.GetWindow(Anchor)?.FindName(targetName);
        }

        return target as FrameworkElement ?? Anchor;
    }

    private Rect GetTargetRect(FrameworkElement? target)
    {
        if (target is null || !target.IsVisible || target.ActualWidth <= 0 || target.ActualHeight <= 0)
        {
            return new Rect(
                PART_OverlayCanvas.ActualWidth / 2 - 24,
                PART_OverlayCanvas.ActualHeight / 2 - 20,
                48,
                40);
        }

        try
        {
            GeneralTransform transform = target.TransformToVisual(PART_OverlayCanvas);
            return transform.TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
        }
        catch (InvalidOperationException)
        {
            return new Rect(
                PART_OverlayCanvas.ActualWidth / 2 - 24,
                PART_OverlayCanvas.ActualHeight / 2 - 20,
                48,
                40);
        }
    }

    private Size GetCardSize()
    {
        double width = PART_GuideCard.ActualWidth > 0 ? PART_GuideCard.ActualWidth : 420;
        double height = PART_GuideCard.ActualHeight > 0 ? PART_GuideCard.ActualHeight : 260;

        return new Size(width, height);
    }

    private Point ChooseCardPoint(Rect targetRect, Size cardSize)
    {
        const double gap = 18;
        const double edge = 18;
        double canvasWidth = PART_OverlayCanvas.ActualWidth;
        double canvasHeight = PART_OverlayCanvas.ActualHeight;

        double rightX = targetRect.Right + gap;
        double leftX = targetRect.Left - cardSize.Width - gap;
        double belowY = targetRect.Bottom + gap;
        double aboveY = targetRect.Top - cardSize.Height - gap;

        double x;
        double y;

        if (rightX + cardSize.Width <= canvasWidth - edge)
        {
            x = rightX;
            y = targetRect.Top;
        }
        else if (leftX >= edge)
        {
            x = leftX;
            y = targetRect.Top;
        }
        else if (belowY + cardSize.Height <= canvasHeight - edge)
        {
            x = targetRect.Left;
            y = belowY;
        }
        else
        {
            x = targetRect.Left;
            y = aboveY;
        }

        x = Math.Clamp(x, edge, Math.Max(edge, canvasWidth - cardSize.Width - edge));
        y = Math.Clamp(y, edge, Math.Max(edge, canvasHeight - cardSize.Height - edge));

        return new Point(x, y);
    }
}
