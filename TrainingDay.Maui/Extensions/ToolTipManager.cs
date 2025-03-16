using System.Collections;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Extensions;

public enum Tooltips
{
    ToolTipAddTrainingButton,
    ToolTipTapHoldTraining,
    ToolTipSuperSet,
    ToolTipFilterMuscle,
    ToolTipGoogleSync,
    ToolTipWorkingCancel,
    ToolTipWorkingSkip,
    ToolTipWorkingReady,
    ToolTipHoldImageExercises,
    ToolTipStartWorkout,
    ToolTipLetsBuildYourWorkout
}

public class ToolTipManager
{
    public bool ToolTipWorkingCancel
    {
        get => Convert(Tooltips.ToolTipWorkingCancel);
        set => ConvertBack(Tooltips.ToolTipWorkingCancel, value);
    }

    public bool ToolTipWorkingSkip
    {
        get => Convert(Tooltips.ToolTipWorkingSkip);
        set => ConvertBack(Tooltips.ToolTipWorkingSkip, value);
    }

    public bool ToolTipWorkingReady
    {
        get => Convert(Tooltips.ToolTipWorkingReady);
        set => ConvertBack(Tooltips.ToolTipWorkingReady, value);
    }

    public bool ToolTipAddTrainingButton
    {
        get => Convert(Tooltips.ToolTipAddTrainingButton);
        set => ConvertBack(Tooltips.ToolTipAddTrainingButton, value);
    }

    public bool ToolTipTapHoldTraining
    {
        get => Convert(Tooltips.ToolTipTapHoldTraining);
        set => ConvertBack(Tooltips.ToolTipTapHoldTraining, value);
    }

    public bool ToolTipSuperSet
    {
        get => Convert(Tooltips.ToolTipSuperSet);
        set => ConvertBack(Tooltips.ToolTipSuperSet, value);
    }

    public bool ToolTipFilterMuscle
    {
        get => Convert(Tooltips.ToolTipFilterMuscle);
        set => ConvertBack(Tooltips.ToolTipFilterMuscle, value);
    }

    public bool ToolTipGoogleSync
    {
        get => Convert(Tooltips.ToolTipGoogleSync);
        set => ConvertBack(Tooltips.ToolTipGoogleSync, value);
    }

    public bool ToolTipHoldImageExercises
    {
        get => Convert(Tooltips.ToolTipHoldImageExercises);
        set => ConvertBack(Tooltips.ToolTipHoldImageExercises, value);
    }

    public bool ToolTipStartWorkout
    {
        get => Convert(Tooltips.ToolTipStartWorkout);
        set => ConvertBack(Tooltips.ToolTipStartWorkout, value);
    }

    public bool ToolTipLetsBuildYourWorkout
    {
        get => Convert(Tooltips.ToolTipLetsBuildYourWorkout);
        set => ConvertBack(Tooltips.ToolTipLetsBuildYourWorkout, value);
    }

    public bool Convert(Tooltips parameter)
    {
        BitArray array = new BitArray(new[] { Settings.ToolTipsState });
        return array.Get((int)parameter);
    }

    public void ConvertBack(Tooltips parameter, bool value)
    {
        BitArray array = new BitArray(new[] { Settings.ToolTipsState });

        array.Set((int)parameter, value);
        var res = new int[1];
        array.CopyTo(res, 0);
        Settings.ToolTipsState = res[0];
    }
}