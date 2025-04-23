using BenchmarkDotNet.Attributes;

namespace XscHeader跑分
{
    [MemoryDiagnoser]
    public class XscHeaderBenchmark
    {
        private HeaderUtility_舊的 _oldUtil;
        private HeaderUtility _newUtil;
        private string _url;

        [GlobalSetup]
        public void Setup()
        {
            // 3) 實例化兩個 Utility
            _oldUtil = new HeaderUtility_舊的();
            _newUtil = new HeaderUtility();

            // 4) URL 固定為你指定的那支
            _url = "https://imsb-9vkjm.utoyen.com/api/Event/GetAllLiveEventsDelta";
        }

        [Benchmark(Baseline = true, Description = "舊版 GetXscHeader")]
        public string OldVersion()
            => _oldUtil.GetXscHeader(_url);

        [Benchmark(Description = "新版 GetXscHeader")]
        public string NewVersion()
            => _newUtil.GetXscHeader(_url);
    }
}