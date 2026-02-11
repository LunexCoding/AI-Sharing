# Visual Structure Diagram

## Window Hierarchy

```
Main.xaml (BaseCustomWindow)
│
├── Ribbon (with RibbonTabBehavior)
│   ├── Dynamic Tab (based on current ViewModel)
│   └── Static Tab (Theme settings)
│
└── ContentControl
    └── Current ViewModel View
        │
        ├── Dev.xaml (vmDev)
        ├── HeadOrderDepartment.xaml (vmHeadOrderDepartment)
        ├── OrderManager.xaml (vmOrderManager)
        └── SubTechnologist.xaml (vmSubTechnologist)
            │
            └── All use BaseApprovalView.xaml
                │
                ├──────────────────┬──────────────────┬──────────────────┐
                │                  │                  │                  │
           LEFT PANEL         CENTER PANEL      RIGHT PANEL         │
                │                  │                  │                  │
                │                  │                  │                  │
        ┌───────────────┐  ┌───────────────┐  ┌───────────────┐        │
        │   DataGrid    │  │ Common Fields │  │  Sub-Orders   │        │
        │   (Static)    │  │  - Product    │  │   DataGrid    │        │
        │               │  │  - Prod. Name │  │               │        │
        │  • Тех. заказ │  │               │  │  Draft Details│        │
        │  • Columns    │  │  ContentCtrl  │  │  • Design     │        │
        │               │  │      ↓        │  │  • Manufact.  │        │
        │  Binds to:    │  │  RoleSpecific │  │               │        │
        │  GroupedData  │  │    Content    │  │               │        │
        │               │  │      ↓        │  │               │        │
        │               │  │  ┌─────────┐  │  │               │        │
        └───────────────┘  │  │ Role-   │  │  └───────────────┘        │
                           │  │Specific │  │                           │
                           │  │ Panel   │  │                           │
                           │  └─────────┘  │                           │
                           └───────────────┘                           │
                                  │                                    │
                    ┌─────────────┼─────────────┐                     │
                    │             │             │                     │
        ┌───────────────────┐ ┌───────────────────┐ ┌───────────────────┐
        │HeadOrderDept      │ │OrderManager       │ │Technologist       │
        │CentralPanel       │ │CentralPanel       │ │CentralPanel       │
        │                   │ │                   │ │                   │
        │• Technologist     │ │• Memo Section     │ │• Technologist     │
        │  (read-only)      │ │  - Number         │ │  (read-only)      │
        │• Memo # (cond.)   │ │  - Author         │ │• Product          │
        │• Order            │ │• Technologist     │ │• Product Name     │
        │• Equipment Type   │ │  (conditional)    │ │• Part             │
        │                   │ │• Order fields     │ │• Part Name        │
        │All READ-ONLY      │ │• Equipment Type   │ │• Workshop         │
        │                   │ │• Balance          │ │• Warehouse        │
        │                   │ │• Nomenclature     │ │• Workplace        │
        │                   │ │• Draft info       │ │• Operation        │
        │                   │ │• Warehouse        │ │• Equipment Name   │
        │                   │ │• Quantity         │ │• Analog           │
        │                   │ │                   │ │• Quantity         │
        │                   │ │All EDITABLE       │ │                   │
        │                   │ │with CONDITIONS    │ │All READ-ONLY      │
        └───────────────────┘ └───────────────────┘ └───────────────────┘
```

## Data Flow

```
User Interaction
       ↓
Main Window (Main.xaml)
       ↓
ViewModel Selection (vmHeadOrderDepartment, vmOrderManager, vmSubTechnologist)
       ↓
Role-Specific View (HeadOrderDepartment.xaml, OrderManager.xaml, SubTechnologist.xaml)
       ↓
BaseApprovalView.xaml
       ↓
       ├→ Left Panel: model.GroupedData → DataGrid
       │
       ├→ Center Panel: RoleSpecificCentralContent → Role Panel UserControl
       │                                                      ↓
       │                                           Model.CurrentItem (Bindings)
       │
       └→ Right Panel: model.CurrentGroup.Items → DataGrid
                      model.CurrentItem.DesignComment
                      model.CurrentItem.ManufacturingComment
```

## Model Hierarchy

```
MBase (Fox Framework)
  ↓
mApproval (Abstract Base)
  │
  ├─ Properties:
  │  • GroupedData
  │  • CurrentGroup
  │  • CurrentItem
  │  • CurrentIndex
  │  • CurrentGroupIndex
  │
  ├─ Methods:
  │  • NavigatePrevious()
  │  • NavigateNext()
  │  • NavigatePreviousGroup()
  │  • NavigateNextGroup()
  │  • FindAndNavigate(searchText)
  │  • LoadData()
  │
  └─ Abstract:
     • BuildBaseQuery()
       ↓
  ┌────┴────┬────────────┬──────────────┐
  │         │            │              │
mHeadOrder mOrderManager mSubTechnologist mMain
Department
  │         │            │
  └─────────┴────────────┴───────→ Implements BuildBaseQuery()
                                    with role-specific LINQ queries
```

## File Organization

```
AI-Sharing/
│
├── Models/
│   ├── mApproval.cs              (Base abstract model)
│   ├── mHeadOrderDepartment.cs   (Head model)
│   ├── mOrderManager.cs          (Manager model)
│   ├── mSubTechnologist.cs       (Technologist model)
│   └── mMain.cs                  (Main app model)
│
├── Views/
│   ├── Main.xaml                 (Main window with Ribbon)
│   ├── BaseApprovalView.xaml     (Base template with 3 panels)
│   ├── RibbonTabBehavior.cs      (Dynamic ribbon behavior)
│   │
│   ├── Dev.xaml                  (Developer view - to be created)
│   ├── HeadOrderDepartment.xaml  (Head view - to be created)
│   ├── OrderManager.xaml         (Manager view - to be created)
│   ├── SubTechnologist.xaml      (Technologist view - to be created)
│   │
│   └── RoleSpecificPanels/
│       ├── HeadOrderDepartmentCentralPanel.xaml/.cs
│       ├── OrderManagerCentralPanel.xaml/.cs
│       └── TechnologistCentralPanel.xaml/.cs
│
├── ViewModels/                   (To be created)
│   ├── vmBaseApproval
│   ├── vmHeadOrderDepartment
│   ├── vmOrderManager
│   ├── vmSubTechnologist
│   └── vmDev
│
├── Converters/                   (To be created)
│   ├── MaskedDateConverter
│   ├── IsByMemoToTextConverter
│   └── BooleanToVisibilityConverter
│
├── Behaviors/                    (To be created)
│   └── ScrollIntoViewBehavior
│
├── README.md                     (Architecture documentation)
├── SUMMARY.md                    (Refactoring summary)
└── DIAGRAM.md                    (This file)
```

## Key Design Decisions

### 1. Left Panel - Static in BaseApprovalView
- ✅ DataGrid with all columns defined directly
- ✅ Same for all roles
- ✅ No duplication

### 2. Center Panel - Dynamic UserControl
- ✅ Common fields in BaseApprovalView
- ✅ Role-specific content via ContentControl
- ✅ Each role has its own UserControl
- ✅ Clean separation of concerns

### 3. Right Panel - Static (Can be made dynamic)
- Currently static
- Could be made dynamic in future if needed
- Same pattern as center panel

### 4. Models - Inheritance
- Base abstract model (mApproval) with common functionality
- Role-specific models inherit and implement BuildBaseQuery()
- Clean separation of data access logic

### 5. ViewModels - To Be Implemented
- Each role needs a ViewModel
- ViewModels should expose RoleSpecificCentralContent property
- ViewModels bind to Models and Views

## Benefits Achieved

1. **No Code Duplication**
   - Common UI is in BaseApprovalView
   - Only role-specific panels are different

2. **Easy Maintenance**
   - Changes to common areas update all roles
   - Clear separation of concerns

3. **Extensibility**
   - Easy to add new roles
   - Just create new panel UserControl and ViewModel

4. **Clean Architecture**
   - Proper MVVM pattern
   - Models handle data
   - ViewModels handle logic
   - Views handle presentation
   - Panels handle role-specific UI
