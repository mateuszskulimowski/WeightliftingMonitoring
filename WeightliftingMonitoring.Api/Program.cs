using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers(); // dodaj to
builder.Services.AddOpenApi();
builder.Services.AddOpenApiDocument();

var allowedOrigins = builder.Configuration.GetValue<string>("allowedOrigins")!.Split(",");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});
// builder.Services.AddSingleton(FirebaseApp.Create());
var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
}

app.UseOpenApi();
app.UseSwaggerUi();

var filePath=File.ReadAllText("./firebase-adminsdk.json");
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",filePath);

app.UseHttpsRedirection();



app.UseCors();
app.MapControllers(); 

app.Run();







