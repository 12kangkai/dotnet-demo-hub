using System.Collections;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;

namespace StepTreeSelectorDemo;

public partial class StepTreeSelector : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(StepTreeSelector),
            new PropertyMetadata(null, OnTreePropertyChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(StepTreeSelector),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register(
            nameof(DisplayMemberPath),
            typeof(string),
            typeof(StepTreeSelector),
            new PropertyMetadata("DisplayName", OnTreePropertyChanged));

    public static readonly DependencyProperty ChildrenMemberPathProperty =
        DependencyProperty.Register(
            nameof(ChildrenMemberPath),
            typeof(string),
            typeof(StepTreeSelector),
            new PropertyMetadata("Children", OnTreePropertyChanged));

    public static readonly DependencyProperty LevelTitleFormatProperty =
        DependencyProperty.Register(
            nameof(LevelTitleFormat),
            typeof(string),
            typeof(StepTreeSelector),
            new PropertyMetadata("{0}级", OnLevelTitleFormatChanged));

    public static readonly DependencyProperty EmptyButtonTextProperty =
        DependencyProperty.Register(
            nameof(EmptyButtonText),
            typeof(string),
            typeof(StepTreeSelector),
            new PropertyMetadata("+ 未维护，点击维护"));

    public static readonly DependencyProperty MaintainCommandProperty =
        DependencyProperty.Register(
            nameof(MaintainCommand),
            typeof(ICommand),
            typeof(StepTreeSelector),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsEmptyProperty =
        DependencyProperty.Register(
            nameof(IsEmpty),
            typeof(bool),
            typeof(StepTreeSelector),
            new PropertyMetadata(true));

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(StepTreeSelector),
            new PropertyMetadata(new CornerRadius(6)));

    public static readonly DependencyProperty LevelHeaderWidthProperty =
        DependencyProperty.Register(
            nameof(LevelHeaderWidth),
            typeof(double),
            typeof(StepTreeSelector),
            new PropertyMetadata(64d));

    private readonly RelayCommandAdapter _selectCommand;
    private readonly ObservableCollection<object> _selectedPath = new();
    private INotifyCollectionChanged? _observedItemsSource;
    private bool _isUpdatingSelectedItem;
    private bool _scrollToLatestLevelAfterRebuild;

    public StepTreeSelector()
    {
        _selectCommand = new RelayCommandAdapter(Select);
        VisibleLevels = new ObservableCollection<StepTreeLevel>();
        BorderBrush = Brushes.Gray;
        BorderThickness = new Thickness(1);
        Padding = new Thickness(12);

        InitializeComponent();
        RebuildLevels();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string ChildrenMemberPath
    {
        get => (string)GetValue(ChildrenMemberPathProperty);
        set => SetValue(ChildrenMemberPathProperty, value);
    }

    public string LevelTitleFormat
    {
        get => (string)GetValue(LevelTitleFormatProperty);
        set => SetValue(LevelTitleFormatProperty, value);
    }

    public string EmptyButtonText
    {
        get => (string)GetValue(EmptyButtonTextProperty);
        set => SetValue(EmptyButtonTextProperty, value);
    }

    public ICommand? MaintainCommand
    {
        get => (ICommand?)GetValue(MaintainCommandProperty);
        set => SetValue(MaintainCommandProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double LevelHeaderWidth
    {
        get => (double)GetValue(LevelHeaderWidthProperty);
        set => SetValue(LevelHeaderWidthProperty, value);
    }

    public ObservableCollection<StepTreeLevel> VisibleLevels { get; }

    public ICommand SelectCommand => _selectCommand;

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        private set => SetValue(IsEmptyProperty, value);
    }

    private static void OnTreePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (StepTreeSelector)d;

        if (e.Property == ItemsSourceProperty)
        {
            control.OnItemsSourceChanged(e.OldValue, e.NewValue);
        }

        control.SyncPathFromSelectedItem();
        control.RebuildLevels();
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (StepTreeSelector)d;

        if (control._isUpdatingSelectedItem)
            return;

        if (Equals(e.NewValue, control._selectedPath.LastOrDefault()))
            return;

        control.SyncPathFromSelectedItem();
        control.RebuildLevels();
    }

    private static void OnLevelTitleFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((StepTreeSelector)d).RefreshLevelTitles();
    }

    private void Select(object? parameter)
    {
        if (parameter is not StepTreeItem stepItem)
            return;

        while (_selectedPath.Count > stepItem.LevelIndex)
        {
            _selectedPath.RemoveAt(_selectedPath.Count - 1);
        }

        if (_selectedPath.Count == stepItem.LevelIndex)
        {
            _selectedPath.Add(stepItem.Value);
        }
        else
        {
            _selectedPath[stepItem.LevelIndex] = stepItem.Value;
        }

        var hasChildren = HasChildren(stepItem.Value);
        _scrollToLatestLevelAfterRebuild = hasChildren;

        _isUpdatingSelectedItem = true;
        try
        {
            SelectedItem = hasChildren ? null : stepItem.Value;
        }
        finally
        {
            _isUpdatingSelectedItem = false;
        }

        RebuildLevels();
    }

    private void OnItemsSourceChanged(object? oldValue, object? newValue)
    {
        if (_observedItemsSource != null)
        {
            _observedItemsSource.CollectionChanged -= OnItemsSourceCollectionChanged;
            _observedItemsSource = null;
        }

        if (newValue is INotifyCollectionChanged collectionChanged)
        {
            _observedItemsSource = collectionChanged;
            _observedItemsSource.CollectionChanged += OnItemsSourceCollectionChanged;
        }
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncPathFromSelectedItem();
        RebuildLevels();
    }

    private void RebuildLevels()
    {
        VisibleLevels.Clear();

        var rootItems = GetRootItems();
        if (rootItems.Count == 0)
        {
            NotifyIsEmptyChanged();
            return;
        }

        VisibleLevels.Add(CreateLevel(0, rootItems));

        for (var i = 0; i < _selectedPath.Count; i++)
        {
            var node = _selectedPath[i];
            var children = GetChildren(node);

            if (children.Count == 0)
                break;

            VisibleLevels.Add(CreateLevel(i + 1, children));
        }

        NotifyIsEmptyChanged();

        if (_scrollToLatestLevelAfterRebuild)
        {
            _scrollToLatestLevelAfterRebuild = false;
            ScrollToLatestLevel();
        }
    }

    private void ScrollToLatestLevel()
    {
        Dispatcher.BeginInvoke(
            new Action(() =>
            {
                if (VisibleLevels.Count == 0)
                    return;

                PART_LevelsItemsControl.UpdateLayout();

                var targetContainer = PART_LevelsItemsControl.ItemContainerGenerator.ContainerFromIndex(VisibleLevels.Count - 1) as FrameworkElement;
                if (targetContainer == null)
                {
                    PART_ScrollViewer.ScrollToEnd();
                    return;
                }

                var relativePoint = targetContainer.TransformToAncestor(PART_ScrollViewer).Transform(new Point(0, 0));
                PART_ScrollViewer.ScrollToVerticalOffset(PART_ScrollViewer.VerticalOffset + relativePoint.Y);
            }),
            DispatcherPriority.ContextIdle);
    }

    private StepTreeLevel CreateLevel(int levelIndex, IReadOnlyList<object> items)
    {
        var selectedValue = _selectedPath.Count > levelIndex ? _selectedPath[levelIndex] : null;
        var stepItems = items
            .Select(item => new StepTreeItem(
                item,
                GetDisplayName(item),
                levelIndex,
                HasChildren(item),
                ReferenceEquals(item, selectedValue) || Equals(item, selectedValue)));

        return new StepTreeLevel(levelIndex + 1, GetLevelTitle(levelIndex + 1), stepItems);
    }

    private void SyncPathFromSelectedItem()
    {
        _selectedPath.Clear();

        if (SelectedItem == null)
            return;

        if (TryBuildPath(GetRootItems(), SelectedItem, out var path))
        {
            foreach (var item in path)
            {
                _selectedPath.Add(item);
            }
        }
    }

    private bool TryBuildPath(IEnumerable<object> nodes, object target, out List<object> path)
    {
        foreach (var node in nodes)
        {
            if (ReferenceEquals(node, target) || Equals(node, target))
            {
                path = new List<object> { node };
                return true;
            }

            if (TryBuildPath(GetChildren(node), target, out var childPath))
            {
                childPath.Insert(0, node);
                path = childPath;
                return true;
            }
        }

        path = new List<object>();
        return false;
    }

    private IReadOnlyList<object> GetRootItems()
    {
        return ToObjectList(ItemsSource);
    }

    private IReadOnlyList<object> GetChildren(object item)
    {
        if (string.IsNullOrWhiteSpace(ChildrenMemberPath))
            return Array.Empty<object>();

        return ToObjectList(GetMemberValue(item, ChildrenMemberPath));
    }

    private bool HasChildren(object item)
    {
        return GetChildren(item).Count > 0;
    }

    private string GetDisplayName(object item)
    {
        if (!string.IsNullOrWhiteSpace(DisplayMemberPath))
        {
            var value = GetMemberValue(item, DisplayMemberPath);
            if (value != null)
                return value.ToString() ?? string.Empty;
        }

        return item.ToString() ?? string.Empty;
    }

    private object? GetMemberValue(object item, string memberPath)
    {
        object? current = item;

        foreach (var memberName in memberPath.Split('.'))
        {
            if (current == null)
                return null;

            var property = current.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            current = property?.GetValue(current);
        }

        return current;
    }

    private static IReadOnlyList<object> ToObjectList(object? value)
    {
        if (value is null or string)
            return Array.Empty<object>();

        if (value is IEnumerable enumerable)
            return enumerable.Cast<object>().ToList();

        return Array.Empty<object>();
    }

    private string GetLevelTitle(int levelIndex)
    {
        return string.Format(LevelTitleFormat, levelIndex);
    }

    private void RefreshLevelTitles()
    {
        foreach (var level in VisibleLevels)
        {
            level.Title = GetLevelTitle(level.Index);
        }
    }

    private void NotifyIsEmptyChanged()
    {
        IsEmpty = GetRootItems().Count == 0;
    }
}

public class StepTreeLevel : INotifyPropertyChanged
{
    private string _title;

    public StepTreeLevel(int index, string title, IEnumerable<StepTreeItem> items)
    {
        Index = index;
        _title = title;
        Items = new ObservableCollection<StepTreeItem>(items);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Index { get; }

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
                return;

            _title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }
    }

    public ObservableCollection<StepTreeItem> Items { get; }
}

public sealed class StepTreeItem
{
    public StepTreeItem(object value, string displayName, int levelIndex, bool hasChildren, bool isSelected)
    {
        Value = value;
        DisplayName = displayName;
        LevelIndex = levelIndex;
        HasChildren = hasChildren;
        IsSelected = isSelected;
    }

    public object Value { get; }

    public string DisplayName { get; }

    public int LevelIndex { get; }

    public bool HasChildren { get; }

    public bool IsSelected { get; }
}

internal sealed class RelayCommandAdapter : ICommand
{
    private readonly Action<object?> _execute;

    public RelayCommandAdapter(Action<object?> execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute(parameter);
}
