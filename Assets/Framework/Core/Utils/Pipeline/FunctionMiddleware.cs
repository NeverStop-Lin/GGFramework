using System;
using System.Threading.Tasks;

namespace Framework.Core
{
    public class FunctionMiddleware : Middleware
    {
        private readonly Func<PipelineContext, Func<Task>, Task> _func;

        public FunctionMiddleware(Func<PipelineContext, Func<Task>, Task> func)
        {
            _func = func;
        }

        public override async Task InvokeAsync(PipelineContext context, Func<Task> next)
        {
            await _func(context, next);
        }
    }
}