# Button Styles Reference

## ? Available Button Styles

All button styles are defined in `Aml.BOM.Import.UI\Styles\AppStyles.xaml`

---

## ?? Button Styles

### 1. PrimaryButtonStyle
**Use for**: Main actions, primary operations

**Appearance**:
- **Background**: Blue (#3498DB)
- **Hover**: Darker Blue (#2980B9)
- **Disabled**: Gray (#BDC3C7)
- **Text**: White
- **Padding**: 15px horizontal, 8px vertical
- **Border Radius**: 4px

**Example**:
```xml
<Button Content="Import File" 
        Command="{Binding ImportFileCommand}"
        Style="{StaticResource PrimaryButtonStyle}"/>
```

**Used For**:
- Import File
- Save Settings
- Primary submit buttons

---

### 2. SecondaryButtonStyle
**Use for**: Secondary actions, cancel, refresh

**Appearance**:
- **Background**: Gray (#95A5A6)
- **Hover**: Darker Gray (#7F8C8D)
- **Text**: White
- **Padding**: 15px horizontal, 8px vertical
- **Border Radius**: 4px

**Example**:
```xml
<Button Content="Refresh" 
        Command="{Binding RefreshCommand}"
        Style="{StaticResource SecondaryButtonStyle}"/>
```

**Used For**:
- Refresh
- Revalidate All
- Cancel
- Clear
- Secondary actions

---

### 3. SuccessButtonStyle ? NEW
**Use for**: Success actions, integration, positive operations

**Appearance**:
- **Background**: Green (#4CAF50)
- **Hover**: Darker Green (#45A049)
- **Disabled**: Gray (#BDC3C7)
- **Text**: White
- **Padding**: 15px horizontal, 8px vertical
- **Border Radius**: 4px

**Example**:
```xml
<Button Content="Integrate BOMs" 
        Command="{Binding IntegrateBomsCommand}"
        Style="{StaticResource SuccessButtonStyle}"/>
```

**Used For**:
- Integrate BOMs
- Integrate Make Items
- Create/Complete actions
- Success operations

---

### 4. NavigationButtonStyle
**Use for**: Sidebar navigation

**Appearance**:
- **Background**: Transparent
- **Hover**: Dark Blue (#34495E)
- **Text**: White
- **Padding**: 15px horizontal, 10px vertical
- **Border Radius**: 4px
- **Alignment**: Left

**Example**:
```xml
<Button Content="Settings" 
        Command="{Binding NavigateToSettingsCommand}"
        Style="{StaticResource NavigationButtonStyle}"/>
```

**Used For**:
- Sidebar navigation buttons
- Menu items

---

## ?? Color Palette

| Style | Normal | Hover | Disabled | Usage |
|-------|--------|-------|----------|-------|
| **Primary** | #3498DB (Blue) | #2980B9 | #BDC3C7 | Main actions |
| **Secondary** | #95A5A6 (Gray) | #7F8C8D | - | Secondary actions |
| **Success** | #4CAF50 (Green) | #45A049 | #BDC3C7 | Success/Integration |
| **Navigation** | Transparent | #34495E | - | Sidebar menu |

---

## ?? Usage Examples

### Toolbar with Multiple Button Styles

```xml
<StackPanel Orientation="Horizontal" Margin="0,0,0,10">
    <!-- Primary action -->
    <Button Content="Import File" 
            Command="{Binding ImportFileCommand}"
            Style="{StaticResource PrimaryButtonStyle}"
            Margin="0,0,10,0"/>
    
    <!-- Secondary action -->
    <Button Content="Revalidate All" 
            Command="{Binding RevalidateAllCommand}"
            Style="{StaticResource SecondaryButtonStyle}"
            Margin="0,0,10,0"/>
    
    <!-- Success action -->
    <Button Content="Integrate BOMs" 
            Command="{Binding IntegrateBomsCommand}"
            Style="{StaticResource SuccessButtonStyle}"
            Margin="0,0,10,0"/>
    
    <!-- Secondary action -->
    <Button Content="Refresh" 
            Command="{Binding RefreshCommand}"
            Style="{StaticResource SecondaryButtonStyle}"/>
</StackPanel>
```

**Result**:
```
[Import File] [Revalidate All] [Integrate BOMs] [Refresh]
   (Blue)         (Gray)           (Green)        (Gray)
```

---

## ?? Button Style Selection Guide

### When to Use Each Style

**PrimaryButtonStyle** ?
- Main action on a page
- Submit buttons
- Import operations
- Save operations
- **Limit**: 1-2 per view

**SecondaryButtonStyle** ?
- Supporting actions
- Refresh/Reload
- Clear/Reset
- Cancel
- **Usage**: Multiple per view

**SuccessButtonStyle** ?
- Integration actions
- Complete/Finish
- Create operations
- Positive confirmations
- **Limit**: 1 per view typically

**NavigationButtonStyle** ?
- Sidebar navigation only
- Menu items
- **Usage**: Navigation panel

---

## ?? Important Notes

### Resource Dictionary Location
```
Aml.BOM.Import.UI\Styles\AppStyles.xaml
```

### Must Be Loaded in App.xaml
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Styles/AppStyles.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### Style Names Are Case-Sensitive
```xml
<!-- ? Correct -->
Style="{StaticResource SuccessButtonStyle}"

<!-- ? Wrong -->
Style="{StaticResource successbuttonstyle}"
Style="{StaticResource SuccessButton}"
```

---

## ?? Customizing Button Styles

### Override Individual Properties

```xml
<Button Content="Custom Button" 
        Style="{StaticResource PrimaryButtonStyle}"
        Padding="20,10"
        FontSize="16"/>
```

### Create Custom Style Based on Existing

```xml
<Style x:Key="LargePrimaryButtonStyle" 
       BasedOn="{StaticResource PrimaryButtonStyle}"
       TargetType="Button">
    <Setter Property="FontSize" Value="16"/>
    <Setter Property="Padding" Value="20,12"/>
</Style>
```

---

## ?? Visual Preview

### Primary Button
```
????????????????????
?   Import File    ?  ? Blue background
????????????????????
    White text
```

### Secondary Button
```
????????????????????
?     Refresh      ?  ? Gray background
????????????????????
    White text
```

### Success Button
```
????????????????????
? Integrate BOMs   ?  ? Green background
????????????????????
    White text
```

### Button States

**Normal**:
```
????????????????????
?      Click       ?
????????????????????
```

**Hover**:
```
????????????????????
?      Click       ?  ? Darker shade
????????????????????
   Cursor: Hand ?
```

**Disabled**:
```
????????????????????
?      Click       ?  ? Gray (#BDC3C7)
????????????????????
   Cannot click
```

---

## ?? Complete Style Definitions

### SuccessButtonStyle (Full Code)

```xml
<Style x:Key="SuccessButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="#4CAF50"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="BorderThickness" Value="0"/>
    <Setter Property="Padding" Value="15,8"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}" 
                       Padding="{TemplateBinding Padding}"
                       CornerRadius="4">
                    <ContentPresenter HorizontalAlignment="Center"
                                    VerticalAlignment="Center"/>
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="#45A049"/>
        </Trigger>
        <Trigger Property="IsEnabled" Value="False">
            <Setter Property="Background" Value="#BDC3C7"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

---

## ? Summary

### Available Styles
- ? **PrimaryButtonStyle** - Blue, for main actions
- ? **SecondaryButtonStyle** - Gray, for secondary actions
- ? **SuccessButtonStyle** - Green, for success/integration
- ? **NavigationButtonStyle** - Transparent, for navigation

### Key Features
- ?? Consistent colors across app
- ??? Hover effects
- ?? Disabled states
- ?? Rounded corners (4px)
- ?? Hand cursor on hover

### File Location
```
Aml.BOM.Import.UI\Styles\AppStyles.xaml
```

---

**Status**: ? All button styles defined and working  
**Build**: ? Successful  
**Ready**: ? For use in all views

?? **Consistent, professional button styling across the entire application!** ??
