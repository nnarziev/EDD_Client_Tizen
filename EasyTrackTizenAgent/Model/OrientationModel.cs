namespace EasyTrackTizenAgent.Model
{
    public class OrientationModel : BaseSensorModel
    {
        private string accuracy;
        private float azimuth;
        private float pitch;
        private float roll;

        public string Accuracy
        {
            get { return accuracy; }
            set
            {
                accuracy = value;
                OnPropertyChanged();
            }
        }
        public float Azimuth
        {
            get { return azimuth; }
            set
            {
                azimuth = value;
                OnPropertyChanged();
            }
        }
        public float Pitch
        {
            get { return pitch; }
            set
            {
                pitch = value;
                OnPropertyChanged();
            }
        }
        public float Roll
        {
            get { return roll; }
            set
            {
                roll = value;
                OnPropertyChanged();
            }
        }
    }
}