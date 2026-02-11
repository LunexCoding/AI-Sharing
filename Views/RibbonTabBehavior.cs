using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;

using Microsoft.Xaml.Behaviors;

using Fox;

using OrderApprovalSystem.ViewModels;


namespace OrderApprovalSystem.Views
{

    public class RibbonTabBehavior : Behavior<Ribbon>
    {
        public static readonly DependencyProperty CurrentViewModelProperty = DependencyProperty.Register(
            "CurrentViewModel",
            typeof(VMBase),
            typeof(RibbonTabBehavior),
            new PropertyMetadata(null, OnCurrentViewModelChanged)
        );

        public VMBase CurrentViewModel
        {
            get { return (VMBase)GetValue(CurrentViewModelProperty); }
            set { SetValue(CurrentViewModelProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            UpdateRibbonTab();
        }

        private RibbonTab _dynamicTab;

        private static void OnCurrentViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (RibbonTabBehavior)d;
            behavior.UpdateRibbonTab();
        }

        private void UpdateRibbonTab()
        {
            if (AssociatedObject == null)
                return;

            // Удаляем старую вкладку
            if (_dynamicTab != null)
            {
                AssociatedObject.Items.Remove(_dynamicTab);
                _dynamicTab = null;
            }

            if (CurrentViewModel == null)
            {
                return;
            }

            // Создаем новую вкладку
            _dynamicTab = CreateTabForViewModel(CurrentViewModel);
            if (_dynamicTab != null)
            {
                // Вставляем первой вкладкой (после ApplicationMenu)
                AssociatedObject.Items.Insert(0, _dynamicTab);
                _dynamicTab.IsSelected = true;
            }


        }

        private RibbonTab CreateTabForViewModel(VMBase viewModel)
        {
            if (viewModel is vmBaseApproval approvalViewModel)
            {
                RibbonTab tab = new RibbonTab
                {
                    Header = approvalViewModel.GetType().Name.Replace("vm", ""),
                    DataContext = approvalViewModel
                };

                // Добавляем общие группы
                var approveGroup = CreateCommonApproveGroup(approvalViewModel);
                if (approveGroup.Items.Count > 0)
                    tab.Items.Add(approveGroup);

                // Добавляем роле-специфичные группы
                if (approvalViewModel is vmOrderManager orderManager)
                {
                    var orderGroup = CreateOrderManagerSpecificGroup(orderManager);
                    tab.Items.Add(orderGroup);
                }

                return tab;
            }

            // Обработка других типов ViewModel (vmDev и т.д.)
            return null;
        }

        private RibbonGroup CreateCommonApproveGroup(vmBaseApproval viewModel)
        {
            RibbonGroup group = new RibbonGroup { Header = "Согласование" };

            if (viewModel.CanApproveOrders && viewModel.ApprovalOrderCommand != null)
            {
                group.Items.Add(new RibbonButton
                {
                    Label = "Согласовать",
                    Command = viewModel.ApprovalOrderCommand,
                    LargeImageSource = CreateCheckIcon()
                });
            }

            if (viewModel.CanRejectOrders && viewModel.RejectOrderCommand != null)
            {
                group.Items.Add(new RibbonButton
                {
                    Label = "Не согласовывать",
                    Command = viewModel.RejectOrderCommand,
                    LargeImageSource = CreateCrossIcon()
                });
            }

            // Кнопка "История" для всех
            group.Items.Add(new RibbonButton
            {
                Label = "История согласования",
                LargeImageSource = CreateHistoryIcon()
            });

            return group;
        }

        private RibbonGroup CreateOrderManagerSpecificGroup(vmOrderManager orderManager)
        {
            RibbonGroup group = new RibbonGroup { Header = "Заказ" };
            group.Items.Add(new RibbonButton
            {
                Label = "По СЗ",
                Command = orderManager.OrderByMemoCommand,
                LargeImageSource = CreateCheckIcon()
            });
            return group;
        }


        // Вспомогательный метод для создания ImageSource из Geometry
        private ImageSource CreateGeometryImageSource(string geometryData, Brush brush, double width, double height)
        {
            Geometry geometry = Geometry.Parse(geometryData);
            GeometryDrawing drawing = new GeometryDrawing(brush, null, geometry);
            DrawingImage drawingImage = new DrawingImage(drawing);
            drawingImage.Freeze();
            return drawingImage;
        }

        private ImageSource CreateCheckIcon()
        {
            return CreateGeometryImageSource(
                "M9 16.17L4.83 12L3.41 13.41L9 19L21 7L19.59 5.59L9 16.17Z",
                Brushes.Green,
                32, 32
            );
        }

        // Метод для создания иконки крестика
        private ImageSource CreateCrossIcon()
        {
            return CreateGeometryImageSource(
                "M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12L19 6.41Z",
                Brushes.Red,
                32, 32
            );
        }

        private ImageSource CreateHistoryIcon()
        {
            // Данные геометрии: круг часов и стрелки
            return CreateGeometryImageSource(
                "M13,3A9,9 0 0,0 4,12H1L4.89,15.89L4.96,16.03L9,12H6A7,7 0 0,1 13,5A7,7 0 0,1 20,12A7,7 0 0,1 13,19C11.07,19 9.32,18.21 8.06,16.94L6.64,18.36C8.27,20 10.5,21 13,21A9,9 0 0,0 22,12A9,9 0 0,0 13,3M12,8V13L16.28,15.54L17,14.33L13.5,12.25V8H12Z",
                (Brush)Application.Current.Resources["TextBrush"],
                32, 32
            );
        }

    }

}
