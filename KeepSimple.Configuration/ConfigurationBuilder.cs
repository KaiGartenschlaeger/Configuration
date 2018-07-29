namespace KeepSimple.Configuration
{
    public sealed class ConfigurationBuilder
    {
        #region Properties

        private readonly IConfiguration _configuration;

        #endregion

        #region Constructor

        public ConfigurationBuilder()
        {
            _configuration = new Configuration();
        }

        #endregion

        #region Properties

        public IConfiguration Configuration
        {
            get { return _configuration; }
        }

        #endregion
    }
}