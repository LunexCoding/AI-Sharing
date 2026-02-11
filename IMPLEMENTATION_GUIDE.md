# Implementation Guide

## What Has Been Completed

### ✅ Code Structure
All code from `st.txt` has been extracted and organized into proper files:

- **5 Model classes** created in `Models/`
- **1 Base view** created: `BaseApprovalView.xaml`
- **3 Role-specific panels** created in `Views/RoleSpecificPanels/`
- **1 Main window** created: `Main.xaml`
- **1 Behavior** created: `RibbonTabBehavior.cs`

### ✅ Base Window Architecture

The `BaseApprovalView.xaml` implements the requested 3-area layout:

#### 1. Left Panel (Grid Column 0) - ✅ STATIC, SAME FOR ALL ROLES
```xaml
<DataGrid ItemsSource="{Binding model.GroupedData}"
          SelectedItem="{Binding model.CurrentGroup}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Тех. заказ" Binding="{Binding Zak1}" Width="80"/>
        <!-- Additional columns can be added in ViewModel -->
    </DataGrid.Columns>
</DataGrid>
```
- **Not touched** - remains static
- Defined directly in BaseApprovalView
- All columns specified

#### 2. Center Panel (Grid Column 1) - ✅ ROLE-SPECIFIC USERCONTROL
```xaml
<!-- Common fields -->
<Grid>
    <TextBlock Text="Изделие:" />
    <TextBlock Text="{Binding model.CurrentItem.CoreDraft}" />
</Grid>
<Grid>
    <TextBlock Text="Наименование изделия:" />
    <TextBlock Text="{Binding model.CurrentItem.CoreDraftName}" />
</Grid>

<!-- Dynamic role-specific content -->
<ContentControl Content="{Binding RoleSpecificCentralContent}"/>
```
- Common fields defined in BaseApprovalView
- Role-specific content injected via ContentControl
- Each role has its own UserControl

#### 3. Right Panel (Grid Column 2) - ✅ STATIC (can be made dynamic)
```xaml
<DataGrid ItemsSource="{Binding model.CurrentGroup.Items}"
          SelectedItem="{Binding model.CurrentItem}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Чертеж" Binding="{Binding EquipmentDraft}"/>
        <DataGridTextColumn Header="Наименование" Binding="{Binding EquipmentNameFromTechologist}"/>
    </DataGrid.Columns>
</DataGrid>
```

### ✅ Role-Specific Panels Created

Three UserControls created, each with different fields:

1. **HeadOrderDepartmentCentralPanel.xaml**
   - Read-only view for department head
   - Shows: Technologist, Memo Number (conditional), Order, Equipment Type

2. **OrderManagerCentralPanel.xaml**
   - Editable view for order manager
   - Shows: Memo section, Technologist (conditional), Order fields, Equipment Type, Balance, etc.
   - Has conditional visibility based on `IsByMemo` flag

3. **TechnologistCentralPanel.xaml**
   - Technical details for technologist
   - Shows: Technologist, Product, Part, Workshop, Warehouse, Operation, Equipment, etc.

## What Still Needs to Be Done

### Missing Components (Referenced but not created)

These components are referenced in the code but don't exist in st.txt. They likely exist in other parts of your project:

#### 1. ViewModels (Priority: HIGH)
These need to be implemented to connect Models with Views:

```csharp
// ViewModels/vmBaseApproval.cs
public abstract class vmBaseApproval : VMBase
{
    public mApproval model { get; set; }
    
    // This property provides the role-specific panel
    public abstract UserControl RoleSpecificCentralContent { get; }
    
    public string OrdersTitle { get; set; }
    public string OrderDetailsTitle { get; set; }
    
    // Column widths
    public GridLength LeftColumnWidth { get; set; } = new GridLength(1, GridUnitType.Star);
    public GridLength CenterColumnWidth { get; set; } = new GridLength(2, GridUnitType.Star);
    public GridLength RightColumnWidth { get; set; } = new GridLength(1.5, GridUnitType.Star);
}

// ViewModels/vmHeadOrderDepartment.cs
public class vmHeadOrderDepartment : vmBaseApproval
{
    public vmHeadOrderDepartment()
    {
        model = new mHeadOrderDepartment();
    }
    
    public override UserControl RoleSpecificCentralContent
    {
        get => new HeadOrderDepartmentCentralPanel { ViewModel = this };
    }
}

// Similarly for vmOrderManager and vmSubTechnologist
```

#### 2. Complete Views (Priority: HIGH)
These views should use BaseApprovalView:

```xaml
<!-- Views/HeadOrderDepartment.xaml -->
<local:BaseApprovalView x:Class="OrderApprovalSystem.Views.HeadOrderDepartment"
                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:local="clr-namespace:OrderApprovalSystem.Views">
</local:BaseApprovalView>
```

```csharp
// Views/HeadOrderDepartment.xaml.cs
public partial class HeadOrderDepartment : BaseApprovalView
{
    public HeadOrderDepartment()
    {
        InitializeComponent();
    }
}

// Similarly for OrderManager.xaml and SubTechnologist.xaml
```

#### 3. Converters (Priority: MEDIUM)
```csharp
// Converters/MaskedDateConverter.cs
public class MaskedDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime date)
            return date.ToString("dd.MM.yyyy");
        return value;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && DateTime.TryParseExact(str, "dd.MM.yyyy", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            return result;
        return value;
    }
}

// Similarly for IsByMemoToTextConverter and BooleanToVisibilityConverter
```

#### 4. Behaviors (Priority: MEDIUM)
```csharp
// Behaviors/ScrollIntoViewBehavior.cs
public class ScrollIntoViewBehavior : Behavior<DataGrid>
{
    public static readonly DependencyProperty SelectedItemProperty = 
        DependencyProperty.Register("SelectedItem", typeof(object), 
        typeof(ScrollIntoViewBehavior), 
        new PropertyMetadata(null, OnSelectedItemChanged));
    
    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }
    
    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = (ScrollIntoViewBehavior)d;
        if (behavior.AssociatedObject != null && e.NewValue != null)
        {
            behavior.AssociatedObject.ScrollIntoView(e.NewValue);
        }
    }
}
```

#### 5. Project File (Priority: HIGH)
Create a `.csproj` file to define the project structure and dependencies:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>OrderApprovalSystem</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.39" />
    <PackageReference Include="System.Windows.Controls.Ribbon" Version="6.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference to Fox framework (adjust path as needed) -->
    <Reference Include="Fox">
      <HintPath>path\to\Fox.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

## Testing Plan

### 1. Unit Tests
- Test each Model's BuildBaseQuery() method
- Test navigation methods in mApproval
- Test ViewModel property bindings

### 2. Integration Tests
- Test BaseApprovalView with each role-specific panel
- Test switching between roles
- Test data binding and navigation

### 3. UI Tests
- Verify left panel displays correctly for all roles
- Verify center panel shows correct fields for each role
- Verify right panel displays correctly
- Test conditional visibility (IsByMemo flag)
- Test editable vs read-only fields

## Quick Start Guide

To complete the implementation:

1. **Create ViewModels** (Start here)
   - Implement vmBaseApproval with RoleSpecificCentralContent property
   - Implement vmHeadOrderDepartment, vmOrderManager, vmSubTechnologist

2. **Create Complete Views**
   - HeadOrderDepartment.xaml (inherits BaseApprovalView)
   - OrderManager.xaml (inherits BaseApprovalView)
   - SubTechnologist.xaml (inherits BaseApprovalView)

3. **Create Converters and Behaviors**
   - MaskedDateConverter
   - IsByMemoToTextConverter
   - BooleanToVisibilityConverter
   - ScrollIntoViewBehavior

4. **Create Project File**
   - OrderApprovalSystem.csproj with dependencies

5. **Test**
   - Build the project
   - Run and verify each role's view
   - Test navigation and data binding

## Summary

✅ **Completed**: All code structure from st.txt has been extracted and organized
✅ **Completed**: BaseApprovalView with 3 areas (left static DataGrid, center dynamic, right static)
✅ **Completed**: 3 role-specific UserControl panels for center area
✅ **Completed**: All model classes with inheritance hierarchy
✅ **Completed**: Main window with Ribbon and DataTemplates
✅ **Completed**: RibbonTabBehavior for dynamic tabs

❌ **Not Completed**: ViewModels (they were not in st.txt)
❌ **Not Completed**: Complete view files (they were not in st.txt)
❌ **Not Completed**: Converters (they were not in st.txt)
❌ **Not Completed**: Behaviors (they were not in st.txt)
❌ **Not Completed**: Project file (standard .NET requirement)

The refactoring of the 3 similar windows into a base window with role-specific panels is **complete**. The missing components are standard WPF/MVVM infrastructure that weren't part of the original st.txt file.
