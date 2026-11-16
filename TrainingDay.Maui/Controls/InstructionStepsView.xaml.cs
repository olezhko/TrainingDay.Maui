using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.Controls;

/// <summary>Read-only exercise instructions, shown as numbered steps, bullets or a paragraph.</summary>
public partial class InstructionStepsView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(InstructionStepsView), propertyChanged: OnContentChanged);

    public static readonly BindableProperty TagsProperty = BindableProperty.Create(
        nameof(Tags), typeof(IEnumerable<ExerciseTags>), typeof(InstructionStepsView), propertyChanged: OnContentChanged);

    public static readonly BindableProperty IsBulletListProperty = BindableProperty.Create(
        nameof(IsBulletList), typeof(bool), typeof(InstructionStepsView), false, propertyChanged: OnContentChanged);

    public static readonly BindableProperty MarkerColorProperty = BindableProperty.Create(
        nameof(MarkerColor), typeof(Color), typeof(InstructionStepsView), Colors.LimeGreen);

    public InstructionStepsView()
    {
        InitializeComponent();
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>The exercise's tags, which decide how execution text is shown.</summary>
    public IEnumerable<ExerciseTags>? Tags
    {
        get => (IEnumerable<ExerciseTags>?)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    /// <summary>Shows sentences that have no order, such as advice, instead of execution steps.</summary>
    public bool IsBulletList
    {
        get => (bool)GetValue(IsBulletListProperty);
        set => SetValue(IsBulletListProperty, value);
    }

    public Color MarkerColor
    {
        get => (Color)GetValue(MarkerColorProperty);
        set => SetValue(MarkerColorProperty, value);
    }

    private static void OnContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((InstructionStepsView)bindable).ShowSteps();
    }

    private void ShowSteps()
    {
        var steps = IsBulletList
            ? InstructionStepsParser.ParseBullets(Text)
            : InstructionStepsParser.ParseExecution(Text, Tags, AppResources.StepOfFormat);
        bool isPlain = steps.Count == 1 && steps[0].Kind == InstructionStepKind.Plain;

        PlainLabel.Text = isPlain ? steps[0].Text : string.Empty;
        PlainLabel.IsVisible = isPlain;

        BindableLayout.SetItemsSource(StepsLayout, isPlain ? null : steps);
        StepsLayout.IsVisible = !isPlain && steps.Count > 0;
    }
}
