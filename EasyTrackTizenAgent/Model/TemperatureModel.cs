namespace EasyTrackTizenAgent.Model
{
    public class TemperatureModel : BaseSensorModel
    {
        private float temperature;

        public float Temperature
        {
            get { return temperature; }
            set
            {
                temperature = value;
                OnPropertyChanged();
            }
        }
    }
}