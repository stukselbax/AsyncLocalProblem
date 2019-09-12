# AsyncLocalProblem
Demonstrates strange behavior with thread context changing.

*SpanHasAlreadyFinished* project is simple aspnetcore web api with minimal reproduced example originally captured.

*AsyncLocalProblem* project is console app which contains the code that demonstrate the problem with less code.

We are using Jaeger to track app preformance and recently encountered in a problem tracking requests:

```
Span has already been finished; will not be reported again. Operation: sqlClient SELECT Trace Id: 3378cd4b82959a046319f2eb1904476b Span Id: 851107e900911a68
```

It ocured when we updated library opentracing-contrib/csharp-netcore from 0.5.0 to 0.6.0 where support for SqlClient diagnostics listener was added.

There is [an issue](https://github.com/opentracing-contrib/csharp-netcore/issues/44) in those library, but as it reproduced without it in a simple console app, it's not their fault.

The scenario is simple - we use EF core with SQL Server, and DiagnosticListener for instrumentation. In order to report span in correct order, jaeger library uses `AsyncLocalScopeManager` from opentracing charp library, which uses AsyncLocal<T> to track active span. When specific command goes to Diagnostics listener, this active span in AsyncLocal<T> is changed to previosly stored state.

So, when we use EF core DbContext with SQL Server to query data, the value of AsyncLocal<T> is changed to old value, which already was disposed. That results in incorrect representation of jaeger ui spans.

In order to see the bug:

```
git clone https://github.com/stukselbax/AsyncLocalProblem
cd AsyncLocalProblem
devenv .
```

_SQLEXPRESS is required_. Build solution, select AsyncLocalProblem as startup project, F5. Inspect red message 

```
System.Data.SqlClient.WriteCommandBefore was already disposed!!
```

Set SpanHasAlreadyFinished as startup project , F5. Inspect red message

```
warn: Jaeger.Tracer[0]
      Span has already been finished; will not be reported again. Operation: sqlClient SELECT Trace Id: af69374e58cb1fe1 Span Id: ab16fb209a1877a4
```