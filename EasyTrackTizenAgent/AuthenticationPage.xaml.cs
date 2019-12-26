using System;
using System.Collections.Generic;
using System.Json;
using System.Net.Http;
using System.Threading;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Forms;

namespace EasyTrackTizenAgent
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AuthenticationPage : CirclePage
    {
        public AuthenticationPage()
        {
            InitializeComponent();
        }

        public void SignInClicked(object sender, EventArgs e)
        {
            IsEnabled = false;
            new Thread(async () =>
            {

                HttpResponseMessage response = await Tools.post(Tools.API_AUTHENTICATE, new Dictionary<string, string>
                    {
                        { "username", loginEntry.Text },
                        { "password", passwordEntry.Text }
                    });
                if (response.IsSuccessStatusCode)
                {
                    JsonValue resJson = JsonValue.Parse(await response.Content.ReadAsStringAsync());
                    if ((ServerResult)(int)resJson["result"] == ServerResult.OK)
                    {
                        Tizen.Applications.Preference.Set("logged_in", true);
                        Tizen.Applications.Preference.Set("username", loginEntry.Text);
                        Tizen.Applications.Preference.Set("password", passwordEntry.Text);

                        /*IsEnabled = true;
                        MainPage mainPage = new MainPage();
                        await Navigation.PushAsync(mainPage);*/

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            IsEnabled = true;
                            Navigation.PushModalAsync(new MainPage());
                        });
                    }
                    else
                    {
                        Toast.DisplayText("Failed to log in, please try again!");
                        IsEnabled = true;
                    }
                }


            }).Start();
        }
    }
}