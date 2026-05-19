using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StepTreeSelectorDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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

        [RelayCommand]
        private void Maintain()
        {
            // 暂不实现
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

        public List<TreeModel> Children { get; } = new();

        public override string ToString()
        {
            return $"{DisplayName}({Id})";
        }
    }
}
