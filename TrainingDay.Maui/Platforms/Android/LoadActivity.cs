using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using System.Text;
using TrainingDay.Maui.Models;

namespace TrainingDay.Maui;

[Activity(Name = "com.olasoft.TrainingDay.LoadActivity", LaunchMode = LaunchMode.SingleTask, Label = "TrainingDay", Exported = true)]
[IntentFilter(new string[] { Intent.ActionView, Intent.ActionEdit, Android.Content.Intent.ActionOpenDocument },
        Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "content",
        DataHost = "*",
        DataMimeType = @"application/octet-stream"
    )]
public class LoadActivity : Activity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        CheckAppPermissions();
        try
        {
            if (Intent.Action.Equals(Intent.ActionView))
            {
                if (Intent.Scheme.Equals(ContentResolver.SchemeContent))
                {
                    Android.Net.Uri fileUri = Intent.Data;
                    var fileContent = LoadBytesFromIntent(fileUri);
                    (App.Current as App).SetIncomingFile(fileContent);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            Finish();
        }
    }

    private string LoadBytesFromIntent(Android.Net.Uri uri)
    {
        Stream stream = ContentResolver.OpenInputStream(uri);
        string training = null;
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            using (StreamReader reader = new StreamReader(memoryStream, Encoding.UTF8))
            {
                training = reader.ReadToEnd();
            }
        }
        return training;
    }

    private void CheckAppPermissions()
    {
        if ((int)Build.VERSION.SdkInt < 23)
        {
            return;
        }
        else
        {
            if (PackageManager.CheckPermission(Manifest.Permission.ReadExternalStorage, PackageName) != Permission.Granted
                && PackageManager.CheckPermission(Manifest.Permission.WriteExternalStorage, PackageName) != Permission.Granted)
            {
                var permissions = new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage };
                RequestPermissions(permissions, 1);
            }
        }
    }
}