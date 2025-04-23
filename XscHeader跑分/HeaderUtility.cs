using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace XscHeader跑分
{
    public class HeaderUtility
    {
        private const string Platform = "3";
        private readonly ILogger<HeaderUtility> _logger;
        private readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");
        private readonly char[] HexMap = "0123456789abcdef".ToCharArray();
        private long _timeDiffMilliseconds = -327;

        public string GetXscHeader(string url)
        {
            try
            {
                // --- 1) 產生 32 位元時間亂數 Span<char> ---
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _timeDiffMilliseconds;
                Span<char> timeHexSpan = stackalloc char[16];
                nowMs.TryFormat(timeHexSpan, out int timeHexLen, "x");
                Span<char> timeRandomSpan = stackalloc char[32];
                for (int i = 0; i < 32; i++)
                    timeRandomSpan[i] = timeHexSpan[i % timeHexLen];

                // --- 2) MD5(Hash) of Hostname+Platform+url into byte[] ---
                ReadOnlySpan<char> hostSpan = "imsb-9vkjm.utoyen.com".AsSpan();
                ReadOnlySpan<char> urlSpan = url.AsSpan();
                int byteCount = Latin1.GetByteCount(hostSpan) + 1 + Latin1.GetByteCount(urlSpan);
                byte[] inputBuffer = new byte[byteCount];
                int pos = 0;
                Latin1.GetBytes(hostSpan, inputBuffer.AsSpan(pos));
                pos += Latin1.GetByteCount(hostSpan);
                inputBuffer[pos++] = (byte)Platform[0];
                Latin1.GetBytes(urlSpan, inputBuffer.AsSpan(pos));
                byte[] hash = MD5.HashData(inputBuffer);

                // --- 3) 16 bytes → 32 hex chars in Span<char> ---
                Span<char> hexSpan = stackalloc char[hash.Length * 2];
                for (int i = 0; i < hash.Length; i++)
                {
                    byte b = hash[i];
                    hexSpan[i * 2] = HexMap[b >> 4];
                    hexSpan[i * 2 + 1] = HexMap[b & 0xF];
                }
                int hexLen = hash.Length * 2;

                // --- 4) partOne: 0x02 前綴 + (hex XOR timeRandom) ---
                int partOneLen = hexLen + 1;
                Span<char> partOneSpan = stackalloc char[partOneLen];
                partOneSpan[0] = (char)2;
                for (int i = 0; i < hexLen; i++)
                    partOneSpan[i + 1] = (char)(hexSpan[i] ^ timeRandomSpan[i]);

                // --- 5) partTwo & partThree from randomString Span ---
                int randomLen = 1 + timeHexLen;
                Span<char> randomSpan = stackalloc char[randomLen];
                randomSpan[0] = (char)128;
                for (int i = 0; i < timeHexLen; i++)
                    randomSpan[1 + i] = timeHexSpan[i];
                int pl = randomLen - 3;
                Span<char> partTwoSpan = stackalloc char[pl];
                Span<char> partThreeSpan = stackalloc char[pl];
                var rnd = Random.Shared;
                for (int i = 0; i < pl; i++)
                {
                    int r = (int)(rnd.NextDouble() * 128);
                    partTwoSpan[i] = (char)(randomSpan[i] ^ r);
                    partThreeSpan[i] = (char)r;
                }

                // --- 6) 合併所有 span into one mergedSpan ---
                int totalLen = partOneLen + pl * 2;
                Span<char> mergedSpan = stackalloc char[totalLen];
                partOneSpan.CopyTo(mergedSpan);
                partTwoSpan.CopyTo(mergedSpan.Slice(partOneLen, pl));
                partThreeSpan.CopyTo(mergedSpan.Slice(partOneLen + pl, pl));

                // --- 7) Latin-1 編碼 & Base64 ---
                int outByteCount = Latin1.GetByteCount(mergedSpan);
                byte[] outBytes = new byte[outByteCount];
                Latin1.GetBytes(mergedSpan, outBytes);
                return Convert.ToBase64String(outBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetXscHeader failed: {ex}");
                return string.Empty;
            }
        }

        public void SetServerDiffTime(long? serverUnixTime)
        {
            if (serverUnixTime == null)
            {
                _logger.LogError("SetServerDiffTime serverUnixTime is null");
                return;
            }

            _timeDiffMilliseconds = serverUnixTime.Value - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
