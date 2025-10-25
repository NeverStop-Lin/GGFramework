using System;
using System.Threading.Tasks;

namespace Framework.Core
{
    public abstract class Middleware
    {
        public abstract Task InvokeAsync(PipelineContext context, Func<Task> next);
    }
}