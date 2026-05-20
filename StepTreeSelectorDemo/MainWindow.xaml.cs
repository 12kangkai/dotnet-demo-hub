using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        }

        public ObservableCollection<TreeModel> ReasonModels { get; }

        [ObservableProperty]
        private TreeModel? _selectedReason;

        [ObservableProperty]
        private bool _isMaintainEditorOpen;

        [ObservableProperty]
        private TreeModel? _maintainParent;

        [ObservableProperty]
        private string _maintainNodeName = string.Empty;

        [RelayCommand]
        private void Maintain(TreeModel? currentNode)
        {
            MaintainParent = currentNode;
            MaintainNodeName = string.Empty;
            IsMaintainEditorOpen = true;
        }

        [RelayCommand]
        private void CloseMaintainEditor()
        {
            IsMaintainEditorOpen = false;
        }

        [RelayCommand]
        private void SaveMaintainNode()
        {
            var nodeName = MaintainNodeName.Trim();
            if (string.IsNullOrWhiteSpace(nodeName))
                return;

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
            IsMaintainEditorOpen = false;
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
