using System.Windows;
using System.Windows.Controls;
using OrderApprovalSystem.ViewModels;

namespace OrderApprovalSystem.Views.RoleSpecificPanels
{
    public partial class OrderManagerCentralPanel : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                "ViewModel",
                typeof(vmOrderManager),
                typeof(OrderManagerCentralPanel),
                new PropertyMetadata(null));

        public vmOrderManager ViewModel
        {
            get => (vmOrderManager)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public OrderManagerCentralPanel()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }
}