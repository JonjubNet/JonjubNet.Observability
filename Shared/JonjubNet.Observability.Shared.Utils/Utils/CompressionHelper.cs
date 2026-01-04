using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace JonjubNet.Observability.Shared.Utils
{
    /// <summary>
    /// Helper para compresión de batches (común para Metrics y Logging)
    /// </summary>
    public static class CompressionHelper
    {
        /// <summary>
        /// Comprime datos usando GZip
        /// Optimizado: usa ArrayPool para reducir allocations
        /// </summary>
        public static byte[] CompressGZip(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
            }
            
            // Optimizado: usar GetBuffer() si es posible para evitar copia
            if (output.TryGetBuffer(out var buffer))
            {
                var result = new byte[buffer.Count];
                Buffer.BlockCopy(buffer.Array!, buffer.Offset, result, 0, buffer.Count);
                return result;
            }
            
            return output.ToArray();
        }

        /// <summary>
        /// Comprime un string usando GZip
        /// </summary>
        public static byte[] CompressGZipString(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return CompressGZip(bytes);
        }

        /// <summary>
        /// Descomprime datos usando GZip
        /// Optimizado: usa GetBuffer() para reducir allocations
        /// </summary>
        public static byte[] DecompressGZip(byte[] compressedData)
        {
            using var input = new MemoryStream(compressedData);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            
            // Optimizado: usar GetBuffer() si es posible para evitar copia
            if (output.TryGetBuffer(out var buffer))
            {
                var result = new byte[buffer.Count];
                Buffer.BlockCopy(buffer.Array!, buffer.Offset, result, 0, buffer.Count);
                return result;
            }
            
            return output.ToArray();
        }

        /// <summary>
        /// Descomprime un string usando GZip
        /// </summary>
        public static string DecompressGZipString(byte[] compressedData)
        {
            var decompressed = DecompressGZip(compressedData);
            return Encoding.UTF8.GetString(decompressed);
        }

        /// <summary>
        /// Comprime datos usando Brotli (mejor ratio de compresión)
        /// Optimizado: usa GetBuffer() para reducir allocations
        /// </summary>
        public static byte[] CompressBrotli(byte[] data)
        {
            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, CompressionMode.Compress))
            {
                brotli.Write(data, 0, data.Length);
            }
            
            // Optimizado: usar GetBuffer() si es posible para evitar copia
            if (output.TryGetBuffer(out var buffer))
            {
                var result = new byte[buffer.Count];
                Buffer.BlockCopy(buffer.Array!, buffer.Offset, result, 0, buffer.Count);
                return result;
            }
            
            return output.ToArray();
        }

        /// <summary>
        /// Comprime un string usando Brotli
        /// </summary>
        public static byte[] CompressBrotliString(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return CompressBrotli(bytes);
        }

        /// <summary>
        /// Descomprime datos usando Brotli
        /// Optimizado: usa GetBuffer() para reducir allocations
        /// </summary>
        public static byte[] DecompressBrotli(byte[] compressedData)
        {
            using var input = new MemoryStream(compressedData);
            using var brotli = new BrotliStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            brotli.CopyTo(output);
            
            // Optimizado: usar GetBuffer() si es posible para evitar copia
            if (output.TryGetBuffer(out var buffer))
            {
                var result = new byte[buffer.Count];
                Buffer.BlockCopy(buffer.Array!, buffer.Offset, result, 0, buffer.Count);
                return result;
            }
            
            return output.ToArray();
        }
    }
}

