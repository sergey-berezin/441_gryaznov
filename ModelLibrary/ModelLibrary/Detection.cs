using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ModelLibrary
{
    public class Detection
    {
        // model is available here:
        // https://github.com/onnx/models/tree/master/vision/object_detection_segmentation/yolov4
        const string modelPath = @"C:\prac\441_gryaznov\yolov4.onnx";

        const string imageOutputFolder = @"C:\prac\441_gryaznov\Assets\Output";

        static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };
        public static BufferBlock<DetectedObject> resultBufferBlock = new BufferBlock<DetectedObject>();
        public static BufferBlock<string> bufferBlock = new BufferBlock<string>();

        public static CancellationTokenSource cancelTokenSource;
        public static CancellationToken token;

        public static byte[] ImageToByte(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        public static async Task Detect(string imageFolder, int labNumber)
        {
            
            MLContext mlContext = new MLContext();
            Directory.CreateDirectory(imageOutputFolder);
            // model is available here:
            // https://github.com/onnx/models/tree/master/vision/object_detection_segmentation/yolov4

            // Define scoring pipeline
            var pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input_1:0", scaleImage: 1f / 255f, interleavePixelColors: true))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    shapeDictionary: new Dictionary<string, int[]>()
                    {
                        { "input_1:0", new[] { 1, 416, 416, 3 } },
                        { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
                        { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                        { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                    },
                    inputColumnNames: new[]
                    {
                        "input_1:0"
                    },
                    outputColumnNames: new[]
                    {
                        "Identity:0",
                        "Identity_1:0",
                        "Identity_2:0"
                    },
                    modelFile: modelPath, recursionLimit: 100));

            var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));
            var predictionEngine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);

            ConcurrentBag<YoloV4Result> detectedObjects = new ConcurrentBag<YoloV4Result>();
            string[] imageNames = Directory.GetFiles(imageFolder);

            Dictionary<string, ConcurrentBag<string>> recognizedObjects = new Dictionary<string, ConcurrentBag<string>>();

            foreach (string name in classesNames)
            {
                recognizedObjects.Add(name, new ConcurrentBag<string>());
            }

            object locker = new object();

            var sw = new Stopwatch();
            sw.Start();
           
            string[] imageName = Directory.GetFiles(imageFolder);


            var ab1 = new ActionBlock<string>(async image => {
                YoloV4Prediction predict;
                string iName = Path.GetFileName(image);
                if (labNumber == 1)
                    Console.WriteLine($"Изображение {iName} в обработке");
                lock (locker)
                {
                    var bitmap = new Bitmap(Image.FromFile(Path.Combine(image)));
                    predict = predictionEngine.Predict(new YoloV4BitmapData() { Image = bitmap });
                }

                var results = predict.GetResults(classesNames, 0.3f, 0.7f);

                foreach (var res in results)
                {
                    recognizedObjects[res.Label].Add(image);
                    if (labNumber == 1)
                        Console.WriteLine($"Объект '{res.Label}' был найден на картинке {iName}");
                    DetectedObject obj = new DetectedObject();
                    var x1 = res.BBox[0];
                    var y1 = res.BBox[1];
                    var x2 = res.BBox[2];
                    var y2 = res.BBox[3];
                    var type = res.Label;

                    Rectangle cropRect = new Rectangle((int)x1, (int)y1, (int)(x2 - x1), (int)(y2 - y1));
                    Bitmap src = Image.FromFile(image) as Bitmap;
                    Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);
                    Bitmap marker = Image.FromFile(image) as Bitmap;
                    using (Graphics g = Graphics.FromImage(marker))
                    {
                        Pen RedPen = new Pen(Color.Red, 3);
                        g.DrawRectangle(RedPen, cropRect);
                    }
                    obj.x1 = x1;
                    obj.x2 = x2;
                    obj.y1 = y1;
                    obj.y2 = y2;
                    obj.Type = type;
                    obj.BitmapImageFull = ImageToByte(marker);
                    using (Graphics g = Graphics.FromImage(target))
                    {
                        g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height), cropRect, GraphicsUnit.Pixel);
                    }
                    
                    string imageOutputPath =$"{imageOutputFolder}/{res.Label}{x1}{y2}.jpg";
                    obj.BitmapImageObj = ImageToByte(target);
                    target.Save(imageOutputPath);

                    if (!token.IsCancellationRequested)
                    {
                        await resultBufferBlock.SendAsync(obj);
                    }
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = token
            });

            Parallel.For(0, imageNames.Length, i => ab1.Post(imageNames[i]));
            ab1.Complete();
            await ab1.Completion;
           // await resultBufferBlock.SendAsync();
            await bufferBlock.SendAsync($"Total number of objects: {detectedObjects.Count}");
            await bufferBlock.SendAsync("end");
            await resultBufferBlock.SendAsync(null);
            sw.Stop();
        }
    }
}
