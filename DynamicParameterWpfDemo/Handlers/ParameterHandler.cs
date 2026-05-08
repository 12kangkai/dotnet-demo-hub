using DynamicParameterWpfDemo.Models;

namespace DynamicParameterWpfDemo.Handlers;

public sealed class HandlerContext
{
    public required DeviceTemplate Template { get; init; }
    public required List<ParameterNode> Parameters { get; init; }
    public List<string> Logs { get; } = new();
}

public interface IParameterHandler
{
    IParameterHandler SetNext(IParameterHandler next);
    void Handle(HandlerContext context);
}

public abstract class ParameterHandlerBase : IParameterHandler
{
    private IParameterHandler? _next;

    public IParameterHandler SetNext(IParameterHandler next)
    {
        _next = next;
        return next;
    }

    public void Handle(HandlerContext context)
    {
        DoHandle(context);
        _next?.Handle(context);
    }

    protected abstract void DoHandle(HandlerContext context);
}

public sealed class ValidateParameterHandler : ParameterHandlerBase
{
    protected override void DoHandle(HandlerContext context)
    {
        if (context.Parameters.Count == 0)
            throw new InvalidOperationException("模板没有配置任何参数。");

        var duplicated = context.Parameters.GroupBy(p => p.Id).Where(g => g.Count() > 1).ToList();
        if (duplicated.Count > 0)
            throw new InvalidOperationException("模板参数存在重复项。");

        context.Logs.Add($"[校验] 模板 {context.Template.Name} 共 {context.Parameters.Count} 个参数。");
    }
}

public sealed class BuildCommandHandler : ParameterHandlerBase
{
    protected override void DoHandle(HandlerContext context)
    {
        foreach (var p in context.Parameters)
        {
            var value = string.IsNullOrWhiteSpace(p.Value) ? "<空>" : p.Value;
            context.Logs.Add($"[组包] {p.Name} = {value}");
        }
    }
}

public sealed class SendToDeviceHandler : ParameterHandlerBase
{
    protected override void DoHandle(HandlerContext context)
    {
        // Demo 中模拟下发设备；真实项目可在这里接 Modbus、串口、TCP、MQTT 等。
        context.Logs.Add($"[下发] 已按模板 {context.Template.Name} 下发参数。");
    }
}

public sealed class AuditHandler : ParameterHandlerBase
{
    protected override void DoHandle(HandlerContext context)
    {
        context.Logs.Add($"[审计] {DateTime.Now:yyyy-MM-dd HH:mm:ss} 执行完成。");
    }
}
