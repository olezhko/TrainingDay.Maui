using CommunityToolkit.Mvvm.Messaging;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Collections.ObjectModel;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Views;

public partial class FilterPage : ContentPage, IQueryAttributable
{
    public FilterModel Filter { get; set; }

    public FilterPage()
    {
        InitializeComponent();

        var itemsMuscles = new ObservableCollection<MuscleCheckItem>();

        for (int i = 0; i < (int)MusclesEnum.None; i++)
        {
            var type = (MusclesEnum)i;
            var newItem = new MuscleCheckItem()
            {
                Text = ExerciseExtensions.GetEnumDescription(type, Settings.GetLanguage()),
                Muscle = type,
            };
            newItem.PropertyChanged += NewItem_PropertyChanged;
            itemsMuscles.Add(newItem);
        }

        BindingContext = this;
        BindableLayout.SetItemsSource(MusclesListView, itemsMuscles);
    }

    private void SetMuscleFilter(FilterModel filter)
    {
        Filter = filter;
        var itemsSource = BindableLayout.GetItemsSource(MusclesListView);
        NoEquipmentCheckBox.IsChecked = filter.IsNoEquipmentFilter;
        foreach (MuscleCheckItem item in itemsSource)
        {
            item.IsChecked = Filter.CurrentMuscles.Contains(item.Muscle);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SkiaView.InvalidateSurface();

        MuscleImage.WidthRequest = DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        MuscleImage.HeightRequest = MuscleImage.WidthRequest / (496.0 / 514.0);

        SkiaView.WidthRequest = MuscleImage.WidthRequest;
        SkiaView.HeightRequest = MuscleImage.HeightRequest;
        MainGrid.RowDefinitions[1].Height = MuscleImage.HeightRequest;
    }

    #region Draw
    private float scaleX;
    private float scaleY;
    SKPaint fillPaint = new SKPaint
    {
        Style = SKPaintStyle.Fill,
        Color = SKColors.Red,
        StrokeWidth = 1,
        StrokeJoin = SKStrokeJoin.Round
    };
    private SKCanvas canvas;
    //HeightRequest="514" WidthRequest="496"
    private double _width, _height;
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width != _width || height != _height)
        {
            _width = width;
            _height = height;
        }
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        scaleX = (float)(SkiaView.Width / 496);
        scaleY = (float)(SkiaView.Height / 514);
        // the the canvas and properties
        canvas = e.Surface.Canvas;
        var scaleW = (float)(e.Info.Width / SkiaView.Width);
        var scaleH = (float)(e.Info.Height / SkiaView.Height);
        // handle the device screen density
        canvas.Scale(scaleW, scaleH);
        canvas.Clear();

        foreach (var currentMuscle in Filter.CurrentMuscles)
        {
            DrawMuscles(currentMuscle);
        }
    }

    private void DrawMuscles(MusclesEnum muscle)
    {
        if (canvas == null)
            return;

        switch (muscle)
        {
            case MusclesEnum.Trapezium:
                DrawTrapezium();
                break;
            case MusclesEnum.ShouldersFront:
                DrawShouldersFront();
                break;
            case MusclesEnum.ShouldersBack:
                DrawShouldersBack();
                break;
            case MusclesEnum.ShouldersMiddle:
                DrawShouldersMiddle();
                break;
            case MusclesEnum.WidestBack:
                DrawWidestBack();
                break;
            case MusclesEnum.MiddleBack:
                DrawMiddleBack();
                break;
            case MusclesEnum.Chest:
                DrawChest();
                break;
            case MusclesEnum.Neck:
                DrawNeck();
                break;
            case MusclesEnum.Abdominal:
                DrawAbdominal();
                break;
            case MusclesEnum.Triceps:
                DrawTriceps();
                break;
            case MusclesEnum.Biceps:
                DrawBiceps();
                break;
            case MusclesEnum.Forearm:
                DrawForearm();
                break;
            case MusclesEnum.Quadriceps:
                DrawQuadriceps();
                break;
            case MusclesEnum.Caviar:
                DrawCaviar();
                break;
            case MusclesEnum.ShinCamboloid:
                DrawShinCamboloid();
                break;
            case MusclesEnum.Thighs:
                DrawThighs();
                break;
            case MusclesEnum.Buttocks:
                DrawButtocks();
                break;
            case MusclesEnum.ShinAnteriorTibialis:
                DrawShinAnteriorTibialis();
                break;
            case MusclesEnum.None:
                break;
            case MusclesEnum.ErectorSpinae:
                break;
        }
    }

    private void DrawShinAnteriorTibialis()
    {
        SKPath path = new SKPath();

        string step1 = "158,386,156,392,153,399,151,407,151,415,151,427,151,438,151,444,151,446,156,446,159,436,161,427,163,418,163,408,162,399";
        DrawStringImplement(step1, ref path);

        string step2 = "79,374,79,382,78,386,76,392,75,402,75,412,77,420,80,431,84,444,85,446,90,447,90,434,91,422,91,414,87,398";
        DrawStringImplement(step2, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawShouldersBack()
    {
        SKPath path = new SKPath();

        string step1 = "409,106,422,97,425,97,428,97,433,98,438,100,443,104,447,107,449,110,450,115,451,120,443,116,438,116,434,115,415,107";
        DrawStringImplement(step1, ref path);

        string step2 = "326,106,313,97,310,97,308,97,302,98,298,98,295,101,291,103,288,106,285,109,284,115,283,119,287,117,293,115,298,115,304,114";
        DrawStringImplement(step2, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawShouldersMiddle()
    {
        SKPath path = new SKPath();

        string step1 = "201,149,204,142,207,134,206,126,204,119,200,112,195,106,188,102,185,100,179,101,177,104,176,106,179,111,183,114,187,119,189,122,191,128,192,134";
        DrawStringImplement(step1, ref path);

        string step2 = "56,100,52,98,48,99,43,101,39,103,35,107,31,113,28,120,27,125,27,131,29,137,32,144,33,145,36,145,41,139,42,134,42,124,43,117,46,112,52,104";
        DrawStringImplement(step2, ref path);

        string step3 = "274,132,282,119,283,115,285,110,287,106,292,102,297,99,303,98,308,97,310,97,305,94,300,92,295,93,291,94,286,96,282,100,280,104,277,109,274,114,273,118,272,122,273,126";
        DrawStringImplement(step3, ref path);

        string step4 = "460,132,462,126,463,119,461,113,458,107,454,101,451,99,446,94,441,92,437,92,430,93,425,95,425,97,430,97,435,98,438,100,443,104,447,107,450,111,451,116,452,121";
        DrawStringImplement(step4, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawShouldersFront()
    {
        SKPath path = new SKPath();

        string step1 = "39,142,41,139,42,133,43,125,43,121,44,114,48,110,50,108,54,105,58,103,64,102,67,103,68,104,67,107,65,110,63,114,61,119,56,126,50,132";
        DrawStringImplement(step1, ref path);

        string step2 = "155,101,162,101,168,102,173,105,178,111,183,116,187,121,191,130,192,134,192,136,186,129,180,124,174,119,170,116,165,113,159,108,155,103";
        DrawStringImplement(step2, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawWidestBack()
    {
        SKPath path = new SKPath();

        string step1 = "341,130,335,133,330,135,323,137,319,138,313,141,306,142,299,142,299,147,300,154,303,161,306,167,311,172,324,182,331,189,338,200,341,199,342,197,342,191,341,187,343,186,347,183,348,181,350,178,352,177,354,176,358,173,360,166,353,155,348,142";
        DrawStringImplement(step1, ref path);

        string step2 = "393,129,387,139,387,143,383,148,376,159,372,165,374,167,375,171,377,172,378,175,384,176,386,182,390,185,392,186,392,190,391,196,393,199,395,201,396,201,400,192,409,184,418,176,427,169,432,160,434,153,435,143,430,143,424,142,411,137,402,135";
        DrawStringImplement(step2, ref path);

        string step3 = "61,159,60,164,58,171,58,173,61,180,66,185,71,190,71,188,65,178,63,170";
        DrawStringImplement(step3, ref path);

        string step4 = "162,185,166,182,170,177,173,173,172,167,171,163,170,161,168,169,166,175,163,180";
        DrawStringImplement(step4, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawMiddleBack()
    {
        SKPath path = new SKPath();

        string step1 = "299,115,305,114,309,112,310,119,313,126,304,118";
        DrawStringImplement(step1, ref path);

        string step2 = "425,112,429,114,434,115,430,118,424,122,421,126,424,118";
        DrawStringImplement(step2, ref path);

        string step3 = "437,117,433,115,428,120,421,127,412,136,415,138,419,139,422,141,427,142,435,142,434,138,437,127";
        DrawStringImplement(step3, ref path);

        string step4 = "298,116,307,121,315,129,322,137,318,138,313,141,306,142,299,142,299,136,296,122";
        DrawStringImplement(step4, ref path);

        string step5 = "309,112,316,109,326,106,330,111,332,114,332,120,330,126,324,136,322,136,319,133,315,129,312,124,310,114";
        DrawStringImplement(step5, ref path);

        string step6 = "425,112,415,108,409,107,407,107,403,112,403,117,403,122,405,128,409,134,411,137,411,137,418,129,421,125";
        DrawStringImplement(step6, ref path);

        string step7 = "325,137,331,135,337,132,341,130,338,126,335,120,333,114,333,119,332,125,328,131";
        DrawStringImplement(step7, ref path);

        string step8 = "409,136,405,135,398,133,394,130,393,129,397,124,401,116,402,113,402,122,405,130";
        DrawStringImplement(step8, ref path);
        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawChest()
    {
        SKPath path = new SKPath();
        string step =
            "46,137,50,133,58,125,63,118,65,112,70,106,72,104,75,104,83,103,89,102,96,101,103,103,109,104,112,107,114,110,115,113,117,108,121,105,126,103,133,102,141,101,145,101,150,102,154,104,159,107,163,111,170,117,175,121,180,124,188,130,192,133,192,134,187,135,180,136,176,140,171,144,165,149,157,153,148,154,141,155,132,155,127,155,122,153,118,149,115,146,112,150,108,154,102,157,93,159,86,159,80,157,74,155,66,151";

        DrawStringImplement(step, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawAbdominal()
    {
        SKPath path = new SKPath();

        string step1 = "72,187,68,181,65,175,63,166,63,160,64,153,73,158,75,163,72,163,75,171,75,172,71,173,75,181,71,182";
        DrawStringImplement(step1, ref path);

        string step2 = "168,148,161,153,159,155,156,161,160,162,160,166,157,170,160,172,156,180,159,181,156,187,159,189,161,183,166,175,168,169,169,161";
        DrawStringImplement(step2, ref path);

        string step3 = "139,226,146,221,150,220,152,220,155,216,158,208,157,201,157,194,159,188,156,187,159,182,156,181,159,172,157,172,160,162,155,162,159,155,155,154,150,157,139,156,140,161,139,164,141,168,140,177,138,184,137,192,137,203,138,221";
        DrawStringImplement(step3, ref path);


        string step4 = "90,227,81,219,78,217,75,215,73,213,73,185,72,182,75,182,72,174,75,172,73,164,75,164,75,160,80,161,83,160,88,159,88,159,88,174,91,182,92,194,93,198";
        DrawStringImplement(step4, ref path);

        string step5 = "89,159,92,159,98,158,106,155,112,151,115,152,118,150,121,153,127,155,135,156,139,157,140,160,139,164,138,165,140,167,140,178,138,183,137,192,136,194,136,206,135,215,129,241,125,254,118,259,116,261,113,260,108,256,104,247,99,233,94,214,93,194,90,179,88,172";
        DrawStringImplement(step5, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawTriceps()
    {
        SKPath path = new SKPath();

        string step1 = "31,141,26,145,21,152,20,158,20,165,19,169,19,177,19,177,21,173,25,169,26,162,27,153,32,146";
        DrawStringImplement(step1, ref path);

        string step2 = "205,141,202,147,204,150,207,156,209,163,210,173,215,178,218,185,218,180,216,176,216,169,217,161,217,156,213,150,207,144";
        DrawStringImplement(step2, ref path);

        string step3 = "259,169,259,163,259,158,260,151,261,145,264,140,268,136,271,133,274,132,272,145,265,155";
        DrawStringImplement(step3, ref path);

        string step4 = "475,169,475,162,474,151,473,145,469,139,461,132,462,146,469,156";
        DrawStringImplement(step4, ref path);

        string step5 = "463,169,460,133,458,128,453,122,447,118,441,116,436,116,438,121,437,130,435,138,435,146,438,155,440,162,445,166,451,169";
        DrawStringImplement(step5, ref path);

        string step6 = "297,116,291,116,285,118,281,122,277,129,274,133,273,145,271,168,274,170,278,170,288,166,294,162,297,155,299,143,296,121";
        DrawStringImplement(step6, ref path);

        string step7 = "462,169,457,170,452,169,447,167,443,164,439,160,436,155,435,153,436,158,439,163,444,170,449,176,454,181,460,184,465,185,469,185,472,185,473,183";
        DrawStringImplement(step7, ref path);

        string step8 = "272,170,276,170,282,169,289,166,294,162,297,153,298,150,299,155,295,163,289,171,284,177,278,182,273,185,267,186,265,186,262,186,260,184";
        DrawStringImplement(step8, ref path);
        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawBiceps()
    {
        SKPath path = new SKPath();

        string step1 = "34,186,32,182,30,178,29,173,29,168,30,162,32,156,34,150,36,146,35,141,33,144,31,148,28,154,27,157,26,162,26,173,27,177,29,181";
        DrawStringImplement(step1, ref path);

        string step2 = "201,193,203,187,206,181,209,175,209,171,209,165,208,159,205,151,202,149,201,149,200,152,202,156,203,163,204,168,204,175,201,181,200,187";
        DrawStringImplement(step2, ref path);

        string step3 = "46,137,40,141,38,143,36,146,32,156,30,165,29,173,31,179,33,183,34,186,40,187,42,186,46,185,49,183,54,178,58,170,60,159,60,152,59,146,53,140";
        DrawStringImplement(step3, ref path);

        string step4 = "169,146,169,154,170,163,174,172,179,182,186,190,194,192,199,190,200,185,202,179,204,173,203,166,202,159,200,152,197,145,195,140,192,136,190,134,182,136,178,139";
        DrawStringImplement(step4, ref path);

        string step5 = "281,188,280,181,285,176,290,170,296,161,300,153,301,160,300,166,297,172,292,177,284,184";
        DrawStringImplement(step5, ref path);

        string step6 = "454,187,454,180,450,177,445,172,440,166,438,161,435,156,434,153,433,158,432,160,434,165,438,173,443,179";
        DrawStringImplement(step6, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawForearm()
    {
        SKPath path = new SKPath();

        string step1 = "9,207,7,214,6,222,7,231,9,240,10,248,10,249,12,249,13,237,13,227,12,214";
        DrawStringImplement(step1, ref path);

        string step2 = "226,205,227,213,228,222,227,234,226,245,226,249,222,249,222,235,222,226,222,219";
        DrawStringImplement(step2, ref path);

        string step3 = "252,186,248,193,244,202,242,212,242,220,244,230,244,238,246,234,247,233,247,222,248,209,251,200,253,194";
        DrawStringImplement(step3, ref path);

        string step4 = "482,186,486,193,490,202,492,212,492,220,491,231,490,237,487,234,487,225,486,215,484,206,482,197,480,194,482,193";
        DrawStringImplement(step4, ref path);

        string step5 = "13,217,17,223,18,226,19,231,19,239,18,238,16,235,15,231,14,221";
        DrawStringImplement(step5, ref path);

        string step6 = "220,225,221,234,218,241,216,245,214,253,212,255,210,254,212,251,212,244,214,235";
        DrawStringImplement(step6, ref path);

        string step7 = "47,185,43,185,40,187,39,190,39,193,40,195,42,197,43,196,45,195,45,194";
        DrawStringImplement(step7, ref path);

        string step8 = "187,191,189,191,192,191,194,194,195,196,195,199,194,201,192,203,191,201,189,201";
        DrawStringImplement(step8, ref path);

        string step9 = "194,210,197,222,202,229,203,233,203,235,199,228,196,225,192,221,190,217,189,214";
        DrawStringImplement(step9, ref path);

        string step10 = "39,209,38,214,36,219,32,224,29,229,28,232,27,241,30,232,32,231,35,229,38,226,40,222,42,216";
        DrawStringImplement(step10, ref path);

        string step11 = "14,228,13,238,13,247,16,252,19,256,24,260,22,255,21,251,20,246,20,243,17,237";
        DrawStringImplement(step11, ref path);

        string step12 = "221,236,221,244,221,250,221,251,217,256,209,260,215,251,215,245";
        DrawStringImplement(step12, ref path);




        string step13 = "25,170,19,176,19,179,15,183,11,189,10,195,9,204,10,208,17,221,19,225,19,236,21,249,23,224,25,221,28,216,31,211,32,200,33,188,29,182,27,177";
        DrawStringImplement(step13, ref path);

        string step14 = "210,173,206,182,202,189,201,197,201,206,201,216,204,224,207,232,208,236,208,243,210,255,212,247,213,236,214,231,218,229,220,223,222,216,222,212,224,205,223,194,220,187,217,184,214,177";
        DrawStringImplement(step14, ref path);

        string step15 = "280,182,280,191,280,203,278,212,272,222,269,227,265,239,260,248,258,248,263,229,265,217,266,212,268,206,269,200,272,193,272,189,270,187";
        DrawStringImplement(step15, ref path);

        string step16 = "455,181,454,195,454,204,455,211,458,217,463,224,465,227,467,234,472,244,473,249,477,248,473,237,470,223,469,214,466,206,464,197,464,191,465,187";
        DrawStringImplement(step16, ref path);

        string step17 = "486,256,487,250,487,240,487,231,487,223,486,212,485,205,482,198,480,193,476,188,473,185,467,187,465,189,465,193,466,199,467,206";
        DrawStringImplement(step17, ref path);

        string step18 = "248,256,247,250,247,237,248,231,247,226,247,216,249,206,253,196,256,191,262,186,266,187,268,189,269,192,270,194,268,202,266,210";
        DrawStringImplement(step18, ref path);


        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawQuadriceps()
    {
        SKPath path = new SKPath();

        string step1 = "82,227,87,242,91,253,95,262,99,274,102,283,104,291,109,300,110,308,113,322,113,333,110,320,107,306,103,296,100,293,97,286,94,279,91,265,88,261,84,254,82,245,80,238,78,228";
        DrawStringImplement(step1, ref path);

        string step2 = "148,227,143,244,136,260,132,270,128,282,126,294,123,306,122,319,123,322,125,312,132,290,135,284,135,273,138,264,143,257,145,256,150,242,154,225";
        DrawStringImplement(step2, ref path);

        string step3 = "106,256,104,261,102,265,99,270,101,279,109,271,114,264,117,264,119,271,123,275,128,281,129,276,131,270,128,264,126,258,125,255,119,260,116,260,112,259";
        DrawStringImplement(step3, ref path);

        string step4 = "127,286,124,282,121,277,120,273,117,265,118,277,117,287,119,292,122,297,124,299";
        DrawStringImplement(step4, ref path);

        string step5 = "103,282,111,269,114,265,115,269,114,272,114,278,113,284,110,291,108,297";
        DrawStringImplement(step5, ref path);

        string step6 = "78,238,73,241,68,248,64,254,63,261,63,268,63,282,66,268,73,254";
        DrawStringImplement(step6, ref path);

        string step7 = "155,225,156,230,163,240,167,249,170,259,170,277,166,271,164,268,162,259,158,249,152,237";
        DrawStringImplement(step7, ref path);

        string step8 = "136,285,132,292,128,302,126,314,123,322,122,331,123,342,125,351,129,356,136,361,141,364,144,359,148,351,151,345,151,336,151,330,149,319";
        DrawStringImplement(step8, ref path);

        string step9 = "94,280,93,295,93,303,90,312,88,320,85,325,83,333,83,340,85,347,88,354,92,359,97,361,103,361,107,356,110,351,112,345,113,340,112,335,111,329,110,320,107,309,102,296,100,295";
        DrawStringImplement(step9, ref path);

        string step10 = "65,276,63,283,59,291,55,304,55,318,57,334,63,346,71,355,74,357,77,356,80,353,81,348,79,336,76,325,71,314,68,304,66,290";
        DrawStringImplement(step10, ref path);

        string step11 = "164,268,165,279,165,288,164,299,161,312,159,320,157,325,155,330,154,338,155,350,158,355,161,356,167,354,173,346,178,334,180,323,180,309,178,298,174,287";
        DrawStringImplement(step11, ref path);

        string step12 = "321,285,316,290,313,295,310,301,310,309,310,320,312,335,315,342,320,348,319,330,318,321,318,303";
        DrawStringImplement(step12, ref path);

        string step13 = "414,286,416,302,416,316,416,330,415,337,415,347,419,341,422,334,423,324,424,314,424,303,421,294";
        DrawStringImplement(step13, ref path);


        string step14 = "152,237,156,246,161,257,163,266,165,277,165,290,163,302,160,313,157,322,155,329,154,331,151,331,151,328,150,323,147,313,141,299,136,286,136,276,138,268,141,261,144,256,145,256";
        DrawStringImplement(step14, ref path);

        string step15 = "79,237,74,252,70,260,67,267,66,273,66,280,67,293,69,305,72,317,77,329,79,333,82,333,85,324,89,314,92,304,93,294,93,281,93,279,92,272,91,267,88,262,87,260,84,255";
        DrawStringImplement(step15, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawCaviar()
    {
        SKPath path = new SKPath();

        string step1 = "135,384,132,391,129,400,128,408,128,417,129,426,132,432,136,437,138,429,140,422,143,414,145,409,145,404,143,398";
        DrawStringImplement(step1, ref path);

        string step2 = "103,379,100,387,98,393,97,401,98,411,101,419,105,427,106,436,109,431,113,424,114,415,114,407,113,400,109,393,106,388";
        DrawStringImplement(step2, ref path);

        string step3 = "392,358,388,362,387,366,387,376,386,384,383,392,380,402,379,411,381,420,386,429,388,432,390,432,393,431,396,428,398,425,403,428,407,431,409,430,413,426,418,419,422,411,424,404,424,396,421,388,414,376,409,368,405,358,398,367,396,361";
        DrawStringImplement(step3, ref path);

        string step4 = "331,359,325,367,321,375,314,386,311,395,309,404,310,409,313,416,318,423,323,430,326,430,329,429,334,425,338,429,342,432,345,432,350,426,354,417,355,408,353,397,351,389,348,385,347,375,347,365,343,359,341,357,338,361,336,367,333,361";
        DrawStringImplement(step4, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawShinCamboloid()
    {
        SKPath path = new SKPath();

        string step1 = "425,408,425,415,420,427,416,437,411,447,407,453,403,458,405,449,406,439,408,433,411,430,419,419";
        DrawStringImplement(step1, ref path);

        string step2 = "310,410,312,423,317,434,321,444,325,451,332,458,326,433,320,427,314,417";
        DrawStringImplement(step2, ref path);

        string step3 = "145,413,145,419,145,436,146,452,146,463,144,473,142,482,141,459,139,453,138,448,137,441,137,433,140,423";
        DrawStringImplement(step3, ref path);

        string step4 = "98,411,97,423,97,434,98,447,98,463,99,473,100,479,101,476,102,459,104,452,106,445,106,437,105,429,102,421";
        DrawStringImplement(step4, ref path);

        string step5 = "345,432,353,419,352,428,351,438,348,445,346,451";
        DrawStringImplement(step5, ref path);

        string step6 = "388,432,389,451,385,442,382,437,381,428,381,420,385,427";
        DrawStringImplement(step6, ref path);
        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawThighs()
    {
        SKPath path = new SKPath();

        string step1 = "390,278,391,284,392,298,393,314,396,328,399,344,404,360,407,363,406,350,405,339,406,335,408,335,411,366,413,359,414,354,414,346,415,329,415,313,413,285,408,282,402,280";
        DrawStringImplement(step1, ref path);

        string step2 = "344,278,337,279,331,280,326,282,321,285,320,293,319,306,318,319,319,331,320,338,320,352,322,361,324,347,325,334,328,336,328,342,327,350,327,361,329,359,333,348,337,332,339,322,342,301,342,289";
        DrawStringImplement(step2, ref path);


        string step3 = "344,278,342,288,342,299,341,315,340,320,342,326,346,333,348,339,350,335,352,331,353,326,355,322,357,311,357,299,358,287,358,276,351,278";
        DrawStringImplement(step3, ref path);

        string step4 = "390,278,391,285,392,297,393,310,394,317,394,322,392,326,389,333,386,339,382,332,380,324,378,320,377,310,376,299,376,289,376,279,376,275,383,278";
        DrawStringImplement(step4, ref path);


        string step5 = "394,322,397,335,398,344,399,347,398,351,396,354,393,356,390,358,388,358,386,358,385,353,385,344,385,340,391,330";
        DrawStringImplement(step5, ref path);

        string step6 = "339,322,338,334,336,342,335,346,335,350,338,354,341,357,344,358,348,357,348,350,348,341,347,339,343,332";
        DrawStringImplement(step6, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawButtocks()
    {
        SKPath path = new SKPath();

        string step1 = "415,238,415,231,415,226,413,221,412,217,411,213,409,208,406,205,402,203,399,203,397,203,393,201,390,196,387,199,382,203,379,208,376,216,377,220,382,222,390,223,396,224,404,230,409,235";
        DrawStringImplement(step1, ref path);

        string step2 = "319,239,319,226,322,218,323,213,324,209,327,206,329,204,332,204,335,204,337,203,339,202,342,199,344,197,348,200,352,204,355,209,357,214,357,219,353,221,347,222,341,223,333,228,326,234";
        DrawStringImplement(step2, ref path);

        string step3 = "157,220,155,225,157,229,160,234,164,240,166,245,167,240,167,234,165,229,161,223";
        DrawStringImplement(step3, ref path);

        string step4 = "66,247,65,243,65,238,67,233,71,228,74,223,74,221,76,224,76,229,75,233,74,239";
        DrawStringImplement(step4, ref path);

        string step5 = "325,236,330,231,337,226,343,224,349,223,353,223,357,223,361,227,364,231,367,237,368,239,370,232,373,228,378,224,382,222,389,223,394,225,401,229,407,233,410,236,406,237,403,240,403,246,403,255,404,261,407,269,409,275,408,279,407,281,402,279,398,279,389,278,383,277,377,276,372,273,369,271,368,267,367,268,365,271,361,274,356,275,350,276,344,277,337,278,330,280,327,281,326,277,325,271,328,264,331,259,332,252,331,245,330,240";
        DrawStringImplement(step5, ref path);


        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawTrapezium()
    {
        SKPath path = new SKPath();

        string step1 = "88,99,96,88,95,78,92,81,86,85,77,88,69,92,65,96,63,97,61,97,57,99";
        DrawStringImplement(step1, ref path);

        string step2 = "142,97,136,87,137,77,145,82,154,87,162,91,168,96,172,100,174,101";
        DrawStringImplement(step2, ref path);

        string step3 = "358,48,355,59,353,65,350,68,347,70,342,73,336,76,329,79,322,82,317,86,309,94,309,96,310,97,313,97,320,97,324,96,327,97,330,99,332,103,332,107,333,112,333,116,334,120,339,126,342,131,353,152,366,174,386,142,386,137,395,126,400,119,401,111,401,104,403,100,407,97,412,96,417,97,420,97,425,96,425,94,425,93,422,90,413,83,405,79,396,75,388,71,382,67,379,61,376,49";
        DrawStringImplement(step3, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawNeck()
    {
        SKPath path = new SKPath();

        string step1 = "114,102,110,85,110,76,119,75,123,86,120,102,115,90";
        DrawStringImplement(step1, ref path);

        string step2 = "97,98,96,68,105,75,109,83,112,97,105,94,104,99";
        DrawStringImplement(step2, ref path);

        string step3 = "136,96,137,60,134,60,134,63,129,69,121,95,129,91,128,97";
        DrawStringImplement(step3, ref path);

        string step4 = "347,51,355,50,352,58,352,65,347,70,346,59";
        DrawStringImplement(step4, ref path);

        string step5 = "378,49,386,50,387,70,381,65,381,59";
        DrawStringImplement(step5, ref path);

        path.Close();
        canvas.DrawPath(path, fillPaint);
    }

    private void DrawStringImplement(string step, ref SKPath path)
    {
        var step1Arr = step.Split(',');
        var stX = Convert.ToSingle(step1Arr[0]);
        var stY = Convert.ToSingle(step1Arr[1]);
        path.MoveTo(new SKPoint(stX * scaleX, stY * scaleY));
        for (int i = 0; i < step1Arr.Length; i += 2)
        {
            var x = Convert.ToSingle(step1Arr[i]);
            var y = Convert.ToSingle(step1Arr[i + 1]);
            path.LineTo(new SKPoint(x * scaleX, y * scaleY));
        }
        path.LineTo(new SKPoint(stX * scaleX, stY * scaleY));
    }
    #endregion

    private void NewItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        var item = sender as MuscleCheckItem;
        if (e.PropertyName == nameof(MuscleCheckItem.IsChecked))
        {
            if (item.IsChecked)
            {
                Filter.CurrentMuscles.Add(item.Muscle);
            }
            else
            {
                Filter.CurrentMuscles.RemoveAll(itemMuscle => itemMuscle == item.Muscle);
            }
        }

        SkiaView.InvalidateSurface();
    }

    private async void AcceptFilter_Click(object sender, EventArgs e)
    {
        MusclesListView.IsVisible = false;
        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new FilterAcceptedForExercisesMessage(Filter));
    }

    private void MusclesListView_OnItemTapped(object sender, object e)
    {
        var item = sender as Border;
        MuscleCheckItem send = item.BindingContext as MuscleCheckItem;
        send.IsChecked = !send.IsChecked;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        SetMuscleFilter(query["Filter"] as FilterModel);
    }
}