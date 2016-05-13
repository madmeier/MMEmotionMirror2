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

        /// <summary>
        /// Initializes the camera and displays preview
        /// </summary>
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

        /// <summary>
        /// Callback in case there is a media capture error
        /// </summary>
        /// <param name="currentCaptureObject"></param>
        /// <param name="currentFailure"></param>
        private  void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
             status.Text = "MediaCaptureFailed: " + currentFailure.Message;
        }




        /// <summary>
        /// Interface with project oxford
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns></returns>
        private async Task<Emotion[]> UploadAndDetectEmotions(string imageFilePath)
        {
            string subscriptionKey = "3b86142b9d614d5caa6bce864e0a8a8c";
            EmotionServiceClient emotionServiceClient = new EmotionServiceClient(subscriptionKey);

            Stream imageFileStream = await Task.Run(() => openFile(imageFilePath));
            Emotion[] emotionResult = await emotionServiceClient.RecognizeAsync(imageFileStream);
            return emotionResult;
        }


        /// <summary>
        /// A little method to allows async stream opening
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns></returns>
        private FileStream openFile(string imageFilePath)
        {

            FileStream imageFileStream = File.OpenRead(imageFilePath);
            return imageFileStream;
        }


       /// <summary>
       /// callbak to take a photo, store it and pass it to oxford for emotional analysis
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
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


                        float maxEmotion = 0.0F;
                        int maxEmotionType = -1;

                        AngerSlide.Value = 100 * emotion.Scores.Anger;
                        if (emotion.Scores.Anger > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Anger;
                            maxEmotionType = 0;
                        }

                        ContemptSlide.Value = 100 * emotion.Scores.Contempt;
                        if (emotion.Scores.Contempt > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Contempt;
                            maxEmotionType = 1;
                        }

                        DisgustSlide.Value = 100 * emotion.Scores.Disgust;
                        if (emotion.Scores.Disgust > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Disgust;
                            maxEmotionType = 2;
                        }

                        FearSlide.Value = 100 * emotion.Scores.Fear;
                        if (emotion.Scores.Fear > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Fear;
                            maxEmotionType = 3;
                        }

                        HappinessSlide.Value = 100 * emotion.Scores.Happiness;
                        if (emotion.Scores.Happiness > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Happiness;
                            maxEmotionType = 4;
                        }

                        NeutralSlide.Value = 100 * emotion.Scores.Neutral;
                        if (emotion.Scores.Neutral > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Neutral;
                            maxEmotionType = 5;
                        }

                        SadnessSlide.Value = 100 * emotion.Scores.Sadness;
                        if (emotion.Scores.Sadness > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Sadness;
                            maxEmotionType = 6;
                        }

                        SurpriseSlide.Value = 100 * emotion.Scores.Surprise;
                        if (emotion.Scores.Surprise > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Surprise;
                            maxEmotionType = 7;
                        }

                        emotionResultCount++;

                        status.Text = "max = " + maxEmotion + " at " + maxEmotionType;

                    }

                }
                else
                {
                    AngerSlide.Value = 0;
                    ContemptSlide.Value = 0;
                    DisgustSlide.Value = 0;
                    FearSlide.Value = 0;
                    HappinessSlide.Value = 0;
                    NeutralSlide.Value =0;
                    SadnessSlide.Value = 0;
                    SurpriseSlide.Value = 0;


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
    

