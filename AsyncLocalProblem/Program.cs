namespace AsyncLocalProblem
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Data;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using static System.Console;

    class Program
    {
        static async Task Main(string[] args)
        {
            WriteLine("Hello World!");

            var services = new ServiceCollection()
                .AddDbContextPool<JaegerDbContext>(o => o.UseSqlServer(SR.ConnectionString))
                .BuildServiceProvider();

            try
            {
                var manager = new DiagnosticManager();
                var dbContext = services.GetRequiredService<JaegerDbContext>();

                var dates = await dbContext.Dates.ToListAsync();
            }
            catch (Exception e)
            {
                WriteLine("Error ocured: {0}", e);
            }
            finally
            {
                WriteLine("Press enter");
                ReadLine();
            }
        }

        internal sealed class DiagnosticManager : IObserver<DiagnosticListener>
        {
            private AsyncLocal<Disposable> status = new AsyncLocal<Disposable>();

            public DiagnosticManager()
            {
                DiagnosticListener.AllListeners.Subscribe(this);
            }

            public Disposable Status
            {
                get => status.Value;
                set => status.Value = value;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(DiagnosticListener value)
            {
                var listening = new DiagnosticsSubscriber(this, value.Name);
                value.Subscribe(listening);
            }

            private class DiagnosticsSubscriber : IObserver<KeyValuePair<string, object>>
            {
                private readonly string name;
                protected readonly DiagnosticManager diagnosticManager;

                public DiagnosticsSubscriber(DiagnosticManager diagnosticManager, string name)
                {
                    this.diagnosticManager = diagnosticManager;
                    this.name = name;
                }

                void IObserver<KeyValuePair<string, object>>.OnCompleted()
                {
                }

                void IObserver<KeyValuePair<string, object>>.OnError(Exception error)
                {
                }

                void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> value)
                {
                    try
                    {
                        OnNext(value.Key, value.Value);
                    }
                    catch (Exception ex)
                    {
                        WriteLine("Event-Exception: {0}, {1}", value.Key, ex);
                    }
                }

                private void OnNext(string key, object value)
                {
                    switch (key)
                    {
                        case "System.Data.SqlClient.WriteCommandBefore":
                        case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting":
                            {
                                diagnosticManager.Status = new Disposable(diagnosticManager, key);
                                WriteLine("Listener: {0}. {1}", name, diagnosticManager.Status);
                            }
                            break;

                        case "System.Data.SqlClient.WriteCommandAfter":
                        case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted":
                            {
                                var activeStatus = diagnosticManager.Status;
                                WriteLine("Going to dispose {1} of {0}", name, activeStatus);
                                activeStatus?.Dispose();
                            }
                            break;
                    }
                }
            }
        }

        public class Disposable : IDisposable
        {
            private readonly DiagnosticManager manager;
            private readonly string name;
            private Disposable prevStatus;
            private bool disposed = false;

            public Disposable(DiagnosticManager manager, string name)
            {
                this.manager = manager;
                this.name = name;
                prevStatus = manager.Status;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine(name + " was already disposed!!!");
                    ResetColor();
                }

                disposed = true;

                WriteLine("Change manager status to previous: {0}", prevStatus?.name);
                manager.Status = prevStatus;
            }

            public override string ToString()
            {
                return "current status: " + name + ". Restore to: " + prevStatus?.name;
            }
        }
    }
}
