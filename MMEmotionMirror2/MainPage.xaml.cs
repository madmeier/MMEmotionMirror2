using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;

using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;

using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MMEmotionMirror2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";
        private DispatcherTimer timer;


        public MainPage()
        {
            this.InitializeComponent();
            initCamera();


            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10000);
            timer.Tick += takePhoto;
            timer.Start();


        }

        private async void initCamera()
        {
            // Disable all buttons until initialization completes

            try
            {


                status.Text = "Initializing camera to capture audio and video...";
                // Use default initialization
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                // Set callbacks for failure and recording limit exceeded
                status.Text = "Device successfully initialized for video recording!";
                mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);

                // Start Preview                
                previewElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                status.Text = "Camera preview succeeded";

            }
            catch (Exception ex)
            {
                status.Text = "Unable to initialize camera for audio/video mode: " + ex.Message;


            }
        }

        private  void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
             status.Text = "MediaCaptureFailed: " + currentFailure.Message;
        }



        private Stream openStream(String fileName)
        {
            return File.OpenRead(fileName);
        }


        private async Task<Emotion[]> UploadAndDetectEmotions(string imageFilePath)
        {
            string subscriptionKey = "3b86142b9d614d5caa6bce864e0a8a8c";
            EmotionServiceClient emotionServiceClient = new EmotionServiceClient(subscriptionKey);

            //            FileStream imageFileStream = File.OpenRead(imageFilePath);

            Stream imageFileStream = await Task.Run(() => openFile(imageFilePath));
            Emotion[] emotionResult = await emotionServiceClient.RecognizeAsync(imageFileStream);
            return emotionResult;
        }


        private FileStream openFile(string imageFilePath)
        {

            FileStream imageFileStream = File.OpenRead(imageFilePath);
            return imageFileStream;
        }

       private async void takePhoto(object sender, object e)
        {
            try
            {
                 captureImage.Source = null;

                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PHOTO_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
                status.Text = "Take Photo succeeded: " + photoFile.Path;

                IRandomAccessStream photoStream = await photoFile.OpenReadAsync();
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(photoStream);
                captureImage.Source = bitmap;

                Emotion[] emotionResult = await UploadAndDetectEmotions(photoFile.Path);


                int emotionResultCount = 0;
 
                if (emotionResult != null && emotionResult.Length > 0)
                {
                    foreach (Emotion emotion in emotionResult)
                    {
                        AngerSlide.Value = 100 * emotion.Scores.Anger;
                        ContemptSlide.Value = 100 * emotion.Scores.Contempt;

                        DisgustSlide.Value = 100 * emotion.Scores.Disgust;
                        FearSlide.Value = 100 * emotion.Scores.Fear;
                        HappinessSlide.Value = 100 * emotion.Scores.Happiness;
                        NeutralSlide.Value = 100 * emotion.Scores.Neutral;
                        SadnessSlide.Value = 100 * emotion.Scores.Sadness;
                        SurpriseSlide.Value = 100 * emotion.Scores.Surprise;


                        emotionResultCount++;
                    }
 
                }
                else
                {
                    status.Text = "no emotions detected";
                }


            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
            finally
            {
            }
        }

    }
}
    

