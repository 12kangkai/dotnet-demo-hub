using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Demo.Wpf.Controls.Controls;
using Wpf.Ui.Controls;

namespace StepTreeSelectorDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }

    public partial class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            ReasonModels = new ObservableCollection<TreeModel>(CreateDemoTree());
            GuideSteps = new ObservableCollection<BeginnerGuideStep>(CreateGuideSteps());
        }

        public ObservableCollection<TreeModel> ReasonModels { get; }

        public ObservableCollection<BeginnerGuideStep> GuideSteps { get; }

        [ObservableProperty]
        private TreeModel? _selectedReason;

        [ObservableProperty]
        private bool _isGuideOpen;

        [ObservableProperty]
        private int _guideCurrentIndex;

        [ObservableProperty]
        private bool _isMaintainEditorOpen;

        [ObservableProperty]
        private bool _isMaintainPreviewOpen;

        [ObservableProperty]
        private TreeModel? _maintainParent;

        [NotifyCanExecuteChangedFor(nameof(SaveMaintainNodeCommand))]
        [ObservableProperty]
        private string _maintainNodeName = string.Empty;

        [RelayCommand]
        private void OpenGuide()
        {
            GuideCurrentIndex = 0;
            IsGuideOpen = true;
        }

        [RelayCommand]
        private void Maintain(TreeModel? currentNode)
        {
            MaintainParent = currentNode;
            MaintainNodeName = string.Empty;
            IsMaintainPreviewOpen = false;
            IsMaintainEditorOpen = true;
        }

        [RelayCommand]
        private void CloseMaintainEditor()
        {
            IsMaintainPreviewOpen = false;
            IsMaintainEditorOpen = false;
            MaintainParent = null;
            MaintainNodeName = string.Empty;
        }

        [RelayCommand]
        private void OpenMaintainPreview()
        {
            IsMaintainPreviewOpen = true;
        }

        [RelayCommand]
        private void CloseMaintainPreview()
        {
            IsMaintainPreviewOpen = false;
        }

        [RelayCommand(CanExecute = nameof(CanSaveMaintainNode))]
        private void SaveMaintainNode()
        {
            var nodeName = MaintainNodeName.Trim();

            var newNode = new TreeModel
            {
                Id = GetNextId(),
                DisplayName = nodeName
            };

            if (MaintainParent == null)
            {
                ReasonModels.Add(newNode);
            }
            else
            {
                MaintainParent.Children.Add(newNode);
            }

            SelectedReason = newNode;
            IsMaintainPreviewOpen = false;
            IsMaintainEditorOpen = false;
            MaintainParent = null;
            MaintainNodeName = string.Empty;
        }

        private bool CanSaveMaintainNode()
        {
            return !string.IsNullOrWhiteSpace(MaintainNodeName);
        }

        private long GetNextId()
        {
            return EnumerateTree(ReasonModels)
                .Select(node => node.Id)
                .DefaultIfEmpty(0)
                .Max() + 1;
        }

        private static IEnumerable<TreeModel> EnumerateTree(IEnumerable<TreeModel> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;

                foreach (var child in EnumerateTree(node.Children))
                {
                    yield return child;
                }
            }
        }

        private static List<TreeModel> CreateDemoTree()
        {
            TreeModel Node(long id, string name, TreeModel? parent = null)
            {
                var node = new TreeModel
                {
                    Id = id,
                    DisplayName = name
                };

                parent?.Children.Add(node);
                return node;
            }

            var electric = Node(1, "电气原因");
            var mechanical = Node(2, "机械原因");
            var software = Node(3, "软件原因");

            var power = Node(11, "电源异常", electric);
            Node(111, "主电源断电", power);
            Node(112, "保险丝烧毁", power);

            var signal = Node(12, "信号异常", electric);
            Node(121, "传感器无信号", signal);
            Node(122, "线路松动", signal);

            var motor = Node(21, "电机异常", mechanical);
            Node(211, "电机卡死", motor);
            Node(212, "轴承磨损", motor);

            Node(31, "PLC 通讯失败", software);
            Node(32, "程序参数错误", software);
            Node(33, "程序参数错误", software);
            Node(34, "程序参数错误", software);
            Node(35, "程序参数错误", software);
            Node(36, "程序参数错误", software);
            Node(37, "程序参数错误", software);
            Node(38, "程序参数错误", software);
            Node(39, "程序参数错误", software);
            Node(40, "程序参数错误", software);
            Node(41, "程序参数错误", software);
            Node(42, "程序参数错误", software);
            Node(43, "程序参数错误", software);
            Node(44, "程序参数错误", software);

            return new List<TreeModel>
            {
                electric,
                mechanical,
                software
            };
        }

        private static List<BeginnerGuideStep> CreateGuideSteps()
        {
            return new List<BeginnerGuideStep>
            {
                new()
                {
                    TargetName = "StepTreeControl",
                    Title = "按层级定位原因",
                    Description = "左侧原因树会按当前选择展开下一层。选择中间节点只负责继续展开，选择叶子节点才会写入最终结果。",
                    AccentText = "适合层级较深、但每次只希望用户看到当前分支的配置场景。"
                },
                new()
                {
                    TargetName = "SelectedResultPanel",
                    Title = "查看最终选择",
                    Description = "右侧结果区会实时显示当前已选中的最终叶子节点。"
                },
                new()
                {
                    TargetName = "ReasonTreePanel",
                    Title = "维护子节点",
                    Description = "树控件底部的维护按钮会打开局部 Overlay 编辑器，可以给当前节点继续添加原因项。",
                    AccentText = "这个引导控件来自共享控件库，其他 demo 只需要传入步骤集合和打开状态即可复用。"
                }
            };
        }
    }

    /// <summary>
    /// 树模型
    /// </summary>
    public partial class TreeModel : ObservableObject
    {
        public long Id { get; set; }

        [ObservableProperty]
        private string _displayName = "None";

        public ObservableCollection<TreeModel> Children { get; } = new();

        public override string ToString()
        {
            return $"{DisplayName}({Id})";
        }
    }
}
