namespace ModelLibrary
{
    public class DetectedObject
    {
        public int DetectedObjectId { get; set; }
        public float x1 { get; set; }
        public float y1 { get; set; }
        public float x2 { get; set; }
        public float y2 { get; set; }
        public byte[]? BitmapImageObj 
        {
            get; set;
        }
        public byte[]? BitmapImageFull { get; set; }
        public string? Type { get; set; }


    }

}
