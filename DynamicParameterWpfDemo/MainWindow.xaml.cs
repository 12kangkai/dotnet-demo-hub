using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using DynamicParameterWpfDemo.Handlers;
using DynamicParameterWpfDemo.Models;
using DynamicParameterWpfDemo.Services;

namespace DynamicParameterWpfDemo;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly DataStore _store = new();

    public ObservableCollection<ParameterNode> Parameters { get; set; }
    public ObservableCollection<DeviceTemplate> Templates { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
        Parameters = _store.LoadParameters();
        Templates = _store.LoadTemplates();
        DataContext = this;
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        Parameters.Add(new ParameterNode { Name = "新文件夹", IsFolder = true });
    }

    private void AddParameter_Click(object sender, RoutedEventArgs e)
    {
        var selectedFolder = Flatten(Parameters).FirstOrDefault(p => p.IsSelected && p.IsFolder);
        var parameter = new ParameterNode { Name = "新参数", Value = "" };

        if (selectedFolder is not null)
            selectedFolder.Children.Add(parameter);
        else
            Parameters.Add(parameter);
    }

    private void DeleteSelectedParameters_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelected(Parameters);
    }

    private void SaveTemplate_Click(object sender, RoutedEventArgs e)
    {
        var selectedIds = Flatten(Parameters)
            .Where(p => p.IsSelected && !p.IsFolder)
            .Select(p => p.Id)
            .Distinct()
            .ToList();

        if (selectedIds.Count == 0)
        {
            MessageBox.Show("请先勾选至少一个叶子参数。", "提示");
            return;
        }

        var existing = Templates.FirstOrDefault(t => t.Name == TemplateNameBox.Text.Trim());
        if (existing is null)
        {
            Templates.Add(new DeviceTemplate
            {
                Name = TemplateNameBox.Text.Trim(),
                ParameterIds = new ObservableCollection<string>(selectedIds)
            });
        }
        else
        {
            existing.ParameterIds = new ObservableCollection<string>(selectedIds);
        }

        SaveAll();
        Log($"已保存模板：{TemplateNameBox.Text.Trim()}，参数数：{selectedIds.Count}");
    }

    private void LoadTemplateToTree_Click(object sender, RoutedEventArgs e)
    {
        if (TemplateList.SelectedItem is not DeviceTemplate template)
        {
            MessageBox.Show("请选择一个模板。", "提示");
            return;
        }

        var ids = template.ParameterIds.ToHashSet();
        foreach (var p in Flatten(Parameters))
            p.IsSelected = ids.Contains(p.Id);

        TemplateNameBox.Text = template.Name;
        Log($"已加载模板到参数树：{template.Name}");
    }

    private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (TemplateList.SelectedItem is DeviceTemplate template)
        {
            Templates.Remove(template);
            SaveAll();
            Log($"已删除模板：{template.Name}");
        }
    }

    private void RunTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (TemplateList.SelectedItem is not DeviceTemplate template)
        {
            MessageBox.Show("请选择要执行的模板。", "提示");
            return;
        }

        try
        {
            var selectedParameters = Flatten(Parameters)
                .Where(p => template.ParameterIds.Contains(p.Id) && !p.IsFolder)
                .ToList();

            var context = new HandlerContext
            {
                Template = template,
                Parameters = selectedParameters
            };

            IParameterHandler chain = new ValidateParameterHandler();
            chain.SetNext(new BuildCommandHandler())
                 .SetNext(new SendToDeviceHandler())
                 .SetNext(new AuditHandler());

            chain.Handle(context);
            Log(string.Join(Environment.NewLine, context.Logs));
        }
        catch (Exception ex)
        {
            Log("执行失败：" + ex.Message);
        }
    }

    private void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        SaveAll();
        Log("参数列表和设备模板已持久化到 Data/*.json。");
    }

    private void SaveAll()
    {
        _store.SaveParameters(Parameters);
        _store.SaveTemplates(Templates);
    }

    private void Log(string message)
    {
        LogBox.AppendText(message + Environment.NewLine + Environment.NewLine);
        LogBox.ScrollToEnd();
    }

    private static IEnumerable<ParameterNode> Flatten(IEnumerable<ParameterNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children))
                yield return child;
        }
    }

    private static bool RemoveSelected(ObservableCollection<ParameterNode> nodes)
    {
        for (var i = nodes.Count - 1; i >= 0; i--)
        {
            var node = nodes[i];
            if (node.IsSelected)
            {
                nodes.RemoveAt(i);
                continue;
            }

            RemoveSelected(node.Children);
        }
        return true;
    }
}
