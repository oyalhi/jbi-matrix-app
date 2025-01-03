using System.Text;
using System.Text.Json;
using MatrixApp.Models;

namespace MatrixApp.Services
{
    public class MatrixService
    {
        private static readonly HttpClient client = new();
        private static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        static readonly string matrixBaseUrl = "http://100.29.16.196:8080/api/matrix";
        static readonly string matrixInitUrl = matrixBaseUrl + "/init";
        static readonly string matrixARowUrl = matrixBaseUrl + "/A/row";
        static readonly string matrixBColumnUrl = matrixBaseUrl + "/B/column";

        private readonly static Dictionary<int, int[]> matrixARows = [];
        private readonly static Dictionary<int, int[]> matrixBColumns = [];

        public static async Task InitializeAsync(int count)
        {
            var requestBody = new StringContent(
                JsonSerializer.Serialize(new { n = count }),
                Encoding.UTF8,
                "application/json"
            );

            var matrixInitResponse = await client.PostAsync(matrixInitUrl, requestBody);

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

        public static async Task PreFetchAllRowsAndColumnsAsync(int count)
        {
            Console.WriteLine("Starting to pre-fetch rows and columns...");

            var fetchRowsTasks = Enumerable.Range(1, count)
                .Select(async i =>
                {
                    await FetchAndCacheRowAsync(i);
                    Console.WriteLine($"Fetched row {i}.");
                });

            var fetchColumnsTasks = Enumerable.Range(1, count)
                .Select(async i =>
                {
                    await FetchAndCacheColumnAsync(i);
                    Console.WriteLine($"Fetched column {i}.");
                });

            // Wait for all tasks
            await Task.WhenAll(fetchRowsTasks.Concat(fetchColumnsTasks));

            Console.WriteLine("All rows and columns have been pre-fetched successfully.");
        }


        private static async Task FetchAndCacheRowAsync(int rowNumber)
        {
            var url = matrixARowUrl + "/" + rowNumber;
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"IsSuccessStatusCode: {response.IsSuccessStatusCode} {rowNumber} {url}");
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to fetch row {rowNumber} from Matrix A. Status: {response.StatusCode}, Response: {responseContent}");
            }

            var rowData = await response.Content.ReadAsStringAsync();
            var row = ParseMatrixData<RowData>(rowData, data => data.Row);
            if (row != null) matrixARows[rowNumber] = row;
        }

        private static async Task FetchAndCacheColumnAsync(int columnNumber)
        {
            var url = matrixBColumnUrl + "/" + columnNumber;
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"IsSuccessStatusCode: {response.IsSuccessStatusCode} {columnNumber} {url}");
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to fetch column {columnNumber} from Matrix A. Status: {response.StatusCode}, Response: {responseContent}");
            }

            var columnData = await response.Content.ReadAsStringAsync();
            var column = ParseMatrixData<ColumnData>(columnData, data => data.Column);
            if (column != null) matrixBColumns[columnNumber] = column;
        }

        public static int MultiplyRowAndColumn(int rowNumber, int columnNumber)
        {
            if (!matrixARows.ContainsKey(rowNumber) || !matrixBColumns.ContainsKey(columnNumber))
                throw new Exception($"Row {rowNumber} or Column {columnNumber} is missing from the cache.");

            var row = matrixARows[rowNumber];
            var column = matrixBColumns[columnNumber];

            if (row.Length != column.Length)
                throw new ArgumentException("Row and Column lengths do not match.");

            return row.Zip(column, (r, c) => r * c).Sum();
        }

        public static int[,] MultiplyMatricesToCreateNewMatrix(int count)
        {
            int[,] resultMatrix = new int[count, count];

            for (int i = 1; i <= count; i++)
            {
                for (int j = 1; j <= count; j++)
                {
                    resultMatrix[i - 1, j - 1] = MultiplyRowAndColumn(i, j);
                }
            }

            return resultMatrix;
        }
    }

}
