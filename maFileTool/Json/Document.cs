using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace maFileTool.Json
{
    public class Document
    {
        private static JsonSerializerOptions? options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            TypeInfoResolver = SourceGenerationContext.Default,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
        public static async Task WriteJsonAsync<T>(T model, string? filename)
        {
            // Открываем или создаем файл для записи (перезапишет файл, если он существует)
            using (FileStream fs = new FileStream(filename!, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync<T>(fs, model, options);
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
        public static async Task<T?> ReadJsonAsync<T>(string? filename)
        {
            if (!File.Exists(filename))
                return default;

            // чтение данных
            using (FileStream fs = new FileStream(filename!, FileMode.OpenOrCreate))
            {
                return await JsonSerializer.DeserializeAsync<T>(fs, options);
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
        public static async Task<T?> DeserializeJsonAsync<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default;

            using MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json));

            return await JsonSerializer.DeserializeAsync<T>(ms, options);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
        public static async Task<string> SerializeJsonAsync<T>(T model)
        {
            if (model is null)
                return string.Empty;

            using MemoryStream ms = new MemoryStream();
            await JsonSerializer.SerializeAsync<T>(ms, model, options);
            ms.Position = 0;
            using StreamReader reader = new StreamReader(ms);

            string json = await reader.ReadToEndAsync();

            return json;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
        public static T? DeserializeJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, options);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
        public static string SerializeJson<T>(T model)
        {
            if (model is null)
                return string.Empty;

            return JsonSerializer.Serialize(model, options);
        }
    }
}
