using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;


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
            RebuildLevels();
        }

        public ObservableCollection<TreeModel> ReasonModels { get; }

        [ObservableProperty]
        private ObservableCollection<TreeModel> _selectedPath = new();

        [ObservableProperty]
        private ObservableCollection<StepTreeLevel> _visibleLevels = new();

        [ObservableProperty]
        private TreeModel? _selectedReason;

        public bool IsEmpty => ReasonModels.Count == 0;

        [RelayCommand]
        private void Select(TreeModel? node)
        {
            if (node == null)
                return;

            ClearSelected();

            var levelIndex = GetNodeLevel(node);

            while (SelectedPath.Count > levelIndex)
            {
                SelectedPath.RemoveAt(SelectedPath.Count - 1);
            }

            if (SelectedPath.Count == levelIndex)
            {
                SelectedPath.Add(node);
            }
            else
            {
                SelectedPath[levelIndex] = node;
            }

            foreach (var item in SelectedPath)
            {
                item.IsSelected = true;
            }

            SelectedReason = node.Children.Count == 0 ? node : null;

            RebuildLevels();
        }

        [RelayCommand]
        private void Maintain()
        {
            // 暂不实现
        }

        private void RebuildLevels()
        {
            VisibleLevels.Clear();

            if (ReasonModels.Count == 0)
                return;

            VisibleLevels.Add(new StepTreeLevel(1, ReasonModels));

            for (var i = 0; i < SelectedPath.Count; i++)
            {
                var node = SelectedPath[i];

                if (node.Children.Count == 0)
                    break;

                VisibleLevels.Add(new StepTreeLevel(i + 2, node.Children));
            }

            OnPropertyChanged(nameof(IsEmpty));
        }

        private void ClearSelected()
        {
            foreach (var node in Flatten(ReasonModels))
            {
                node.IsSelected = false;
            }
        }

        private static int GetNodeLevel(TreeModel node)
        {
            var level = 0;
            var parent = node.Parent;

            while (parent != null)
            {
                level++;
                parent = parent.Parent;
            }

            return level;
        }

        private static IEnumerable<TreeModel> Flatten(IEnumerable<TreeModel> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;

                foreach (var child in Flatten(node.Children))
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
                    DisplayName = name,
                    Parent = parent
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

        [ObservableProperty]
        private bool _isSelected;

        public TreeModel? Parent { get; set; }

        public List<TreeModel> Children { get; } = new();

        public override string ToString()
        {
            return $"{DisplayName}({Id})";
        }
    }

    public class StepTreeLevel
    {
        public StepTreeLevel(int index, IEnumerable<TreeModel> items)
        {
            Index = index;
            Items = new ObservableCollection<TreeModel>(items);
        }

        public int Index { get; }

        public string Title => $"{Index}级";

        public ObservableCollection<TreeModel> Items { get; }
    }


}