namespace EasyTrackTizenAgent.Model
{
    public class PressureModel : BaseSensorModel
    {
        private float pressure;

        public float Pressure
        {
            get { return pressure; }
            set
            {
                pressure = value;
                OnPropertyChanged();
            }
        }
    }
}