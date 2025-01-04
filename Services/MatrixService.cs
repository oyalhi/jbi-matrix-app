using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MatrixApp.Models;

namespace MatrixApp.Services
{
	public class MatrixService
	{
		private static readonly HttpClient client = new();
		private static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

		static readonly string baseUrl = "http://100.29.16.196:8080/api/matrix";
		static readonly string initUrl = baseUrl + "/init";
		static readonly string validateUrl = baseUrl + "/validate";
		static readonly string matrixARowUrl = baseUrl + "/A/row";
		static readonly string matrixBColumnUrl = baseUrl + "/B/column";

		private readonly static ConcurrentDictionary<int, int[]> matrixARows = [];
		private readonly static ConcurrentDictionary<int, int[]> matrixBColumns = [];

		public static async Task InitializeAsync(int count)
		{
			var requestBody = new StringContent(
				JsonSerializer.Serialize(new { n = count }),
				Encoding.UTF8,
				"application/json"
			);

			var matrixInitResponse = await client.PostAsync(initUrl, requestBody);

			if (!matrixInitResponse.IsSuccessStatusCode)
			{
				throw new Exception("Failed to initialize matrices.");
			}

			Console.WriteLine("Matrices initialized successfully.");
		}

		public static int[]? ParseMatrixData<T>(string data, Func<T, int[]?> selector)
		{
			var deserializedData = JsonSerializer.Deserialize<T>(data, jsonSerializerOptions);

			int[]? result = deserializedData != null ? selector(deserializedData) : null;
			return result;
		}

		public static async Task FetchAllRowsAndColumnsInBatchesAsync(int count, int batchSize)
		{
			Console.WriteLine("Starting to pre-fetch rows and columns in batches...");

			for (int start = 0; start < count; start += batchSize)
			{
				int end = Math.Min(start + batchSize, count);

				var fetchRowsTasks = Enumerable.Range(start, end - start)
					.Select(async i =>
					{
						await FetchAndCacheRowAsync(i);
					});

				var fetchColumnsTasks = Enumerable.Range(start, end - start)
					.Select(async i =>
					{
						await FetchAndCacheColumnAsync(i);
					});

				await Task.WhenAll(fetchRowsTasks.Concat(fetchColumnsTasks));
			}

			Console.WriteLine("All rows and columns have been pre-fetched successfully in batches.");
		}

		private static async Task FetchAndCacheRowAsync(int rowNumber)
		{
			var url = matrixARowUrl + "/" + rowNumber;
			var response = await client.GetAsync(url);
			if (!response.IsSuccessStatusCode)
			{
				var responseContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Failed to fetch row {rowNumber} from Matrix A. Status: {response.StatusCode}, Response: {responseContent}");
			}

			var rowData = await response.Content.ReadAsStringAsync();
			var row = ParseMatrixData<RowData>(rowData, data => data.Row) ?? throw new Exception($"Failed to parse row {rowNumber} from Matrix A.");
			matrixARows[rowNumber] = row;
		}

		private static async Task FetchAndCacheColumnAsync(int columnNumber)
		{
			var url = matrixBColumnUrl + "/" + columnNumber;
			var response = await client.GetAsync(url);
			if (!response.IsSuccessStatusCode)
			{
				var responseContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Failed to fetch column {columnNumber} from Matrix A. Status: {response.StatusCode}, Response: {responseContent}");
			}

			var columnData = await response.Content.ReadAsStringAsync();
			var column = ParseMatrixData<ColumnData>(columnData, data => data.Column) ?? throw new Exception($"Failed to parse column {columnNumber} from Matrix B.");
			matrixBColumns[columnNumber] = column;
		}

		public static int MultiplyRowAndColumn(int rowNumber, int columnNumber)
		{
			if (!matrixARows.TryGetValue(rowNumber, out _))
				throw new Exception($"Row {rowNumber} is missing from the cache.");

			if (!matrixBColumns.TryGetValue(columnNumber, out _))
				throw new Exception($"Column {columnNumber} is missing from the cache.");

			var row = matrixARows[rowNumber];
			var column = matrixBColumns[columnNumber];

			if (row.Length != column.Length)
				throw new ArgumentException("Row and Column lengths do not match.");

			return row.Zip(column, (r, c) => r * c).Sum();
		}

		public static int[,] MultiplyMatricesToCreateNewMatrix(int count)
		{
			int[,] resultMatrix = new int[count, count];

			for (int i = 0; i < count; i++)
			{
				for (int j = 0; j < count; j++)
				{
					resultMatrix[i, j] = MultiplyRowAndColumn(i, j);
				}
			}

			return resultMatrix;
		}

		public static async Task ValidateMatrixAsync(int[,] resultMatrix)
		{
			var arrayOfArray = ConvertToArrayOfArrays(resultMatrix);
			var serializedMatrix = JsonSerializer.Serialize(arrayOfArray);
			var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(serializedMatrix));
			var base64Hash = Convert.ToBase64String(hashBytes);

			Console.WriteLine($"Generated MD5 Base64 Hash: {base64Hash}");

			var requestBody = new StringContent(
				JsonSerializer.Serialize(new { hash = base64Hash }),
				Encoding.UTF8,
				"application/json"
			);

			var response = await client.PostAsync(validateUrl, requestBody);

			if (!response.IsSuccessStatusCode)
			{
				var errorMessage = await response.Content.ReadAsStringAsync();
				throw new Exception($"Validation failed. Status: {response.StatusCode}, Response: {errorMessage}");
			}
		}

		private static int[][] ConvertToArrayOfArrays(int[,] multiDimensionalArray)
		{
			int rows = multiDimensionalArray.GetLength(0);
			int cols = multiDimensionalArray.GetLength(1);

			var arrayOfArrays = new int[rows][];

			for (int i = 0; i < rows; i++)
			{
				arrayOfArrays[i] = new int[cols];
				for (int j = 0; j < cols; j++)
				{
					arrayOfArrays[i][j] = multiDimensionalArray[i, j];
				}
			}

			return arrayOfArrays;
		}
	}
}
