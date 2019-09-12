using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTracing;

namespace SpanHasAlreadyFinished
{
    /// <summary>
    /// Source are from https://github.com/opentracing/opentracing-csharp/blob/master/src/OpenTracing/Util/AsyncLocalScopeManager.cs
    /// </summary>
    public class AsyncLocalScopeManagerToy : IScopeManager
    {
        private AsyncLocal<IScope> _current;
        private ILoggerFactory loggerFactory;
        private ILogger logger;

        public AsyncLocalScopeManagerToy(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<AsyncLocalScopeManagerToy>();
            this.loggerFactory = loggerFactory;
            _current = new AsyncLocal<IScope>((args) =>
            {

                var str = args.CurrentValue?.Span?.ToString();
                var str2 = args.PreviousValue?.Span?.ToString();

                // Some one call it.
                logger.LogInformation("New Value {0}", str);
                if (str?.EndsWith("sqlClient SELECT") == true && args.ThreadContextChanged)
                {
                    logger.LogInformation("Old value {0}", str2);
                    //logger.LogInformation(Environment.StackTrace);
                }
            });
        }

        public IScope Active
        {
            get
            {
                return _current.Value;
            }
            set
            {
                logger.LogInformation("Set IScopeManager.Active to value {0}", value?.Span?.ToString());
                _current.Value = value;
            }
        }



        public IScope Activate(ISpan span, bool finishSpanOnDispose)
        {
            logger.LogInformation("Activate: {0} {1}", finishSpanOnDispose ? "Y" : "N", span.ToString());
            return new AsyncLocalScopeToy(this, span, finishSpanOnDispose);
        }
    }
    
    public class AsyncLocalScopeToy : IScope
    {
        private readonly AsyncLocalScopeManagerToy _scopeManager;
        private readonly ISpan _wrappedSpan;
        private readonly bool _finishOnDispose;
        private readonly IScope _scopeToRestore;

        public AsyncLocalScopeToy(AsyncLocalScopeManagerToy scopeManager, ISpan wrappedSpan, bool finishOnDispose)
        {
            _scopeManager = scopeManager;
            _wrappedSpan = wrappedSpan;
            _finishOnDispose = finishOnDispose;

            _scopeToRestore = scopeManager.Active;
            scopeManager.Active = this;
        }

        public ISpan Span => _wrappedSpan;

        public void Dispose()
        {
            if (_scopeManager.Active != this)
            {
                // This shouldn't happen if users call methods in the expected order. Bail out.
                return;
            }

            if (_finishOnDispose)
            {
                _wrappedSpan.Finish();
            }

            _scopeManager.Active = _scopeToRestore;
        }
    }
}
