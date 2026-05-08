# 动态参数 WPF Demo

这个 Demo 覆盖四个点：

1. 参数列表支持树状组织，类似“文件夹 / 文件”。
2. 可以从参数树中自由勾选叶子参数，保存成设备模板。
3. 执行设备模板时，通过责任链依次处理模板中的参数列表。
4. 参数树和设备模板使用 JSON 持久化。

## 运行环境

- Windows
- Visual Studio 2022 或 Rider
- .NET 8 SDK

> WPF 只能在 Windows 上运行。本环境无法直接编译 WPF，所以项目文件是手工生成的标准 SDK 风格 WPF 项目。

## 项目结构

```text
DynamicParameterWpfDemo
├─ Models
│  ├─ ParameterNode.cs        # 参数节点：文件夹或叶子参数
│  └─ DeviceTemplate.cs       # 设备模板：保存参数 ID 列表
├─ Services
│  └─ DataStore.cs            # JSON 持久化
├─ Handlers
│  └─ ParameterHandler.cs     # 责任链接口与处理节点
├─ MainWindow.xaml            # Demo UI
├─ MainWindow.xaml.cs         # 交互逻辑
├─ App.xaml
└─ DynamicParameterWpfDemo.csproj
```

## 操作流程

1. 启动程序后，左侧显示默认参数树。
2. 勾选需要的叶子参数。
3. 输入模板名称，点击“用勾选参数保存为模板”。
4. 在模板列表中选择模板，点击“执行选中模板”。
5. 右侧日志会显示责任链执行结果：
   - ValidateParameterHandler：校验参数。
   - BuildCommandHandler：模拟参数组包。
   - SendToDeviceHandler：模拟下发设备。
   - AuditHandler：记录执行审计。
6. 点击“保存全部数据”后，数据会保存到程序运行目录下的 `Data/*.json`。

## 可扩展点

- `SendToDeviceHandler` 可以替换成真实 Modbus、TCP、串口、MQTT 或 OPC UA 下发逻辑。
- `ParameterNode.Value` 可以扩展为类型、单位、范围、默认值、只读、校验规则等字段。
- `DeviceTemplate` 可以扩展为设备型号、协议、版本号、厂商、模板分组等字段。
- 当前 Demo 为轻量 code-behind 写法，后续可迁移到 MVVM Toolkit。
