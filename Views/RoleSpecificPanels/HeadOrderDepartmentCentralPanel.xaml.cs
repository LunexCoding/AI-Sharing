using System.Windows;
using System.Windows.Controls;
using OrderApprovalSystem.ViewModels;

namespace OrderApprovalSystem.Views.RoleSpecificPanels
{
    public partial class HeadOrderDepartmentCentralPanel : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                "ViewModel",
                typeof(vmHeadOrderDepartment),
                typeof(HeadOrderDepartmentCentralPanel),
                new PropertyMetadata(null));

        public vmHeadOrderDepartment ViewModel
        {
            get => (vmHeadOrderDepartment)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public HeadOrderDepartmentCentralPanel()
        {
            InitializeComponent();
        }
    }
}
