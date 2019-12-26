using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyTrackTizenAgent.Model
{
    public abstract class BaseSensorModel : INotifyPropertyChanged
    {
        private bool isSupported;
        private int sensorCount;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSupported
        {
            get { return isSupported; }
            set
            {
                isSupported = value;
                OnPropertyChanged();
            }
        }
        public int SensorCount
        {
            get { return sensorCount; }
            set
            {
                sensorCount = value;
                OnPropertyChanged();
            }
        }
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}