---
name: dotnet-wpf
description: WPF UI patterns and Material Design standards — applies when working with XAML, Views, or UI files
alwaysApply: false
conditions:
  - filename:
      contains:
        - .xaml
        - View
        - Dialog
        - Window
        - Control
        - Form
  - language:
      contains:
        - xml
---

# dotnet-wpf: WPF & Material Design Patterns — Radio Project

## WPF Namespace (ALWAYS EXACT)
```xml
xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
```
> ❌ NEVER use `xmlns:md="..."` — causes runtime XAML load failure.

## Exact Style Keys (MANDATORY)
| Element | Style Key |
|:---|:---|
| TextBox | `Input.Text` |
| TextBox multiline | `Input.Text.Multiline` |
| ComboBox | `Input.ComboBox` |
| DatePicker | `Input.DatePicker` |
| TimePicker | `Input.TimePicker` |
| Primary button | `Btn.Primary` |
| Cancel button | `Btn.Cancel` |
| Add new button | `Btn.AddNew` |
| Header color zone | `Zone.Header.Primary` |
| Window footer | `Window.Footer` |
| DataGrid | `DataGrid.Main` |
| DataGrid row | `DataGrid.Row` |
| DataGrid cell | `DataGrid.Cell` |
| DataGrid cell centered | `DataGrid.Cell.Center` |
| DataGrid cell actions | `DataGrid.Cell.Actions` |
| DataGrid column header | `DataGrid.ColumnHeader.Center` |
| View base | `View.Base` |
| Stat card | `Card.Stat` |
| Search input | `Input.Search` |

## Dialog Pattern
```csharp
// Open dialog
var view = new EpisodeFormControl(services..., session, episodeId);
var result = await DialogHost.Show(view, "RootDialog");
if (result is true) await LoadDataAsync();

// Close from inside dialog
DialogHost.Close("RootDialog", true);   // saved
DialogHost.Close("RootDialog", false);  // cancelled
```

## Standard View Layout
```xml
<UserControl Style="{StaticResource View.Base}" FlowDirection="RightToLeft">
  <Grid>
    <materialDesign:ColorZone Style="{StaticResource Zone.Header.Primary}">
      <!-- icon + title + Btn.AddNew -->
    </materialDesign:ColorZone>
    <Grid Grid.Row="1" Margin="24,20,24,10">
      <TextBox Style="{StaticResource Input.Search}" />
      <materialDesign:Card Style="{StaticResource Card.Stat}" />
    </Grid>
    <materialDesign:Card Grid.Row="2" Margin="24,10,24,24"
                         UniformCornerRadius="12"
                         materialDesign:ElevationAssist.Elevation="Dp1">
      <DataGrid Style="{StaticResource DataGrid.Main}"
                RowStyle="{StaticResource DataGrid.Row}"
                CellStyle="{StaticResource DataGrid.Cell}"
                ColumnHeaderStyle="{StaticResource DataGrid.ColumnHeader.Center}" />
    </materialDesign:Card>
  </Grid>
</UserControl>
```

## Form Dialog Layout (fixed size — prevents tab resize)
```xml
<UserControl Width="860" Height="700" FlowDirection="RightToLeft">
  <materialDesign:Card Background="{StaticResource SurfaceBrush}"
                        Effect="{StaticResource Shadow.Dialog}"
                        UniformCornerRadius="16">
    <Grid>
      <materialDesign:ColorZone Style="{StaticResource Zone.Header.Primary}" />
      <TabControl Grid.Row="1" Style="{StaticResource MaterialDesignTabControl}" />
      <Border Grid.Row="2" Style="{StaticResource Window.Footer}">
        <Button Style="{StaticResource Btn.Cancel}" Click="BtnCancel_Click" />
        <Button Style="{StaticResource Btn.Primary}" Click="BtnSave_Click" />
      </Border>
    </Grid>
  </materialDesign:Card>
</UserControl>
```

## Input Section (inside forms — for adding items to DataGrid)
```xml
<Border Background="{StaticResource PrimaryXLightBrush}"
        Padding="12" CornerRadius="8,8,0,0">
  <Grid>
    <ComboBox Style="{StaticResource Input.ComboBox}" />
    <TextBox  Style="{StaticResource Input.Text}" Grid.Column="2" />
    <materialDesign:TimePicker Style="{StaticResource Input.TimePicker}" Grid.Column="4" />
    <Button Width="40" Height="40" Style="{StaticResource Btn.Primary}" Grid.Column="6">
      <materialDesign:PackIcon Kind="Plus" Foreground="{StaticResource SurfaceBrush}" />
    </Button>
  </Grid>
</Border>
<DataGrid Style="{StaticResource DataGrid.Main}" ... />
```

## MainWindow Navigation (4 steps to add a tab)
1. `MainWindow.xaml` — add RadioButton with `Style="{StaticResource HubTabItem}"` and `Click="Tab_Click"`
2. `MainWindow.xaml.cs` → `LoadView()` — add case with `_serviceProvider.GetRequiredService<IService>()`
3. `MainWindow.xaml.cs` → `ApplyPermissionSecurity()` — set Visibility based on `_session.HasPermission()`
4. `App.xaml.cs` — register service with `services.AddScoped<IService, Service>()`

## Anti-Patterns (DO NOT DO)
- ❌ `xmlns:md="...materialDesign..."` → ✅ `xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"`
- ❌ `BroadcastTextBox` → ✅ `Input.Text`
- ❌ `BroadcastComboBox` → ✅ `Input.ComboBox`
- ❌ `BroadcastDatePicker` → ✅ `Input.DatePicker`
- ❌ Set ItemsSource in XAML → ✅ Set in code-behind after data loads
- ❌ `new ServiceClass()` in View → ✅ `_serviceProvider.GetRequiredService<IService>()`
