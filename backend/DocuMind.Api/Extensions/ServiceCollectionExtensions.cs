using System.Text;
using DocuMind.Api.Authentication;
using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Infrastructure.Ai;
using DocuMind.Infrastructure.Chunking;
using DocuMind.Infrastructure.Ingestion;
using DocuMind.Infrastructure.Pdf;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Search;
using DocuMind.Infrastructure.Repositories;
using DocuMind.Infrastructure.Security;
using DocuMind.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using IPasswordHasher = DocuMind.Application.Interfaces.IPasswordHasher;

namespace DocuMind.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string SpaCorsPolicy = "DocuMindSpa";

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<DocuMindDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    // A pooled connection can be dropped by the server, a restart or a network
                    // blip. Without this, the first request after that fails outright instead of
                    // reconnecting.
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);

                    // Teaches the driver the vector type, so embeddings can be written as
                    // parameters instead of being pasted into SQL.
                    npgsql.UseVector();
                }));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentTextRepository, DocumentTextRepository>();
        services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();

        return services;
    }

    /// <summary>File storage plus the validator that guards what may enter it.</summary>
    public static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        // Binding appends to collections, so the allow-list is normalised after the fact:
        // de-duplicated, lower-cased, and defaulted when configuration supplies nothing.
        services.PostConfigure<StorageOptions>(options =>
            options.AllowedExtensions = options.AllowedExtensions.Length == 0
                ? StorageOptions.DefaultExtensions
                : options.AllowedExtensions
                    .Select(extension => extension.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToArray());

        services.AddSingleton<IFileValidator, FileValidator>();

        // Extraction, cleaning and chunking are stateless, so they are shared singletons.
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddSingleton<ITextCleaner, TextCleaner>();
        services.AddSingleton<ITextChunker, ParagraphTextChunker>();

        // Validated at startup: a bad chunk size should stop the app, not quietly produce
        // useless chunks for every document uploaded afterwards.
        services.AddOptions<ChunkingOptions>()
            .Bind(configuration.GetSection(ChunkingOptions.SectionName))
            .Validate(
                options => !options.Validate().Any(),
                "Rag chunking settings are invalid — see Rag:ChunkSize and Rag:ChunkOverlap.")
            .ValidateOnStart();

        var provider = configuration[$"{StorageOptions.SectionName}:Provider"]
            ?? StorageProviders.Local;

        var cloudinary = configuration
            .GetSection($"{StorageOptions.SectionName}:{StorageProviders.Cloudinary}")
            .Get<CloudinaryOptions>() ?? new CloudinaryOptions();

        var useCloudinary =
            string.Equals(provider, StorageProviders.Cloudinary, StringComparison.OrdinalIgnoreCase)
            && cloudinary.IsConfigured;

        if (string.Equals(provider, StorageProviders.Cloudinary, StringComparison.OrdinalIgnoreCase)
            && !cloudinary.IsConfigured)
        {
            // Falling back rather than refusing to start: an unconfigured cloud account should not
            // stop a developer working, and the warning says exactly what is missing.
            Console.WriteLine(
                "[Storage] Provider is 'Cloudinary' but CloudName/ApiKey/ApiSecret are not set — " +
                "falling back to local disk. Set them with:\n" +
                "  dotnet user-secrets set \"Storage:Cloudinary:CloudName\" \"<cloud>\"\n" +
                "  dotnet user-secrets set \"Storage:Cloudinary:ApiKey\" \"<key>\"\n" +
                "  dotnet user-secrets set \"Storage:Cloudinary:ApiSecret\" \"<secret>\"");
        }

        if (useCloudinary)
        {
            // Direct upload: the browser sends the file to Cloudinary, and this API only signs
            // the permission and verifies the result.
            services.AddHttpClient(nameof(CloudinaryFileStorageService));
            services.AddSingleton<IFileStorageService, CloudinaryFileStorageService>();
            services.AddSingleton<IDirectUploadService, CloudinaryDirectUploadService>();
        }
        else
        {
            // Local disk keeps development working with no credentials. There is no direct-upload
            // service, so the ticket endpoints report that cleanly rather than failing obscurely.
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        }

        return services;
    }

    /// <summary>
    /// The AI provider. Embedding is stateless and the client underneath pools its own connections,
    /// so one instance is shared.
    /// </summary>
    public static IServiceCollection AddAiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validated at startup: a dimension that disagrees with module 7's vector column, or an
        // unimplemented provider, should stop the app rather than fail every upload later.
        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .Validate(
                options => !options.Validate().Any(),
                "AI settings are invalid — see AI:Provider, AI:EmbeddingModel and AI:EmbeddingDimensions.")
            .ValidateOnStart();

        services.AddSingleton<IEmbeddingService, OpenAIEmbeddingService>();
        services.AddSingleton<ILLMService, OpenAILLMService>();

        // The prompt is pure text and the RAG service holds no state of its own; it is scoped only
        // because the search it depends on reads through the request's DbContext.
        services.AddSingleton<IPromptBuilder, GroundedPromptBuilder>();
        services.AddScoped<IRagService, RagService>();

        // How much to retrieve and how close it must be. Same 'Rag' section as chunking: one
        // section for the retrieval pipeline, one options class per half of it.
        services.AddOptions<RetrievalOptions>()
            .Bind(configuration.GetSection(RetrievalOptions.SectionName))
            .Validate(
                options => !options.Validate().Any(),
                "Rag retrieval settings are invalid — see Rag:TopK and Rag:SimilarityThreshold.")
            .ValidateOnStart();

        // Scoped, because the search reads through the request's DbContext.
        services.AddScoped<IVectorSearchService, VectorSearchService>();

        var ai = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

        if (!ai.IsConfigured)
        {
            // A warning rather than a failure to start, for the same reason as Cloudinary above: a
            // missing paid credential should not stop a developer working on everything else.
            Console.WriteLine(
                "[AI] AI:ApiKey is not set — uploads will reach the embedding step and fail there. " +
                "Set it with:\n  dotnet user-secrets set \"AI:ApiKey\" \"<key>\"");
        }

        return services;
    }

    /// <summary>
    /// The ingestion queue and the worker that drains it. The queue is a singleton because the
    /// worker and every request thread must share the same instance; the worker creates its own
    /// scope per document so it can resolve the scoped processor and DbContext.
    /// </summary>
    public static IServiceCollection AddBackgroundIngestion(this IServiceCollection services)
    {
        services.AddSingleton<ChannelIngestionQueue>();
        services.AddSingleton<IIngestionQueue>(sp => sp.GetRequiredService<ChannelIngestionQueue>());
        services.AddHostedService<IngestionWorker>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentProcessor, DocumentProcessor>();

        return services;
    }

    /// <summary>
    /// Bearer authentication plus the two services that back it. Registering ICurrentUser here
    /// keeps the rule that a user id only ever comes from a validated token.
    /// </summary>
    public static IServiceCollection AddTokenAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

        if (string.IsNullOrWhiteSpace(jwt.Key))
        {
            throw new InvalidOperationException(
                "Jwt:Key is not configured. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<32+ char secret>\"");
        }

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),

                    // Default is five minutes of leeway, which would keep short-lived access
                    // tokens working well past their stated expiry.
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// The SPA sits on a different origin and sends the refresh cookie, so the policy must name
    /// exact origins — AllowAnyOrigin is incompatible with AllowCredentials.
    /// </summary>
    public static IServiceCollection AddSpaCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];

        services.AddCors(options =>
            options.AddPolicy(SpaCorsPolicy, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));

        return services;
    }
}
