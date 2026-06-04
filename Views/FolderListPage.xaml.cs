// Views/FolderListPage.xaml.cs
using System.Collections.Specialized;
using CopilotCostTracker.Models;
using CopilotCostTracker.ViewModels;
using Microsoft.Maui.Controls;

namespace CopilotCostTracker.Views;

public partial class FolderListPage : ContentPage
{
    private readonly FolderListViewModel _vm;

    public FolderListPage(FolderListViewModel vm)
    {
        _vm = vm;
        BindingContext = vm;
        InitializeComponent();

        // Build initial rows
        RebuildFolderRows();

        // Keep in sync with collection changes
        vm.WatchedFolders.CollectionChanged += OnFoldersChanged;
    }

    private void OnFoldersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RebuildFolderRows();

    private void RebuildFolderRows()
    {
        FoldersLayout.Children.Clear();
        foreach (var folder in _vm.WatchedFolders)
            FoldersLayout.Children.Add(BuildFolderRow(folder));
    }

    private View BuildFolderRow(WatchedFolder folder)
    {
        var pathLabel = new Label
        {
            FontSize = 13,
            LineBreakMode = LineBreakMode.MiddleTruncation,
            VerticalOptions = LayoutOptions.Center,
        };
        pathLabel.SetBinding(Label.TextProperty, new Binding(nameof(WatchedFolder.Path)));
        pathLabel.SetDynamicResource(Label.StyleProperty, "TextPrimaryStyle");

        var removeBtn = new Button
        {
            Text = "✕",
            FontSize = 14,
            WidthRequest = 32,
            HeightRequest = 32,
            CornerRadius = 16,
            BackgroundColor = Colors.Transparent,
            CommandParameter = folder,
        };
        removeBtn.SetDynamicResource(Button.TextColorProperty, "TextSecondaryColor");
        removeBtn.Clicked += OnRemoveClicked;

        var topRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(32)) };
        topRow.Add(pathLabel, 0, 0);
        topRow.Add(removeBtn, 1, 0);

        var subdirsLabel = new Label
        {
            Text = "Include subdirectories",
            FontSize = 13,
            VerticalOptions = LayoutOptions.Center,
        };
        subdirsLabel.SetDynamicResource(Label.StyleProperty, "TextSecondaryStyle");

        var toggle = new Switch { BindingContext = folder };
        toggle.SetBinding(Switch.IsToggledProperty, new Binding(nameof(WatchedFolder.IncludeSubdirectories)));
        toggle.SetDynamicResource(Switch.OnColorProperty, "AccentBlueColor");
        toggle.Toggled += OnToggleSubdirs;

        var bottomRow = new HorizontalStackLayout { Spacing = 8 };
        bottomRow.Children.Add(subdirsLabel);
        bottomRow.Children.Add(toggle);

        var inner = new Grid { RowSpacing = 8 };
        inner.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        inner.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        inner.Add(topRow, 0, 0);
        inner.Add(bottomRow, 0, 1);

        var border = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Padding = new Thickness(12, 10),
            BindingContext = folder,
        };
        border.SetDynamicResource(Border.BackgroundColorProperty, "SurfaceSecondaryColor");
        border.Content = inner;

        return border;
    }

    private void OnRemoveClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is WatchedFolder folder)
            _vm.RemoveFolderCommand.Execute(folder);
    }

    private void OnToggleSubdirs(object? sender, ToggledEventArgs e)
    {
        if (sender is Switch sw && sw.BindingContext is WatchedFolder folder)
            _vm.ToggleSubdirsCommand.Execute(folder);
    }
}
