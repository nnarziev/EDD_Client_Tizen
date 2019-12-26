namespace EasyTrackTizenAgent.Model
{
    public class RotationVectorModel : BaseSensorModel
    {
        private string proximity;
        private float w;
        private float x;
        private float y;
        private float z;

        public string Accuracy
        {
            get { return proximity; }
            set
            {
                proximity = value;
                OnPropertyChanged();
            }
        }
        public float W
        {
            get { return w; }
            set
            {
                w = value;
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