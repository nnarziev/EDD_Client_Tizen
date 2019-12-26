using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace EasyTrackTizenAgent
{
    class Tools
    {
        // Readonly values
        internal static readonly string APP_DIR = Tizen.Applications.Application.Current.DirectoryInfo.Data;

        // Common constants
        internal const string TAG = "YouNoOne";
        internal const ushort CHANNEL_ID = 104;
        internal const uint SENSOR_SAMPLING_INTERVAL = 1000; // milliseconds
        internal const string HEALTHINFO_PRIVILEGE = "http://tizen.org/privilege/healthinfo";
        // API constants
        internal const string API_REGISTER = "register";
        internal const string API_UNREGISTER = "unregister";
        internal const string API_AUTHENTICATE = "user/login";
        internal const string API_SUBMIT_HEARTBEAT = "user/heartbeat";
        internal const string API_SUBMIT_DATA = "sensor_data/submit";
        internal const string API_NOTIFY = "notify";

        // Actions
        public const byte REQUEST_DATA = 0x01;

        // Sensors codes
        internal const ushort DATA_SRC_HRM = 50;
        internal const ushort DATA_SRC_ACC = 51;

        internal const int HEARTBEAT_PERIOD = 5 * 60;  //in sec
        internal const int FILE_SUBMISSION_PERIOD = 10 * 60;  //in sec

        internal const double HRM_SENSING_PERIOD = 5;    //in min
        internal const double HRM_SENSING_DURATION = 1;    //in min

        internal const double ACC_SENSING_PERIOD = 30;    //in minutes
        internal const double ACC_SENSING_DURATION = 3;    //in minutes


        internal const int FILE_SUBMIT_PERIOD = 2;    //in minutes

        internal const int NEW_FILE_CREATE_PERIOD = 1;    //in minutes

        internal async static Task<HttpResponseMessage> post(string api, Dictionary<string, string> body, byte[] fileContent = null, string fileName = null)
        {
            const string SERVER_URL = "http://165.246.21.204:8765";

            try
            {
                if (fileContent == null)
                    using (HttpClient client = new HttpClient())
                        return await client.PostAsync($"{SERVER_URL}/{api}", new FormUrlEncodedContent(body));
                else
                    using (HttpContent bytesContent = new ByteArrayContent(fileContent))
                    using (MultipartFormDataContent formData = new MultipartFormDataContent())
                    using (HttpClient client = new HttpClient())
                    {
                        foreach (var elem in body)
                            formData.Add(new StringContent(elem.Value), elem.Key, elem.Key);
                        formData.Add(bytesContent, "file", fileName);
                        return await client.PostAsync($"{SERVER_URL}/{api}", formData);
                    }
            }
            catch
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
            }
        }

        internal static void sendHeartBeatMessage()
        {
            new Thread(async () =>
            {
                await post(API_SUBMIT_HEARTBEAT, new Dictionary<string, string>
                    {
                        { "username", Tizen.Applications.Preference.Get<string>("username") },
                        { "password", Tizen.Applications.Preference.Get<string>("password") }
                    });
            }).Start();
        }
    }

    // Result codes from server
    enum ServerResult
    {
        OK = 0,
        FAIL = 1,
        BAD_JSON_PARAMETERS = -1,
        USERNAME_TAKEN = 3,
        TOO_SHORT_PASSWORD = 4,
        TOO_LONG_PASSWORD = 5,
        USER_DOES_NOT_EXIST = 6,
        BAD_PASSWORD = 7
    }
}
