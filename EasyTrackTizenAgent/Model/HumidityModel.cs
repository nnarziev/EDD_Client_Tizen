namespace EasyTrackTizenAgent.Model
{
    public class HumidityModel : BaseSensorModel
    {
        private float humidity;

        public float Humidity
        {
            get { return humidity; }
            set
            {
                humidity = value;
                OnPropertyChanged();
            }
        }
    }
}