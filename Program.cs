using System.Diagnostics;
using System.Net;
using MatrixApp.Services;

class Program
{
	static readonly int count = 1_000;
	static readonly int batchSize = 200;

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

		Console.WriteLine($"Fetching {count} rows and columns in batches of {batchSize}...");
		operationStopwatch.Start();
		await MatrixService.FetchAllRowsAndColumnsInBatchesAsync(count, batchSize);
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
