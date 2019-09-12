namespace SpanHasAlreadyFinished
{
    using Data;
    using Jaeger;
    using Jaeger.Samplers;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using OpenTracing.Util;

    public class Startup
    {
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            LoggerFactory = loggerFactory;
        }

        public IConfiguration Configuration { get; }
        public ILoggerFactory LoggerFactory { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var tracer = new Tracer.Builder("Problem")
            .WithLoggerFactory(LoggerFactory)
            .WithSampler(new ConstSampler(true))
            // You can play with this toys to get more details.
            // .WithScopeManager(new AsyncLocalScopeManagerToy(LoggerFactory))
            .Build();

            GlobalTracer.Register(tracer);

            services
                .AddDbContextPool<JaegerDbContext>(o =>
                o.UseSqlServer(SR.ConnectionString))
                .AddSingleton(tracer)
                .AddOpenTracing()
                .AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            using (var s = app.ApplicationServices.CreateScope())
            {
                s.ServiceProvider.GetRequiredService<JaegerDbContext>().Database.Migrate();
            }
        }
    }
}
