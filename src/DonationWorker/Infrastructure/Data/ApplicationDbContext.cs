using DonationWorker.Domain.Entities.Campanhas;
using DonationWorker.Domain.Entities.Doacoes;
using Microsoft.EntityFrameworkCore;

namespace DonationWorker.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public ApplicationDbContext()
        {
        }




        /****** DbSets ******/
        public DbSet<Doacao> Doacao { get; set; }
        public DbSet<Campanha> Campanha { get; set; }






        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string? connectionString = null;
            if (optionsBuilder.IsConfigured)
                return;

            try
            {
                // Tenta ler do ambiente (Prioridade para Kubernetes)
                connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
                               ?? Environment.GetEnvironmentVariable("ConnectionStrings:Database");

                // Se não encontrar no ambiente, tenta carregar o JSON - Quando utilizado migration, vai cair aqui.
                // $env:ASPNETCORE_ENVIRONMENT="Development"; Add-Migration GestorOngInitial -StartupProject src\Usuarios.Infrastructure
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("Connection string é nula...");
                    string basePath = null;
                    // Verifica se o DbContext não foi configurado (ou seja, se veio do construtor vazio)
                    if (optionsBuilder.IsConfigured)
                    {
                        basePath = Path.Combine(Directory.GetCurrentDirectory(), "src","DonationWorker.Api");
                    }
                    else
                    {
                        
                        basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DonationWorker.Api"));
                    }

                    while (!File.Exists(Path.Combine(basePath, "appsettings.json")) && basePath != null)
                    {
                        basePath = Directory.GetParent(basePath)?.FullName;
                        if (basePath == null) break;
                    }
                    Console.WriteLine(basePath);
                    if (basePath == null)
                    {
                        throw new FileNotFoundException("Não foi possível encontrar o arquivo appsettings.json.");
                    }

                    string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                                              ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                                              ?? "Production"; // Define um default se não encontrar

                    Console.WriteLine("Environment: " + environmentName);

                    IConfiguration configuration = new ConfigurationBuilder()
                   // Define o caminho para o projeto de startup
                   .SetBasePath(basePath)
                   // 2. Carrega o arquivo base (appsettings.json)
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   // 3. Carrega o arquivo específico do ambiente (ex: appsettings.Development.json)
                   .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                   .Build();

                    connectionString = configuration.GetConnectionString("Database");

                }
            }
            catch { /* fallback */ }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("ERRO: A Connection String da base de dados não foi encontrada nem no appsettings nem nas Variáveis de Ambiente.");
            }

            optionsBuilder.UseNpgsql(connectionString);
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            modelBuilder.HasDefaultSchema("identidade");
        }
    }
}
