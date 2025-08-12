using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Google.Cloud.Storage.V1;
using Grpc.Auth;
using ReactorApi.Controllers;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);




// Add services to the container.

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin() // React app origin
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddSingleton(sp =>
{
    var credential = GoogleCredential.FromFile("Service/nuclearreactor-2b876-firebase-adminsdk-fbsvc-a01871a38f.json");
    var channelCredentials = credential.ToChannelCredentials();

    var clientBuilder = new FirestoreClientBuilder
    {
        ChannelCredentials = channelCredentials
    };

    FirestoreClient client = clientBuilder.Build();

    return FirestoreDb.Create("nuclearreactor-2b876", client);
});

var storage = StorageClient.Create(GoogleCredential.FromFile("Service/nuclearreactor-2b876-firebase-adminsdk-fbsvc-a01871a38f.json"));



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.MapGet("/", () => "Hello from Azure!");

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseForwardedHeaders();

app.MapControllers();

app.Run();


