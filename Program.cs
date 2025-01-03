using System.Diagnostics;
using MatrixApp.Services;


class Program
{
    static readonly int count = 1;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Initializing matrices...");
        Stopwatch stopwatch = Stopwatch.StartNew();
        await MatrixService.InitializeAsync(count);

        Console.WriteLine("Pre-fetching all rows and columns...");
        await MatrixService.PreFetchAllRowsAndColumnsAsync(count);

        Console.WriteLine("Performing matrix multiplication...");
        var resultMatrix = MatrixService.MultiplyMatricesToCreateNewMatrix(count);

        stopwatch.Stop();
        Console.WriteLine($"Total time taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");

        Console.WriteLine("Resultant Matrix:");
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                Console.Write(resultMatrix[i, j] + " ");
            }
            Console.WriteLine();
        }

        Console.WriteLine("Matrix multiplication completed.");
    }
}
