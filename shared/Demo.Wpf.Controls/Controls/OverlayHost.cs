using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Demo.Wpf.Controls.Controls;

/// <summary>
/// Hosts arbitrary WPF content as an in-tree overlay above the panel that contains the configured anchor.
/// The control is designed for WPF-UI ContentDialog scenarios where a secondary editor must cover the
/// current dialog/window without using Popup or nesting another ContentDialog.
/// </summary>
[TemplatePart(Name = HiddenPresenterPartName, Type = typeof(ContentPresenter))]
public sealed class OverlayHost : ContentControl
{
    private const string HiddenPresenterPartName = "PART_HiddenPresenter";

    private static readonly DependencyProperty IsOverlayBoundaryProperty =
        DependencyProperty.RegisterAttached(
            "IsOverlayBoundary",
            typeof(bool),
            typeof(OverlayHost),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty AnchorProperty =
        DependencyProperty.Register(
            nameof(Anchor),
            typeof(FrameworkElement),
            typeof(OverlayHost),
            new FrameworkPropertyMetadata(null, OnAnchorChanged));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(OverlayHost),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsOpenChanged));

    public static readonly DependencyProperty OverlayBackgroundProperty =
        DependencyProperty.Register(
            nameof(OverlayBackground),
            typeof(Brush),
            typeof(OverlayHost),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(112, 0, 0, 0)), OnOverlayVisualPropertyChanged));

    public static readonly DependencyProperty DisposeContentOnCloseProperty =
        DependencyProperty.Register(
            nameof(DisposeContentOnClose),
            typeof(bool),
            typeof(OverlayHost),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty DisposeContentOnUnloadProperty =
        DependencyProperty.Register(
            nameof(DisposeContentOnUnload),
            typeof(bool),
            typeof(OverlayHost),
            new FrameworkPropertyMetadata(false));

    private Border? _overlayRoot;
    private ContentPresenter? _overlayPresenter;
    private FrameworkElement? _subscribedAnchor;
    private Panel? _hostPanel;
    private object? _detachedContent;
    private bool _hasDetachedContent;
    private bool _isOpeningOrClosing;

    static OverlayHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(OverlayHost), new FrameworkPropertyMetadata(typeof(OverlayHost)));
        VisibilityProperty.OverrideMetadata(typeof(OverlayHost), new FrameworkPropertyMetadata(Visibility.Collapsed));
        FocusableProperty.OverrideMetadata(typeof(OverlayHost), new FrameworkPropertyMetadata(false));
    }

    public OverlayHost()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    /// <summary>
    /// A visual element inside the window or WPF-UI ContentDialog that the overlay should cover.
    /// When omitted, the OverlayHost itself is used as the anchor.
    /// </summary>
    public FrameworkElement? Anchor
    {
        get => (FrameworkElement?)GetValue(AnchorProperty);
        set => SetValue(AnchorProperty, value);
    }

    /// <summary>
    /// Opens or closes the overlay. This property is MVVM-friendly and binds two-way by default.
    /// </summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    /// Background brush applied to the generated overlay root.
    /// </summary>
    public Brush OverlayBackground
    {
        get => (Brush)GetValue(OverlayBackgroundProperty);
        set => SetValue(OverlayBackgroundProperty, value);
    }

    /// <summary>
    /// Disposes Content when Close is called if the content implements IDisposable.
    /// Default is false because XAML-declared views are commonly reused across open/close cycles.
    /// </summary>
    public bool DisposeContentOnClose
    {
        get => (bool)GetValue(DisposeContentOnCloseProperty);
        set => SetValue(DisposeContentOnCloseProperty, value);
    }

    /// <summary>
    /// Disposes Content when the OverlayHost leaves the visual tree if the content implements IDisposable.
    /// Default is false to avoid disposing externally owned view instances.
    /// </summary>
    public bool DisposeContentOnUnload
    {
        get => (bool)GetValue(DisposeContentOnUnloadProperty);
        set => SetValue(DisposeContentOnUnloadProperty, value);
    }

    /// <summary>
    /// Opens the overlay and attaches it to the nearest appropriate root panel.
    /// Repeated calls are safe.
    /// </summary>
    public void Open()
    {
        if (_isOpeningOrClosing)
        {
            return;
        }

        if (!IsLoaded)
        {
            return;
        }

        FrameworkElement anchor = Anchor ?? this;
        if (!anchor.IsLoaded && anchor != this)
        {
            return;
        }

        Panel? targetPanel = FindRootPanel(anchor);
        if (targetPanel is null)
        {
            return;
        }

        if (_overlayRoot is not null && ReferenceEquals(_hostPanel, targetPanel) && targetPanel.Children.Contains(_overlayRoot))
        {
            Refresh();
            return;
        }

        CloseCore(disposeContent: false, restoreContent: true);

        try
        {
            _isOpeningOrClosing = true;
            EnsureContentDetached();
            CreateOverlay(targetPanel);
            AttachOverlay(targetPanel);
        }
        finally
        {
            _isOpeningOrClosing = false;
        }
    }

    /// <summary>
    /// Closes the overlay and removes the dynamically added visual from the host panel.
    /// Repeated calls are safe.
    /// </summary>
    public void Close()
    {
        CloseCore(disposeContent: DisposeContentOnClose, restoreContent: true);
    }

    /// <summary>
    /// Recomputes target panel, Grid span, ZIndex and stretch settings.
    /// </summary>
    public void Refresh()
    {
        if (!IsOpen)
        {
            Close();
            return;
        }

        if (!IsLoaded)
        {
            return;
        }

        FrameworkElement anchor = Anchor ?? this;
        Panel? targetPanel = FindRootPanel(anchor);

        if (targetPanel is null)
        {
            CloseCore(disposeContent: false, restoreContent: true);
            return;
        }

        if (_overlayRoot is null || !ReferenceEquals(targetPanel, _hostPanel))
        {
            Open();
            return;
        }

        ApplyOverlayLayout(_overlayRoot, targetPanel);
        ApplyZIndex(_overlayRoot, targetPanel);
    }

    /// <summary>
    /// Finds the outermost Panel that should host the overlay for the provided anchor.
    /// The search uses VisualTreeHelper and stops at WPF-UI ContentDialog and Window boundaries.
    /// </summary>
    public static Panel? FindRootPanel(DependencyObject? anchor)
    {
        if (anchor is null)
        {
            return null;
        }

        Panel? nearestPanel = null;
        Panel? outermostPanel = null;
        DependencyObject? current = anchor;

        while (current is not null)
        {
            if (current is Panel panel)
            {
                nearestPanel ??= panel;
                outermostPanel = panel;
            }

            if (IsGeneratedOverlayBoundary(current) || IsContentDialogBoundary(current) || current is Window)
            {
                break;
            }

            current = GetVisualParent(current);
        }

        return outermostPanel ?? nearestPanel;
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (_isOpeningOrClosing)
        {
            return;
        }

        if (_overlayPresenter is not null)
        {
            _detachedContent = newContent;
            _hasDetachedContent = newContent is not null;
            _overlayPresenter.Content = newContent;
            SetCurrentValue(ContentProperty, null);
        }
    }

    private static void OnAnchorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OverlayHost host)
        {
            host.UnsubscribeAnchorEvents(e.OldValue as FrameworkElement);
            host.SubscribeAnchorEvents(e.NewValue as FrameworkElement);
            host.Refresh();
        }
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not OverlayHost host)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            host.Open();
        }
        else
        {
            host.Close();
        }
    }

    private static void OnOverlayVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OverlayHost host && host._overlayRoot is not null)
        {
            host.ApplyOverlayVisualProperties(host._overlayRoot);
        }
    }

    private static DependencyObject? GetVisualParent(DependencyObject current)
    {
        if (current is Visual or Visual3D)
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(current);
            if (parent is not null)
            {
                return parent;
            }
        }

        return LogicalTreeHelper.GetParent(current);
    }

    private static bool IsContentDialogBoundary(DependencyObject current)
    {
        Type? type = current.GetType();

        while (type is not null)
        {
            if (type.Name == "ContentDialog" && type.Namespace?.StartsWith("Wpf.Ui", StringComparison.Ordinal) == true)
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    private static bool IsGeneratedOverlayBoundary(DependencyObject current)
    {
        return current.GetValue(IsOverlayBoundaryProperty) is true;
    }

    private static int GetMaxPanelZIndex(Panel panel, UIElement? excludedElement)
    {
        int maxZIndex = 0;

        foreach (UIElement child in panel.Children)
        {
            if (ReferenceEquals(child, excludedElement))
            {
                continue;
            }

            maxZIndex = Math.Max(maxZIndex, Panel.GetZIndex(child));
        }

        return maxZIndex;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SubscribeAnchorEvents(Anchor);

        if (IsOpen)
        {
            Open();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeAnchorEvents(_subscribedAnchor);
        CloseCore(disposeContent: DisposeContentOnUnload, restoreContent: true);
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsOpen && IsLoaded)
        {
            Refresh();
        }
    }

    private void OnAnchorLoaded(object sender, RoutedEventArgs e)
    {
        if (IsOpen)
        {
            Open();
        }
    }

    private void OnAnchorUnloaded(object sender, RoutedEventArgs e)
    {
        if (IsOpen && !IsAncestorOf(this, (DependencyObject)sender))
        {
            CloseCore(disposeContent: false, restoreContent: true);
        }
    }

    private void OnHostPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsOpen)
        {
            Refresh();
        }
    }

    private void SubscribeAnchorEvents(FrameworkElement? anchor)
    {
        if (anchor is null || ReferenceEquals(anchor, this) || ReferenceEquals(anchor, _subscribedAnchor))
        {
            return;
        }

        UnsubscribeAnchorEvents(_subscribedAnchor);
        anchor.Loaded += OnAnchorLoaded;
        anchor.Unloaded += OnAnchorUnloaded;
        _subscribedAnchor = anchor;
    }

    private void UnsubscribeAnchorEvents(FrameworkElement? anchor)
    {
        if (anchor is null)
        {
            return;
        }

        anchor.Loaded -= OnAnchorLoaded;
        anchor.Unloaded -= OnAnchorUnloaded;

        if (ReferenceEquals(anchor, _subscribedAnchor))
        {
            _subscribedAnchor = null;
        }
    }

    private void EnsureContentDetached()
    {
        if (_hasDetachedContent)
        {
            return;
        }

        _detachedContent = Content;
        _hasDetachedContent = _detachedContent is not null;

        if (_hasDetachedContent)
        {
            SetCurrentValue(ContentProperty, null);
        }
    }

    private void CreateOverlay(Panel targetPanel)
    {
        _overlayPresenter = new ContentPresenter
        {
            Content = _detachedContent,
            ContentTemplate = ContentTemplate,
            ContentTemplateSelector = ContentTemplateSelector,
            ContentStringFormat = ContentStringFormat,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            RecognizesAccessKey = true
        };

        _overlayRoot = new Border
        {
            Child = _overlayPresenter,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            MinWidth = 0,
            MinHeight = 0,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true
        };

        _overlayRoot.SetValue(IsOverlayBoundaryProperty, true);

        BindingOperations.SetBinding(
            _overlayRoot,
            FrameworkElement.DataContextProperty,
            new Binding(nameof(DataContext))
            {
                Source = this,
                Mode = BindingMode.OneWay
            });

        ApplyOverlayVisualProperties(_overlayRoot);
        ApplyOverlayLayout(_overlayRoot, targetPanel);
        ApplyZIndex(_overlayRoot, targetPanel);
    }

    private void AttachOverlay(Panel targetPanel)
    {
        if (_overlayRoot is null)
        {
            return;
        }

        if (!targetPanel.Children.Contains(_overlayRoot))
        {
            targetPanel.Children.Add(_overlayRoot);
        }

        _hostPanel = targetPanel;
        _hostPanel.SizeChanged += OnHostPanelSizeChanged;
        _overlayRoot.UpdateLayout();
    }

    private void CloseCore(bool disposeContent, bool restoreContent)
    {
        if (_isOpeningOrClosing)
        {
            return;
        }

        try
        {
            _isOpeningOrClosing = true;

            object? contentToDispose = _detachedContent;

            if (_overlayPresenter is not null)
            {
                _overlayPresenter.Content = null;
            }

            if (_overlayRoot is not null)
            {
                _overlayRoot.Child = null;
                BindingOperations.ClearBinding(_overlayRoot, FrameworkElement.DataContextProperty);
            }

            if (_hostPanel is not null)
            {
                _hostPanel.SizeChanged -= OnHostPanelSizeChanged;
            }

            if (_hostPanel is not null && _overlayRoot is not null && _hostPanel.Children.Contains(_overlayRoot))
            {
                _hostPanel.Children.Remove(_overlayRoot);
            }

            _overlayPresenter = null;
            _overlayRoot = null;
            _hostPanel = null;

            if (restoreContent && _hasDetachedContent && !disposeContent)
            {
                SetCurrentValue(ContentProperty, _detachedContent);
            }

            if (disposeContent)
            {
                DisposeIfNeeded(contentToDispose);
                _detachedContent = null;
                _hasDetachedContent = false;
                SetCurrentValue(ContentProperty, null);
            }
            else if (!restoreContent)
            {
                _detachedContent = null;
                _hasDetachedContent = false;
            }
        }
        finally
        {
            _isOpeningOrClosing = false;
        }
    }

    private void ApplyOverlayVisualProperties(Border overlayRoot)
    {
        overlayRoot.Background = OverlayBackground;
    }

    private void ApplyOverlayLayout(FrameworkElement overlayRoot, Panel targetPanel)
    {
        overlayRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
        overlayRoot.VerticalAlignment = VerticalAlignment.Stretch;

        if (targetPanel is Grid grid)
        {
            Grid.SetRow(overlayRoot, 0);
            Grid.SetColumn(overlayRoot, 0);
            Grid.SetRowSpan(overlayRoot, Math.Max(1, grid.RowDefinitions.Count));
            Grid.SetColumnSpan(overlayRoot, Math.Max(1, grid.ColumnDefinitions.Count));
        }
        else
        {
            Grid.SetRow(overlayRoot, 0);
            Grid.SetColumn(overlayRoot, 0);
            Grid.SetRowSpan(overlayRoot, 1);
            Grid.SetColumnSpan(overlayRoot, 1);
        }

        if (targetPanel is Canvas)
        {
            Canvas.SetLeft(overlayRoot, 0);
            Canvas.SetTop(overlayRoot, 0);
            overlayRoot.Width = targetPanel.ActualWidth;
            overlayRoot.Height = targetPanel.ActualHeight;
        }
        else
        {
            overlayRoot.ClearValue(WidthProperty);
            overlayRoot.ClearValue(HeightProperty);
        }
    }

    private void ApplyZIndex(UIElement overlayRoot, Panel targetPanel)
    {
        Panel.SetZIndex(overlayRoot, GetMaxPanelZIndex(targetPanel, overlayRoot) + 1);
    }

    private void DisposeIfNeeded(object? value)
    {
        switch (value)
        {
            case null:
                return;
            case IDisposable disposable:
                disposable.Dispose();
                break;
            case IEnumerable enumerable:
                foreach (object? item in enumerable)
                {
                    if (!ReferenceEquals(item, value))
                    {
                        DisposeIfNeeded(item);
                    }
                }

                break;
        }
    }

    private static bool IsAncestorOf(DependencyObject possibleAncestor, DependencyObject current)
    {
        DependencyObject? parent = current;

        while (parent is not null)
        {
            if (ReferenceEquals(parent, possibleAncestor))
            {
                return true;
            }

            parent = GetVisualParent(parent);
        }

        return false;
    }
}
