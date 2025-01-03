using System.Text;
using System.Text.Json;
using MatrixApp.Models;

namespace MatrixApp.Services
{
    public class MatrixService
    {
        private static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        private static readonly HttpClient client = new();

        static readonly string matrixBaseUrl = "http://100.29.16.196:8080/api/matrix";
        static readonly string matrixInitUrl = matrixBaseUrl + "/init";
        static readonly string matrixARowUrl = matrixBaseUrl + "/A/row";
        static readonly string matrixBColumnUrl = matrixBaseUrl + "/B/column";

        private static readonly Dictionary<int, int[]> cachedRows = [];
        private static readonly Dictionary<int, int[]> cachedColumns = [];

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

        public static int MultiplyRowAndColumn(int[] row, int[] column)
        {
            if (row.Length != column.Length)
                throw new ArgumentException("Row and Column must have the same length." + row.Length + " " + column.Length);

            int result = 0;
            for (int i = 0; i < row.Length; i++)
            {
                result += row[i] * column[i];
            }
            return result;
        }

        public static int[]? RowSelector(RowData rowData) => rowData.Row;

        public static int[]? ColumnSelector(ColumnData columnData) => columnData.Column;

        public static int[]? ParseMatrixData<T>(string data, Func<T, int[]?> selector)
        {
            var deserializedData = JsonSerializer.Deserialize<T>(data, jsonSerializerOptions);

            int[]? result = deserializedData != null ? selector(deserializedData) : null;
            return result;
        }

        public static async Task<int[]?> GetMatrixARowAsync(int rowNumber)
        {
            if (cachedColumns.TryGetValue(rowNumber, out var cachedRow))
            {
                return cachedRow;
            }

            var rowResponse = await client.GetAsync(matrixARowUrl + "/" + rowNumber);

            if (!rowResponse.IsSuccessStatusCode) throw new Exception("Failed to fetch data for multiplication.");

            var rowData = rowResponse.Content.ReadAsStringAsync().Result;
            var row = ParseMatrixData<RowData>(rowData, RowSelector);

            if (row != null)
            {
                cachedRows[rowNumber] = row;
            }

            return row;
        }

        public static async Task<int[]?> GetMatrixBColumnAsync(int columnNumber)
        {
            if (cachedColumns.TryGetValue(columnNumber, out var cachedColumn))
            {
                return cachedColumn;
            }

            var columnResponse = await client.GetAsync(matrixBColumnUrl + "/" + columnNumber);
            if (!columnResponse.IsSuccessStatusCode) throw new Exception("Failed to fetch data for multiplication.");

            var columnData = columnResponse.Content.ReadAsStringAsync().Result;
            var column = ParseMatrixData<ColumnData>(columnData, ColumnSelector);

            if (column != null)
            {
                cachedColumns[columnNumber] = column;
            }

            return column;
        }

        public static async Task<int> FetchAndMultiplyMatricesAsync(int rowNumber, int columnNumber)
        {
            Console.WriteLine($"Fetching data for multiplication from row {rowNumber} and column {columnNumber}...");

            var row = await GetMatrixARowAsync(rowNumber);
            var column = await GetMatrixBColumnAsync(columnNumber);

            if (row == null || column == null)
            {
                throw new Exception("Failed to fetch data for multiplication.");
            }

            return MultiplyRowAndColumn(row, column);
        }

        public static async Task<int[,]> MultiplyMatricesToCreateNewMatrixAsync(int count)
        {
            int[,] resultMatrix = new int[count, count];
            var tasks = new List<Task<(int Row, int Column, int Result)>>();

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    int row = i;
                    int column = j;

                    tasks.Add(Task.Run(async () =>
                    {
                        var rowData = await GetMatrixARowAsync(row);
                        var columnData = await GetMatrixBColumnAsync(column);

                        if (rowData == null || columnData == null)
                            throw new Exception($"Failed to fetch data for row {row} and column {column}.");

                        int result = MultiplyRowAndColumn(rowData, columnData);
                        return (Row: row, Column: column, Result: result);
                    }));
                }
            }

            var results = await Task.WhenAll(tasks);

            foreach (var (Row, Column, Result) in results)
            {
                resultMatrix[Row, Column] = Result;
            }

            return resultMatrix;
        }
    }
}
