using System.Diagnostics;
using MatrixApp.Services;

class Program
{
	static readonly int count = 1000;

	static async Task Main(string[] args)
	{
		var totalStopwatch = new Stopwatch();
		var operationStopwatch = new Stopwatch();

		totalStopwatch.Start();

		Console.WriteLine("Initializing matrices...");
		operationStopwatch.Start();
		await MatrixService.InitializeAsync(count);
		operationStopwatch.Stop();
		Console.WriteLine($"Initialization took: {operationStopwatch.ElapsedMilliseconds} ms");
		Console.WriteLine();

		operationStopwatch.Reset();

		Console.WriteLine("Fetching all rows and columns...");
		operationStopwatch.Start();
		await MatrixService.FetchAllRowsAndColumnsInBatchesAsync(count, 100);
		operationStopwatch.Stop();
		Console.WriteLine($"Fetching took: {operationStopwatch.ElapsedMilliseconds} ms");
		Console.WriteLine();

		operationStopwatch.Reset();

		Console.WriteLine("Performing matrix multiplication...");
		operationStopwatch.Start();
		var resultMatrix = MatrixService.MultiplyMatricesToCreateNewMatrix(count);
		operationStopwatch.Stop();
		Console.WriteLine("Matrix multiplication completed.");
		Console.WriteLine($"Matrix multiplication took: {operationStopwatch.ElapsedMilliseconds} ms");
		Console.WriteLine();

		operationStopwatch.Reset();

		operationStopwatch.Start();
		try
		{
			Console.WriteLine("Validating result matrix...");
			await MatrixService.ValidateMatrixAsync(resultMatrix);
			Console.WriteLine("Matrix multiplication validated successfully.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Validation failed: {ex.Message}");
		}
		operationStopwatch.Stop();
		Console.WriteLine($"Validation took: {operationStopwatch.ElapsedMilliseconds} ms");
		Console.WriteLine();

		totalStopwatch.Stop();
		Console.WriteLine($"Total execution time: {totalStopwatch.ElapsedMilliseconds} ms");
	}
}
