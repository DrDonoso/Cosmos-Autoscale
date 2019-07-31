using System;

namespace CosmosClient
{
    public class CustomConfiguration
    {
        private const int DefaultMaxRetries = 5;
        private const int DefaultScaleUpBatch = 100;
        private const int DefaultScaleDownBatch = -100;
        private const int MinThroughputPossible = 400;

        public int MaxRetries { get; set; } = DefaultMaxRetries;
        public int MinThroughput { get; set; } = MinThroughputPossible;
        public int MaxThroughput { get; set; } = int.MaxValue;
        public int ScaleUpBatch { get; set; } = DefaultScaleUpBatch;
        public int ScaleDownBatch { get; set; } = DefaultScaleDownBatch;


        public void CheckConfiguration()
        {
            if(MinThroughput < MinThroughputPossible) throw new Exception();
            if(ScaleUpBatch <= 0) throw new Exception();
            if(ScaleDownBatch >= 0) throw new Exception();
        }
    }
}
