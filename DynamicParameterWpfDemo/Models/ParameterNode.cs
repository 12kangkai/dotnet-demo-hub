using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DynamicParameterWpfDemo.Models;

public class ParameterNode : INotifyPropertyChanged
{
    private bool _isSelected;
    private string? _value;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public ObservableCollection<ParameterNode> Children { get; set; } = new();

    public string? Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
