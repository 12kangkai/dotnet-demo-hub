using System.Collections.ObjectModel;

namespace DynamicParameterWpfDemo.Models;

public class DeviceTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "新设备模板";
    public ObservableCollection<string> ParameterIds { get; set; } = new();

    public override string ToString() => Name;
}
