using OOP.Domain.Entities;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace OOP.Infrastructure.Storage
{
    public class JsonStorage : IStorage
    {
        private readonly string _basePath;

        private static readonly Type[] KnownTypes =
        {
            typeof(User), typeof(Passenger), typeof(Driver), typeof(Admin),
            typeof(Vehicle), typeof(Motorbike), typeof(Car),
            typeof(Trip), typeof(Payment), typeof(Rating), typeof(Fare), typeof(Location)
        };

        public JsonStorage(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
                throw new ArgumentException("BasePath không được để trống.");
            _basePath = basePath;
            Directory.CreateDirectory(_basePath);
        }

        public async Task SaveAsync<T>(string fileName, T data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var filePath = GetPath(fileName);
            var tempPath = filePath + ".tmp";

            try
            {
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    CreateSerializer<T>().WriteObject(fs, data);
                    await fs.FlushAsync();
                }
                File.Copy(tempPath, filePath, overwrite: true);
                File.Delete(tempPath);
            }
            catch (SerializationException ex)
            {
                throw new InvalidOperationException(
                    $"Lỗi ghi file '{fileName}': không thể serialize.", ex);
            }
        }

        public async Task<T?> LoadAsync<T>(string fileName)
        {
            var filePath = GetPath(fileName);
            if (!File.Exists(filePath)) return default;

            try
            {
                var bytes = await File.ReadAllBytesAsync(filePath);
                if (bytes.Length == 0) return default;

                using var stream = new MemoryStream(bytes);
                var result = CreateSerializer<T>().ReadObject(stream);
                return result is T value ? value : default;
            }
            catch (SerializationException ex)
            {
                throw new InvalidOperationException(
                    $"Lỗi đọc file '{fileName}': dữ liệu bị hỏng hoặc thiếu KnownType.\n{ex.Message}", ex);
            }
        }

        private string GetPath(string fileName) =>
            Path.Combine(_basePath, fileName.EndsWith(".json") ? fileName : fileName + ".json");

        private static DataContractJsonSerializer CreateSerializer<T>() =>
            new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
            {
                KnownTypes = KnownTypes,
                UseSimpleDictionaryFormat = true,
                EmitTypeInformation = EmitTypeInformation.Always
            });
    }
}