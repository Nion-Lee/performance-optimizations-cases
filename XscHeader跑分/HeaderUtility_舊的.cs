using Microsoft.Extensions.Logging;
using System.Text;

namespace XscHeader跑分
{
    public class HeaderUtility_舊的
    {
        private const string Platform = "3";
        private const string Hostname = "imsb-9vkjm.utoyen.com";

        private readonly ILogger<HeaderUtility_舊的> _logger;
        private long _timeDiffMilliseconds = -327;

        public string GetXscHeader(string url)
        {
            var base64EncodeResult = string.Empty;

            try
            {
                #region 取得時間亂數

                var timeString = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _timeDiffMilliseconds).ToString("x");
                string timeRandomString = "";
                while (timeRandomString.Length < 32)
                {
                    timeRandomString += timeString;
                }
                timeRandomString = timeRandomString.Substring(0, 32);
                #endregion

                #region 取得網址亂數
                var combinedUrl = Hostname + Platform + url;
                var combinedUrlLength = combinedUrl.Length;
                int[] encryptedArray = { 1732584193, -271733879, -1732584194, 271733878 };
                int operateNum = 64;
                int[] obfuscationArray;

                if (operateNum < combinedUrlLength)
                {
                    for (operateNum = 64; operateNum < combinedUrlLength; operateNum += 64)
                    {
                        var retrieveText = combinedUrl.Substring(operateNum - 64, operateNum);
                        obfuscationArray = new int[16];
                        for (int index = 0; index < 64; index += 4)
                        {
                            obfuscationArray[index >> 2] = Convert.ToInt32(retrieveText[index]) + (Convert.ToInt32(retrieveText[index + 1]) << 8)
                                         + (Convert.ToInt32(retrieveText[index + 2]) << 16)
                                         + (Convert.ToInt32(retrieveText[index + 3]) << 24);
                        }

                        GetEncryptedArray(encryptedArray, obfuscationArray);
                    }
                }

                obfuscationArray = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                var obfuscationCount = combinedUrl.Substring(operateNum - 64).Length;

                for (operateNum = 0; operateNum < obfuscationCount; operateNum++)
                {
                    obfuscationArray[operateNum >> 2] |= (Convert.ToInt32(combinedUrl[operateNum]) << ((operateNum % 4) << 3));
                }
                obfuscationArray[operateNum >> 2] |= (128 << (operateNum % 4 << 3));
                if (operateNum > 55)
                {
                    GetEncryptedArray(encryptedArray, obfuscationArray);
                    for (operateNum = 16; operateNum >= 0; operateNum--)
                    {
                        obfuscationArray[operateNum] = 0;
                    }
                }
                obfuscationArray[14] = 8 * combinedUrlLength;
                GetEncryptedArray(encryptedArray, obfuscationArray);

                string[] randomstringArray = new string[encryptedArray.Length];
                for (var index = 0; index < encryptedArray.Length; index++)
                {
                    string n = "";
                    char[] randomCharArray = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
                    for (int x = 0; x < 4; x++)
                    {
                        n += randomCharArray[((encryptedArray[index] >> ((8 * x) + 4)) & 15)].ToString() + randomCharArray[((encryptedArray[index] >> (8 * x)) & 15)].ToString();
                    }
                    randomstringArray[index] = n;
                }
                var urlRandomString = string.Join("", randomstringArray);

                #endregion

                #region 產生亂數Base64編碼

                var partOneCharArray = new char[urlRandomString.Length + 1];
                partOneCharArray[0] = Convert.ToChar(2);
                for (int index = 0; index < urlRandomString.Length; index++)
                {
                    partOneCharArray[index + 1] = Convert.ToChar((Convert.ToInt32(urlRandomString[index]) ^ Convert.ToInt32(timeRandomString[index])));
                }
                string randomString = Convert.ToChar(128) + timeString;

                var partTwoCharArray = new char[randomString.Length - 3];
                var partThreeCharArray = new char[randomString.Length - 3];

                var random = new Random();
                for (var index = 0; index < randomString.Length - 3; index++)
                {
                    var randomInt = Convert.ToInt32(Math.Floor(128 * random.NextDouble()));
                    partTwoCharArray[index] = Convert.ToChar(Convert.ToInt32(randomString[index]) ^ randomInt);
                    partThreeCharArray[index] = Convert.ToChar(randomInt);
                }

                var partOne = string.Join("", partOneCharArray);
                var partTwo = string.Join("", partTwoCharArray);
                var partThree = string.Join("", partThreeCharArray);
                string result = partOne + partTwo + partThree;

                //將字串轉換為 Latin-1 編碼的 byte 數組
                var Latin1Bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(result);

                base64EncodeResult = Convert.ToBase64String(Latin1Bytes);

                return base64EncodeResult;

                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetXscHeader ERROR: {ex}");
            }

            return base64EncodeResult;
        }

        public void SetServerDiffTime(long? serverUnixTime)
        {
            if (serverUnixTime == null)
            {
                _logger.LogError("SetServerDiffTime serverUnixTime is null");
                return;
            }

            _timeDiffMilliseconds = serverUnixTime.Value - DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        private void GetEncryptedArray(int[] encryptedArray, int[] obfuscationArray)
        {
            var D = encryptedArray[0];
            var O = encryptedArray[1];
            var P = encryptedArray[2];
            var C = encryptedArray[3];

            D = g(D, O, P, C, obfuscationArray[0], 7, -680876936);
            C = g(C, D, O, P, obfuscationArray[1], 12, -389564586);
            P = g(P, C, D, O, obfuscationArray[2], 17, 606105819);
            O = g(O, P, C, D, obfuscationArray[3], 22, -1044525330);
            D = g(D, O, P, C, obfuscationArray[4], 7, -176418897);
            C = g(C, D, O, P, obfuscationArray[5], 12, 1200080426);
            P = g(P, C, D, O, obfuscationArray[6], 17, -1473231341);
            O = g(O, P, C, D, obfuscationArray[7], 22, -45705983);
            D = g(D, O, P, C, obfuscationArray[8], 7, 1770035416);
            C = g(C, D, O, P, obfuscationArray[9], 12, -1958414417);
            P = g(P, C, D, O, obfuscationArray[10], 17, -42063);
            O = g(O, P, C, D, obfuscationArray[11], 22, -1990404162);
            D = g(D, O, P, C, obfuscationArray[12], 7, 1804603682);
            C = g(C, D, O, P, obfuscationArray[13], 12, -40341101);
            P = g(P, C, D, O, obfuscationArray[14], 17, -1502002290);
            O = g(O, P, C, D, obfuscationArray[15], 22, 1236535329);

            D = v(D, O, P, C, obfuscationArray[1], 5, -165796510);
            C = v(C, D, O, P, obfuscationArray[6], 9, -1069501632);
            P = v(P, C, D, O, obfuscationArray[11], 14, 643717713);
            O = v(O, P, C, D, obfuscationArray[0], 20, -373897302);
            D = v(D, O, P, C, obfuscationArray[5], 5, -701558691);
            C = v(C, D, O, P, obfuscationArray[10], 9, 38016083);
            P = v(P, C, D, O, obfuscationArray[15], 14, -660478335);
            O = v(O, P, C, D, obfuscationArray[4], 20, -405537848);
            D = v(D, O, P, C, obfuscationArray[9], 5, 568446438);
            C = v(C, D, O, P, obfuscationArray[14], 9, -1019803690);
            P = v(P, C, D, O, obfuscationArray[3], 14, -187363961);
            O = v(O, P, C, D, obfuscationArray[8], 20, 1163531501);
            D = v(D, O, P, C, obfuscationArray[13], 5, -1444681467);
            C = v(C, D, O, P, obfuscationArray[2], 9, -51403784);
            P = v(P, C, D, O, obfuscationArray[7], 14, 1735328473);
            O = v(O, P, C, D, obfuscationArray[12], 20, -1926607734);

            D = y(D, O, P, C, obfuscationArray[5], 4, -378558);
            C = y(C, D, O, P, obfuscationArray[8], 11, -2022574463);
            P = y(P, C, D, O, obfuscationArray[11], 16, 1839030562);
            O = y(O, P, C, D, obfuscationArray[14], 23, -35309556);
            D = y(D, O, P, C, obfuscationArray[1], 4, -1530992060);
            C = y(C, D, O, P, obfuscationArray[4], 11, 1272893353);
            P = y(P, C, D, O, obfuscationArray[7], 16, -155497632);
            O = y(O, P, C, D, obfuscationArray[10], 23, -1094730640);
            D = y(D, O, P, C, obfuscationArray[13], 4, 681279174);
            C = y(C, D, O, P, obfuscationArray[0], 11, -358537222);
            P = y(P, C, D, O, obfuscationArray[3], 16, -722521979);
            O = y(O, P, C, D, obfuscationArray[6], 23, 76029189);
            D = y(D, O, P, C, obfuscationArray[9], 4, -640364487);
            C = y(C, D, O, P, obfuscationArray[12], 11, -421815835);
            P = y(P, C, D, O, obfuscationArray[15], 16, 530742520);
            O = y(O, P, C, D, obfuscationArray[2], 23, -995338651);

            D = b(D, O, P, C, obfuscationArray[0], 6, -198630844);
            C = b(C, D, O, P, obfuscationArray[7], 10, 1126891415);
            P = b(P, C, D, O, obfuscationArray[14], 15, -1416354905);
            O = b(O, P, C, D, obfuscationArray[5], 21, -57434055);
            D = b(D, O, P, C, obfuscationArray[12], 6, 1700485571);
            C = b(C, D, O, P, obfuscationArray[3], 10, -1894986606);
            P = b(P, C, D, O, obfuscationArray[10], 15, -1051523);
            O = b(O, P, C, D, obfuscationArray[1], 21, -2054922799);
            D = b(D, O, P, C, obfuscationArray[8], 6, 1873313359);
            C = b(C, D, O, P, obfuscationArray[15], 10, -30611744);
            P = b(P, C, D, O, obfuscationArray[6], 15, -1560198380);
            O = b(O, P, C, D, obfuscationArray[13], 21, 1309151649);
            D = b(D, O, P, C, obfuscationArray[4], 6, -145523070);
            C = b(C, D, O, P, obfuscationArray[11], 10, -1120210379);
            P = b(P, C, D, O, obfuscationArray[2], 15, 718787259);
            O = b(O, P, C, D, obfuscationArray[9], 21, -343485551);

            encryptedArray[0] = c(D, encryptedArray[0]);
            encryptedArray[1] = c(O, encryptedArray[1]);
            encryptedArray[2] = c(P, encryptedArray[2]);
            encryptedArray[3] = c(C, encryptedArray[3]);
        }

        private int b(int t, int r, int n, int i, int a, int o, int s)
        {
            return m(n ^ (r | ~i), t, r, a, o, s);
        }

        private int y(int t, int r, int n, int i, int a, int o, int s)
        {
            return m(((r ^ n) ^ i), t, r, a, o, s);
        }

        private int v(int t, int r, int n, int i, int a, int o, int u)
        {
            return m(r & i | n & ~i, t, r, a, o, u);
        }

        private int g(int t, int r, int n, int i, int a, int o, int s)
        {
            return m(((r & n) | (~r & i)), t, r, a, o, s);
        }

        private int m(int t, int r, int n, int i, int a, int o)
        {
            var e = p(p(r, t), p(i, o));
            return p(((e << a) | (e >>> (32 - a))), n);
        }

        private int p(int e, int t)
        {
            return (int)((e + t) & 4294967295);
        }

        private int c(int t, int n)
        {
            var x = (65535 & t) + (65535 & n);
            var y = ((t >> 16) + (n >> 16));
            var z = (y + (x >> 16));

            return ((z << 16) | (65535 & x));
        }
    }
}