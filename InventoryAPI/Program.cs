using InventoryAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("port") ?? "443";
builder.WebHost.UseUrls($"https://*:{port}");
builder.Services.AddHealthChecks();
//var port=Environment.GetEnvironmentVariable("port") ??"8080" ;
//builder.WebHost.UseUrls($"http://*:{port}");
// 🔹 إضافة CORS للسماح للـ React بالاتصال بالـ API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policyBuilder => policyBuilder
            .AllowAnyOrigin()   // السماح بأي مصدر (React Frontend)
            .AllowAnyMethod()   // السماح بأي نوع من الطلبات (GET, POST, PUT, DELETE)
            .AllowAnyHeader()); // السماح بأي نوع من الهيدر
});

//هيحل مشكلة الـ Reference Loops اللي بتسبب إرجاع $ref بدل البيانات الأصلية.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
});
// 🔹 إعداد JSON لتجاهل الحلقات لتفادي الخطأ
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

});

// 🔹 إضافة خدمات التحكم في البيانات
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });

// 🔹 إعداد اتصال قاعدة البيانات MySQL
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
                     new MySqlServerVersion(new Version(8, 0, 41))));

// 🔹 إضافة Swagger لتوثيق الـ API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔹 تفعيل Swagger فقط في بيئة التطوير
app.UseHealthChecks("./health");
app.UseSwagger();
app.UseSwaggerUI();


// 🔹 تفعيل CORS قبل Middleware الخاص بـ Authorization
app.UseCors("AllowAll");

// 🔹 تشغيل Middleware الأساسي
app.UseHttpsRedirection();
//app.UseHealthChecks("/health");
app.UseAuthorization();
app.MapControllers();

app.Run();
