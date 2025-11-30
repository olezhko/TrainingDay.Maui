namespace TrainingDay.Maui.Extensions;

public static class ConstantKeys
{
    public const string WorkoutsAndroidAds = "ca-app-pub-5313002236683041/3406765721";
    public const string WorkoutsiOSAds = "ca-app-pub-5313002236683041/6962867352";

    public const string ImplementAndroidAds = "ca-app-pub-5313002236683041/8012225141";
    public const string ImplementiOSAds = "ca-app-pub-5313002236683041/1240499908";

    public const string ExercisesAndroidAds = "ca-app-pub-5313002236683041/7154439044";
    public const string ExercisesiOSAds = "ca-app-pub-5313002236683041/7996747424";

    public const string WorkoutAndroidAds = "ca-app-pub-5313002236683041/6699143472";
    public const string WorkoutiOSAds = "ca-app-pub-5313002236683041/5047017006";

    public const string FinishWorkoutAndroidAds = "ca-app-pub-5313002236683041/7746198508";
    public const string FinishWorkoutiOSAds = "ca-app-pub-5313002236683041/5370584086";

    public const string NotFinishedTrainingName = "NotFinished.trday";
    public const string DatabaseName = "exercise.db";

    public static string Version => "1.3.1";

    public class AwsS3
    {
        public const string accessKey = "";
        public const string secretKey = "";
        public const string BucketName = "trainingday-exercise-images";
    }
}