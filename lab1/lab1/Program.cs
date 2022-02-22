using ModelLibrary;
using System.Threading.Tasks.Dataflow;

namespace lab1
{
    public class program
    {
        private static async Task Consumer()
        {
            string print;
            while (true)
            {
                print = await Detection.bufferBlock.ReceiveAsync();
                if (print == "end")
                    break;
                Console.WriteLine("Ready");
            }
        }

        public static async Task Main()
        {
            const string imageFolder = @"C:\prac\441_gryaznov\Assets\Images";
            await Task.WhenAll(Detection.Detect(imageFolder, 1), Consumer());       
        }
    }
}