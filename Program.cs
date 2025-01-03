using System.Diagnostics;
using MatrixApp.Services;

class Program
{
    static readonly int count = 3;
    static int[,] resultMatrix = new int[count, count];

    static async Task Main(string[] args)
    {
        Console.WriteLine("Performing matrix multiplication...");

        Stopwatch stopwatch = Stopwatch.StartNew();

        await MatrixService.InitializeAsync(count);

        resultMatrix = await MatrixService.MultiplyMatricesToCreateNewMatrixAsync(count);

        stopwatch.Stop();

        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                Console.Write(resultMatrix[i, j] + " ");
            }
            Console.WriteLine();
        }

        Console.WriteLine("Matrix multiplication completed.");
        Console.WriteLine($"Total time taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
}
