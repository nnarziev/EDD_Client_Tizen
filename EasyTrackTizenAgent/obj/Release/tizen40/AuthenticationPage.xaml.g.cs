//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

[assembly: global::Xamarin.Forms.Xaml.XamlResourceIdAttribute("EasyTrackTizenAgent.AuthenticationPage.xaml", "AuthenticationPage.xaml", typeof(global::EasyTrackTizenAgent.AuthenticationPage))]

namespace EasyTrackTizenAgent {
    
    
    [global::Xamarin.Forms.Xaml.XamlFilePathAttribute("AuthenticationPage.xaml")]
    public partial class AuthenticationPage : global::Tizen.Wearable.CircularUI.Forms.CirclePage {
        
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Xamarin.Forms.Build.Tasks.XamlG", "0.0.0.0")]
        private global::Tizen.Wearable.CircularUI.Forms.PopupEntry loginEntry;
        
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Xamarin.Forms.Build.Tasks.XamlG", "0.0.0.0")]
        private global::Tizen.Wearable.CircularUI.Forms.PopupEntry passwordEntry;
        
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Xamarin.Forms.Build.Tasks.XamlG", "0.0.0.0")]
        private global::Xamarin.Forms.Button signInButton;
        
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Xamarin.Forms.Build.Tasks.XamlG", "0.0.0.0")]
        private void InitializeComponent() {
            global::Xamarin.Forms.Xaml.Extensions.LoadFromXaml(this, typeof(AuthenticationPage));
            loginEntry = global::Xamarin.Forms.NameScopeExtensions.FindByName<global::Tizen.Wearable.CircularUI.Forms.PopupEntry>(this, "loginEntry");
            passwordEntry = global::Xamarin.Forms.NameScopeExtensions.FindByName<global::Tizen.Wearable.CircularUI.Forms.PopupEntry>(this, "passwordEntry");
            signInButton = global::Xamarin.Forms.NameScopeExtensions.FindByName<global::Xamarin.Forms.Button>(this, "signInButton");
        }
    }
}