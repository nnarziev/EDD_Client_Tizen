namespace EasyTrackTizenAgent.Model
{
    public class HRMModel : BaseSensorModel
    {
        private int heartRate;

        public int HeartRate
        {
            get { return heartRate; }
            set
            {
                heartRate = value;
                OnPropertyChanged();
            }
        }
    }
}