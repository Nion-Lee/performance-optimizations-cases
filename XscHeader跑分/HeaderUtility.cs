using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace XscHeader跑分
{
    public class HeaderUtility
    {
        private const string Platform = "3";
        private readonly ILogger<HeaderUtility> _logger;
        private readonly MD5 _md5 = MD5.Create();
        private readonly char[] HexMap = "0123456789abcdef".ToCharArray();
        private readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");
        private long _timeDiffMilliseconds = -327;

        public string GetXscHeader(string url)
        {
            try
            {
                // 1) 時間戳 + 隨機填充
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _timeDiffMilliseconds;
                Span<char> timeHexSpan = stackalloc char[16];
                nowMs.TryFormat(timeHexSpan, out int timeHexLen, "x");
                Span<char> timeRandomSpan = stackalloc char[32];
                for (int i = 0; i < 32; i++)
                    timeRandomSpan[i] = timeHexSpan[i % timeHexLen];

                // 2) 準備 Latin1 bytes of Hostname + Platform + URL
                ReadOnlySpan<char> hostSpan = "imsb-9vkjm.utoyen.com".AsSpan();
                ReadOnlySpan<char> urlSpan = url.AsSpan();
                int hostBytes = Latin1.GetByteCount(hostSpan);
                int urlBytes = Latin1.GetByteCount(urlSpan);
                Span<byte> inputBuffer = stackalloc byte[hostBytes + 1 + urlBytes];
                int pos = 0;
                Latin1.GetBytes(hostSpan, inputBuffer.Slice(pos, hostBytes));
                pos += hostBytes;
                inputBuffer[pos++] = (byte)Platform[0];
                Latin1.GetBytes(urlSpan, inputBuffer.Slice(pos, urlBytes));

                // 3) MD5 雜湊到 stackalloc 的緩衝區
                Span<byte> hashBuffer = stackalloc byte[16];
                _md5.TryComputeHash(inputBuffer, hashBuffer, out int hashLen);

                // 4) 轉成 32 位 hex chars
                Span<char> hexSpan = stackalloc char[hashBuffer.Length * 2];
                for (int i = 0; i < hashBuffer.Length; i++)
                {
                    byte b = hashBuffer[i];
                    hexSpan[2 * i] = HexMap[b >> 4];
                    hexSpan[2 * i + 1] = HexMap[b & 0xF];
                }
                int hexLen = hashBuffer.Length * 2;

                // 5) partOne = 0x02 前綴 + (hex XOR timeRandom)
                int partOneLen = 1 + hexLen;
                Span<char> partOneSpan = stackalloc char[partOneLen];
                partOneSpan[0] = (char)2;
                for (int i = 0; i < hexLen; i++)
                    partOneSpan[i + 1] = (char)(hexSpan[i] ^ timeRandomSpan[i]);

                // 6) partTwo & partThree 從 randomSpan
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

                // 7) 全部拼成 mergedSpan
                int totalLen = partOneLen + pl * 2;
                Span<char> mergedSpan = stackalloc char[totalLen];
                partOneSpan.CopyTo(mergedSpan);
                partTwoSpan.CopyTo(mergedSpan.Slice(partOneLen, pl));
                partThreeSpan.CopyTo(mergedSpan.Slice(partOneLen + pl, pl));

                // 8) Latin-1 編碼到位元組，最後 Base64
                int outByteCount = Latin1.GetByteCount(mergedSpan);
                Span<byte> outBytes = stackalloc byte[outByteCount];
                Latin1.GetBytes(mergedSpan, outBytes);

                // 利用 Convert 的 Span<byte> 版方法直接產生字串
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
