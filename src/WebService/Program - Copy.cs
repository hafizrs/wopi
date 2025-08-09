//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;

//namespace Selise.Ecap.SC.Wopi.WebService
//{
//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            var builder = WebApplication.CreateBuilder(args);

//            // Add services to the container
//            builder.Services.AddControllers();
//            builder.Services.AddHttpClient();
            
//            // Configure CORS for WOPI
//            builder.Services.AddCors(options =>
//            {
//                options.AddDefaultPolicy(policy =>
//                {
//                    policy.AllowAnyOrigin()
//                          .AllowAnyMethod()
//                          .AllowAnyHeader()
//                          .WithExposedHeaders("Content-Disposition", "X-WOPI-ItemVersion", "X-WOPI-Lock");
//                });
//            });

//            var app = builder.Build();

//            // Configure the HTTP request pipeline
//            if (app.Environment.IsDevelopment())
//            {
//                app.UseDeveloperExceptionPage();
//            }

//            app.UseCors();
//            app.UseRouting();
//            app.MapControllers();

//            app.Run();
//        }
//    }
//}
