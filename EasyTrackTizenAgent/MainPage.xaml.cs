using System;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms.Xaml;
using System.Diagnostics;
using System.IO;
using Tizen.Sensor;
using EasyTrackTizenAgent.Model;
using Tizen.Security;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Json;
using Tizen.Network.WiFi;
using Tizen.System;
using Xamarin.Forms;

namespace EasyTrackTizenAgent
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : CirclePage
    {
        public MainPage()
        {
            InitializeComponent();

            #region BLE connection
            /*try
            {
                initAgentConnection();
            }
            catch (Exception)
            {
                log("BLE Connection failed!");
            }*/
            #endregion

            initDataSourcesWithPrivileges();

            //startSensors();
        }

        protected override void OnAppearing()
        {
            Power.RequestCpuLock(0);

            if (countSensorDataFiles() > 2)
                Device.BeginInvokeOnMainThread(() => { EnableUploadBtn(true); });
            else
                Device.BeginInvokeOnMainThread(() => { EnableUploadBtn(false); });


            if (WiFiManager.ConnectionState == WiFiConnectionState.Connected)
                Device.BeginInvokeOnMainThread(() => { statusLabelConnection.Text = "Internet: Connected :)"; });
            else
                Device.BeginInvokeOnMainThread(() => { statusLabelConnection.Text = "Internet: Not connected :("; });

            //Check service is running and enable / disable start button
            if (dataCollectorThread == null)
            {
                Device.BeginInvokeOnMainThread(() => { statusLabelService.Text = "Service: Not running :("; });
                Device.BeginInvokeOnMainThread(() => { EnableStartBtn(true); });
            }
            else
            {
                if (dataCollectorThread.ThreadState.Equals(System.Threading.ThreadState.Unstarted) || dataCollectorThread.ThreadState.Equals(System.Threading.ThreadState.WaitSleepJoin) || dataCollectorThread.ThreadState.Equals(System.Threading.ThreadState.Stopped))
                {
                    Device.BeginInvokeOnMainThread(() => { statusLabelService.Text = "Service: Not running :("; });
                    Device.BeginInvokeOnMainThread(() => { EnableStartBtn(true); });
                }
                else
                {
                    Device.BeginInvokeOnMainThread(() => { statusLabelService.Text = "Service: Running :)"; });
                    Device.BeginInvokeOnMainThread(() => { EnableStartBtn(false); });
                }
            }

            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            //terminateFilesCounterThread();
            base.OnDisappearing();
        }

        private void initDataSourcesWithPrivileges()
        {
            PrivacyPrivilegeManager.ResponseContext context = null;
            if (PrivacyPrivilegeManager.GetResponseContext(Tools.HEALTHINFO_PRIVILEGE).TryGetTarget(out context))
                context.ResponseFetched += (s, e) =>
                {
                    if (e.result != RequestResult.AllowForever)
                    {
                        Toast.DisplayText("Please provide the necessary privileges for the application to run!");
                        Environment.Exit(1);
                    }
                    else
                        initDataSources();
                };
            else
            {
                Toast.DisplayText("Please provide the necessary privileges for the application to run!");
                Environment.Exit(1);
            }

            switch (PrivacyPrivilegeManager.CheckPermission(Tools.HEALTHINFO_PRIVILEGE))
            {
                case CheckResult.Allow:
                    initDataSources();
                    break;
                case CheckResult.Deny:
                    Toast.DisplayText("Please provide the necessary privileges for the application to run!");
                    Environment.Exit(1);
                    break;
                case CheckResult.Ask:
                    PrivacyPrivilegeManager.RequestPermission(Tools.HEALTHINFO_PRIVILEGE);
                    break;
                default:
                    break;
            }
        }

        private void initDataSources()
        {
            #region Assign sensor model references
            accelerometerModel = new AccelerometerModel
            {
                IsSupported = Accelerometer.IsSupported,
                SensorCount = Accelerometer.Count
            };
            hRMModel = new HRMModel
            {
                IsSupported = HeartRateMonitor.IsSupported,
                SensorCount = HeartRateMonitor.Count
            };
            #endregion

            #region Assign sensor references and sensor measurement event handlers
            if (accelerometerModel.IsSupported)
            {
                accelerometer = new Accelerometer();
                accelerometer.Interval = 28;
                accelerometer.PausePolicy = SensorPausePolicy.None;
                accelerometer.DataUpdated += storeAccDataCallback;
            }
            if (hRMModel.IsSupported)
            {
                hRM = new HeartRateMonitor();
                hRM.Interval = Tools.SENSOR_SAMPLING_INTERVAL;
                hRM.DataUpdated += storeHRMDataCallback;
                hRM.PausePolicy = SensorPausePolicy.None;
            }
            #endregion
        }

        public void SignOutClicked(object sender, EventArgs e)
        {
            terminateDataCollectorThread();
            terminateFileSubmitThread();
            terminateSubmitDataThread();
            Tizen.Applications.Preference.Set("logged_in", false);
            Tizen.Applications.Preference.Set("username", "");
            Tizen.Applications.Preference.Set("password", "");
            /*AuthenticationPage authPage = new AuthenticationPage();
            await Navigation.PushAsync(authPage);*/

            Device.BeginInvokeOnMainThread(() =>
            {
                IsEnabled = true;
                Navigation.PushModalAsync(new AuthenticationPage());
            });

        }

        public void EnableStartBtn(bool enable)
        {
            if (enable)
            {
                startDataColButton.IsEnabled = true;
                startDataColButton.Source = ImageSource.FromFile("start.png");
            }
            else
            {
                startDataColButton.IsEnabled = false;
                startDataColButton.Source = ImageSource.FromFile("start_disable.png");
            }
        }

        public void EnableUploadBtn(bool enable)
        {
            if (enable)
            {
                reportDataColButton.IsEnabled = true;
                reportDataColButton.Source = ImageSource.FromFile("upload.png");
            }
            else
            {
                reportDataColButton.IsEnabled = false;
                reportDataColButton.Source = ImageSource.FromFile("upload_disable.png");
            }
        }

        #region Variables
        // Log properties
        private const string TAG = "MainPage";

        private Thread submitDataThread;
        private Thread dataCollectorThread;
        private Thread fileSubmitThread;

        private bool stopSubmitDataThread;
        private bool stopCollectorThread;
        private bool stopFileSubmitThread;

        private bool isDataSubmitRunning;

        private string openLogStreamStamp;
        private StreamWriter logStreamWriter;
        private int logLinesCount = 1;
        
        private long prevHeartbeatTime = 0;
        private long prevSubmissionTime = 0;
        private bool hrmStoppedFromCallback = false;


        private short EMA_ORDER = 0;

        // Sensors and their SensorModels
        internal Accelerometer accelerometer { get; private set; }
        internal AccelerometerModel accelerometerModel { get; private set; }
        internal HRMModel hRMModel { get; private set; }
        internal HeartRateMonitor hRM { get; private set; }
        #endregion

        #region UI Event callbacks
        private void reportDataCollectionClick(object sender, EventArgs e)
        {
            if (countSensorDataFiles() > 2)
            {
                if (WiFiManager.ConnectionState == WiFiConnectionState.Connected)
                {
                    if (submitDataThread == null)
                    {
                        Device.BeginInvokeOnMainThread(() => { EnableUploadBtn(false); });
                        startSubmitDataThread();
                    }
                    else
                    {
                        if (submitDataThread.ThreadState.Equals(System.Threading.ThreadState.Unstarted) ||
                            submitDataThread.ThreadState.Equals(System.Threading.ThreadState.WaitSleepJoin) ||
                            submitDataThread.ThreadState.Equals(System.Threading.ThreadState.Stopped))
                        {
                            Device.BeginInvokeOnMainThread(() => { EnableUploadBtn(false); });
                            startSubmitDataThread();
                        }
                    }

                }
            }
        }

        private void startDataCollectionClick(object sender, EventArgs e)
        {

            if (dataCollectorThread == null)
            {
                stopCollectorThread = false;
                startDataCollectorThread();
                Device.BeginInvokeOnMainThread(() => { statusLabelService.Text = "Service: Running :)"; });
                Device.BeginInvokeOnMainThread(() => { EnableStartBtn(false); });
            }
            else
            {
                if (dataCollectorThread.ThreadState.Equals(System.Threading.ThreadState.Unstarted) || dataCollectorThread.ThreadState.Equals(System.Threading.ThreadState.WaitSleepJoin) || dataCollectorThread.ThreadState.Equals(System.Threading.ThreadState.Stopped))
                {
                    //starting Data collector thread
                    stopCollectorThread = false;
                    startDataCollectorThread();
                    Device.BeginInvokeOnMainThread(() => { statusLabelService.Text = "Service: Running :)"; });
                    Device.BeginInvokeOnMainThread(() => { EnableStartBtn(false); });


                }
            }

            if (fileSubmitThread == null)
            {
                stopFileSubmitThread = false;
                startFileSubmitThread();
            }
            else if (!fileSubmitThread.ThreadState.Equals(System.Threading.ThreadState.Running))
            {
                stopFileSubmitThread = false;
                startFileSubmitThread();
            }

        }

        private void stopDataCollectionClick(object sender, EventArgs e)
        {
            terminateDataCollectorThread();
            startDataColButton.IsEnabled = true;
            stopDataColButton.IsEnabled = false;
        }

        private void startACC()
        {
            accelerometer?.Start();
        }
        private void stopACC()
        {
            accelerometer?.Stop();
        }
        private void startHRM()
        {
            hRM?.Start();
        }
        private void stopHRM()
        {
            hRM?.Stop();
        }
        #endregion

        #region Sensor DataUpdated Callbacks
        private void storeAccDataCallback(object sender, AccelerometerDataUpdatedEventArgs e)
        {
            checkUpdateCurrentLogStream();
            logStreamWriter?.Flush();
            lock (logStreamWriter)
            {
                logStreamWriter?.WriteLine($"{Tools.DATA_SRC_ACC},{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()} {e.X} {e.Y} {e.Z} {EMA_ORDER}");
            }
        }
        private void storeHRMDataCallback(object sender, HeartRateMonitorDataUpdatedEventArgs e)
        {
            checkUpdateCurrentLogStream();
            logStreamWriter?.Flush();
            if (e.HeartRate > 0)
            {
                lock (logStreamWriter)
                {
                    logStreamWriter?.WriteLine($"{Tools.DATA_SRC_HRM},{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()} {e.HeartRate} {EMA_ORDER}");
                }
                stopHRM();
                hrmStoppedFromCallback = true;
                /*DateTime nowDateTime = DateTime.UtcNow.ToLocalTime();
                double nowMillis = new TimeSpan(hours: nowDateTime.Hour, minutes: nowDateTime.Minute, seconds: nowDateTime.Second).TotalMilliseconds;
                hrmSensingStoppedPrevTime = nowMillis;*/
            }
        }
        #endregion


        private async Task reportToApiServer(
            string message = default(string),
            string path = default(string),
            Task postTransferTask = null)
        {
            if (message != default(string))
            {
                HttpResponseMessage result = await Tools.post(Tools.API_NOTIFY, new Dictionary<string, string> {
                    { "username", Tizen.Applications.Preference.Get<string>("username") },
                    { "password", Tizen.Applications.Preference.Get<string>("password") },
                    { "message", message }
                });
                if (result.IsSuccessStatusCode)
                {
                    JsonValue resJson = JsonValue.Parse(await result.Content.ReadAsStringAsync());
                    //log($"RESULT: {resJson["result"]}");
                    Debug.WriteLine(Tools.TAG, $"Message has been submitted to the Server. length={message.Length}");
                }
                else
                    Toast.DisplayText("Failed to submit a notification to server!");
            }
            else if (path != null)
            {
                HttpResponseMessage result = await Tools.post(
                    Tools.API_SUBMIT_DATA,
                    new Dictionary<string, string>
                    {
                        {"username", Tizen.Applications.Preference.Get<string>("username") },
                        {"password", Tizen.Applications.Preference.Get<string>("password") },
                    },
                    fileContent: File.ReadAllBytes(path),
                    fileName: path.Substring(path.LastIndexOf('\\') + 1)
                );
                if (result == null)
                {
                    Toast.DisplayText("Please check your WiFi connection first!");
                    return;
                }
                if (result.IsSuccessStatusCode)
                {
                    JsonValue resJson = JsonValue.Parse(await result.Content.ReadAsStringAsync());
                    ServerResult resCode = (ServerResult)int.Parse(resJson["result"].ToString());
                    if (resCode == ServerResult.OK)
                        postTransferTask?.Start();
                    /*else
                        log($"Failed to upload {path.Substring(path.LastIndexOf(Path.PathSeparator) + 1)}");*/
                }
                /*else
                    log($"Failed to upload {path.Substring(path.LastIndexOf(Path.PathSeparator) + 1)}");*/
            }
        }

        internal void log(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (logLinesCount == logLabel.MaxLines)
                    logLabel.Text = $"{logLabel.Text.Substring(logLabel.Text.IndexOf('\n') + 1)}\n{message}";
                else
                {
                    logLabel.Text = $"{logLabel.Text}\n{message}";
                    logLinesCount++;
                }
            });
        }

        private void eraseSensorData()
        {
            foreach (string file in Directory.GetFiles(Tools.APP_DIR, "*.csv"))
                File.Delete(file);
        }

        private int countSensorDataFiles()
        {
            return Directory.GetFiles(Tools.APP_DIR, "*.csv").Length;
        }

        private void checkUpdateCurrentLogStream()
        {
            DateTime nowTimestamp = DateTime.UtcNow.ToLocalTime();
            nowTimestamp = new DateTime(year: nowTimestamp.Year, month: nowTimestamp.Month, day: nowTimestamp.Day, hour: nowTimestamp.Hour, minute: nowTimestamp.Minute - nowTimestamp.Minute % Tools.NEW_FILE_CREATE_PERIOD, second: 0);
            string nowStamp = $"{new DateTimeOffset(nowTimestamp).ToUnixTimeMilliseconds()}";

            if (logStreamWriter == null)
            {
                openLogStreamStamp = nowStamp;
                string filePath = Path.Combine(Tools.APP_DIR, $"sw_{nowStamp}.csv");
                logStreamWriter = new StreamWriter(path: filePath, append: true);

                //log("Data-log file created/attached");
                Tools.sendHeartBeatMessage();
            }
            else if (!nowStamp.Equals(openLogStreamStamp))
            {
                logStreamWriter.Flush();
                logStreamWriter.Close();
                openLogStreamStamp = nowStamp;
                string filePath = Path.Combine(Tools.APP_DIR, $"sw_{nowStamp}.csv");
                logStreamWriter = new StreamWriter(path: filePath, append: false);

                //log("New data-log file created");
                Tools.sendHeartBeatMessage();
            }
        }

        private void terminateDataCollectorThread()
        {
            if (dataCollectorThread != null && dataCollectorThread.IsAlive)
            {
                stopACC();
                stopHRM();
                stopCollectorThread = true;
                dataCollectorThread?.Join();
                stopCollectorThread = false;
            }
        }

        private void startDataCollectorThread()
        {
            dataCollectorThread = new Thread(() =>
            {
                while (!stopCollectorThread)
                {
                    Tizen.Log.Debug(TAG, "Files: " + countSensorDataFiles());

                    long curTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    DateTime nowDateTime = DateTime.UtcNow.ToLocalTime();
                    double nowMins = new TimeSpan(hours: nowDateTime.Hour, minutes: nowDateTime.Minute, seconds: nowDateTime.Second).TotalMinutes;

                    wakeUpCpu();  // need to periodically wakeup CPU

                    #region UI buttons en/disable
                    if (!isDataSubmitRunning && countSensorDataFiles() > 2)
                        Device.BeginInvokeOnMainThread(() => { EnableUploadBtn(true); });
                    else
                        Device.BeginInvokeOnMainThread(() => { EnableUploadBtn(false); });
                    #endregion

                    #region Sending Heartbeat to Server & setting Internet connection status
                    if (WiFiManager.ConnectionState == WiFiConnectionState.Connected)
                    {
                        Device.BeginInvokeOnMainThread(() => { statusLabelConnection.Text = "Internet: Connected :)"; });
                        if (curTime > prevHeartbeatTime + Tools.HEARTBEAT_PERIOD * 1000)
                        {
                            Tools.sendHeartBeatMessage();
                            prevHeartbeatTime = curTime;
                        }
                    }
                    else
                    {
                        Device.BeginInvokeOnMainThread(() => { statusLabelConnection.Text = "Internet: Not connected :("; });
                    }
                    #endregion

                    #region Periodically un/registering sensors according to time sonstraints
                    if (checkInEMARange(nowMins))
                    {
                        if (!accelerometer.IsSensing && nowMins % Tools.ACC_SENSING_PERIOD < Tools.ACC_SENSING_DURATION)
                        {
                            startACC();
                        }
                        else if (accelerometer.IsSensing && nowMins % Tools.ACC_SENSING_PERIOD >= Tools.ACC_SENSING_DURATION)
                        {
                            stopACC();
                        }

                        if (!hRM.IsSensing && !hrmStoppedFromCallback && nowMins % Tools.HRM_SENSING_PERIOD < Tools.HRM_SENSING_DURATION)
                        {
                            startHRM();
                        }
                        else if (nowMins % Tools.HRM_SENSING_PERIOD >= Tools.HRM_SENSING_DURATION)
                        {
                            hrmStoppedFromCallback = false;
                            if (hRM.IsSensing)
                                stopHRM();
                        }
                    }
                    else
                    {
                        if (hRM.IsSensing || accelerometer.IsSensing)
                        {
                            stopACC();
                            stopHRM();
                            hrmStoppedFromCallback = false;
                        }
                    }
                    #endregion

                    Thread.Sleep(2000);
                }
            });
            dataCollectorThread.IsBackground = true;
            dataCollectorThread.Start();
        }

        private void terminateFileSubmitThread()
        {
            if (fileSubmitThread != null && fileSubmitThread.IsAlive)
            {
                stopFileSubmitThread = true;
                fileSubmitThread?.Join();
                stopFileSubmitThread = false;
            }

        }

        private void startFileSubmitThread()
        {
            fileSubmitThread = new Thread(() =>
            {
                while (!stopFileSubmitThread)
                {
                    Tizen.Log.Debug(TAG, "StartFileSubmit called");
                    if (countSensorDataFiles() > 2)
                        startSubmitDataThread();

                    Thread.Sleep(Tools.FILE_SUBMIT_PERIOD * 60 * 1000);
                }
            });
            fileSubmitThread.IsBackground = true;
            fileSubmitThread.Start();
        }

        private void wakeUpCpu()
        {
            Power.RequestCpuLock(0);
            accelerometer.PausePolicy = SensorPausePolicy.None;
            hRM.PausePolicy = SensorPausePolicy.None;
        }

        private void terminateSubmitDataThread()
        {
            if (submitDataThread != null && submitDataThread.IsAlive)
            {
                stopSubmitDataThread = true;
                submitDataThread?.Join();
                stopSubmitDataThread = false;
            }
        }

        private void startSubmitDataThread()
        {
            if (isDataSubmitRunning)
                return;

            submitDataThread = new Thread(async () =>
            {
                Tizen.Log.Debug(TAG, "File submit STARTED");
                isDataSubmitRunning = true;
                Device.BeginInvokeOnMainThread(() => { EnableUploadBtn(false); });
                // Get list of files and sort in increasing order
                string[] filePaths = Directory.GetFiles(Tools.APP_DIR, "*.csv");
                List<long> fileNamesInLong = new List<long>();
                for (int n = 0; !stopSubmitDataThread && n < filePaths.Length; n++)
                {
                    string tmp = filePaths[n].Substring(filePaths[n].LastIndexOf('_') + 1);
                    fileNamesInLong.Add(long.Parse(tmp.Substring(0, tmp.LastIndexOf('.'))));
                }
                fileNamesInLong.Sort();

                // Submit files to server except the last file
                for (int n = 0; !stopSubmitDataThread && n < fileNamesInLong.Count - 1; n++)
                {
                    string filepath = Path.Combine(Tools.APP_DIR, $"sw_{fileNamesInLong[n]}.csv");
                    await reportToApiServer(path: filepath, postTransferTask: new Task(() => { File.Delete(filepath); }));
                }
                Thread.Sleep(300);

                stopSubmitDataThread = true;
                stopSubmitDataThread = false;


                isDataSubmitRunning = false;
                long curTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                prevSubmissionTime = curTime;
                Device.BeginInvokeOnMainThread(() => { EnableUploadBtn(true); });
            });
            submitDataThread.IsBackground = true;
            submitDataThread.Start();
        }

        private bool checkInEMARange(double nowMins)
        {

            if (6 * 60 <= nowMins && nowMins <= 10 * 60)
            {
                EMA_ORDER = 1;
                return true;
            }
            else if (10 * 60 <= nowMins && nowMins <= 14 * 60)
            {
                EMA_ORDER = 2;
                return true;
            }
            else if (14 * 60 <= nowMins && nowMins <= 18 * 60)
            {
                EMA_ORDER = 3;
                return true;
            }
            else if (18 * 60 <= nowMins && nowMins <= 22 * 60)
            {
                EMA_ORDER = 4;
                return true;
            }
            else
            {
                EMA_ORDER = 0;
                return false;
            }
        }
    }

}