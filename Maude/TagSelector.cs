using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls.Shapes;

namespace Maude;

/// <summary>
/// Simple horizontal tag selector used to present a small number of options as "chips".
/// </summary>
internal class TagSelector : HorizontalStackLayout
{
    public TagSelector()
    {
        Spacing = 8;
    }

    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
        nameof(Items),
        typeof(IReadOnlyList<TagSelectorOption>),
        typeof(TagSelector),
        defaultValue: null,
        propertyChanged: OnItemsChanged);

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem),
        typeof(TagSelectorOption),
        typeof(TagSelector),
        defaultValue: null,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: OnSelectedItemChanged);

    public IReadOnlyList<TagSelectorOption> Items
    {
        get => (IReadOnlyList<TagSelectorOption>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public TagSelectorOption? SelectedItem
    {
        get => (TagSelectorOption?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public event EventHandler<TagSelectorSelectionChangedEventArgs>? SelectionChanged;

    private readonly Dictionary<TagSelectorOption, Border> optionViews = new();

    private static void OnItemsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TagSelector selector)
        {
            selector.RenderTags();
        }
    }

    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TagSelector selector)
        {
            selector.UpdateSelectionVisuals();
            selector.SelectionChanged?.Invoke(selector, new TagSelectorSelectionChangedEventArgs((TagSelectorOption?)newValue));
        }
    }

    private void RenderTags()
    {
        Children.Clear();
        foreach (var kvp in optionViews)
        {
            foreach (var recognizer in kvp.Value.GestureRecognizers.OfType<TapGestureRecognizer>())
            {
                recognizer.Tapped -= OnTapped;
            }
        }

        optionViews.Clear();

        if (Items == null || Items.Count == 0)
        {
            SelectedItem = null;
            return;
        }

        foreach (var option in Items)
        {
            var label = new Label
            {
                Text = option.Label,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Padding = new Thickness(8, 4),
                FontSize = 12
            };

            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
                StrokeThickness = 1,
                Padding = new Thickness(0),
                Content = label
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += OnTapped;
            border.GestureRecognizers.Add(tap);

            optionViews[option] = border;
            Children.Add(border);
        }

        if (SelectedItem == null || !optionViews.ContainsKey(SelectedItem))
        {
            SelectedItem = Items.First();
        }
        else
        {
            UpdateSelectionVisuals();
        }
    }

    private void UpdateSelectionVisuals()
    {
        foreach (var kvp in optionViews)
        {
            var isSelected = kvp.Key.Equals(SelectedItem);
            kvp.Value.BackgroundColor = isSelected ? MaudeConstants.MaudeBrandColor : Colors.Transparent;
            kvp.Value.Stroke = isSelected ? MaudeConstants.MaudeBrandColor : Colors.Gray;
            if (kvp.Value.Content is Label label)
            {
                label.TextColor = isSelected ? Colors.White : Colors.Black;
            }
        }
    }

    private void OnTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border)
        {
            var target = optionViews.FirstOrDefault(kvp => ReferenceEquals(kvp.Value, border)).Key;
            if (target != null)
            {
                SelectedItem = target;
            }
        }
    }
}

internal record TagSelectorOption(string Label, TimeSpan Duration);

internal sealed class TagSelectorSelectionChangedEventArgs : EventArgs
{
    public TagSelectorSelectionChangedEventArgs(TagSelectorOption? selected)
    {
        Selected = selected;
    }

    public TagSelectorOption? Selected { get; }
}
