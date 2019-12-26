namespace EasyTrackTizenAgent.Model
{
    public class MagnetometerModel : BaseSensorModel
    {
        private string accuracy;
        private float x;
        private float y;
        private float z;

        public string Accuracy
        {
            get { return accuracy; }
            set
            {
                accuracy = value;
                OnPropertyChanged();
            }
        }
        public float X
        {
            get { return x; }
            set
            {
                x = value;
                OnPropertyChanged();
            }
        }
        public float Y
        {
            get { return y; }
            set
            {
                y = value;
                OnPropertyChanged();
            }
        }
        public float Z
        {
            get { return z; }
            set
            {
                z = value;
                OnPropertyChanged();
            }
        }
    }
}