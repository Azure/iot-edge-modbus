namespace AzureIoTEdgeModbus.Configuration
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public class FileConfiguration<T> : DeviceConfiguration<T>, IDisposable
    {
        private bool disposed = false;

        private TextReader FileTextReader { get; }

        public FileConfiguration(TextReader textReader)
        {
            this.FileTextReader = textReader;
        }

        public override async Task<T> GetConfigurationAsync(CancellationToken cancellationToken)
        {
            // Get desired properties from local file.
            var desiredProperties = await this.FileTextReader.ReadToEndAsync().ConfigureAwait(false);
            Console.WriteLine($"Desired properties retrieved from file: {Environment.NewLine}{desiredProperties}");

            return this.DeserialiseDesiredProperties(desiredProperties);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.FileTextReader.Dispose();
            }

            this.disposed = true;
        }

        ~FileConfiguration()
        {
            this.Dispose(false);
        }
    }
}
