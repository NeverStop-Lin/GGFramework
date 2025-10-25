using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Core
{

    public class AsyncPipeline
    {
        private readonly List<Middleware> _middlewares = new List<Middleware>();

        // ���ӻ�������м��
        public void AddMiddleware(Middleware middleware)
        {
            _middlewares.Add(middleware);
        }

        // ���Ӻ�����ʽ���м��
        public void AddMiddleware(Func<PipelineContext, Func<Task>, Task> middlewareFunc)
        {
            _middlewares.Add(new FunctionMiddleware(middlewareFunc));
        }

        // ִ�йܵ�
        public async Task ExecuteAsync(PipelineContext context)
        {
            Func<Task> pipeline = () => Task.CompletedTask;

            // �����װ�м��
            for (int i = _middlewares.Count - 1; i >= 0; i--)
            {
                var current = _middlewares[i];
                var next = pipeline;
                pipeline = () => current.InvokeAsync(context, next);
            }

            await pipeline();
        }
    }

}