namespace AzureIoTEdgeModbus.Configuration
{
    using AzureIoTEdgeModbus.Instrumentation;

    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public class FileConfiguration<T> : DeviceConfiguration<T>, IDisposable
    {
        private bool disposed = false;

        private TextReader FileTextReader { get; }

        public FileConfiguration(MicrosoftExtensionsLog log, TextReader textReader)
            : base(log)
        {
            this.FileTextReader = textReader;
        }

        protected override async Task<string> GetConfigurationAsync(CancellationToken cancellationToken)
        {
            // Get desired properties from local file.
            var desiredProperties = await this.FileTextReader.ReadToEndAsync().ConfigureAwait(false);
            this.Log.DesiredPropertiesReceivedFromFile(desiredProperties);

            return desiredProperties;
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
