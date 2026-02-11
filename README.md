# Order Approval System - Refactored Structure

## Overview

This refactoring implements a base window pattern with role-specific panels to eliminate code duplication across 3 similar windows.

## Architecture

### Base Window (BaseApprovalView.xaml)

The `BaseApprovalView` is divided into 3 main areas:

1. **Left Panel (Static)** - DataGrid with order groups
   - Same for all roles
   - Shows technical orders (Тех. заказ)
   - Columns are defined directly in BaseApprovalView
   - Binds to `model.GroupedData`

2. **Center Panel (Dynamic)** - Order details
   - Common fields (Product, Product Name) are in BaseApprovalView
   - Role-specific content is injected via `ContentControl`
   - Binds to `RoleSpecificCentralContent` property in ViewModel
   - Each role has its own UserControl:
     - `HeadOrderDepartmentCentralPanel` - Read-only view for department head
     - `OrderManagerCentralPanel` - Editable fields for order manager
     - `TechnologistCentralPanel` - Technical details for technologist

3. **Right Panel (Static)** - Sub-orders and draft details
   - Same for all roles
   - Shows sub-order grid and design/manufacturing comments

### Role-Specific Panels

Located in `Views/RoleSpecificPanels/`:

#### HeadOrderDepartmentCentralPanel
- Read-only view
- Shows: Technologist, Memo Number (conditional), Order, Equipment Type

#### OrderManagerCentralPanel
- Editable fields
- Shows: Memo data (conditional), Technologist (conditional), Order details, Equipment type, Balance, Nomenclature group, Draft info, Warehouse, Quantity
- Has conditional visibility based on `IsByMemo` flag

#### TechnologistCentralPanel
- Technical details
- Shows: Technologist, Product, Product Name, Part, Part Name, Workshop, Warehouse, Workplace, Operation, Equipment Name, Analog, Quantity per operation

### Models

Located in `Models/`:

- **mApproval** - Abstract base model with common functionality
  - Data loading
  - Navigation (Previous/Next, PreviousGroup/NextGroup)
  - Search
  - Properties: GroupedData, CurrentGroup, CurrentItem, etc.

- **mHeadOrderDepartment** - Model for department head
- **mOrderManager** - Model for order manager
- **mSubTechnologist** - Model for technologist
- **mMain** - Main application model

### Views Structure

```
Views/
├── Main.xaml - Main window with Ribbon
├── BaseApprovalView.xaml - Base approval view with 3 areas
├── RibbonTabBehavior.cs - Dynamic ribbon tab behavior
├── Dev.xaml - Developer view (uses BaseApprovalView)
├── HeadOrderDepartment.xaml - Department head view (uses BaseApprovalView)
├── OrderManager.xaml - Order manager view (uses BaseApprovalView)
├── SubTechnologist.xaml - Technologist view (uses BaseApprovalView)
└── RoleSpecificPanels/
    ├── HeadOrderDepartmentCentralPanel.xaml
    ├── OrderManagerCentralPanel.xaml
    └── TechnologistCentralPanel.xaml
```

## Benefits

1. **No Code Duplication** - Common UI is in BaseApprovalView
2. **Easy Maintenance** - Changes to common areas update all roles
3. **Role-Specific Customization** - Each role can have unique center panel
4. **Clean Separation** - Models, Views, and Panels are properly separated

## Usage

Each role's view should:
1. Use BaseApprovalView as the main template
2. Set the `RoleSpecificCentralContent` property to the appropriate UserControl
3. Implement the required ViewModel (vmHeadOrderDepartment, vmOrderManager, vmSubTechnologist)

Example ViewModel binding:
```csharp
public UserControl RoleSpecificCentralContent 
{
    get 
    {
        return new OrderManagerCentralPanel { ViewModel = this };
    }
}
```
