using System.Collections.Generic;

namespace Framework.Core
{
    public class PipelineContext
    {
        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();
        public object Result;
    }
}