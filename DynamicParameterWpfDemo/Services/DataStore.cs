using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using DynamicParameterWpfDemo.Models;

namespace DynamicParameterWpfDemo.Services;

public sealed class DataStore
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly string _dataDir;
    private string ParameterFile => Path.Combine(_dataDir, "parameters.json");
    private string TemplateFile => Path.Combine(_dataDir, "device-templates.json");

    public DataStore()
    {
        _dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(_dataDir);
    }

    public ObservableCollection<ParameterNode> LoadParameters()
    {
        if (!File.Exists(ParameterFile))
            return SeedParameters();

        var json = File.ReadAllText(ParameterFile);
        return JsonSerializer.Deserialize<ObservableCollection<ParameterNode>>(json, _options) ?? SeedParameters();
    }

    public ObservableCollection<DeviceTemplate> LoadTemplates()
    {
        if (!File.Exists(TemplateFile))
            return new ObservableCollection<DeviceTemplate>();

        var json = File.ReadAllText(TemplateFile);
        return JsonSerializer.Deserialize<ObservableCollection<DeviceTemplate>>(json, _options) ?? new();
    }

    public void SaveParameters(IEnumerable<ParameterNode> nodes) =>
        File.WriteAllText(ParameterFile, JsonSerializer.Serialize(nodes, _options));

    public void SaveTemplates(IEnumerable<DeviceTemplate> templates) =>
        File.WriteAllText(TemplateFile, JsonSerializer.Serialize(templates, _options));

    private static ObservableCollection<ParameterNode> SeedParameters() => new()
    {
        new ParameterNode
        {
            Name = "通讯参数", IsFolder = true,
            Children = new ObservableCollection<ParameterNode>
            {
                new() { Name = "IP地址", Value = "192.168.1.10" },
                new() { Name = "端口", Value = "502" },
                new() { Name = "站号", Value = "1" }
            }
        },
        new ParameterNode
        {
            Name = "采集参数", IsFolder = true,
            Children = new ObservableCollection<ParameterNode>
            {
                new() { Name = "采集周期(ms)", Value = "1000" },
                new() { Name = "超时时间(ms)", Value = "3000" },
                new() { Name = "重试次数", Value = "3" }
            }
        },
        new ParameterNode
        {
            Name = "报警参数", IsFolder = true,
            Children = new ObservableCollection<ParameterNode>
            {
                new() { Name = "温度上限", Value = "80" },
                new() { Name = "压力上限", Value = "1.5" }
            }
        }
    };
}
